using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class MicController : MonoBehaviour
{
    [Header("UI 元件")]
    public Button micButton;
    public Image micButtonImage;
    public TMP_Text aiText;
    public Image sceneImage;
    public TMP_Text inputText;

    [Header("假資料")]
    public Sprite[] testImages;

    private bool isRecording = false;
    private int responseIndex = 0;

    private string placeholderText = "想到什麼就說什麼，按下麥克風可以用說的…";

    private string[] fakeInputs = {
        "我記得以前在紡織廠工作，大家感情都很好，中午都會一起吃飯。",
        "小時候住在眷村，鄰居都很親，常常互相串門子。",
        "年輕的時候很喜歡去西門町看電影，那時候票價很便宜。",
        "我媽媽很會做紅燒肉，每次過年都會做一大鍋。"
    };

    private string[] fakeResponses = {
        "美麗阿姨，這幅畫裡的紅瓦房跟老家好像！看著看著，仿佛又聽見那個夏天熱鬧的蟬鳴聲。",
        "阿公，您說的紡織廠讓我想起了那個年代，大家一起打拼的日子真的很珍貴。",
        "聽您說起淡水的往事，那個年代的台灣真的很美，充滿了人情味。",
        "謝謝您分享這段回憶，這些故事會永遠留在我們心裡。"
    };

    void Start()
    {
        micButton.onClick.AddListener(OnMicButtonClick);
        ResetInputText();
    }

    void ResetInputText()
    {
        if (inputText == null) return;
        inputText.text = placeholderText;
        inputText.color = new Color(0.67f, 0.67f, 0.67f, 1f); // 灰色
    }

    void OnMicButtonClick()
    {
        if (!isRecording)
            StartCoroutine(SimulateRecording());
    }

    IEnumerator SimulateRecording()
    {
        isRecording = true;

        // 顯示錄音中
        inputText.text = "錄音中...";
        inputText.color = new Color(1f, 0.4f, 0.4f, 1f); // 紅色
        micButtonImage.color = new Color(1f, 0.3f, 0.3f, 1f); // 按鈕變紅

        yield return new WaitForSeconds(2f);

        // 顯示辨識中
        inputText.text = "辨識中...";
        inputText.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        micButtonImage.color = Color.white;

        yield return new WaitForSeconds(1f);

        // 顯示假的語音辨識結果
        int index = responseIndex % fakeInputs.Length;
        inputText.text = fakeInputs[index];
        inputText.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // 更新 AI 回應文字
        aiText.text = fakeResponses[index];

        // 更新假圖片
        if (testImages != null && testImages.Length > 0)
            sceneImage.sprite = testImages[index % testImages.Length];

        responseIndex++;
        isRecording = false;
    }
}