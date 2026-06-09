using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHPController : MonoBehaviour
{
    public static BossHPController Instance;

    [Header("UI 오브젝트 묶음")]
    public GameObject hpRootObject; // Canvas 안의 'BossHPRoot'
    public Slider hpSlider;         // 'BossHPBar'
    public TextMeshProUGUI bossNameText; // 보스 이름 텍스트

    [Header("보스 HP UI 고유 사운드 설정")]
    public string bossHpShowSoundName = "BossHPFill";  // ★ 보스 HP바가 나타날 때
    public string bossHpHurtSoundName = "BossHPHurt";  // ★ 보스 HP가 깎일 때 

    private bool isHpHurtSoundActive = false; // 보스가 맞을 때 소리가 너무 겹쳐서 고장나는 것 방지

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 보스방 진입 시 UI를 켜고 초기화하는 함수
    public void ShowBossHP(string name, int currentHP, int maxHP)
    {
        if (hpRootObject == null) return;

        // ★ 이미 켜져 있는 상태라면 사운드가 중복 실행되지 않도록 방어 코드를 넣었습니다.
        if (!hpRootObject.activeSelf)
        {
            // [보스 체력 바 UI 등장 효과음 재생]
            // 플레이어가 보스방 구역에 진입해 게이지가 처음 켜지는 순간 웅장한 사운드를 출력합니다.
            if (SoundManager.Instance != null && !string.IsNullOrEmpty(bossHpShowSoundName))
            {
                SoundManager.Instance.PlaySFX(bossHpShowSoundName);
            }
        }

        hpRootObject.SetActive(true); // UI 켜기
        hpSlider.maxValue = maxHP;
        hpSlider.value = currentHP;

        if (bossNameText != null)
        {
            bossNameText.text = name;
        }
    }

    // 실시간 체력 업데이트 함수
    public void UpdateBossHP(int currentHP)
    {
        if (hpSlider != null)
        {
            hpSlider.value = currentHP;
        }

        // ★ [보스 HP 감소 효과음 재생 (선택사항)]
        // 보스가 연타를 맞을 때 소리가 찢어지지 않도록 코루틴 없이 간단한 쿨타임 처리 방식으로 구현했습니다.
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(bossHpHurtSoundName) && !isHpHurtSoundActive)
        {
            StartCoroutine(PlayHurtSoundWithDelay());
        }
    }

    System.Collections.IEnumerator PlayHurtSoundWithDelay()
    {
        isHpHurtSoundActive = true;
        SoundManager.Instance.PlaySFX(bossHpHurtSoundName);
        yield return new WaitForSeconds(0.15f); // 0.15초 동안은 피격 사운드가 중복으로 안 겹치게 제한
        isHpHurtSoundActive = false;
    }

    // 보스가 죽거나 범위를 벗어나면 UI를 숨기는 함수
    public void HideBossHP()
    {
        if (hpRootObject != null)
        {
            hpRootObject.SetActive(false); // UI 끄기
        }
    }
}