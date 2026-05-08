using UnityEngine;
using TMPro;

public class SpeechTextHandler : MonoBehaviour
{
    [Header("UI 連結")]
    public TextMeshProUGUI aiText; // 把你的 AIText 物件拖進來

    [Header("設定")]
    public int maxLines = 5; // 你設定的五行限制

    /// <summary>
    /// 當語音辨識有新結果時呼叫此 Function
    /// </summary>
    /// <param name="receivedText">語音辨識傳回來的字串</param>
    public void OnReceiveSpeech(string receivedText)
    {
        // 1. 將文字直接丟給 TMP
        aiText.text = receivedText;

        // 2. 為了對齊美觀，你可以在前面加一點點 Padding (非必要)
        // aiText.text = "　" + receivedText; 

        // 3. 檢查行數（如果超過五行，可以根據需求處理）
        Canvas.ForceUpdateCanvases(); // 強制更新排版以獲取最新行數
        if (aiText.textInfo.lineCount > maxLines)
        {
            // 處理方式 A：字體稍微縮小，試著塞進去
            // aiText.fontSize -= 1f;

            // 處理方式 B：或者你可以加上邏輯，讓字只顯示最後五行 (進階做法)
            Debug.LogWarning("注意：目前文字已超過五行橫線範圍！");
        }
    }

    /// <summary>
    /// 清除所有文字（進入下一回合時使用）
    /// </summary>
    public void ClearText()
    {
        aiText.text = "";
    }
}