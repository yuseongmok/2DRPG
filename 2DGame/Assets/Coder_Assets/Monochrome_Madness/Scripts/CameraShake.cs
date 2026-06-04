using System.Collections;
using UnityEngine;


namespace Coder_Assets.Monochrome_Madness

{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance;

        private void Awake()
        {
            Instance = this;
        }

        public IEnumerator Shake(float duration, float magnitude)
        {
            Vector3 originalPos = transform.localPosition;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                transform.localPosition = originalPos + new Vector3(x, y, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originalPos;
        }
    }
}