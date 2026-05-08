using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class WarmupController : MonoBehaviour
{
    [Header("UI 元件")]
    public Button startButton;
    public Image statusBadge;

    [Header("圖片")]
    public Sprite detectingSprite;  // 設備偵測中
    public Sprite successSprite;    // 設備偵測成功

    void Start()
    {
        startButton.onClick.AddListener(OnStart);
        StartCoroutine(SimulateDetection());
    }

    IEnumerator SimulateDetection()
    {
        // 3 秒後換成成功圖片
        yield return new WaitForSeconds(3f);
        statusBadge.sprite = successSprite;
    }

    void OnStart()
    {
        PlayerPrefs.SetString("NextScene", "GameScene-1");
        SceneManager.LoadScene("LoadingScene");
    }
}