using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("추적 대상 설정")]
    public Transform target;          // 카메라가 쫓아갈 플레이어의 Transform

    [Header("카메라 움직임 설정")]
    public float smoothSpeed = 0.125f; // 카메라가 따라가는 부드러움 정도 (낮을수록 부드럽고 묵직함)
    public Vector3 offset = new Vector3(0f, 1f, -10f); // 플레이어와 카메라 사이의 거리 유지 ($Z$축 -10 필수)

    [Header("맵 제한 범위 (선택)")]
    public bool useBounds = false;     // 카메라가 맵 밖으로 나가지 못하게 제한할 것인가?
    public Vector2 minBounds;          // 카메라가 갈 수 있는 최소 $X, Y$ 좌표
    public Vector2 maxBounds;          // 카메라가 갈 수 있는 최대 $X, Y$ 좌표

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 카메라가 이동해야 할 목적지 좌표 계산 (플레이어 위치 + 오프셋)
        Vector3 desiredPosition = target.position + offset;

        // 2. 부드러운 이동을 위한 보간 연산 (Lerp)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // 3. 맵 제한 범위를 사용한다면 좌표를 좁혀줍니다 (Clamp)
        if (useBounds)
        {
            float clampedX = Mathf.Clamp(smoothedPosition.x, minBounds.x, maxBounds.x);
            float clampedY = Mathf.Clamp(smoothedPosition.y, minBounds.y, maxBounds.y);
            smoothedPosition = new Vector3(clampedX, clampedY, smoothedPosition.z);
        }

        // 4. 최종적으로 카메라의 위치를 업데이트
        transform.position = smoothedPosition;
    }
}