using UnityEngine;

public class DestroyEffect : MonoBehaviour
{
    [Header("이펙트가 유지될 시간 (초)")]
    public float delayTime = 1.0f;

    void Start()
    {
        Destroy(gameObject, delayTime);
    }
}