using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameController : MonoBehaviour
{
    [Header("回合設定")]
    public int totalRounds = 4;
    public static int currentRound = 1;

    [Header("UI 元件")]
    public TMP_Text roundNumber;
    public TMP_Text roundTitle;
    public TMP_Text roundSub;
    public Button submitButton;
    public TMP_Text aiText;
    public TMP_Text inputText;
    public GameObject loadingSpinner;

    private string[] roundNames = { "第一回合", "第二回合", "第三回合", "第四回合" };
    private string placeholderText = "想到什麼就說什麼，按下麥克風可以用說的…";

    private string[] fakeResponses = {
        "美麗阿姨，這幅畫裡的紅瓦房跟老家好像！看著看著，仿佛又聽見那個夏天熱鬧的蟬鳴聲。",
        "阿公，您說的紡織廠讓我想起了那個年代，大家一起打拼的日子真的很珍貴。",
        "聽您說起淡水的往事，那個年代的台灣真的很美，充滿了人情味。",
        "謝謝您分享這段回憶，這些故事會永遠留在我們心裡。"
    };

    void Start()
    {
        submitButton.onClick.AddListener(OnSubmit);
        UpdateRoundBadge();
        loadingSpinner.SetActive(false);
    }

    void OnSubmit()
    {
        StartCoroutine(ProcessSubmit());
    }

    IEnumerator ProcessSubmit()
    {
        // 清空輸入欄
        inputText.text = placeholderText;
        inputText.color = new Color(0.67f, 0.67f, 0.67f, 1f);

        // 隱藏 AI 文字，顯示 loading
        aiText.gameObject.SetActive(false);
        loadingSpinner.SetActive(true);

        // 模擬 AI 處理等待
        yield return new WaitForSeconds(2f);

        // 隱藏 loading，顯示新回應
        loadingSpinner.SetActive(false);
        aiText.gameObject.SetActive(true);

        // 更新回合
        if (currentRound < totalRounds)
        {
            aiText.text = fakeResponses[currentRound % fakeResponses.Length];
            currentRound++;
            UpdateRoundBadge();
        }
        else
        {
            currentRound = 1;
            PlayerPrefs.SetString("NextScene", "ShareScene");
            SceneManager.LoadScene("LoadingScene");
        }
    }

    void UpdateRoundBadge()
    {
        roundNumber.text = currentRound.ToString();
        roundTitle.text = roundNames[currentRound - 1];
        roundSub.text = $"ROUND {currentRound} / {totalRounds}";
    }
}