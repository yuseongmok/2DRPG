using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHPController : MonoBehaviour
{
    public static BossHPController Instance;

    [Header("UI 오브젝트 묶음")]
    public GameObject hpRootObject; // Canvas 안의 'BossHPRoot'
    public Slider hpSlider;         // 'BossHPBar'
    public TextMeshProUGUI bossNameText; // 보스 이름 텍스트 (일반 UI Text면 Text로 변경)

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    //보스방 진입 시 UI를 켜고 초기화하는 함수
    public void ShowBossHP(string name, int currentHP, int maxHP)
    {
        if (hpRootObject == null) return;

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