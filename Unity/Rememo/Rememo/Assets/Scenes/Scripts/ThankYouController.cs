using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ThankYouController : MonoBehaviour
{
    public TMP_Text countdownText;
    public float countdownTime = 5f;

    private float timer;

    void Start()
    {
        timer = countdownTime;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        int seconds = Mathf.CeilToInt(timer);

        countdownText.text = $"系統在{seconds}秒後會跳回主選單";

        if (timer <= 0)
            SceneManager.LoadScene("UserSelectScene");
    }
}