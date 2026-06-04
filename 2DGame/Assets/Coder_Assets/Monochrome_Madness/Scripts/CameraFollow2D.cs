using UnityEngine;


namespace Coder_Assets.Monochrome_Madness

{
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform target;                // The player
        public Vector2 offset = new Vector2(0, 1); // Offset from the player
        public float smoothTime = 0.15f;        // Camera smoothing speed
        public float lookAheadDistance = 2f;    // How far to look ahead when moving
        public float verticalSmoothTime = 0.2f; // Optional: separate vertical smooth

        private Vector3 velocity = Vector3.zero;
        private Vector3 currentVelocity;
        private Vector3 lastTargetPosition;
        private float lookAheadDirectionX;
        private float targetLookAheadX;

        void Start()
        {
            if (target == null && Camera.main != null)
            {
                Debug.LogWarning("No target set for camera! Assign your Player transform.");
            }

            lastTargetPosition = target.position;
        }

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 deltaMovement = target.position - lastTargetPosition;
            lastTargetPosition = target.position;

            // Determine movement direction for lookahead
            if (Mathf.Abs(deltaMovement.x) > 0.01f)
            {
                lookAheadDirectionX = Mathf.Sign(deltaMovement.x);
            }

            targetLookAheadX = lookAheadDirectionX * lookAheadDistance;

            Vector3 desiredPosition = new Vector3(
                target.position.x + targetLookAheadX + offset.x,
                target.position.y + offset.y,
                transform.position.z
            );

            Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
            transform.position = smoothedPosition;
        }
    }
}