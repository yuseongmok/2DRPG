using System.Collections;
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

    [Header("공격 설정")]
    public Transform attackPoint;    
    public float attackRange = 0.5f; 
    public LayerMask enemyLayers;    
    private float nextAttackTime = 0f;

    [Header("대쉬 설정")]
    public float dashSpeed = 40f;       
    public float dashTime = 0.1f;        
    public float dashCooldown = 0.5f;    
    private bool canDash = true;         
    private bool isDashing = false;      
    public bool isInvincible = false;    
    public GameObject dashEffectPrefab; 

    [Header("무기 시스템 (루팅 및 스왑)")]
    public WeaponData defaultWeapon;   // 게임 시작할 때 들고 있을 기본 무기 데이터
    private WeaponData currentWeapon;  // 현재 들고 있는 무기 데이터 상자

    [Header("스킬 시스템")]
    private float nextSkillTime = 0f; // 다음 스킬 사용 가능 시간 타이머
    private bool isUsingSkill = false; // 현재 스킬 시전 중인가?

    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 moveInput; 
    private bool isFacingRight = true;
    private bool isGrounded;

    private string currentAnimationState;
    private bool isAttacking = false;
    private TrailRenderer trailRenderer; 

    private const string ANIM_IDLE = "Player_Idle";
    private const string ANIM_WALK = "Player_Run";
    private const string ANIM_JUMP = "Player_Jump";
    private const string ANIM_DASH = "Player_Dash"; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        trailRenderer = GetComponent<TrailRenderer>(); 
        if (trailRenderer != null) trailRenderer.enabled = false;

        if (defaultWeapon != null)
        {
            currentWeapon = defaultWeapon;
            Debug.Log("기본 무기 장착 완료: " + currentWeapon.weaponName);
        }
    }

    void Update()
    {
        // 대시 즉시 캔슬 규칙
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            isAttacking = false;
            isUsingSkill = false; //스킬 캔슬
            StartCoroutine(DashCoroutine());
            return; 
        }

        if (isDashing) return;

        // 0. 실시간 공격 종료 감지
        if (isAttacking && currentWeapon != null)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(currentWeapon.attackAnimationStateName) && stateInfo.normalizedTime >= 0.9f)
            {
                isAttacking = false; 
            }
        }

        moveInput.x = 0f;
        if (Input.GetKey(KeyCode.RightArrow)) moveInput.x = 1f;
        else if (Input.GetKey(KeyCode.LeftArrow)) moveInput.x = -1f;

        // 방향 전환 제어 (공격 중이 아닐 때만 방향키에 따라 회전)
        if (!isAttacking && !isUsingSkill)
        {
            if (moveInput.x > 0 && !isFacingRight) Flip();
            else if (moveInput.x < 0 && isFacingRight) Flip();
        }

        if (Input.GetKey(KeyCode.LeftControl) || Input.GetMouseButton(0) || Input.GetKey(KeyCode.A)) 
        {
            if (!isAttacking && Time.time >= nextAttackTime) 
            {
               Attack();
               
               // 공격 속도에 따른 다음 공격 가능 시간 계산
               if (currentWeapon != null)
               {
                   nextAttackTime = Time.time + 1f / currentWeapon.attackRate;
               }
            }
        }
        //S키로 무기의 고유 스킬 발동
        if (Input.GetKeyDown(KeyCode.S) && currentWeapon != null)
        {
            if (!isUsingSkill && !isAttacking && Time.time >= nextSkillTime)
            {
                StartCoroutine(UseWeaponSkillCoroutine());
            }
        }

        // 3. 바닥 체크
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, whatIsGround);

        // 4. 점프 입력 처리 (공격 중에는 차단)
        if (Input.GetButtonDown("Jump") && isGrounded && !isAttacking)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // 7. 실시간 애니메이션 상태 결정 연산
        UpdateAnimationState();

    }

    void FixedUpdate()
    {
        if (isDashing) return;

        if ((isAttacking || isUsingSkill) && isGrounded)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else
        {

            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        }
    }

    public WeaponData SwapWeapon(WeaponData newWeapon)
    {
        if (newWeapon == null) return null;

        WeaponData oldWeapon = currentWeapon; 
        currentWeapon = newWeapon;            

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && currentWeapon.weaponSprite != null)
        {
            sr.sprite = currentWeapon.weaponSprite;
        }
        
        Debug.Log("★ 무기 교체 완료! 현재 무기: " + currentWeapon.weaponName);
        return oldWeapon;                     
    }

    void Attack()
    {
        isAttacking = true;
        currentAnimationState = ""; 
        
        ChangeAnimationState(currentWeapon.attackAnimationStateName); 

        float finalRange = (currentWeapon != null) ? currentWeapon.attackRange : attackRange;

        if (currentWeapon.attackEffectPrefab != null && attackPoint != null)
        {
            GameObject effect = Instantiate(currentWeapon.attackEffectPrefab, attackPoint.position, attackPoint.rotation);
            Vector3 effectScale = effect.transform.localScale;
            
            effectScale.x = (isFacingRight ? Mathf.Abs(effectScale.x) : -Mathf.Abs(effectScale.x));
            effect.transform.localScale = effectScale;
        }

        Vector2 attackPosition = new Vector2(attackPoint.position.x, attackPoint.position.y);
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPosition, finalRange, enemyLayers);
        
        foreach (Collider2D enemy in hitEnemies)
        {
            if (currentWeapon.hitEffectPrefab != null)
            {
                Vector2 hitPoint = enemy.ClosestPoint(attackPoint.position);
                Instantiate(currentWeapon.hitEffectPrefab, hitPoint, Quaternion.identity);
            }
            Debug.Log(enemy.name + "에게 " + currentWeapon.weaponName + "(으)로 공격! 대미지: " + currentWeapon.attackDamage);
        }
    }

    IEnumerator DashCoroutine()
    {
        canDash = false;
        isDashing = true;
        isInvincible = true; 
        isAttacking = false; 
        isUsingSkill = false;

        if (trailRenderer != null) trailRenderer.enabled = true;
        float dashDirection = isFacingRight ? 1f : -1f;

        if (dashEffectPrefab != null)
        {
            GameObject dashFX = Instantiate(dashEffectPrefab, transform.position, Quaternion.identity);
            Vector3 fxScale = dashFX.transform.localScale;
            if (!isFacingRight)
            {
                fxScale.x *= -1;
                dashFX.transform.localScale = fxScale;
            }
        }

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);

        ChangeAnimationState(ANIM_DASH);

        yield return new WaitForSeconds(dashTime);

        rb.gravityScale = originalGravity;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); 
        isDashing = false;
        isInvincible = false; 

        isAttacking = false;
        isUsingSkill = false;

        if (trailRenderer != null) trailRenderer.enabled = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    void UpdateAnimationState()
    {
        if (isDashing || isAttacking || isUsingSkill) return;

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

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
        if (attackPoint != null && currentWeapon != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, currentWeapon.attackRange);

            Gizmos.color = new Color(1f, 0.5f, 0f); // 주황색
            Gizmos.DrawWireSphere(attackPoint.position, currentWeapon.skillRange);
        }
    }

    IEnumerator UseWeaponSkillCoroutine()
    {
        isUsingSkill = true;
        currentAnimationState = ""; // 애니메이션 강제 리프레시

        // 1. 스킬 애니메이션 재생
        ChangeAnimationState(currentWeapon.skillAnimationName);

        // 2. 쿨타임 세팅
        nextSkillTime = Time.time + currentWeapon.skillCooldown;

        // 3. 스킬 전용 대형 이펙트 생성 및 방향 조절
        if (currentWeapon.skillEffectPrefab != null && attackPoint != null)
        {
            GameObject effect = Instantiate(currentWeapon.skillEffectPrefab, attackPoint.position, attackPoint.rotation);
            Vector3 effectScale = effect.transform.localScale;
            effectScale.x = (isFacingRight ? Mathf.Abs(effectScale.x) : -Mathf.Abs(effectScale.x));
            effect.transform.localScale = effectScale;
        }

        // 4. 넓게 베기 링 연산
        Vector2 attackPosition = new Vector2(attackPoint.position.x, attackPoint.position.y);
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPosition, currentWeapon.skillRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            if (currentWeapon.hitEffectPrefab != null)
            {
                Vector2 hitPoint = enemy.ClosestPoint(attackPoint.position);
                Instantiate(currentWeapon.hitEffectPrefab, hitPoint, Quaternion.identity);
            }
            Debug.Log($"💥 [스킬] {enemy.name}에게 [{currentWeapon.skillName}] 발동! 강력한 대미지: {currentWeapon.skillDamage}");
        }

        // 현재는 넉넉하게 0.4초 뒤에 움직임이 풀리도록 세팅했습니다. (원하는 시간으로 조절 가능)
        yield return new WaitForSeconds(0.4f);

        // 시간이 지나면 안전하게 스킬 상태를 해제하여 다시 움직일 수 있게 만듭니다!
        isUsingSkill = false;
    }
}