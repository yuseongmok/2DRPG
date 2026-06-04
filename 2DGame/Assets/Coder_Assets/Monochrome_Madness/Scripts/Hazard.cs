using UnityEngine;
using UnityEngine.SceneManagement;


namespace Coder_Assets.Monochrome_Madness

{
    public class Hazard : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                // 🧠 Trigger screen shake
                if (CameraShake.Instance != null)
                    CameraShake.Instance.StartCoroutine(CameraShake.Instance.Shake(0.2f, 0.4f));

                // 🐢 Slow motion
                Time.timeScale = 0.3f;

                // ⏱ Restart after delay
                Invoke(nameof(RestartLevel), 0.4f);
            }
        }

        void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}