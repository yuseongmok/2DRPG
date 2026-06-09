using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LobbyManager : MonoBehaviour
{
    [Header("UI 요소 연결")]
    public RectTransform arrowPointer; 
    public Button tutorialButton;      // 튜토리얼 버튼
    public Button newGameButton;       // 새게임 버튼

    [Header("화살표 위치 미세조정")]
    public float xOffset = -20f;       // 버튼 글자 기준으로 화살표를 얼마나 왼쪽에 띄울지 설정

    [Header("이동할 인게임 씬 이름")]
    public string inGameSceneName = "TutorialScenes"; 

    void Start()
    {
        // 버튼 클릭 이벤트 리스너 등록
        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(OnTutorialClicked);

        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);

        // 게임 시작 시 기본적으로 튜토리얼 버튼이 선택되어 있도록 세팅 (화살표 자동 위치 잡기용)
        if (tutorialButton != null)
        {
            EventSystem.current.SetSelectedGameObject(tutorialButton.gameObject);
            MoveArrowTo(tutorialButton.GetComponent<RectTransform>());
        }
    }

    void Update()
    {
        // 현재 마우스나 키보드로 '선택된' UI 오브젝트를 실시간으로 감시
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected != null)
        {
            RectTransform selectedRect = currentSelected.GetComponent<RectTransform>();
            if (selectedRect != null && (currentSelected == tutorialButton.gameObject || currentSelected == newGameButton.gameObject))
            {
                // 선택된 버튼 옆으로 화살표를 스무스하게 또는 즉시 이동
                MoveArrowTo(selectedRect);
            }
        }
    }

    // 화살표 위치를 선택된 버튼의 왼쪽으로 옮기는 함수
    void MoveArrowTo(RectTransform targetButton)
    {
        if (arrowPointer == null) return;

        // 대상 버튼의 중심 또는 왼쪽 끝 좌표를 계산하여 화살표 위치를 잡아줍니다.
        Vector3 targetPos = targetButton.position;
        
        // 버튼 내부 글자가 가운데 정렬일 경우 왼쪽으로 xOffset만큼 밀어줍니다.
        targetPos.x += xOffset; 

        // Y축 높이는 버튼과 똑같이 맞춰줍니다.
        arrowPointer.position = new Vector3(targetPos.x, targetButton.position.y, arrowPointer.position.z);
    }

    // 튜토리얼 버튼 클릭 시 실행될 함수
    void OnTutorialClicked()
    {
        Debug.Log("튜토리얼을 시작합니다. 게임 씬으로 이동!");
        SceneManager.LoadScene(inGameSceneName);
    }

    // 새게임 버튼 클릭 시 실행될 함수 (지금은 예시용)
    void OnNewGameClicked()
    {
        Debug.Log("새 게임을 시작합니다.");
        // 필요하다면 다른 씬으로 보내거나 시스템 처리
    }
}