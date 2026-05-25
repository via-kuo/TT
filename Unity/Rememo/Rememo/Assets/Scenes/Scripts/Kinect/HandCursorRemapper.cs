using UnityEngine;

/// <summary>
/// 正確架構：
/// 1. 從 KinectManager 讀取手部「世界座標」
/// 2. 用 CalibrationData 的 WorldX/YMin/Max 做 InverseLerp（正規化）
/// 3. 套用輸出邊界後，寫回 RectTransform.anchoredPosition
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class HandCursorRemapper : MonoBehaviour
{
    [Header("輸出邊界（0~1，相對於 Canvas）")]
    [Range(0f, 0.3f)] public float marginLeft = 0.05f;
    [Range(0.7f, 1f)] public float marginRight = 0.95f;
    [Range(0f, 0.3f)] public float marginBottom = 0.05f;
    [Range(0.7f, 1f)] public float marginTop = 0.95f;

    [Header("平滑係數（越小越平滑，0.1~0.3 建議值）")]
    [Range(0.05f, 1f)] public float smoothing = 0.08f;

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

        // ── 2. 用 CalibrationData 正規化到 [0, 1] ────────────
        // X 軸：Kinect 右手向右是正 X，但螢幕左→右，需對應
        // Kinect 的 X 是：左手邊為正，右手邊為負（鏡像），視你的設定而定
        // 如果游標左右相反，把下面 nx 改為 1f - nx
        float nx = Mathf.InverseLerp(
            CalibrationData.WorldXMin,
            CalibrationData.WorldXMax,
            handWorld.x);

        float ny = Mathf.InverseLerp(
            CalibrationData.WorldYMin,
            CalibrationData.WorldYMax,
            handWorld.y);

        // ── 3. 套用輸出邊界，映射到 Canvas 座標 ─────────────
        Vector2 canvasSize = _canvas.GetComponent<RectTransform>().sizeDelta;

        float targetX = Mathf.Lerp(marginLeft, marginRight, nx) * canvasSize.x;
        float targetY = Mathf.Lerp(marginBottom, marginTop, ny) * canvasSize.y;

        // ── 4. 平滑（指數移動平均）────────────────────────────
        if (!_initialized)
        {
            _smoothedPos = new Vector2(targetX, targetY);
            _initialized = true;
        }
        else
        {
            _smoothedPos = Vector2.Lerp(_smoothedPos,
                new Vector2(targetX, targetY), smoothing);
        }

        // ── 5. 寫回（用 anchoredPosition，Canvas 左下角為原點）
        _rect.anchoredPosition = _smoothedPos;
    }
}