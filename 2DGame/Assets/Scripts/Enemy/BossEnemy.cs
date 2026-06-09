using System.Collections;
using UnityEngine;

public class BossEnemy : MonoBehaviour
{
    public enum BossState { Idle, Chase, Attack, Cast, Death }
    [Header("현재 상태")]
    public BossState currentState = BossState.Idle;

    [Header("보스 능력치")]
    public string bossName = "보스 이름";
    public int maxHealth = 500;
    private int currentHealth;
    public float moveSpeed = 3f;
    public int attackDamage = 20;

    [Header("추적 및 거리 설정")]
    public float attackRange = 2.0f;
    public float chaseRange = 12.0f;
    private Vector2 startPosition;

    [Header("마법 패턴 설정")]
    public GameObject magicEffectPrefab; 
    public float magicDamageRadius = 1.8f; // 마법 폭발이 대미지를 줄 반경 크기

    [Header("보스 고유 사운드 이름 설정")]
    public string bossBgmName = "BossBGM";            // 보스전 시작 시 재생할 배경음 이름
    public string closeAttackSoundName = "BossAttack"; //  근접 공격 시 터질 효과음 이름
    public string magicCastSoundName = "BossCast";     // 마법 기 모을 때(캐스팅) 터질 효과음 이름
    public string magicExplodeSoundName = "BossExplode"; // 마법이 실제로 쾅! 터질 때 효과음 이름

    [Header("컴포넌트 및 레이어")]
    public Transform player;
    public LayerMask playerLayer;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private bool isFacingRight = false;
    private bool isHandlingPattern = false;
    private bool isBgmStarted = false; // BGM이 중복으로 다시 켜지는 것을 방지하는 플래그

    [Header("트랜스폼 설정")]
    public Transform visualChild;

    private const string ANIM_IDLE = "Idle";
    private const string ANIM_WALK = "Walk";
    private const string ANIM_ATTACK = "Attack";
    private const string ANIM_CAST = "Cast";
    private const string ANIM_DEATH = "Death";

    [Header("보스 보상 설정")]
    public GameObject portalPrefab;
    public Transform portalSpawnPoint;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        currentHealth = maxHealth;
        startPosition = transform.position;

