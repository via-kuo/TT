using UnityEngine;

/// <summary>
/// 優化版架構：
/// 1. 從 KinectManager 讀取手部「世界座標」
/// 2. 用 CalibrationData 的 WorldX/YMin/Max 做 InverseLerp（正規化）
/// 3. 改用 Clamp01 避免超出範圍時腳本直接 return 造成卡死
/// 4. 套用輸出邊界與最低高度限制後，寫回 RectTransform.anchoredPosition
/// 5. 游標永不隱藏：未舉手/未追蹤時固定停在左下角，舉手後才開始跟隨手部
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

    [Header("舉手才開始操作")]
    [Tooltip("手部正規化高度（0~1，相對於校正的 Y 範圍）超過這個值才開始跟隨手部移動")]
    [Range(0f, 1f)] public float raiseThreshold = 0.15f;
    [Tooltip("開始操作後，正規化高度低於這個值就放手歸位（建議比 raiseThreshold 低一點，避免邊緣抖動）")]
    [Range(0f, 1f)] public float lowerThreshold = 0.08f;

    private RectTransform _rect;
    private Canvas _canvas;
    private KinectManager _kinect;
    private Vector2 _smoothedPos;
    private bool _initialized = false;

    // 是否正在跟隨手部移動（false = 停在左下角）
    private bool _isActive = false;
    // 按鈕觸發歸位後，要求使用者先把手放下才能再次舉手操作，避免同一個按鈕被連續誤觸
    private bool _waitingForLower = false;

    void Start()
    {
        _rect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _kinect = KinectManager.Instance;
    }

    /// <summary>
    /// 由按鈕觸發（例如 KinectButtonHover 完成 dwell click 後）呼叫，
    /// 讓游標歸位到左下角，並要求使用者放下手再重新舉起才能繼續操作。
    /// </summary>
    public void ResetToCorner()
    {
        _isActive = false;
        _waitingForLower = true;
    }

    Vector2 GetCornerPos(Vector2 canvasSize)
    {
        return new Vector2(marginLeft * canvasSize.x, marginBottom * canvasSize.y);
    }

    void LateUpdate()
    {
        if (_canvas == null) return;
        Vector2 canvasSize = _canvas.GetComponent<RectTransform>().sizeDelta;
        Vector2 goalPos = GetCornerPos(canvasSize);

        bool trackingOk =
            CalibrationData.IsCalibrated &&
            _kinect != null && _kinect.IsInitialized();

        long userId = trackingOk ? _kinect.GetPrimaryUserID() : 0;
        trackingOk = trackingOk && userId != 0;

        int jointIndex = useRightHand
            ? (int)KinectInterop.JointType.HandRight
            : (int)KinectInterop.JointType.HandLeft;

        trackingOk = trackingOk && _kinect.IsJointTracked(userId, jointIndex);

        if (trackingOk)
        {
            Vector3 handWorld = _kinect.GetJointPosition(userId, jointIndex);

            // ── 用 CalibrationData 正規化 ────────────────────
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

            if (_waitingForLower)
            {
                // 按鈕觸發後，等手真正放下（低於 lowerThreshold）才解除鎖定
                if (ny < lowerThreshold)
                    _waitingForLower = false;
            }
            else
            {
                if (!_isActive && ny >= raiseThreshold)
                    _isActive = true;
                else if (_isActive && ny < lowerThreshold)
                    _isActive = false;
            }

            if (_isActive)
            {
                float targetX = Mathf.Lerp(marginLeft, marginRight, nx) * canvasSize.x;
                float targetY = Mathf.Lerp(marginBottom, marginTop, ny) * canvasSize.y;

                // 【安全修正】：確保 targetY 絕對不會低於你設定的安全高度
                targetY = Mathf.Max(targetY, minCanvasY);

                goalPos = new Vector2(targetX, targetY);
            }

            if (Time.frameCount % 5 == 0)
            {
                Debug.Log($"[Cursor] active={_isActive} waitingForLower={_waitingForLower} hand={handWorld:F3} | " +
                  $"nx={nx:F3} ny={ny:F3} | goal=({goalPos.x:F1},{goalPos.y:F1})");
            }
        }
        else
        {
            // 沒追蹤到手：視為手放下了，解除歸位鎖定，回到左下角待命
            _isActive = false;
            _waitingForLower = false;
        }

        // ── 平滑（指數移動平均），讓「歸位 ↔ 跟隨」之間的過渡不會跳格 ──
        if (!_initialized)
        {
            _smoothedPos = goalPos;
            _initialized = true;
        }
        else
        {
            _smoothedPos = Vector2.Lerp(_smoothedPos, goalPos, smoothing);
        }

        // ── 寫回（用 anchoredPosition，Canvas 左下角為原點）
        _rect.anchoredPosition = _smoothedPos;
    }
}
