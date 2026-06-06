using UnityEngine;
using UnityEngine.UI;

public class HPBarController : MonoBehaviour
{
    // 어디서나 접근할 수 있도록 싱글톤 인스턴스 생성
    public static HPBarController Instance { get; private set; }

    [Header("UI 컴포넌트 연결")]
    [SerializeField] private Slider hpSlider;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //체력 바의 최대치를 설정하는 함수
    public void SetupMaxHP(int maxHP)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = maxHP; // 시작할 때는 꽉 채우기
        }
    }

    //실시간으로 체력 바 수치를 변경하는 함수
    public void UpdateHPBar(int currentHP)
    {
        if (hpSlider != null)
        {
            hpSlider.value = currentHP;
        }
    }
}