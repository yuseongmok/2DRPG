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

    [Header("플레이어 체력 설정")]
    public int maxHealth = 100;
    private int currentHealth;
    private bool isHurt = false; // 피격 시 넉백 동안 조작을 막기 위한 플래그

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

        if (SkillUIController.Instance != null)
        {
            SkillUIController.Instance.UpdateSkillIcon(currentWeapon);
        }

        if (HPBarController.Instance != null)
        {
            HPBarController.Instance.SetupMaxHP(maxHealth);
        }

        currentHealth = maxHealth;
    }

    void Update()
    {
        // 대시 즉시 캔슬 규칙
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            if (isHurt) return;
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
        if (isHurt) return;
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

        if (SkillUIController.Instance != null)
        {
            SkillUIController.Instance.UpdateSkillIcon(currentWeapon);
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

            Enemy enemyScript = enemy.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                // 🎯 평타 대미지(attackDamage)를 적에게 전달합니다!
                enemyScript.TakeDamage(currentWeapon.attackDamage, transform.position); 
            }

            DestructibleObject destObj = enemy.GetComponent<DestructibleObject>();
            if (destObj != null)
            {
                destObj.TakeDamage(currentWeapon.attackDamage);
            }

            TreasureChest chest = enemy.GetComponent<TreasureChest>();
            if (chest != null)
            {
                chest.OpenChest(); // 상자 전용 열기 함수 호출!
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
        moveInput = Vector2.zero; // 대시 중 이동 입력 방해 차단

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


        int originalLayer = gameObject.layer; // 원래 레이어(Player)를 기억
        gameObject.layer = LayerMask.NameToLayer("Dash"); // 대시 레이어로 전환 (적만 통과 가능)

        ChangeAnimationState(ANIM_DASH);

        yield return new WaitForSeconds(dashTime);


        gameObject.layer = originalLayer;

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

    public void TakeDamage(int damage, Vector2 attackerPosition)
    {
        // 1. 이미 무적 상태이거나 죽었다면 대미지 연산을 완전히 무시합니다.
        if (isInvincible || currentHealth <= 0) return; 

        currentHealth -= damage;
        Debug.Log($"🩸 플레이어 피격! 남은 체력: {currentHealth}/{maxHealth}");

        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.2f, 0.3f);

        if (HPBarController.Instance != null)
        {
            HPBarController.Instance.UpdateHPBar(currentHealth);
        }

        // 사망 처리
        if (currentHealth <= 0)
        {
            Die();
            return; // 죽었다면 아래 무적 루틴을 탈 필요가 없으므로 종료
        }

        // 2. 넉백과 동시에 '무적 및 깜빡임 코루틴'을 실행합니다.
        StartCoroutine(KnockbackCoroutine(attackerPosition));
        StartCoroutine(BecomeInvincibleCoroutine(1.0f)); // 1.0초 동안 무적 시간 부여 (원하는 대로 조절 가능)
    }

    IEnumerator KnockbackCoroutine(Vector2 attackerPosition)
    {
        isHurt = true;
        isAttacking = false;
        isUsingSkill = false;

        // 적이 있는 방향의 반대 방향 계산
        float pushDirection = transform.position.x > attackerPosition.x ? 1f : -1f;
        
        // 순간적으로 위+뒤쪽으로 튕겨 나가게 힘을 줍니다 (수치는 원하는 대로 조절 가능)
        rb.linearVelocity = new Vector2(pushDirection * 7f, 5f);

        // 0.2초 동안 아파하며 조작 불가
        yield return new WaitForSeconds(0.2f); 
        
        isHurt = false;
    }

    void Die()
    {
        Debug.Log("💀 플레이어 사망... 게임 오버!");
        // TODO: 여기에 게임 오버 UI 띄우기 등의 연출을 추가하시면 됩니다.
        gameObject.SetActive(false); 
    }

    IEnumerator BecomeInvincibleCoroutine(float duration)
    {
        isInvincible = true; // 무적 상태 ON

        // 깜빡임 연출을 위해 플레이어의 SpriteRenderer를 가져옵니다.
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr.color;
        
        // 반투명하게 반짝일 타겟 색상 (알파값 0.4 정도로 세팅)
        Color flashColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0.4f);

        float elapsed = 0f;
        float flashInterval = 0.1f; // 깜빡이는 속도 주기 (0.1초 마다 투명도 전환)

        // 지정된 무적 시간 동안 무한 루프를 돌며 깜빡입니다.
        while (elapsed < duration)
        {
            // 현재 색상이 원래 색상이면 반투명하게, 반투명하면 원래 색상으로 스왑
            sr.color = (sr.color == originalColor) ? flashColor : originalColor;

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        // 무적 시간이 끝났으므로 색상을 완벽하게 원래대로 되돌리고 무적 해제
        sr.color = originalColor;
        isInvincible = false; // 무적 상태 OFF
        Debug.Log("🛡️ 플레이어 무적 시간 종료!");
    }



    IEnumerator UseWeaponSkillCoroutine()
    {
        isUsingSkill = true;
        currentAnimationState = ""; // 애니메이션 강제 리프레시를 위해 초기화

        // 1. 스킬 애니메이션 재생
        ChangeAnimationState(currentWeapon.skillAnimationName);

        // 2. 다음 스킬 사용 가능 시간 타이머 계산
        nextSkillTime = Time.time + currentWeapon.skillCooldown;

        // 3. UI 쿨타임 컴포넌트 호출 (아이콘 위 어두운 막 및 숫자 표기 시작)
        if (SkillUIController.Instance != null)
        {
            SkillUIController.Instance.TriggerCooldown(currentWeapon.skillCooldown);
        }

        // =================================================================
        // [원거리 스킬 분기] 무기 데이터에 투사체 프리팹이 등록되어 있는 경우
        // =================================================================
        if (currentWeapon.projectilePrefab != null)
        {
            if (attackPoint != null)
            {
                // 1) 투사체 오브젝트 동적 생성
                GameObject projGO = Instantiate(currentWeapon.projectilePrefab, attackPoint.position, Quaternion.identity);
                Projectile proj = projGO.GetComponent<Projectile>();

                if (proj != null)
                {
                    // 2) 플레이어가 바라보는 2D 방향 축 계산
                    Vector2 shootDir = isFacingRight ? Vector2.right : Vector2.left;
                    
                    // 3) 투사체 스크립트에 속도(15f), 대미지, 타겟 레이어, 타격 이펙트 데이터 주입
                    proj.Setup(shootDir, 15f, currentWeapon.skillDamage, enemyLayers, currentWeapon.hitEffectPrefab);
                    
                    // 4) 발사 반동 연출을 위한 카메라 진동 발생
                    if (CameraShake.Instance != null)
                    {
                        CameraShake.Instance.Shake(currentWeapon.shakeDuration, currentWeapon.shakeMagnitude);
                    }
                }
            }
        }
        // =================================================================
        // [근접 스킬 분기] 투사체가 없을 경우 (기존 광역 베기 모드)
        // =================================================================
        else
        {
            // 1) 스킬 전용 대형 근접 검기/이펙트 생성 및 방향 조절
            if (currentWeapon.skillEffectPrefab != null && attackPoint != null)
            {
                GameObject effect = Instantiate(currentWeapon.skillEffectPrefab, attackPoint.position, attackPoint.rotation);
                Vector3 effectScale = effect.transform.localScale;
                
                // 캐릭터가 바라보는 방향에 맞춰 이펙트 $X$축 좌우 반전 제어
                effectScale.x = (isFacingRight ? Mathf.Abs(effectScale.x) : -Mathf.Abs(effectScale.x));
                effect.transform.localScale = effectScale;
            }

            // 2) 스킬 시전 순간 묵직한 타격감을 위한 카메라 진동 발생
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.Shake(currentWeapon.shakeDuration, currentWeapon.shakeMagnitude);
            }

            // 3) OverlapCircle을 활용한 무기 고유 스킬 범위(skillRange) 내의 광역 타격 연산
            Vector2 attackPosition = new Vector2(attackPoint.position.x, attackPoint.position.y);
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
                
           attackPosition, currentWeapon.skillRange, enemyLayers);

            foreach (Collider2D enemy in hitEnemies)
            {
                // 적 피격 위치에 피격(Hit) 이펙트 생성
                if (currentWeapon.hitEffectPrefab != null)
                {
                    Vector2 hitPoint = enemy.ClosestPoint(attackPoint.position);
                    Instantiate(currentWeapon.hitEffectPrefab, hitPoint, Quaternion.identity);
                }

                Enemy enemyScript = enemy.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    // 🎯 스킬 대미지(skillDamage)를 적에게 전달합니다!
                    enemyScript.TakeDamage(currentWeapon.skillDamage, transform.position);
                }
                
                // 콘솔 로그 출력 (대미지 연산 확인용)
                Debug.Log($"💥 [스킬] {enemy.name}에게 [{currentWeapon.skillName}] 발동! 강력한 대미지: {currentWeapon.skillDamage}");
            }
        }

        // 4. 스킬 시전 후 액션이 고정되는 채널링 시간 (기존 0.4초 유지)
        yield return new WaitForSeconds(0.4f);

        // 5. 안전하게 스킬 플래그를 꺼서 이동 제한 및 애니메이션 덮어쓰기 제한 해제
        isUsingSkill = false;
    }
}