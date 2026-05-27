using UnityEngine;

/// <summary>
/// 優化版架構：
/// 1. 從 KinectManager 讀取手部「世界座標」
/// 2. 用 CalibrationData 的 WorldX/YMin/Max 做 InverseLerp（正規化）
/// 3. 改用 Clamp01 避免超出範圍時腳本直接 return 造成卡死
/// 4. 套用輸出邊界與最低高度限制後，寫回 RectTransform.anchoredPosition
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class HandCursorRemapper : MonoBehaviour
{
    [Header("輸出邊界（0~1，相對於 Canvas）")]
    [Range(0f, 0.3f)] public float marginLeft = 0.05f;
    [Range(0.7f, 1f)] public float marginRight = 0.95f;
    [Range(0f, 0.3f)] public float marginBottom = 0.05f;
    [Range(0.7f, 1f)] public float marginTop = 0.95f;

    [Header("游標最低高度安全限制 (Canvas 像素)")]
    [Tooltip("避免手放太低時，游標陷進最底部的 UI 死角。如果按鈕點不到，把這個值調高 (例如 200~250)")]
    public float minCanvasY = 180f;

    [Header("平滑係數（越小越平滑，0.1~0.3 建議值）")]
    [Range(0.05f, 1f)] public float smoothing = 0.25f;

    [Header("追蹤哪隻手")]
    public bool useRightHand = true;

    private RectTransform _rect;
    private Canvas _canvas;
    private KinectManager _kinect;
    private Vector2 _smoothedPos;
    private bool _initialized = false;

    void Start()
    {
        _rect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _kinect = KinectManager.Instance;
    }

    void LateUpdate()
    {
        if (!CalibrationData.IsCalibrated) return;
        if (_kinect == null || !_kinect.IsInitialized()) return;

        long userId = _kinect.GetPrimaryUserID();
        if (userId == 0) return;

        // ── 1. 取得手部世界座標 ──────────────────────────────
        int jointIndex = useRightHand
            ? (int)KinectInterop.JointType.HandRight
            : (int)KinectInterop.JointType.HandLeft;

        if (!_kinect.IsJointTracked(userId, jointIndex)) return;

        Vector3 handWorld = _kinect.GetJointPosition(userId, jointIndex);

        // ── 2. 用 CalibrationData 正規化 ────────────────────
        float rawNx = Mathf.InverseLerp(
            CalibrationData.WorldXMin,
            CalibrationData.WorldXMax,
            handWorld.x);

        float rawNy = Mathf.InverseLerp(
            CalibrationData.WorldYMin,
            CalibrationData.WorldYMax,
            handWorld.y);

        // 使用 Clamp01，即使手超出範圍，依然保持游標運作並限制在邊緣
        float nx = Mathf.Clamp01(rawNx);
        float ny = Mathf.Clamp01(rawNy);

        // ── 3. 套用輸出邊界，映射到 Canvas 座標 ─────────────
        Vector2 canvasSize = _canvas.GetComponent<RectTransform>().sizeDelta;

        float targetX = Mathf.Lerp(marginLeft, marginRight, nx) * canvasSize.x;
        float targetY = Mathf.Lerp(marginBottom, marginTop, ny) * canvasSize.y;

        // 【安全修正】：確保 targetY 絕對不會低於你設定的安全高度
        targetY = Mathf.Max(targetY, minCanvasY);

        // ── 4. 平滑（指數移動平均）────────────────────────────
        if (!_initialized)
        {
            _smoothedPos = new Vector2(targetX, targetY);
            _initialized = true;
        }
        else
        {
            _smoothedPos = Vector2.Lerp(_smoothedPos, new Vector2(targetX, targetY), smoothing);
        }

        // 為了避免 Log 刷太快影響效能，可以觀察數值是否正常
        if (Time.frameCount % 5 == 0) 
        {
            Debug.Log($"[Cursor] hand={handWorld:F3} | " +
              $"nx={nx:F3}(raw={rawNx:F3}) ny={ny:F3}(raw={rawNy:F3}) | " +
              $"target=({targetX:F1},{targetY:F1}) | " +
              $"smoothed=({_smoothedPos.x:F1},{_smoothedPos.y:F1})");
        }

        // ── 5. 寫回（用 anchoredPosition，Canvas 左下角為原點）
        _rect.anchoredPosition = _smoothedPos;
    }
}