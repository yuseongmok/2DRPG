using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [Header("점프대 설정")]
    public float launchForce = 15f; // 튕겨 나갈 힘의 세기

    [Header("점프대 사운드 설정")]
    public string launchSoundName = "JumpPadLaunch"; // ★ 사운드매니저에 등록할 점프대 작동음 이름

    // 애니메이션이 있다면 연결
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // 2D 물체가 이 오브젝트의 트리거 충돌체에 들어왔을 때 발동
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 밟은 대상이 '플레이어'인지 확인합니다.
        if (collision.CompareTag("Player"))
        {
            Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();

            if (playerRb != null)
            {
                // 1. 플레이어의 기존 하강/상승 속도를 깔끔하게 초기화합니다.
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0f);

                // 2. 위쪽(Vector2.up) 방향으로 설정한 힘을 순간적으로 가합니다.
                playerRb.AddForce(Vector2.up * launchForce, ForceMode2D.Impulse);

                // ★ [점프대 작동 효과음 재생]
                // 플레이어가 튕겨 나가는 순간에 사운드매니저를 통해 효과음을 출력합니다.
                if (SoundManager.Instance != null && !string.IsNullOrEmpty(launchSoundName))
                {
                    SoundManager.Instance.PlaySFX(launchSoundName);
                }

                // 3. (선택) 점프대 작동 애니메이션 트리거 작동
                if (animator != null)
                {
                    animator.SetTrigger("Launch");
                }

                Debug.Log("🚀 플레이어가 점프대를 밟아 높이 튕겨 나갑니다!");
            }
        }
    }
}