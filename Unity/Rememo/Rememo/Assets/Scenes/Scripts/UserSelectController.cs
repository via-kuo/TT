using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class UserSelectController : MonoBehaviour
{
    [Header("UI 元件")]
    public GameObject userCardPrefab;
    public Transform userGrid;
    public Button logoutButton;
    public TMP_Text topBarText;

    void Start()
    {
        logoutButton.onClick.AddListener(OnLogout);
        LoadTestData();
    }

    void LoadTestData()
    {
        List<string[]> testUsers = new List<string[]>
        {
            new string[] { "張美麗", "60" },
            new string[] { "蘇有才", "60" },
            new string[] { "郭坤宏", "59" },
            new string[] { "郭坤宏", "59" },
        };

        foreach (var user in testUsers)
        {
            CreateUserCard(user[0], int.Parse(user[1]));
        }
    }

    void CreateUserCard(string name, int age)
    {
        GameObject card = Instantiate(userCardPrefab, userGrid);

        TMP_Text nameText = card.transform.Find("UserName").GetComponent<TMP_Text>();
        nameText.text = name;

        TMP_Text ageText = card.transform.Find("UserAge").GetComponent<TMP_Text>();
        ageText.text = $"年齡: {age}";

        Button cardButton = card.GetComponent<Button>();
        if (cardButton != null)
        {
            string userName = name;
            cardButton.onClick.AddListener(() => OnUserSelected(userName));
        }
    }

    void OnUserSelected(string userName)
    {
        Debug.Log($"選擇使用者：{userName}");
        // 選擇使用者後跳到 LoadingScene 再去 WarmupScene
        PlayerPrefs.SetString("NextScene", "WarmupScene");
        SceneManager.LoadScene("LoadingScene");
    }

    void OnLogout()
    {
        SceneManager.LoadScene("LoginScene");
    }
}