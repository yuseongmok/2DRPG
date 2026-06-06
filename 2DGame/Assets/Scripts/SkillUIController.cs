using UnityEngine;
using UnityEngine.UI;

public class SkillUIController : MonoBehaviour
{
    public static SkillUIController Instance; // 어디서든 쉽게 접근 가능한 싱글톤

    [Header("UI 컴포넌트 연결")]
    public Image skillIconImage;       // 스킬 아이콘 이미지
    public Image cooldownDarkImage;    // 쿨타임용 반투명 Filled 이미지
    public Text cooldownText;          // 쿨타임 숫자 텍스트

    private float currentCooldown = 0f;
    private float maxCooldown = 0f;
    private bool isCooldown = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 시작할 때는 쿨타임 UI를 가려둡니다.
        if (cooldownDarkImage != null) cooldownDarkImage.fillAmount = 0f;
        if (cooldownText != null) cooldownText.text = "";
    }

    void Update()
    {
        if (!isCooldown) return;

        currentCooldown -= Time.deltaTime;

        if (currentCooldown <= 0f)
        {
            // 쿨타임 종료
            isCooldown = false;
            currentCooldown = 0f;
            if (cooldownDarkImage != null) cooldownDarkImage.fillAmount = 0f;
            if (cooldownText != null) cooldownText.text = "";
        }
        else
        {
            // 쿨타임 진행 중 시각화 연산
            if (cooldownDarkImage != null && maxCooldown > 0f)
            {
                // 비율($0 \sim 1$)에 맞게 다크 이미지를 채워줍니다.
                cooldownDarkImage.fillAmount = currentCooldown / maxCooldown;
            }

            if (cooldownText != null)
            {
                // 소수점 첫째 자리까지만 심플하게 노출 (예: 2.5)
                cooldownText.text = currentCooldown.ToString("F1");
            }
        }
    }

    // ★ 무기가 바뀌거나 게임이 시작될 때 아이콘을 갱신해 주는 함수
    public void UpdateSkillIcon(WeaponData newWeapon)
    {
        if (newWeapon == null || skillIconImage == null) return;

        if (newWeapon.skillIcon != null)
        {
            skillIconImage.gameObject.SetActive(true);
            skillIconImage.sprite = newWeapon.skillIcon;
        }
        else
        {
            // 스킬 아이콘이 비어있다면 UI를 숨깁니다.
            skillIconImage.gameObject.SetActive(false);
        }

        // 무기가 바뀌면 쿨타임 UI 초기화
        isCooldown = false;
        if (cooldownDarkImage != null) cooldownDarkImage.fillAmount = 0f;
        if (cooldownText != null) cooldownText.text = "";
    }

    // ★ 플레이어가 스킬을 쓰면 호출할 함수
    public void TriggerCooldown(float cooldownTime)
    {
        if (cooldownTime <= 0f) return;

        maxCooldown = cooldownTime;
        currentCooldown = cooldownTime;
        isCooldown = true;
    }
}