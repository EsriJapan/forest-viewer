
using UnityEngine;

[DisallowMultipleComponent]
public class BillboardYawOnly : MonoBehaviour
{
    [Tooltip("未設定なら Camera.main を使います（Awakeで1回だけ取得）")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("向きの反転が必要な場合は 180 を入れる")]
    [SerializeField] private float yawOffsetDegrees = 0f;

    private Transform camTf;

    void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        camTf = targetCamera != null ? targetCamera.transform : null;
    }

    void LateUpdate()
    {
        if (camTf == null)
        {
            // カメラが後から生成されるケース対策
            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;
            camTf = cam.transform;
        }

        // カメラ方向ベクトルをXZ平面に投影（＝水平だけ追従）
        Vector3 dir = transform.position - camTf.position;
        dir.y = 0f;

        // 同一点だと回転できないのでガード
        if (dir.sqrMagnitude < 0.0001f) return;

        // “カメラに正面を向ける”回転を作る
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

        // UIが裏向きになる場合の調整（多くは 180度）
        if (Mathf.Abs(yawOffsetDegrees) > 0.001f)
            rot *= Quaternion.Euler(0f, yawOffsetDegrees, 0f);

        transform.rotation = rot;
    }
}
