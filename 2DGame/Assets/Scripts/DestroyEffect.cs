using UnityEngine;
public class DestroyEffect : MonoBehaviour {
    void Start() {
        // 0.2초 뒤에 이펙트 오브젝트를 자동으로 파괴 (애니메이션 길이에 맞게 조절 가능)
        Destroy(gameObject, 0.2f); 
    }
}