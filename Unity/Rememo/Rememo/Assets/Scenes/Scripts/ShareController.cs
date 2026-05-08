using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class ShareController : MonoBehaviour
{
    [Header("UI 元件")]
    public Button submitButton;
    public Button micButton;
    public Image micButtonImage;
    public TMP_Text inputText;

    private bool isRecording = false;
    private string placeholderText = "想到什麼就說什麼，按下麥克風可以用說的…";

    void Start()
    {
        submitButton.onClick.AddListener(OnSubmit);
        micButton.onClick.AddListener(OnMicToggle);
    }

    void OnSubmit()
    {
        PlayerPrefs.SetString("NextScene", "ThankYouScene");
        SceneManager.LoadScene("LoadingScene");
    }

    void OnMicToggle()
    {
        if (!isRecording)
            StartCoroutine(SimulateRecording());
    }

    IEnumerator SimulateRecording()
    {
        isRecording = true;
        inputText.text = "錄音中...";
        inputText.color = new Color(1f, 0.4f, 0.4f, 1f);
        micButtonImage.color = new Color(1f, 0.3f, 0.3f, 1f);

        yield return new WaitForSeconds(2f);

        inputText.text = "辨識中...";
        inputText.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        micButtonImage.color = Color.white;

        yield return new WaitForSeconds(1f);

        inputText.text = "今天很開心，學到了很多東西。";
        inputText.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        isRecording = false;
    }
}