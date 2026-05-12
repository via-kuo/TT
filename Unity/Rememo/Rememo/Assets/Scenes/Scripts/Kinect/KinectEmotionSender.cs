using System;
using UnityEngine;
using WebSocketSharp;
using Windows.Kinect;
using Microsoft.Kinect.Face;



public class KinectEmotionSender : MonoBehaviour
{

    public float lastHappy { get; private set; }
    public float lastLookingAway { get; private set; }
    public float lastMouthMoved { get; private set; }

    [Header("WebSocket 設定")]
    public string serverUrl = "ws://localhost:8000/ws/emotion";

    private WebSocket ws;
    private KinectSensor sensor;
    private BodyFrameReader bodyReader;
    private FaceFrameSource[] faceSources;
    private FaceFrameReader[] faceReaders;
    private Body[] bodies;

    private const int BODY_COUNT = 6;

    void Start()
    {
        ws = new WebSocket(serverUrl);
        ws.OnOpen += (s, e) => Debug.Log("[Emotion WS] 已連線");
        ws.OnError += (s, e) => Debug.LogError($"[Emotion WS] 錯誤: {e.Message}");
        ws.OnClose += (s, e) => Debug.Log("[Emotion WS] 已關閉");
        ws.ConnectAsync();

        sensor = KinectSensor.GetDefault();
        if (sensor == null)
        {
            Debug.LogError("[Emotion] 找不到 Kinect 感測器");
            return;
        }

        var features = FaceFrameFeatures.Happy
                     | FaceFrameFeatures.LeftEyeClosed
                     | FaceFrameFeatures.RightEyeClosed
                     | FaceFrameFeatures.LookingAway
                     | FaceFrameFeatures.MouthOpen
                     | FaceFrameFeatures.MouthMoved;

        bodyReader = sensor.BodyFrameSource.OpenReader();
        bodies = new Body[BODY_COUNT];

        faceSources = new FaceFrameSource[BODY_COUNT];
        faceReaders = new FaceFrameReader[BODY_COUNT];

        for (int i = 0; i < BODY_COUNT; i++)
        {
            faceSources[i] = FaceFrameSource.Create(sensor, 0, features);
            faceReaders[i] = faceSources[i].OpenReader();
        }

        if (!sensor.IsOpen) sensor.Open();
    }

    void Update()
    {
        if (ws == null || ws.ReadyState != WebSocketState.Open) return;

        // 更新 body tracking ID
        using (var frame = bodyReader?.AcquireLatestFrame())
        {
            if (frame != null)
            {
                frame.GetAndRefreshBodyData(bodies);
                for (int i = 0; i < BODY_COUNT; i++)
                {
                    if (bodies[i] != null && bodies[i].IsTracked)
                        faceSources[i].TrackingId = bodies[i].TrackingId;
                }
            }
        }

        // 讀取表情數據
        for (int i = 0; i < BODY_COUNT; i++)
        {
            using (var faceFrame = faceReaders[i]?.AcquireLatestFrame())
            {
                if (faceFrame == null || !faceFrame.IsTrackingIdValid) continue;

                var result = faceFrame.FaceFrameResult;
                if (result == null) continue;

                lastHappy = result.FaceProperties[FaceProperty.Happy] == DetectionResult.Yes ? 1f :
            result.FaceProperties[FaceProperty.Happy] == DetectionResult.Maybe ? 0.5f : 0f;

                lastLookingAway = result.FaceProperties[FaceProperty.LookingAway] == DetectionResult.Yes ? 1f :
                                  result.FaceProperties[FaceProperty.LookingAway] == DetectionResult.Maybe ? 0.5f : 0f;

                lastMouthMoved = result.FaceProperties[FaceProperty.MouthMoved] == DetectionResult.Yes ? 1f :
                                 result.FaceProperties[FaceProperty.MouthMoved] == DetectionResult.Maybe ? 0.5f : 0f;

                string GetState(DetectionResult r) => r.ToString();

                string payload = JsonUtility.ToJson(new EmotionPayload
                {
                    type = "emotion",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    happy = GetState(result.FaceProperties[FaceProperty.Happy]),
                    leftEyeClosed = GetState(result.FaceProperties[FaceProperty.LeftEyeClosed]),
                    rightEyeClosed = GetState(result.FaceProperties[FaceProperty.RightEyeClosed]),
                    lookingAway = GetState(result.FaceProperties[FaceProperty.LookingAway]),
                    mouthOpen = GetState(result.FaceProperties[FaceProperty.MouthOpen]),
                    mouthMoved = GetState(result.FaceProperties[FaceProperty.MouthMoved])
                });

                ws.SendAsync(payload, null);
            }
        }
    }

    void OnDestroy()
    {
        if (faceReaders != null)
            foreach (var r in faceReaders) r?.Dispose();
        if (faceSources != null)
            foreach (var s in faceSources) s?.Dispose(true);
        bodyReader?.Dispose();
        ws?.Close();
        sensor?.Close();
    }
}

[Serializable]
public class EmotionPayload
{
    public string type;
    public long timestamp;
    public string happy;
    public string leftEyeClosed;
    public string rightEyeClosed;
    public string lookingAway;
    public string mouthOpen;
    public string mouthMoved;
}