        if (player == null)
        {
            GameObject pGO = GameObject.FindGameObjectWithTag("Player");
            if (pGO != null) player = pGO.transform;
        }
    }

    private void OnEnable()
    {
        if (rb != null)
        {
            StopAllCoroutines(); 
            isBgmStarted = false; // 활성화될 때 BGM 플래그 리셋
            StartCoroutine(BossAIBackgroundLoop());
        }
    }

    IEnumerator BossAIBackgroundLoop()
    {
        yield return new WaitForSeconds(1.0f);

        while (currentState != BossState.Death)
        {
            if(player == null || isHandlingPattern)
            {
                yield return null;
                continue;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // 플레이어가 추적 제한 범위를 완전히 벗어났을 때 (보스가 전투를 포기하고 돌아감)
            if (distanceToPlayer > chaseRange)
            {
                if (BossHPController.Instance != null)
                {
                    BossHPController.Instance.HideBossHP(); 
                }

                // ★ [보스 전용 BGM을 끄거나 필드 BGM으로 전환하고 싶을 때]
                // 다시 플레이어가 오기 전까지 보스 음악을 중단시킵니다.
                if (isBgmStarted)
                {
                    if (SoundManager.Instance != null) SoundManager.Instance.StopBGM();
                    isBgmStarted = false;
                }

                currentState = BossState.Idle;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                anim.Play(ANIM_IDLE);

                float distanceToStart = Vector2.Distance(transform.position, startPosition);
                if (distanceToStart > 1f)
                {
                    LookAtTarget(startPosition);
                    float dir = startPosition.x > transform.position.x ? 1f : -1f;
                    rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
                    anim.Play(ANIM_WALK);
                }
                yield return null;
                continue;
            }
            else
            {
                if (BossHPController.Instance != null)
                {
                    BossHPController.Instance.ShowBossHP(bossName, currentHealth, maxHealth);
                }

                // ★ [보스전 BGM 재생 부하 분산] 플레이어가 범위 안에 들어오는 즉시 BGM 연출 시작!
                if (!isBgmStarted && SoundManager.Instance != null && !string.IsNullOrEmpty(bossBgmName))
                {
                    SoundManager.Instance.PlayBGM(bossBgmName, 0.6f); // 0.6 볼륨으로 웅장하게 재생
                    isBgmStarted = true;
                }
            }

            currentState = BossState.Idle;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            anim.Play(ANIM_IDLE);
            LookAtTarget(player.position);

            yield return new WaitForSeconds(Random.Range(1.0f, 1.5f)); 

            float randomPattern = Random.Range(0f, 100f);

            if (randomPattern < 50f)
            {
                while (Vector2.Distance(transform.position, player.position) > attackRange)
                {
                    if (Vector2.Distance(transform.position, player.position) > chaseRange) break;

                    currentState = BossState.Chase;
                    LookAtTarget(player.position);
                    float dir = player.position.x > transform.position.x ? 1f : -1f;
                    rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
                    anim.Play(ANIM_WALK);
                    yield return null;
                }

                if (Vector2.Distance(transform.position, player.position) <= attackRange)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    yield return StartCoroutine(Pattern_CloseAttack());
                }
            }
            else
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                yield return StartCoroutine(Pattern_TargetMagic());
            }

            yield return null;
        }
    }

    IEnumerator Pattern_CloseAttack()
    {
        isHandlingPattern = true;
        currentState = BossState.Attack;

        LookAtTarget(player.position);
        anim.Play(ANIM_ATTACK);

        // ★ [근접 공격 효과음 재생] 모션을 크게 취하며 휘두르는 사운드 출력
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(closeAttackSoundName))
        {
            SoundManager.Instance.PlaySFX(closeAttackSoundName);
        }

        yield return new WaitForSeconds(0.4f);

        Vector2 attackPos = transform.position + (isFacingRight ? transform.right : -transform.right) * 1.2f;
        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPos, attackRange, playerLayer);
        if (hitPlayer != null)
        {
            PlayerController pScript = hitPlayer.GetComponent<PlayerController>();
            if (pScript != null) pScript.TakeDamage(attackDamage, transform.position);
        }

        yield return new WaitForSeconds(0.6f);
        isHandlingPattern = false;
    }

    IEnumerator Pattern_TargetMagic()
    {
        isHandlingPattern = true;
        LookAtTarget(player.position);

        currentState = BossState.Cast;
        anim.Play(ANIM_IDLE); 
        anim.Play(ANIM_CAST);

        // ★ [마법 캐스팅/기 모으기 효과음 재생] 보스가 하단에서 마법진을 생성하는 충전음 출력
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(magicCastSoundName))
        {
            SoundManager.Instance.PlaySFX(magicCastSoundName);
        }

        yield return new WaitForSeconds(0.5f);

        Vector3 targetPosition = player.position;

        if (magicEffectPrefab != null)
        {
            Instantiate(magicEffectPrefab, targetPosition, Quaternion.identity);
        }

        // 이펙트 생성 후 '쾅!' 터지기 전까지 플레이어가 피할 수 있는 유예 시간 (1.0초)
        yield return new WaitForSeconds(1.0f);

        // ★ [마법 폭발 효과음 재생] 유예 시간이 지나고 바닥이 폭발하는 타이밍에 콰쾅!
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(magicExplodeSoundName))
        {
            SoundManager.Instance.PlaySFX(magicExplodeSoundName);
        }

        Collider2D hitPlayer = Physics2D.OverlapCircle(targetPosition, magicDamageRadius, playerLayer);
        if (hitPlayer != null)
        {
            PlayerController pScript = hitPlayer.GetComponent<PlayerController>();
            if (pScript != null)
            {
                pScript.TakeDamage(attackDamage, targetPosition);
            }
        }

        yield return new WaitForSeconds(0.5f);
        isHandlingPattern = false;
    }

    public void TakeDamage(int damage, Vector2 attackerPos)
    {
        if (currentState == BossState.Death) return;

        currentHealth -= damage;
        Debug.Log($"🤖 [{bossName}] 피격! 남은 체력: {currentHealth}/{maxHealth}");

        if (BossHPController.Instance != null)
        {
            BossHPController.Instance.UpdateBossHP(currentHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        currentState = BossState.Death;
        StopAllCoroutines(); 

        rb.linearVelocity = Vector2.zero;

        // ★ [보스가 죽었으므로 배경음 정지] 
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
        }

        anim.Play(ANIM_DEATH);
        Debug.Log($"🎉 보스 [{bossName}] 처치 완료");

        if (BossHPController.Instance != null) BossHPController.Instance.HideBossHP();

        if (portalPrefab != null)
        {
            Vector3 spawnPosition = portalSpawnPoint != null ? portalSpawnPoint.position : transform.position;
            Instantiate(portalPrefab, spawnPosition, Quaternion.identity);
        }

        Destroy(gameObject, 1.0f);
        this.enabled = false;
    }

    void LookAtTarget(Vector2 targetPos)
    {
        if (targetPos.x > transform.position.x && !isFacingRight) Flip();
        else if (targetPos.x < transform.position.x && isFacingRight) Flip();
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;

        if (visualChild != null)
        {
            Vector3 scale = visualChild.localScale;
            scale.x *= -1;
            visualChild.localScale = scale;
        }
        else
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.red;
        Vector2 attackPos = transform.position + (isFacingRight ? transform.right : -transform.right) * 1.2f;
        Gizmos.DrawWireSphere(attackPos, attackRange);
    }
}