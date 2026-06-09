using System.Collections;
using UnityEngine;
using Unity.Cinemachine; 

public class BossCutsceneManager : MonoBehaviour
{
    [Header("카메라 설정")]
    public CinemachineCamera playerCamera; // 기본 플레이어 카메라
    public CinemachineCamera bossCamera;   // 연출용 보스 카메라

    [Header("연동 오브젝트")]
    public BossEnemy bossScript;           // 보스 AI 스크립트
    public GameObject bossUIObject;       // BossHPRoot UI 오브젝트

    private bool isActivated = false;      // 컷신이 중복 실행되지 않도록 방지

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어가 보스방 트리거에 들어왔을 때 딱 1번만 실행
        if (collision.CompareTag("Player") && !isActivated)
        {
            isActivated = true;
            StartCoroutine(BossIntroCutsceneSequence(collision.gameObject));
        }
    }

    IEnumerator BossIntroCutsceneSequence(GameObject player)
    {
        // -----------------------------------------------------------------
        // 1. 플레이어 조작 멈추기 & 보스 AI 대기
        // -----------------------------------------------------------------
        // 대시 중이거나 이동 중 물리 속도가 튀지 않게 플레이어 입력을 잠금 처리
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null) playerRb.linearVelocity = new Vector2(0f, playerRb.linearVelocity.y);
        player.GetComponent<MonoBehaviour>().enabled = false; // 임시로 플레이어 스크립트 컴포넌트 끄기

        // 보스 AI 루프가 미리 돌지 않도록 보스 스크립트도 잠시 꺼둡니다.
        if (bossScript != null) bossScript.enabled = false;

        // -----------------------------------------------------------------
        // 2. 카메라를 보스에게로 전환 (우선순위를 높여서 시네머신이 부드럽게 이동하게 만듦)
        // -----------------------------------------------------------------
        bossCamera.Priority = 20; 

        // 카메라가 플레이어에게서 보스에게로 도달할 때까지 잠시 대기 (약 1.5초)
        yield return new WaitForSeconds(1.5f);

        // -----------------------------------------------------------------
        // 3. 보스 체력 바 UI가 스르륵 차오르는 연출
        // -----------------------------------------------------------------
        if (BossHPController.Instance != null && bossScript != null)
        {
            // UI 전체 루트를 강제로 켜줍니다.
            if (bossUIObject != null) bossUIObject.SetActive(true);

            int maxHP = bossScript.maxHealth;
            
            // 0부터 MaxHP까지 게이지가 탁탁탁 차오르는 코루틴 연출
            float duration = 1.5f; // 체력 바가 차오르는 시간 (1.5초 동안)
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // 시간에 따라 0에서 maxHP까지 부드럽게 보간 계산
                int currentInterpolatedHP = (int)Mathf.Lerp(0, maxHP, elapsed / duration);
                BossHPController.Instance.ShowBossHP(bossScript.bossName, currentInterpolatedHP, maxHP);
                yield return null;
            }

            // 확실하게 최종 체력으로 고정
            BossHPController.Instance.ShowBossHP(bossScript.bossName, maxHP, maxHP);
        }

        // 보스가 포효하거나 애니메이션을 재생하고 싶다면 여기에 코드를 넣으세요. (예: bossScript.GetComponent<Animator>().Play("Roar");)
        yield return new WaitForSeconds(1.0f);

        // -----------------------------------------------------------------
        // 4. 다시 카메라를 플레이어에게 돌려주기
        // -----------------------------------------------------------------
        bossCamera.Priority = 5; // 보스 카메라 우선순위를 다시 낮추면 시네머신이 플레이어 카메라로 돌아갑니다.

        // 카메라가 플레이어에게 돌아오는 시간 대기
        yield return new WaitForSeconds(1.5f);

        // -----------------------------------------------------------------
        // 5. 전투 시작! 조작 및 AI 해제
        // -----------------------------------------------------------------
        player.GetComponent<MonoBehaviour>().enabled = true; // 플레이어 조작 복구
        if (bossScript != null) bossScript.enabled = true;   // 보스 AI 작동 개시!

        Debug.Log("보스전 시작! ");
        
        // 트리거 오브젝트는 이제 필요 없으므로 파괴하거나 비활성화
        Destroy(gameObject);
    }
}