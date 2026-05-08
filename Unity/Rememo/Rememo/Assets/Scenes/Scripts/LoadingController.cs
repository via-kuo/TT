using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingController : MonoBehaviour
{
    public Image spinner;
    public float rotateSpeed = 200f;
    public float loadingTime = 2f;

    private float timer = 0f;
    private string nextScene = "UserSelectScene";

    void Start()
    {
        if (PlayerPrefs.HasKey("NextScene"))
        {
            nextScene = PlayerPrefs.GetString("NextScene");
        }
    }

    void Update()
    {
        spinner.transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer >= loadingTime)
        {
            SceneManager.LoadScene(nextScene);
        }
    }
}