using System;
using System.IO;
using UnityEngine;
using WebSocketSharp;
using Windows.Kinect;

public class KinectAudioSender : MonoBehaviour
{
    [Header("WebSocket 設定")]
    public string audioUrl = "ws://localhost:8000/ws/audio";
    public string sttUrl   = "ws://localhost:8000/ws/stt";

    // MicController 設定此 callback 以接收 STT 回傳訊息（在 WS 背景執行緒呼叫）
    public System.Action<string> OnSttMessage;

    private WebSocket wsAudio;
    private WebSocket wsStt;

    private KinectSensor sensor;
    private AudioBeamFrameReader audioReader;
    private MemoryStream audioAccumulator = new MemoryStream();
    private const int CHUNK_SIZE = 3200;
    private bool isInitialized = false;
    private bool isSttActive = false;

    void Start()
    {
        wsAudio = new WebSocket(audioUrl);
        wsAudio.OnOpen  += (s, e) => Debug.Log("[Audio WS] 已連線");
        wsAudio.OnError += (s, e) => Debug.LogError($"[Audio WS] 錯誤: {e.Message}");
        wsAudio.OnClose += (s, e) => Debug.Log("[Audio WS] 已關閉");
        wsAudio.ConnectAsync();

        wsStt = new WebSocket(sttUrl);
        wsStt.OnOpen    += (s, e) => Debug.Log("[STT WS Kinect] 已連線");
        wsStt.OnError   += (s, e) => Debug.LogError($"[STT WS Kinect] 錯誤: {e.Message}");
        wsStt.OnClose   += (s, e) => Debug.Log("[STT WS Kinect] 已關閉");
        wsStt.OnMessage += (s, e) => { if (e.IsText) OnSttMessage?.Invoke(e.Data); };
        wsStt.ConnectAsync();
    }

    // ── 公開 API，讓 MicController 在按下/放開麥克風時呼叫 ──

    public void StartSTT()
    {
        isSttActive = true;
        audioAccumulator.SetLength(0);
        SendSttControl("start");
        Debug.Log("[KinectAudio] StartSTT");
    }

    public void StopSTT()
    {
        isSttActive = false;
        SendSttControl("end");
        Debug.Log("[KinectAudio] StopSTT");
    }

    private void SendSttControl(string type)
    {
        if (wsStt?.ReadyState == WebSocketState.Open)
            wsStt.SendAsync($"{{\"type\":\"{type}\"}}", null);
    }

    private void TryInitAudio()
    {
        if (isInitialized) return;

        KinectManager km = KinectManager.Instance;
        if (km == null || !km.IsInitialized()) return;

        Kinect2Interface sensorInterface = km.GetSensorData().sensorInterface as Kinect2Interface;
        sensor = sensorInterface?.kinectSensor;
        if (sensor == null) { isInitialized = true; return; }

        audioReader = sensor.AudioSource.OpenReader();
        if (audioReader == null) { isInitialized = true; return; }

        var audioBeams = sensor.AudioSource.AudioBeams;
        if (audioBeams != null && audioBeams.Count > 0)
            audioBeams[0].AudioBeamMode = AudioBeamMode.Automatic;

        isInitialized = true;
        Debug.Log("[Audio] 初始化成功");
    }

    void Update()
    {
        TryInitAudio();
        PollAudio();
    }

    private void PollAudio()
    {
        if (audioReader == null) return;

        var frameList = audioReader.AcquireLatestBeamFrames();
        if (frameList == null) return;

        foreach (AudioBeamFrame frame in frameList)
        {
            if (frame?.SubFrames == null) continue;
            foreach (AudioBeamSubFrame subFrame in frame.SubFrames)
            {
                if (subFrame == null) continue;
                uint frameBytes = subFrame.FrameLengthInBytes;
                if (frameBytes == 0) continue;

                byte[] floatBuffer = new byte[frameBytes];
                subFrame.CopyFrameDataToArray(floatBuffer);

                byte[] int16Buffer = ConvertFloat32ToInt16(floatBuffer);

                // 送到原始音訊端點（logging）
                audioAccumulator.Write(int16Buffer, 0, int16Buffer.Length);
                if (audioAccumulator.Length >= CHUNK_SIZE)
                {
                    if (wsAudio?.ReadyState == WebSocketState.Open)
                        wsAudio.SendAsync(audioAccumulator.ToArray(), null);

                    // 錄音中：同步送 STT 端點
                    if (isSttActive && wsStt?.ReadyState == WebSocketState.Open)
                        wsStt.SendAsync(audioAccumulator.ToArray(), null);

                    audioAccumulator.SetLength(0);
                }
            }
            frame.Dispose();
        }
    }

    private byte[] ConvertFloat32ToInt16(byte[] float32Bytes)
    {
        int sampleCount = float32Bytes.Length / 4;
        byte[] result = new byte[sampleCount * 2];
        for (int i = 0; i < sampleCount; i++)
        {
            float sample = BitConverter.ToSingle(float32Bytes, i * 4);
            sample = Mathf.Clamp(sample, -1f, 1f);
            short s = (short)(sample * 32767);
            byte[] b = BitConverter.GetBytes(s);
            result[i * 2]     = b[0];
            result[i * 2 + 1] = b[1];
        }
        return result;
    }

    void OnDestroy()
    {
        audioReader?.Dispose();
        audioReader = null;
        audioAccumulator?.Dispose();
        wsAudio?.Close();
        wsStt?.Close();
    }
}
