using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;


public class KinectCalibrationManager : MonoBehaviour
{
    [Header("校正設定")]
    public float calibrationDuration = 15f;  // 校正時間（秒）
    public float stabilityThreshold  = 0.05f; // 骨架穩定閾值
    public bool IsCalibrated { get; private set; }

    [Header("UI 元件")]
    public Image  statusIndicator;   // 橘色/綠色圓點
    public Text   statusText;        // 「設備偵測中」/「設備偵測成功」
    public Slider progressBar;       // 進度條（可選）

    [Header("WebSocket 設定")]
    public string calibrationUrl = "ws://localhost:8000/ws/calibration";

    // 顏色設定
    private readonly Color COLOR_ORANGE = new Color(1f, 0.6f, 0f);
    private readonly Color COLOR_GREEN  = new Color(0.2f, 0.8f, 0.2f);

    // 校正狀態
    private bool  isCalibrating   = false;
    private bool  isCalibrated    = false;
    private float calibrationTimer = 0f;

    // 數據累積
    private List<Dictionary<string, float[]>> skeletonBuffer = new List<Dictionary<string, float[]>>();
    private List<float> happyBuffer      = new List<float>();
    private List<float> lookingAwayBuffer= new List<float>();
    private List<float> mouthMovedBuffer = new List<float>();

    // WebSocket
    private WebSocket ws;

    // 外部腳本引用
    private KinectManager      kinectManager;
    private KinectSkeletonSender skeletonSender;
    private KinectEmotionSender  emotionSender;

    void Start()
    {
        kinectManager  = KinectManager.Instance;
        skeletonSender = GetComponent<KinectSkeletonSender>();
        emotionSender  = GetComponent<KinectEmotionSender>();

        // 初始化 UI
        SetStatus(false);

        // 連接 WebSocket
        ws = new WebSocket(calibrationUrl);
        ws.OnOpen  += (s, e) => Debug.Log("[Calibration WS] 已連線");
        ws.OnError += (s, e) => Debug.LogError($"[Calibration WS] 錯誤: {e.Message}");
        ws.ConnectAsync();

        // 開始校正
        StartCoroutine(CalibrationRoutine());
    }

    IEnumerator CalibrationRoutine()
    {
        isCalibrating = true;
        calibrationTimer = 0f;

        Debug.Log("[Calibration] 開始蒐集基準值");

        while (calibrationTimer < calibrationDuration)
        {
            calibrationTimer += Time.deltaTime;

            // 更新進度條
            if (progressBar != null)
                progressBar.value = calibrationTimer / calibrationDuration;

            // 蒐集骨架數據
            CollectSkeletonData();

            // 蒐集情緒數據（從 KinectEmotionSender 取得）
            CollectEmotionData();

            yield return null;
        }

        // 時間到，檢查穩定度
        bool isStable = CheckStability();

        if (isStable)
        {
            // 兩個條件都達成
            isCalibrated = true;
            IsCalibrated = true; 
            SetStatus(true);
            SendCalibrationData();
            Debug.Log("[Calibration] 校正完成，等待治療師確認");
        }
        else
        {
            // 不夠穩定，重新校正
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
        // 從 EmotionSender 取得最新情緒數據
        // 這裡用簡單的隨機模擬，實際要從 EmotionSender 取得
        // 之後可以在 KinectEmotionSender 加上 public 屬性來取得
        if (emotionSender == null) return;

        happyBuffer.Add(emotionSender.lastHappy);
        lookingAwayBuffer.Add(emotionSender.lastLookingAway);
        mouthMovedBuffer.Add(emotionSender.lastMouthMoved);
    }

    bool CheckStability()
    {
        if (skeletonBuffer.Count < 10) return false;

        // 計算 SpineBase 位移標準差
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

        // 計算平均位置
        Vector3 avg = Vector3.zero;
        foreach (var p in spinePositions) avg += p;
        avg /= spinePositions.Count;

        // 計算標準差
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

        // 計算骨架基準值（各關節平均位置）
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

        // 計算情緒基準值
        float happyBaseline      = Average(happyBuffer);
        float lookingAwayBaseline = Average(lookingAwayBuffer);
        float mouthMovedBaseline  = Average(mouthMovedBuffer);

        // 組成 payload 送給 FastAPI
        var payload = new CalibrationPayload
        {
            type              = "calibration",
            duration          = calibrationDuration,
            happyBaseline     = happyBaseline,
            lookingAwayBaseline = lookingAwayBaseline,
            mouthMovedBaseline  = mouthMovedBaseline,
            jointKeys         = new List<string>(baselineJoints.Keys).ToArray(),
            jointX            = GetAxis(baselineJoints, 0),
            jointY            = GetAxis(baselineJoints, 1),
            jointZ            = GetAxis(baselineJoints, 2)
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
    public string  type;
    public float   duration;
    public float   happyBaseline;
    public float   lookingAwayBaseline;
    public float   mouthMovedBaseline;
    public string[] jointKeys;
    public float[]  jointX;
    public float[]  jointY;
    public float[]  jointZ;
}