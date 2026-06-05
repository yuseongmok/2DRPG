using UnityEngine;
using TMPro;

public class DroppedWeapon : MonoBehaviour
{
    [Header("Weapon Data")]
    public WeaponData weaponData; // 이 아이템이 가질 무기 정보

    [Header("UI Reference")]
    public GameObject tooltipPanel;   // 무기 머리 위에 띄울 캔버스 내 패널
    public TextMeshProUGUI tooltipText; // 정보를 표시할 텍스트 (TMP)

    [Header("Floating Animation")]
    public float bounceSpeed = 2f;      // 둥둥 뜨는 속도
    public float bounceAmplitude = 0.2f; // 위아래 움직임 범위
    
    private Vector3 startPosition;
    private bool isPlayerNearby = false;
    private PlayerController playerController;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;


        if (spriteRenderer != null && weaponData != null && weaponData.weaponSprite != null)
        {
            spriteRenderer.sprite = weaponData.weaponSprite;
        }

        // 시작할 때 툴팁은 꺼둡니다.
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);

        // 무기 데이터가 있다면 텍스트를 미리 세팅합니다.
        UpdateTooltipUI();
    }

    void Update()
    {
        // 1. 아이템 둥둥 떠다니는 애니메이션
        float newY = startPosition.y + Mathf.Sin(Time.time * bounceSpeed) * bounceAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // 2. 플레이어가 근처에 있고 F키를 누르면 교체 진행
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.F))
        {
            SwapLogic();
        }
    }

    private void SwapLogic()
    {
        if (playerController == null) return;

        // 플레이어에게 무기를 주고, 기존에 들고 있던 무기 데이터를 받아옵니다.
        WeaponData oldWeapon = playerController.SwapWeapon(weaponData);

        if (oldWeapon != null)
        {
            // [핵심] 기존에 들고 있던 무기를 플레이어 위치에 새로 생성합니다.
            // 현재 오브젝트(gameObject)를 복제하여 똑같은 툴팁 구조를 가진 새 아이템을 만듭니다.
            GameObject tossedWeaponObj = Instantiate(gameObject, playerController.transform.position, Quaternion.identity);
            
            // 새로 생성된 오브젝트의 데이터를 기존 무기 데이터로 교체합니다.
            DroppedWeapon tossedScript = tossedWeaponObj.GetComponent<DroppedWeapon>();
            if (tossedScript != null)
            {
                tossedScript.weaponData = oldWeapon;
                // 생성 직후 텍스트 갱신
                tossedScript.UpdateTooltipUI(); 
            }
        }

        // [핵심] 주운 원래 아이템은 필드에서 제거합니다.
        Destroy(gameObject);
    }

    // 툴팁 텍스트를 무기 데이터에 맞게 조립하는 함수
    public void UpdateTooltipUI()
    {
        if (tooltipText != null && weaponData != null)
        {
            // 한글 깨짐 방지 및 가독성을 위한 서식 적용 (Dynamic 폰트 에셋 적용 필수)
            tooltipText.text = $"<color=#FFCC00>[F] 교체</color>\n" +
                               $"<b><size=120%>{weaponData.weaponName}</size></b>\n" +
                               $"<color=#FF5555>공격력: {weaponData.attackDamage}</color> | " +
                               $"<color=#55FF55>공속: {weaponData.attackRate}</color>\n\n" +
                               $"<i>\"{weaponData.weaponDescription}\"</i>";
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = true;
            playerController = collision.GetComponent<PlayerController>();
            
            if (tooltipPanel != null)
            {
                UpdateTooltipUI(); // 보여주기 직전에 최신 데이터로 갱신
                tooltipPanel.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = false;
            playerController = null;

            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }
    }
}