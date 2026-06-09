using UnityEngine;
using UnityEngine.SceneManagement; 

public class LobbyPortal : MonoBehaviour
{
    [Header("이동할 로비 씬 이름")]
    public string lobbySceneName = "LobbyScene";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어가 포탈에 닿았을 때만 작동
        if (collision.CompareTag("Player"))
        {
            Debug.Log("로비로 돌아갑니다!");
            SceneManager.LoadScene(lobbySceneName);
        }
    }
}