using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // ★ 씬 전환 네임스페이스 추가

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
    private bool isDead = false;  // 중복 사망 판정을 방지하기 위한 플래그

    [Header("대쉬 설정")]
    public float dashSpeed = 40f;       
    public float dashTime = 0.1f;       
    public float dashCooldown = 0.5f;    
    private bool canDash = true;         
    private bool isDashing = false;      
    public bool isInvincible = false;    
    public GameObject dashEffectPrefab; 

    [Header("플레이어 공통 사운드 이름")]
    public string jumpSoundName = "PlayerJump";   // 유니티 사운드매니저에 등록할 점프음 이름
    public string dashSoundName = "PlayerDash";   // 유니티 사운드매니저에 등록할 대시음 이름
    public string hurtSoundName = "PlayerHurt";   // 유니티 사운드매니저에 등록할 피격음 이름
    public string dieSoundName = "PlayerDie";     // 유니티 사운드매니저에 등록할 사망음 이름

    [Header("씬 전환 설정")]
    public string lobbySceneName = "LobbyScene";  // 빌드 세팅에 등록된 로비 씬 이름 (에디터에서 수정 가능)

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
    private const string ANIM_DIE = "Player_Die";

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
        isDead = false;
    }

    void Update()
    {
        // 사망 상태일 경우 모든 키 입력 및 업데이트 루틴 완전 차단
        if (isDead) return;

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
            
            // 점프 효과음 재생
            if (SoundManager.Instance != null && !string.IsNullOrEmpty(jumpSoundName))
            {
                SoundManager.Instance.PlaySFX(jumpSoundName);
            }
        }

        // 7. 실시간 애니메이션 상태 결정 연산
        UpdateAnimationState();
    }

    void FixedUpdate()
    {
        if (isDead) return; // 사망 시 물리 연산 정지
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
        if (newWeapon == null || isDead) return null;

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

        // [무기 고유 일반 공격 효과음 재생]
        if (SoundManager.Instance != null && currentWeapon != null && !string.IsNullOrEmpty(currentWeapon.attackSoundName))
        {
            SoundManager.Instance.PlaySFX(currentWeapon.attackSoundName);
        }

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
                enemyScript.TakeDamage(currentWeapon.attackDamage, transform.position); 
            }

            BossEnemy bossScript = enemy.GetComponent<BossEnemy>();
            if (bossScript != null)
            {
                bossScript.TakeDamage(currentWeapon.attackDamage, transform.position);
            }

            DestructibleObject destObj = enemy.GetComponent<DestructibleObject>();
            if (destObj != null)
            {
                destObj.TakeDamage(currentWeapon.attackDamage);
            }

            TreasureChest chest = enemy.GetComponent<TreasureChest>();
            if (chest != null)
            {
                chest.OpenChest(); 
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
        moveInput = Vector2.zero; 

        if (trailRenderer != null) trailRenderer.enabled = true;
        float dashDirection = isFacingRight ? 1f : -1f;

        // [대시 효과음 재생]
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(dashSoundName))
        {
            SoundManager.Instance.PlaySFX(dashSoundName);
        }

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

        int originalLayer = gameObject.layer; 
        gameObject.layer = LayerMask.NameToLayer("Dash"); 

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
        if (isDead || isDashing || isAttacking || isUsingSkill) return;

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

    public void TakeDamage(int damage, Vector2 attackerPosition)
    {
        if (isInvincible || currentHealth <= 0 || isDead) return; 

        currentHealth -= damage;
        Debug.Log($"🩸 플레이어 피격! 남은 체력: {currentHealth}/{maxHealth}");

        // [플레이어 피격 사운드 재생]
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(hurtSoundName))
        {
            SoundManager.Instance.PlaySFX(hurtSoundName);
        }

        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.2f, 10.0f);

        if (HPBarController.Instance != null)
        {
            HPBarController.Instance.UpdateHPBar(currentHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
            return; 
        }

        StartCoroutine(KnockbackCoroutine(attackerPosition));
        StartCoroutine(BecomeInvincibleCoroutine(1.0f)); 
    }

    IEnumerator KnockbackCoroutine(Vector2 attackerPosition)
    {
        isHurt = true;
        isAttacking = false;
        isUsingSkill = false;

        float pushDirection = transform.position.x > attackerPosition.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(pushDirection * 7f, 5f);

        yield return new WaitForSeconds(0.2f); 
        
        isHurt = false;
    }

    // =================================================================
    // ★ [개조된 사망 처리 루틴]
    // =================================================================
    void Die()
    {
        if (isDead) return;
        isDead = true;

        // 모든 활성화된 행동 제어 코루틴 강제 셧다운
        StopAllCoroutines(); 
        
        Debug.Log("💀 플레이어 사망... 로비 전환 시퀀스 시작!");
        StartCoroutine(DieSequenceCoroutine());
    }

    IEnumerator DieSequenceCoroutine()
    {
        // 1. 물리 제어권을 뺏고 멈춥니다.
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f; 
        GetComponent<Collider2D>().enabled = false; // 몬스터들에게 추가 유령 타격당하는 것 방지

        // 2. 만약 사운드매니저가 배경음(보스나 필드 BGM)을 틀고 있었다면 조용히 정지시킵니다.
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
            
            // 3. 플레이어 사망 효과음(절명하는 소리 등)을 재생합니다.
            if (!string.IsNullOrEmpty(dieSoundName))
            {
                SoundManager.Instance.PlaySFX(dieSoundName);
            }
        }

        // 4. 사망 애니메이션 상태 재생 (HasState 체크로 애니메이션이 없어도 에러가 나지 않습니다)
        ChangeAnimationState(ANIM_DIE);

        // 5. 웅장하게 쓰러져 있는 모습을 잠시 보여주기 위해 2초간 대기합니다. (원하는 만큼 수정 가능)
        yield return new WaitForSeconds(2.0f);

        // 6. 대기 시간이 끝나면 지정한 로비 씬으로 안전하게 전환합니다!
        SceneManager.LoadScene(lobbySceneName);
    }

    IEnumerator BecomeInvincibleCoroutine(float duration)
    {
        isInvincible = true; 

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr.color;
        Color flashColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0.4f);

        float elapsed = 0f;
        float flashInterval = 0.1f; 

        while (elapsed < duration)
        {
            sr.color = (sr.color == originalColor) ? flashColor : originalColor;

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        sr.color = originalColor;
        isInvincible = false; 
        Debug.Log("🛡️ 플레이어 무적 시간 종료!");
    }

    IEnumerator UseWeaponSkillCoroutine()
    {
        isUsingSkill = true;
        currentAnimationState = ""; 

        ChangeAnimationState(currentWeapon.skillAnimationName);

        // [무기 고유 스킬 사운드 재생]
        if (SoundManager.Instance != null && currentWeapon != null && !string.IsNullOrEmpty(currentWeapon.skillSoundName))
        {
            SoundManager.Instance.PlaySFX(currentWeapon.skillSoundName);
        }

        nextSkillTime = Time.time + currentWeapon.skillCooldown;

        if (SkillUIController.Instance != null)
        {
            SkillUIController.Instance.TriggerCooldown(currentWeapon.skillCooldown);
        }

        // [원거리 스킬 분기]
        if (currentWeapon.projectilePrefab != null)
        {
            if (attackPoint != null)
            {
                GameObject projGO = Instantiate(currentWeapon.projectilePrefab, attackPoint.position, Quaternion.identity);
                Projectile proj = projGO.GetComponent<Projectile>();

                if (proj != null)
                {
                    Vector2 shootDir = isFacingRight ? Vector2.right : Vector2.left;
                    proj.Setup(shootDir, 15f, currentWeapon.skillDamage, enemyLayers, currentWeapon.hitEffectPrefab);
                    
                    if (CameraShake.Instance != null)
                    {
                        CameraShake.Instance.Shake(currentWeapon.shakeDuration, currentWeapon.shakeMagnitude);
                    }
                }
            }
        }
        // [근접 스킬 분기]
        else
        {
            if (currentWeapon.skillEffectPrefab != null && attackPoint != null)
            {
                GameObject effect = Instantiate(currentWeapon.skillEffectPrefab, attackPoint.position, attackPoint.rotation);
                Vector3 effectScale = effect.transform.localScale;
                
                effectScale.x = (isFacingRight ? Mathf.Abs(effectScale.x) : -Mathf.Abs(effectScale.x));
                effect.transform.localScale = effectScale;
            }

            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.Shake(currentWeapon.shakeDuration, currentWeapon.shakeMagnitude);
            }

            Vector2 attackPosition = new Vector2(attackPoint.position.x, attackPoint.position.y);
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPosition, currentWeapon.skillRange, enemyLayers);

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
                    enemyScript.TakeDamage(currentWeapon.skillDamage, transform.position);
                }
                
                Debug.Log($"💥 [스킬] {enemy.name}에게 [{currentWeapon.skillName}] 발동! 강력한 대미지: {currentWeapon.skillDamage}");
            }
        }

        yield return new WaitForSeconds(0.4f);
        isUsingSkill = false;
    }
}