using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextLineDrawer : MonoBehaviour
{
    public TMP_Text targetText;
    public RectTransform lineContainer;
    public Color lineColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public float lineHeight = 4f;
    public float leftPadding = 20f;
    public float rightPadding = 20f;

    private string lastText = "";

    void Start()
    {
        DrawLines();
    }

    void Update()
    {
        if (targetText != null && targetText.text != lastText)
        {
            lastText = targetText.text;
            DrawLines();
        }
    }

    public void DrawLines()
    {
        foreach (Transform child in lineContainer)
            Destroy(child.gameObject);

        if (targetText == null) return;

        targetText.ForceMeshUpdate();
        var textInfo = targetText.textInfo;

        for (int i = 0; i < textInfo.lineCount; i++)
        {
            var lineInfo = textInfo.lineInfo[i];
            if (lineInfo.characterCount == 0) continue;

            GameObject line = new GameObject($"Line_{i}");
            line.transform.SetParent(targetText.rectTransform, false);
            Image lineImage = line.AddComponent<Image>();
            lineImage.color = lineColor;

            RectTransform rect = line.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.pivot = new Vector2(0, 0.5f);

            float halfWidth = targetText.rectTransform.rect.width * 0.5f;
            float endX = lineInfo.lineExtents.max.x + halfWidth;
            float yPos = lineInfo.descender - (targetText.rectTransform.rect.height * 0.5f) + targetText.rectTransform.rect.height;

            rect.anchoredPosition = new Vector2(leftPadding, yPos);
            rect.sizeDelta = new Vector2(endX - leftPadding, lineHeight);
        }
    }
}