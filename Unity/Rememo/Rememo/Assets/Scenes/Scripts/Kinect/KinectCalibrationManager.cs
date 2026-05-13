using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class KinectCalibrationManager : MonoBehaviour
{
    [Header("校正設定")]
    public float calibrationDuration = 15f;
    public float stabilityThreshold  = 0.05f;
    public bool  IsCalibrated { get; private set; }

    [Header("UI 元件")]
    public Image  statusIndicator;
    public Text   statusText;
    public Slider progressBar;

    [Header("WebSocket 設定")]
    public string calibrationUrl = "ws://localhost:8000/ws/calibration";

    private readonly Color COLOR_ORANGE = new Color(1f, 0.6f, 0f);
    private readonly Color COLOR_GREEN  = new Color(0.2f, 0.8f, 0.2f);

    private bool  isCalibrating    = false;
    private float calibrationTimer = 0f;

    private List<Dictionary<string, float[]>> skeletonBuffer = new List<Dictionary<string, float[]>>();
    private List<float> happyBuffer       = new List<float>();
    private List<float> lookingAwayBuffer = new List<float>();
    private List<float> mouthMovedBuffer  = new List<float>();

    private WebSocket            ws;
    private KinectManager        kinectManager;
    private KinectEmotionSender  emotionSender;

    void Start()
    {
        kinectManager = KinectManager.Instance;
        emotionSender = GetComponent<KinectEmotionSender>();

        SetStatus(false);

        ws = new WebSocket(calibrationUrl);
        ws.OnOpen  += (s, e) => Debug.Log("[Calibration WS] 已連線");
        ws.OnError += (s, e) => Debug.LogError($"[Calibration WS] 錯誤: {e.Message}");
        ws.ConnectAsync();

        StartCoroutine(CalibrationRoutine());
    }

    IEnumerator CalibrationRoutine()
    {
        isCalibrating    = true;
        calibrationTimer = 0f;

        Debug.Log("[Calibration] 開始蒐集基準值");

        while (calibrationTimer < calibrationDuration)
        {
            calibrationTimer += Time.deltaTime;

            if (progressBar != null)
                progressBar.value = calibrationTimer / calibrationDuration;

            CollectSkeletonData();
            CollectEmotionData();

            yield return null;
        }

        bool isStable = CheckStability();

        if (isStable)
        {
            IsCalibrated = true;
            SetStatus(true);
            SendCalibrationData();
            Debug.Log("[Calibration] 校正完成，等待治療師確認");
        }
        else
        {
            Debug.Log("[Calibration] 數據不穩定，重新校正");
            skeletonBuffer.Clear();
            happyBuffer.Clear();
            lookingAwayBuffer.Clear();
            mouthMovedBuffer.Clear();
            StartCoroutine(CalibrationRoutine());
        }
    }

    void CollectSkeletonData()
    {
        if (kinectManager == null || !kinectManager.IsInitialized()) return;

        long userId = kinectManager.GetPrimaryUserID();
        if (userId == 0) return;

        var joints = new Dictionary<string, float[]>();
        for (int j = 0; j < 25; j++)
        {
            if (kinectManager.IsJointTracked(userId, j))
            {
                Vector3 pos = kinectManager.GetJointPosition(userId, j);
                joints[((KinectInterop.JointType)j).ToString()] = new float[]
                {
                    pos.x, pos.y, pos.z
                };
            }
        }

        if (joints.Count > 0)
            skeletonBuffer.Add(joints);
    }

    void CollectEmotionData()
    {
        if (emotionSender == null) return;

        happyBuffer.Add(emotionSender.LastHappy);
        lookingAwayBuffer.Add(emotionSender.LastLookingAway);
        mouthMovedBuffer.Add(emotionSender.LastMouthMoved);
    }

    bool CheckStability()
    {
        if (skeletonBuffer.Count < 10) return false;

        var spinePositions = new List<Vector3>();
        foreach (var frame in skeletonBuffer)
        {
            if (frame.ContainsKey("SpineBase"))
            {
                var pos = frame["SpineBase"];
                spinePositions.Add(new Vector3(pos[0], pos[1], pos[2]));
            }
        }

        if (spinePositions.Count < 10) return false;

        Vector3 avg = Vector3.zero;
        foreach (var p in spinePositions) avg += p;
        avg /= spinePositions.Count;

        float variance = 0f;
        foreach (var p in spinePositions)
            variance += (p - avg).sqrMagnitude;
        variance /= spinePositions.Count;
        float stdDev = Mathf.Sqrt(variance);

        Debug.Log($"[Calibration] 骨架穩定度: {stdDev}（閾值: {stabilityThreshold}）");
        return stdDev < stabilityThreshold;
    }

    void SendCalibrationData()
    {
        if (ws == null || ws.ReadyState != WebSocketState.Open) return;

        var baselineJoints = new Dictionary<string, float[]>();
        var jointSums      = new Dictionary<string, float[]>();
        var jointCounts    = new Dictionary<string, int>();

        foreach (var frame in skeletonBuffer)
        {
            foreach (var kvp in frame)
            {
                if (!jointSums.ContainsKey(kvp.Key))
                {
                    jointSums[kvp.Key]   = new float[] { 0, 0, 0 };
                    jointCounts[kvp.Key] = 0;
                }
                jointSums[kvp.Key][0] += kvp.Value[0];
                jointSums[kvp.Key][1] += kvp.Value[1];
                jointSums[kvp.Key][2] += kvp.Value[2];
                jointCounts[kvp.Key]++;
            }
        }

        foreach (var kvp in jointSums)
        {
            int count = jointCounts[kvp.Key];
            baselineJoints[kvp.Key] = new float[]
            {
                kvp.Value[0] / count,
                kvp.Value[1] / count,
                kvp.Value[2] / count
            };
        }

        float happyBaseline       = Average(happyBuffer);
        float lookingAwayBaseline = Average(lookingAwayBuffer);
        float mouthMovedBaseline  = Average(mouthMovedBuffer);

        var payload = new CalibrationPayload
        {
            type                = "calibration",
            duration            = calibrationDuration,
            happyBaseline       = happyBaseline,
            lookingAwayBaseline = lookingAwayBaseline,
            mouthMovedBaseline  = mouthMovedBaseline,
            jointKeys           = new List<string>(baselineJoints.Keys).ToArray(),
            jointX              = GetAxis(baselineJoints, 0),
            jointY              = GetAxis(baselineJoints, 1),
            jointZ              = GetAxis(baselineJoints, 2)
        };

        ws.SendAsync(JsonUtility.ToJson(payload), null);
        Debug.Log("[Calibration] 基準值已送出");
    }

    float Average(List<float> list)
    {
        if (list.Count == 0) return 0f;
        float sum = 0f;
        foreach (var v in list) sum += v;
        return sum / list.Count;
    }

    float[] GetAxis(Dictionary<string, float[]> joints, int axis)
    {
        var result = new float[joints.Count];
        int i = 0;
        foreach (var v in joints.Values)
            result[i++] = v[axis];
        return result;
    }

    void SetStatus(bool calibrated)
    {
        if (statusIndicator != null)
            statusIndicator.color = calibrated ? COLOR_GREEN : COLOR_ORANGE;

        if (statusText != null)
            statusText.text = calibrated ? "設備偵測成功" : "設備偵測中";
    }

    void OnDestroy()
    {
        ws?.Close();
    }
}

[System.Serializable]
public class CalibrationPayload
{
    public string   type;
    public float    duration;
    public float    happyBaseline;
    public float    lookingAwayBaseline;
    public float    mouthMovedBaseline;
    public string[] jointKeys;
    public float[]  jointX;
    public float[]  jointY;
    public float[]  jointZ;
}