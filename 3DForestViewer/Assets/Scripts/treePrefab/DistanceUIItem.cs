
using UnityEngine;

[DisallowMultipleComponent]
public class DistanceUIItem : MonoBehaviour
{
    [Header("Refs")]
    public Transform anchor;            // 距離を測る基準点（頭の位置など）
    public Canvas targetCanvas;         // World Space Canvas
    public CanvasGroup canvasGroup;     // フェード用

    [Header("Distance")]
    public float fadeStartDistance = 10f;
    public float hideDistance = 15f;

    [Header("Options")]
    public bool disableCanvasWhenHidden = true;

    // 内部キャッシュ（変更があった時だけ適用するため）
    float _lastAlpha = -1f;
    bool _lastCanvasEnabled = true;

    void Awake()
    {
        if (!anchor) anchor = transform;

        if (!targetCanvas) targetCanvas = GetComponentInChildren<Canvas>(true);
        if (!canvasGroup) canvasGroup = GetComponentInChildren<CanvasGroup>(true);
    }

    void OnEnable()
    {
        DistanceUIManager.Instance?.Register(this);
    }

    void OnDisable()
    {
        DistanceUIManager.Instance?.Unregister(this);
    }

    /// <summary>
    /// マネージャから呼ばれる更新関数（毎フレーム呼ばれない想定）
    /// </summary>
    public void Evaluate(Vector3 playerPos)
    {
        if (!anchor) return;

        float fadeStart = fadeStartDistance;
        float hide = hideDistance;

        // 安全策：fadeStart <= hide を保証
        if (hide < fadeStart) hide = fadeStart;

        float sqrDist = (anchor.position - playerPos).sqrMagnitude;
        float fadeStartSqr = fadeStart * fadeStart;
        float hideSqr = hide * hide;

        if (sqrDist >= hideSqr)
        {
            Apply(0f, false);
            return;
        }

        // フェード範囲外（近い）
        if (sqrDist <= fadeStartSqr)
        {
            Apply(1f, true);
            return;
        }

        // fadeStart～hide の間を 1→0
        float dist = Mathf.Sqrt(sqrDist);
        float t = Mathf.InverseLerp(fadeStart, hide, dist);
        float a = Mathf.Lerp(1f, 0f, t);

        Apply(a, true);
    }

    void Apply(float alpha, bool visible)
    {
        // CanvasGroup（変更時のみ適用）
        if (!Mathf.Approximately(_lastAlpha, alpha))
        {
            canvasGroup.alpha = alpha;
            // World Space UIはクリック不要なら常にfalseが軽い
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            _lastAlpha = alpha;
        }

        if (disableCanvasWhenHidden && targetCanvas)
        {
            bool enableCanvas = visible && alpha > 0.001f;
            if (_lastCanvasEnabled != enableCanvas)
            {
                targetCanvas.enabled = enableCanvas;
                _lastCanvasEnabled = enableCanvas;
            }
        }
    }
}

