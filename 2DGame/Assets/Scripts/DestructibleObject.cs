using UnityEngine;

public class DestructibleObject : MonoBehaviour
{
    [Header("오브젝트 체력")]
    public int maxHealth = 1;
    private int currentHealth;

    [Header("파괴 연출 에셋")]
    public GameObject destroyedVersionPrefab; // 부서진 파편들이 담긴 프리팹
    public GameObject dropItemPrefab;         // 파괴 시 떨어질 아이템 (선택사항)

    private bool isDestroyed = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    //플레이어나 적의 공격 스크립트에서 호출할 대미지 함수
    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;

        //체력이 다하면 파괴 프로세스 작동
        if (currentHealth <= 0)
        {
            DestroyObject();
        }
    }

    void DestroyObject()
    {
        isDestroyed = true;

        // 1. 멀쩡한 현재 오브젝트 자리에 '부서진 파편 프리팹'을 생성합니다.
        if (destroyedVersionPrefab != null)
        {
            Instantiate(destroyedVersionPrefab, transform.position, transform.rotation);
        }

        // 2. 보상 아이템이나 코인이 있다면 생성합니다.
        if (dropItemPrefab != null)
        {
            Instantiate(dropItemPrefab, transform.position, Quaternion.identity);
        }

        // 3. 원래 있던 멀쩡한 오브젝트는 화면에서 삭제합니다.
        Destroy(gameObject);
    }
}