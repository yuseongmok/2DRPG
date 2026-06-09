using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("적 능력치 설정")]
    public string enemyName = "슬라임 기사";
    public int maxHealth = 100;
    private int currentHealth;

    [Header("이동 및 AI 설정")]
    public float moveSpeed = 3f;         
    public float detectRange = 8f;       
    public float attackRange = 1.8f;     // 플레이어를 공격하기 위해 접근하는 범위
    private Transform playerTransform;   
    private bool isChasing = false;      

    [Header("적 공격 설정")]
    public int attackDamage = 20;        // 톳통일된 단일 공격 대미지
    public float attackCooldown = 1.5f;  // 공격 후 다음 공격까지의 쿨타임
    public float damageDelay = 0.35f;    // ★ 애니메이션 시작 후 데미지가 들어갈 때까지의 시간 (여기서 타이밍 조절!)
    private float nextAttackTime = 0f;

    [Header("피격 효과 프리팹 (선택)")]
    public GameObject deathEffectPrefab;

    public GameObject attackEffectPrefab; // 휘두르는 검기 등의 이펙트 프리팹
    public Transform attackPoint;         // 이펙트가 생성될 중심 위치 (미지정 시 적 본인 위치)

    // =================================================================
    // 애니메이션 상태 이름 상수 정의 (하나의 공격 모션만 사용)
    // =================================================================
    private const string ANIM_IDLE = "Enemy_Idle";
    private const string ANIM_RUN = "Enemy_Run";
    private const string ANIM_HIT = "Enemy_Hit";
    private const string ANIM_DIE = "Enemy_Die";
    private const string ANIM_ATTACK = "Enemy_Attack1"; // 가져오신 3개 중 가장 맘에 드는 모션 이름으로 맞추세요!

    private Rigidbody2D rb;
    private Animator anim; 
    private string currentAnimationState; 
    
    private bool isFacingRight = false;  
    private bool isDead = false;          
    private bool isHurtState = false;     
    private bool isAttacking = false;     

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>(); 
    }

    void Start()
    {
        currentHealth = maxHealth;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (isDead || playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // 1. 공격 범위 내에 있고 쿨타임이 지났다면 단발 공격 시작
        if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime && !isAttacking && !isHurtState)
        {
            StartCoroutine(AttackCoroutine());
        }
        // 2. 공격 중이 아닐 때만 추적 활성화
        else if (!isAttacking)
        {
            if (distanceToPlayer <= detectRange) isChasing = true;
            else isChasing = false;
        }

        if (!isHurtState && !isAttacking)
        {
            UpdateAnimationState();
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        if (isChasing && playerTransform != null && !isHurtState && !isAttacking)
        {
            float directionX = playerTransform.position.x - transform.position.x;

            if (directionX > 0)
            {
                rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);
                if (isFacingRight) Flip(); 
            }
            else if (directionX < 0)
            {
                rb.linearVelocity = new Vector2(-moveSpeed, rb.linearVelocity.y);
                if (!isFacingRight) Flip(); 
            }
        }
        else if (!isHurtState) 
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    // =================================================================
    // ★ [수정] 단발성 깔끔한 공격 루틴
    // =================================================================
    System.Collections.IEnumerator AttackCoroutine()
    {
        isAttacking = true;
        isChasing = false;
        rb.linearVelocity = Vector2.zero; // 공격 순간 정지

        currentAnimationState = ""; 
        ChangeAnimationState(ANIM_ATTACK);

        // ⏳ 인스펙터창의 damageDelay에 설정한 시간만큼 기다린 후 대미지를 줍니다.
        yield return new WaitForSeconds(damageDelay); 
        PerformAttackDamage();             

        // 공격 모션의 남은 후딜레이 처리 (적당히 휘두르고 마무리 자세 잡는 시간)
        yield return new WaitForSeconds(0.3f); 

        // 공격 완료 후 다음 쿨타임 세팅
        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    void PerformAttackDamage()
    {
        if (isDead || isHurtState || playerTransform == null) return;

        // -------------------------------------------------------------
        // 1. 공격 이펙트 생성 및 방향 제어
        // -------------------------------------------------------------
        if (attackEffectPrefab != null)
        {
            // attackPoint를 지정하지 않았다면 적의 중심(transform.position)에서 생성합니다.
            Vector3 spawnPos = attackPoint != null ? attackPoint.position : transform.position;
            Quaternion spawnRot = attackPoint != null ? attackPoint.rotation : Quaternion.identity;

            GameObject effect = Instantiate(attackEffectPrefab, spawnPos, spawnRot);
            
            // 적이 왼쪽/오른쪽 바라보는 것에 맞춰 이펙트도 좌우 반전 시켜줍니다.
            Vector3 effectScale = effect.transform.localScale;
            // 적의 원래 기본 방향(isFacingRight의 세팅)에 맞춰 X축 스케일을 부호 제어합니다.
            if (isFacingRight)
            {
                effectScale.x = -Mathf.Abs(effectScale.x); // 오른쪽에 있을 때 뒤집기 (기본 스프라이트 기준에 맞춰 조절 가능)
            }
            else
            {
                effectScale.x = Mathf.Abs(effectScale.x);
            }
            effect.transform.localScale = effectScale;
        }

        // -------------------------------------------------------------
        // 2. 실제 플레이어 타격 판정 (기존 로직 유지)
        // -------------------------------------------------------------
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        
        if (distanceToPlayer <= attackRange + 0.5f) 
        {
            PlayerController player = playerTransform.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(attackDamage, transform.position);
                Debug.Log($"⚔️ 적이 플레이어를 단일 공격! 대미지: {attackDamage}");
            }
        }
    }

    void UpdateAnimationState()
    {
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f) ChangeAnimationState(ANIM_RUN);
        else ChangeAnimationState(ANIM_IDLE);
    }

    void ChangeAnimationState(string newState)
    {
        if (anim == null) return;
        if (!anim.HasState(0, Animator.StringToHash(newState))) return; 
        if (currentAnimationState == newState) return;

        anim.Play(newState, 0, 0f);
        currentAnimationState = newState;
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 currentScale = transform.localScale;
        currentScale.x *= -1;
        transform.localScale = currentScale;
    }

    public void TakeDamage(int damage, Vector2 attackerPosition)
    {
        if (isDead) return; 

        currentHealth -= damage;
        Debug.Log($"{enemyName}이(가) {damage}의 대미지를 입음! 남은 체력: {currentHealth}");

        // 공격 도중 대미지를 받으면 공격을 캔슬하고 피격 상태로 전환
        if (isAttacking)
        {
            StopAllCoroutines();
            isAttacking = false;
        }

        isChasing = true;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(HitAnimationCoroutine(attackerPosition));
        }
    }

    System.Collections.IEnumerator HitAnimationCoroutine(Vector2 attackerPosition)
    {
        isHurtState = true;
        
        rb.linearVelocity = Vector2.zero; 
        float knockbackDirection = transform.position.x > attackerPosition.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(knockbackDirection * 6.5f, 0f); 

        ChangeAnimationState(ANIM_HIT);

        yield return new WaitForSeconds(0.2f);
        isHurtState = false;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        
        StopAllCoroutines();
        Debug.Log($"{enemyName} 사망!");

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f; 
        GetComponent<Collider2D>().enabled = false; 

        ChangeAnimationState(ANIM_DIE);

        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject, 1.5f);
    }

    // 몸빵(접촉 대미지) 로직 유지
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                if (player.isInvincible) return;

                int bumpDamage = Mathf.RoundToInt(attackDamage * 0.5f); // 몸빵은 원래 공격력의 절반
                player.TakeDamage(bumpDamage, transform.position);
                Debug.Log($"💥 {enemyName}의 몸빵에 부딪힘! 대미지: {bumpDamage}");
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                int bumpDamage = Mathf.RoundToInt(attackDamage * 0.5f);
                player.TakeDamage(bumpDamage, transform.position);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}