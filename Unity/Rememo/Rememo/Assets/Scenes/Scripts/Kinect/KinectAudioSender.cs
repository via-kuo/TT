using System;
using System.IO;
using UnityEngine;
using WebSocketSharp;
using Windows.Kinect;

public class KinectAudioSender : MonoBehaviour
{
    [Header("WebSocket 設定")]
    public string serverUrl = "ws://localhost:8000/ws/audio";

    private WebSocket ws;
    private KinectSensor sensor;
    private AudioBeamFrameReader audioReader;
    private MemoryStream audioAccumulator = new MemoryStream();

    // 100ms chunk @ 16kHz 16bit = 3200 bytes
    private const int CHUNK_SIZE = 3200;

    void Start()
    {
        ws = new WebSocket(serverUrl);
        ws.OnOpen += (s, e) => Debug.Log("[Audio WS] 已連線");
        ws.OnError += (s, e) => Debug.LogError($"[Audio WS] 錯誤: {e.Message}");
        ws.OnClose += (s, e) => Debug.Log("[Audio WS] 已關閉");
        ws.ConnectAsync();

        sensor = KinectSensor.GetDefault();
        if (sensor == null)
        {
            Debug.LogError("[Audio] 找不到 Kinect 感測器");
            return;
        }

        audioReader = sensor.AudioSource.OpenReader();
        audioReader.FrameArrived += OnAudioFrameArrived;

        if (!sensor.IsOpen) sensor.Open();
    }

    private void OnAudioFrameArrived(object sender, AudioBeamFrameArrivedEventArgs e)
    {
        var frameReference = e.FrameReference;
        var frameList = frameReference.AcquireBeamFrames();
        if (frameList == null) return;

        foreach (AudioBeamFrame frame in frameList)
        {
            foreach (AudioBeamSubFrame subFrame in frame.SubFrames)
            {
                // 直接用 BytesPerSample 和 Duration 計算大小
                int sampleCount = (int)(subFrame.Duration.TotalSeconds * 16000);
                byte[] floatBuffer = new byte[sampleCount * 4];
                subFrame.CopyFrameDataToArray(floatBuffer);

                byte[] int16Buffer = ConvertFloat32ToInt16(floatBuffer);
                audioAccumulator.Write(int16Buffer, 0, int16Buffer.Length);

                if (audioAccumulator.Length >= CHUNK_SIZE)
                {
                    if (ws?.ReadyState == WebSocketState.Open)
                    {
                        ws.SendAsync(audioAccumulator.ToArray(), null);
                    }
                    audioAccumulator.SetLength(0);
                }
            }
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
            result[i * 2] = b[0];
            result[i * 2 + 1] = b[1];
        }
        return result;
    }

    void OnDestroy()
    {
        audioReader?.Dispose();
        audioAccumulator?.Dispose();
        ws?.Close();
        sensor?.Close();
    }
}