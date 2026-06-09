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

    [Header("컴포넌트 및 레이어")]
    public Transform player;
    public LayerMask playerLayer;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private bool isFacingRight = false;
    private bool isHandlingPattern = false;

    [Header("트랜스폼 설정")]
    public Transform visualChild;

    private const string ANIM_IDLE = "Idle";
    private const string ANIM_WALK = "Walk";
    private const string ANIM_ATTACK = "Attack";
    private const string ANIM_CAST = "Cast";
    private const string ANIM_DEATH = "Death";

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

        StartCoroutine(BossAIBackgroundLoop());
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

            //플레이어가 추적 제한 범위를 완전히 벗어났을 때 
            if (distanceToPlayer > chaseRange)
            {
                if (BossHPController.Instance != null)
                {
                    BossHPController.Instance.HideBossHP(); // 안전하게 계속 끄기
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
            }

            // 플레이어가 추적 범위 안에 들어와 있을 때의 행동 정의
            // 패턴과 패턴 사이에 잠시 플레이어를 쳐다보며 숨을 고르는 시간
            currentState = BossState.Idle;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            anim.Play(ANIM_IDLE);
            LookAtTarget(player.position);

            yield return new WaitForSeconds(Random.Range(1.0f, 1.5f)); // 공격 멈춤 딜레이 (지나치게 쉴 새 없이 공격하는 것 방지)

            // 무작위로 패턴 고르기 (50% 확률로 근접 추적 공격 또는 원거리 마법 폭발)
            float randomPattern = Random.Range(0f, 100f);

            if (randomPattern < 50f)
            {
                //근접 공격 패턴 선택
                // 플레이어가 평타 사거리보다 멀리 있다면 Walk 애니메이션으로 끝까지 쫓아간 뒤 공격합니다.
                while (Vector2.Distance(transform.position, player.position) > attackRange)
                {
                    // 만약 쫓아가다가 플레이어가 추적 범위를 탈출하면 추적 취소
                    if (Vector2.Distance(transform.position, player.position) > chaseRange) break;

                    currentState = BossState.Chase;
                    LookAtTarget(player.position);
                    float dir = player.position.x > transform.position.x ? 1f : -1f;
                    rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
                    anim.Play(ANIM_WALK);
                    yield return null;
                }

                // 사거리 안에 도달했다면 쾅!
                if (Vector2.Distance(transform.position, player.position) <= attackRange)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    yield return StartCoroutine(Pattern_CloseAttack());
                }
            }
            else
            {
                //마법 폭발 패턴 선택
                //플레이어가 가까이 있든 멀리 있든 상관없이 제자리에서 즉시 주문을 외워 타격합니다!
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

        // 1. Cast 애니메이션 재생 (보스가 마법을 준비하는 하나의 긴 모션으로 처리)
        currentState = BossState.Cast;
        anim.Play(ANIM_IDLE); // 상태를 확실히 리셋하기 위해 넣어주거나, 바로 아래 Cast 재생
        anim.Play(ANIM_CAST);

        // 보스가 기를 모으는 선딜레이 시간
        yield return new WaitForSeconds(0.5f);

        // 2. 캐스팅이 끝나자마자 플레이어의 현재 위치를 조준하고 이펙트 생성!
        Vector3 targetPosition = player.position;

        if (magicEffectPrefab != null)
        {
            Instantiate(magicEffectPrefab, targetPosition, Quaternion.identity);
        }

        // 3. 이펙트 생성 후 '쾅!' 터지기 전까지 플레이어가 피할 수 있는 유예 시간 (0.5초)
        yield return new WaitForSeconds(1.0f);

        // 4. 폭발 타이밍에 범위 내 플레이어 타격 판정
        Collider2D hitPlayer = Physics2D.OverlapCircle(targetPosition, magicDamageRadius, playerLayer);
        if (hitPlayer != null)
        {
            PlayerController pScript = hitPlayer.GetComponent<PlayerController>();
            if (pScript != null)
            {
                pScript.TakeDamage(attackDamage, targetPosition);
            }
        }


        // 5. 마법 시전이 완전히 끝나고 다시 움직이기 전까지의 후딜레이
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
        StopAllCoroutines(); // 기존 패턴 루프 코루틴 모두 정지

        rb.linearVelocity = Vector2.zero;

        // 죽음 애니메이션 재생
        anim.Play(ANIM_DEATH);
        Debug.Log($"🎉 보스 [{bossName}] 처치 완료!! 잠시 후 오브젝트가 삭제됩니다.");

        // 보스 체력 UI 숨기기
        if (BossHPController.Instance != null) BossHPController.Instance.HideBossHP();

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
            // 부모(전체 오브젝트)는 가만히 두고, 이미지가 그려진 자식의 X 스케일만 뒤집습니다.
            Vector3 scale = visualChild.localScale;
            scale.x *= -1;
            visualChild.localScale = scale;
        }
        else
        {
            // 만약 자식을 안 구비해뒀다면 기존 방식으로 예외 처리
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