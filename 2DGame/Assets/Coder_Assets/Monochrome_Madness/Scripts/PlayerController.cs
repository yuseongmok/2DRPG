using System.Collections;
using UnityEngine;


namespace Coder_Assets.Monochrome_Madness

{
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 6f;
        public float jumpForce = 16f;
        public float dashSpeed = 20f;
        public float dashDuration = 0.2f;
        public float dashCooldown = 1f;

        private Rigidbody2D rb;
        private bool isGrounded;
        private bool isDashing;
        private float dashTime;
        private float lastDashTime;
        public GameObject dashParticles;
        public GameObject jumpBurstFX;
        public GameObject dashTrail;
        public Transform groundCheck;
        public LayerMask groundLayer;
        private Animator anim;
        private bool wasGroundedLastFrame;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>(); // ADD THIS
        }

        void Update()
        {
            float moveInput = Input.GetAxisRaw("Horizontal");
            anim.SetFloat("Speed", Mathf.Abs(moveInput)); // ADD THIS

            if (moveInput != 0)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Sign(moveInput) * Mathf.Abs(scale.x);
                transform.localScale = scale;
            }


            // Check if just landed
            if (!wasGroundedLastFrame && isGrounded)
            {
                // ✅ TRIGGER SCREEN SHAKE HERE
                CameraShake.Instance.StartCoroutine(CameraShake.Instance.Shake(0.1f, 0.2f));
                Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y - 1.3f, transform.position.z);
                GameObject jumpFX = Instantiate(jumpBurstFX, spawnPosition, Quaternion.identity);
                Destroy(jumpFX, 0.5f);
            }

            wasGroundedLastFrame = isGrounded;


            if (!isDashing)
            {
                rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

                isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

                if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                    anim.SetTrigger("Jump"); // ADD THIS
                    Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y - 1.3f, transform.position.z);
                    GameObject jumpFX = Instantiate(jumpBurstFX, spawnPosition, Quaternion.identity);
                    Destroy(jumpFX, 0.5f);

                }

                if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time > lastDashTime + dashCooldown)
                {
                    StartDash(moveInput);
                }
            }

            if (isDashing && Time.time > dashTime)
            {
                isDashing = false;
            }
        }

        void StartDash(float direction)
        {
            if (direction == 0) direction = transform.localScale.x > 0 ? 1 : -1;
            rb.linearVelocity = new Vector3(direction * dashSpeed, 3, 0f);
            isDashing = true;
            dashTime = Time.time + dashDuration;
            lastDashTime = Time.time;

            // ?? Spawn dash particles
            Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

            GameObject dashFX = Instantiate(dashParticles, spawnPosition, Quaternion.identity);
            Destroy(dashFX, 0.6f);

            StartCoroutine(FlashDashTrail());



            // ?? Camera shake
            CameraShake.Instance.StartCoroutine(CameraShake.Instance.Shake(0.15f, 0.3f));

            // ? Optional: slow-mo
            anim.SetTrigger("dash");
        }


        private IEnumerator FlashDashTrail()
        {
            dashTrail.SetActive(true);
            yield return new WaitForSeconds(0.5f); // how long it's visible
            dashTrail.SetActive(false);
        }

    }
}