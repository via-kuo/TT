using UnityEngine;
using UnityEngine.UI;
using Windows.Kinect;

public class KinectMicVisualizer : MonoBehaviour
{
    [Header("UI 元件")]
    public RawImage waveformDisplay;

    [Header("波形設定")]
    public int textureWidth  = 512;
    public int textureHeight = 128;
    public Color waveColor   = Color.green;
    public Color bgColor     = Color.black;

    private KinectSensor sensor;
    private AudioBeamFrameReader audioReader;
    private Texture2D waveTexture;
    private float[] audioBuffer;
    private int bufferIndex = 0;

    void Start()
    {
        // 建立波形 Texture
        waveTexture = new Texture2D(textureWidth, textureHeight);
        if (waveformDisplay != null)
            waveformDisplay.texture = waveTexture;

        audioBuffer = new float[textureWidth];

        // 初始化 Kinect 音訊
        sensor = KinectSensor.GetDefault();
        if (sensor == null)
        {
            Debug.LogError("[MicVisualizer] 找不到 Kinect 感測器");
            return;
        }

        audioReader = sensor.AudioSource.OpenReader();
        audioReader.FrameArrived += OnAudioFrameArrived;

        if (!sensor.IsOpen) sensor.Open();
    }

    private void OnAudioFrameArrived(object sender, AudioBeamFrameArrivedEventArgs e)
    {
        var frameList = e.FrameReference.AcquireBeamFrames();
        if (frameList == null) return;

        foreach (AudioBeamFrame frame in frameList)
        {
            foreach (AudioBeamSubFrame subFrame in frame.SubFrames)
            {
                int sampleCount = (int)(subFrame.Duration.TotalSeconds * 16000);
                byte[] floatBuffer = new byte[sampleCount * 4];
                subFrame.CopyFrameDataToArray(floatBuffer);

                for (int i = 0; i < sampleCount; i++)
                {
                    float sample = System.BitConverter.ToSingle(floatBuffer, i * 4);
                    audioBuffer[bufferIndex % textureWidth] = sample;
                    bufferIndex++;
                }
            }
        }
    }

    void Update()
    {
        DrawWaveform();
    }

    void DrawWaveform()
    {
        // 清空背景
        for (int x = 0; x < textureWidth; x++)
            for (int y = 0; y < textureHeight; y++)
                waveTexture.SetPixel(x, y, bgColor);

        // 畫波形
        int mid = textureHeight / 2;
        for (int x = 0; x < textureWidth; x++)
        {
            int idx = (bufferIndex + x) % textureWidth;
            float sample = audioBuffer[idx];
            int height = Mathf.Clamp((int)(sample * mid), -mid, mid);

            int yStart = Mathf.Min(mid, mid + height);
            int yEnd   = Mathf.Max(mid, mid + height);

            for (int y = yStart; y <= yEnd; y++)
                waveTexture.SetPixel(x, y, waveColor);
        }

        waveTexture.Apply();
    }

    void OnDestroy()
    {
        audioReader?.Dispose();
        sensor?.Close();
    }
}