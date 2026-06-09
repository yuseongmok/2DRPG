using UnityEngine;

public class TreasureChest : MonoBehaviour
{
    [Header("상자 상태 설정")]
    private bool isOpened = false;

    // 애니메이션을 제어할 컴포넌트 변수
    private Animator animator;

    [Header("보상 설정")]
    public GameObject dropItemPrefab; // 상자가 열릴 때 튀어나올 아이템 프리팹

    [Header("보물상자 사운드 설정")]
    public string chestOpenSoundName = "ChestOpen"; // ★ 사운드매니저에 등록할 상자 열림 효과음 이름

    void Start()
    {
        // 내 오브젝트에 붙은 Animator를 자동으로 가져옵니다.
        animator = GetComponent<Animator>();
    }

    // 플레이어가 때렸을 때 호출될 상자 열기 함수
    public void OpenChest()
    {
        // 이미 열려있다면 중복 실행 방지
        if (isOpened) return;

        isOpened = true;

        // ★ [보물상자 열림 효과음 재생]
        // 상자가 열리는 판정이 시작되자마자 사운드매니저를 통해 효과음을 출력합니다.
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(chestOpenSoundName))
        {
            SoundManager.Instance.PlaySFX(chestOpenSoundName);
        }

        // 1. 애니메이터에 설정한 'Open' 트리거 발동 (애니메이션 재생)
        if (animator != null)
        {
            animator.SetTrigger("IsOpened");
            Debug.Log($"🎁 {gameObject.name} 상자가 열렸습니다!");
        }

        // 2. 아이템 보상 스폰
        if (dropItemPrefab != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(0f, 0.5f, 0f); // 상자 살짝 위
            Instantiate(dropItemPrefab, spawnPos, Quaternion.identity);
        }

        // 3. 더 이상 공격 타격판정에 걸리지 않도록 충돌체(Collider)를 꺼버립니다.
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
    }
}