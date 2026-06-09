using System.Collections;
using UnityEngine;
using Unity.Cinemachine; 

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    [Header("시네머신 카메라 연결")]
    public CinemachineCamera playerCamera; 

    private CinemachineBasicMultiChannelPerlin noiseComponent;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 가상 카메라에서 진동을 담당하는 컴포넌트를 미리 가져옵니다.
        if (playerCamera != null)
        {
            noiseComponent = playerCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }
    }

    // 기존 외부 호출 방식(시간, 세기) 그대로 유지!
    public void Shake(float duration, float magnitude)
    {
        if (noiseComponent == null && playerCamera != null)
        {
            noiseComponent = playerCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }

        if (noiseComponent != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShakeCoroutine(duration, magnitude));
        }
    }

    IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        // 노이즈 세기(Amplitude)를 인자값으로 들어온 magnitude로 설정하여 흔들기 시작
        noiseComponent.AmplitudeGain = magnitude;
        noiseComponent.FrequencyGain = 1.0f; // 진동 속도 (기본값 1)

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 진동이 끝나면 세기를 0으로 만들어 원래대로 얌전하게 만듦
        noiseComponent.AmplitudeGain = 0f;
        noiseComponent.FrequencyGain = 0f;
    }
}