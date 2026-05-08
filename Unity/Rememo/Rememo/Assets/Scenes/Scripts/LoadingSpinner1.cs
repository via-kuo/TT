using UnityEngine;
using UnityEngine.UI;

public class LoadingSpinner1 : MonoBehaviour
{
    [Header("設定")]
    public int dotCount = 8;
    public float radius = 40f;
    public float dotSize = 16f;
    public Color dotColor = new Color(0.83f, 0.58f, 0.23f, 1f);
    public float rotateSpeed = 150f;

    void Start()
    {
        // 自動生成圓點
        for (int i = 0; i < dotCount; i++)
        {
            GameObject dot = new GameObject($"Dot_{i}");
            dot.transform.SetParent(transform, false);

            Image img = dot.AddComponent<Image>();
            img.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");

            // 顏色漸變（越後面越淡）
            float alpha = (float)(i + 1) / dotCount;
            img.color = new Color(dotColor.r, dotColor.g, dotColor.b, alpha);

            RectTransform rect = dot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(dotSize, dotSize);

            // 排成圓形
            float angle = i * (360f / dotCount) * Mathf.Deg2Rad;
            rect.anchoredPosition = new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );
        }
    }

    void Update()
    {
        transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime);
    }
}