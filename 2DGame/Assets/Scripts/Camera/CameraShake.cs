using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance; // 어디서든 부를 수 있게 싱글톤 세팅

    private Vector3 originalPos;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 카메라의 원래 시작 위치를 기억해 둡니다.
        originalPos = transform.localPosition;
    }

    // ★ 외부에서 호출할 진동 시작 함수 (진동 시간, 진동 세기)
    public void Shake(float duration, float magnitude)
    {
        // 기존에 돌고 있던 진동이 있다면 끄고 새로 시작
        StopAllCoroutines();
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 무작위로 살짝 틀어진 좌표 계산
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // 카메라 위치를 순간적으로 변경 ($Z$축 위치는 유지)
            transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;

            // 다음 프레임까지 대기
            yield return null;
        }

        // 진동이 끝나면 안전하게 원래 위치로 정밀 복구
        transform.localPosition = originalPos;
    }
}