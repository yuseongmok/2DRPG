using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float speed;
    private int damage;
    private Vector2 direction;
    private LayerMask targetLayers;
    private GameObject hitEffect;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Setup(Vector2 dir, float projSpeed, int projDamage, LayerMask targets, GameObject fx)
    {
        direction = dir.normalized;
        speed = projSpeed;
        damage = projDamage;
        targetLayers = targets;
        hitEffect = fx;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Destroy(gameObject, 7f);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = direction * speed;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & targetLayers) != 0)
        {
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }

            // 🎯 투사체의 위치를 넘겨서 넉백 연산 주입
            Enemy enemyScript = collision.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(damage, transform.position);
            }

            Debug.Log($"🎯 투사체가 {collision.name}에게 적중! 대미지: {damage}");
            Destroy(gameObject);
        }
        else if (collision.gameObject.CompareTag("Ground")) 
        {
            Destroy(gameObject);
        }
    }
}