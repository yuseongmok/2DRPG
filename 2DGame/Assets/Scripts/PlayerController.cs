using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("이동 및 점프 설정")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;

    [Header("바닥 체크 설정")]
    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public LayerMask whatIsGround;

    [Header("공격 설정 (기본 공격)")]
    public Transform attackPoint;    
    public float attackRange = 0.5f; 
    public LayerMask enemyLayers;    
    public int attackDamage = 10;     
    public float attackRate = 2.5f;    // 초당 공격 횟수 (연사 속도 조절: 숫자가 클수록 꾹 눌렀을 때 더 빠르게 연사합니다)
    private float nextAttackTime = 0f;

    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 moveInput;
    private bool isFacingRight = true;
    private bool isGrounded;

    [Header("이펙트 설정")]
    public GameObject attackEffectPrefab;

    // --- 애니메이션 상태 관리를 위한 변수 ---
    private string currentAnimationState;
    private bool isAttacking = false;

    private const string ANIM_IDLE = "Player_Idle";
    private const string ANIM_WALK = "Player_Run";
    private const string ANIM_JUMP = "Player_Jump";
    private const string ANIM_ATTACK = "Player_Attack";

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // --- 0. 실시간 공격 종료 감지 ---
        if (isAttacking)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            // 공격 애니메이션이 90% 이상 진행되었을 때 공격 상태를 해제하여 다음 행동(혹은 연속 공격)이 가능하게 합니다.
            if (stateInfo.IsName(ANIM_ATTACK) && stateInfo.normalizedTime >= 0.9f)
            {
                isAttacking = false; 
            }
        }

        // 1. 이동 입력 (오직 좌우 화살표 키만 인식)
        moveInput.x = 0f;
        if (Input.GetKey(KeyCode.RightArrow)) moveInput.x = 1f;
        else if (Input.GetKey(KeyCode.LeftArrow)) moveInput.x = -1f;
        
        // 2. 방향 뒤집기 (공격 도중에는 고개 돌리기 금지하여 묵직함 유지)
        if (!isAttacking)
        {
            if (moveInput.x > 0 && !isFacingRight) Flip();
            else if (moveInput.x < 0 && isFacingRight) Flip();
        }

        // 3. 바닥 체크
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, whatIsGround);

        // 4. 점프 입력 (스페이스바)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // 쿨타임이 지났고, 현재 공격 판정이 끝난 타이밍이라면
        if (Time.time >= nextAttackTime && !isAttacking)
        {
            // GetKeyDown 대신 GetKey를 사용하여 키를 '꾹 누르고 있는 상태'를 감지합니다.
            if (Input.GetKey(KeyCode.A))
            {
                Attack();
                // 다음 공격이 나갈 쿨타임 계산
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }

        // 6. 실시간 애니메이션 상태 결정 연산
        UpdateAnimationState();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

    void Attack()
    {
        isAttacking = true;
        
        // 중요: 연속 공격 시 애니메이션이 굳지 않고 매번 첫 프레임부터 새로 나가도록 강제 리셋
        currentAnimationState = ""; 
        ChangeAnimationState(ANIM_ATTACK); 

        if (attackEffectPrefab != null && attackPoint != null)
        {
        // AttackPoint의 위치와 회전값 그대로 이펙트를 찍어냅니다.
        GameObject effect = Instantiate(attackEffectPrefab, attackPoint.position, attackPoint.rotation);
        
        // 캐릭터가 왼쪽을 보고 있다면 이펙트도 왼쪽을 보게 뒤집어줍니다.
        Vector3 effectScale = effect.transform.localScale;
           if (!isFacingRight)
           {
            effectScale.x *= -1;
            effect.transform.localScale = effectScale;
           }
        }


        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log(enemy.name + "을(를) 공격했습니다! 대미지: " + attackDamage);
        }
    }

    void UpdateAnimationState()
    {
        if (isAttacking) return;

        if (!isGrounded)
        {
            ChangeAnimationState(ANIM_JUMP); 
        }
        else
        {
            if (moveInput.x != 0) ChangeAnimationState(ANIM_WALK); 
            else ChangeAnimationState(ANIM_IDLE); 
        }
    }

    void ChangeAnimationState(string newState)
    {
        if (currentAnimationState == newState) return;

        anim.Play(newState, 0, 0f); 
        currentAnimationState = newState;
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }

        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}