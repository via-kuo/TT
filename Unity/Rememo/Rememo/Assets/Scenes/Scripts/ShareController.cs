using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using WebSocketSharp;

public class ShareController : MonoBehaviour
{
    [Header("UI 元件")]
    public Button submitButton;
    public Button micButton;
    public Image micButtonImage;
    public TMP_Text inputText;

    [Header("Kinect 整合")]
    [Tooltip("拖入場景中的 KinectAudioSender；若留空則退回使用內建麥克風")]
    public KinectAudioSender kinectAudioSender;

    [Header("WebSocket 設定（內建麥克風模式用）")]
    public string serverUrl = "ws://localhost:8000/ws/stt";

    [Header("錄音設定（內建麥克風模式用）")]
    public int sampleRate = 16000;
    public int maxRecordSeconds = 60;

    [Header("逐字動畫")]
    [Tooltip("每個字之間的秒數，建議 0.02~0.05")]
    public float charInterval = 0.03f;

    private WebSocket ws;
    private AudioClip micClip;
    private string micDevice;
    private int lastSamplePos = 0;
    private bool isRecording = false;

    private readonly Queue<string> incomingMessages = new Queue<string>();
    private readonly object queueLock = new object();

    private readonly string placeholderText = "想到什麼就說什麼，按下麥克風可以用說的…";
    private string displayedText = "";
    private Coroutine typingCoroutine;

    [System.Serializable]
    private class ControlPayload { public string type; }

    [System.Serializable]
    private class STTMessage { public string type; public string text; public bool isFinal; }

    private bool UseKinect => kinectAudioSender != null;

    void Start()
    {
        submitButton.onClick.AddListener(OnSubmit);
        micButton.onClick.AddListener(OnMicToggle);
        ResetInputText();

        if (!UseKinect)
            ConnectWebSocket();
    }

    void ConnectWebSocket()
    {
        ws = new WebSocket(serverUrl);
        ws.OnOpen  += (s, e) => Debug.Log("[Share STT WS] 已連線");
        ws.OnError += (s, e) => Debug.LogError($"[Share STT WS] 錯誤: {e.Message}");
        ws.OnClose += (s, e) => Debug.Log("[Share STT WS] 已關閉");
        ws.OnMessage += (s, e) => {
            if (!e.IsText) return;
            lock (queueLock) incomingMessages.Enqueue(e.Data);
        };
        ws.ConnectAsync();
    }

    void ResetInputText()
    {
        if (inputText == null) return;
        inputText.text = placeholderText;
        inputText.color = new Color(0.67f, 0.67f, 0.67f, 1f);
        displayedText = "";
    }

    void OnSubmit()
    {
        PlayerPrefs.SetString("NextScene", "ThankYouScene");
        SceneManager.LoadScene("LoadingScene");
    }

    void OnMicToggle()
    {
        if (!isRecording) StartRecording();
        else              StopRecording();
    }

    void StartRecording()
    {
        isRecording = true;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        displayedText  = "";
        inputText.text = "錄音中...";
        inputText.color = new Color(1f, 0.4f, 0.4f, 1f);
        micButtonImage.color = new Color(1f, 0.3f, 0.3f, 1f);

        if (UseKinect)
        {
            kinectAudioSender.OnSttMessage = OnKinectSttMessage;
            kinectAudioSender.StartSTT();
        }
        else
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[Share Mic] 找不到麥克風裝置");
                isRecording = false;
                return;
            }
            if (ws == null || ws.ReadyState != WebSocketState.Open)
                ConnectWebSocket();

            micDevice = Microphone.devices[0];
            micClip   = Microphone.Start(micDevice, true, maxRecordSeconds, sampleRate);
            lastSamplePos = 0;
            SendControl("start");
        }
    }

    void StopRecording()
    {
        isRecording = false;

        inputText.text  = "辨識中...";
        inputText.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        micButtonImage.color = Color.white;

        if (UseKinect)
            kinectAudioSender.StopSTT();
        else
        {
            Microphone.End(micDevice);
            SendControl("end");
        }
    }

    void SendControl(string type)
    {
        if (ws?.ReadyState == WebSocketState.Open)
            ws.SendAsync(JsonUtility.ToJson(new ControlPayload { type = type }), null);
    }

    void Update()
    {
        if (!UseKinect && isRecording)
            StreamMicAudio();

        DrainIncomingMessages();
    }

    void StreamMicAudio()
    {
        int pos = Microphone.GetPosition(micDevice);
        if (pos < lastSamplePos) lastSamplePos = 0;
        int sampleCount = pos - lastSamplePos;
        if (sampleCount <= 0) return;

        float[] samples = new float[sampleCount];
        micClip.GetData(samples, lastSamplePos);
        lastSamplePos = pos;

        if (ws?.ReadyState == WebSocketState.Open)
            ws.SendAsync(FloatToInt16Bytes(samples), null);
    }

    byte[] FloatToInt16Bytes(float[] samples)
    {
        byte[] result = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short s = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767);
            result[i * 2]     = (byte)(s & 0xFF);
            result[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }
        return result;
    }

    void DrainIncomingMessages()
    {
        while (true)
        {
            string json;
            lock (queueLock)
            {
                if (incomingMessages.Count == 0) return;
                json = incomingMessages.Dequeue();
            }
            HandleSTTMessage(json);
        }
    }

    private void OnKinectSttMessage(string json)
    {
        lock (queueLock) incomingMessages.Enqueue(json);
    }

    void HandleSTTMessage(string json)
    {
        STTMessage msg;
        try { msg = JsonUtility.FromJson<STTMessage>(json); }
        catch { Debug.LogWarning("[Share STT] 無法解析: " + json); return; }

        if (msg == null || msg.type != "transcript") return;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeCharByChar(msg.text));
    }

    IEnumerator TypeCharByChar(string target)
    {
        inputText.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        if (target.StartsWith(displayedText))
        {
            for (int i = displayedText.Length; i <= target.Length; i++)
            {
                string partial = target.Substring(0, i);
                inputText.text = partial;
                displayedText  = partial;
                yield return new WaitForSeconds(charInterval);
            }
        }
        else
        {
            inputText.text = target;
            displayedText  = target;
        }
    }

    void OnDestroy()
    {
        if (!UseKinect && isRecording)
            Microphone.End(micDevice);
        ws?.Close();
    }
}
