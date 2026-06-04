using UnityEngine;
using UnityEngine.SceneManagement;


namespace Coder_Assets.Monochrome_Madness

{
    public class DeathZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                // Optional: slow motion
                Time.timeScale = 0.3f;
                Invoke(nameof(Restart), 0.4f);
            }
        }

        void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}