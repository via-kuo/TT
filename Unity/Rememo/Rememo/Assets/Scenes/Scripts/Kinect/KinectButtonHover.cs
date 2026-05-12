using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class KinectButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("懸停設定")]
    public float hoverDuration = 2f;  // 懸停幾秒觸發

    [Header("UI 元件")]
    public Image progressRing;  // 懸停進度圓圈（可選）

    private Button button;
    private float hoverTimer = 0f;
    private bool isHovering = false;

    void Start()
    {
        button = GetComponent<Button>();

        // 從 HandCursor 取得 CursorProgressRing
        var handCursor = GameObject.Find("HandCursor");
        if (handCursor != null)
        {
            var rings = handCursor.GetComponentsInChildren<Image>();
            foreach (var ring in rings)
            {
                if (ring.gameObject.name == "CursorProgressRing")
                {
                    progressRing = ring;
                    break;
                }
            }
        }
    }

    void Update()
    {
        if (!isHovering) return;

        hoverTimer += Time.deltaTime;

        // 更新進度圓圈
        if (progressRing != null)
            progressRing.fillAmount = hoverTimer / hoverDuration;

        // 懸停時間到，觸發按鈕
        if (hoverTimer >= hoverDuration)
        {
            hoverTimer = 0f;
            isHovering = false;

            if (progressRing != null)
                progressRing.fillAmount = 0f;

            button?.onClick.Invoke();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        hoverTimer = 0f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        hoverTimer = 0f;

        if (progressRing != null)
            progressRing.fillAmount = 0f;
    }
}