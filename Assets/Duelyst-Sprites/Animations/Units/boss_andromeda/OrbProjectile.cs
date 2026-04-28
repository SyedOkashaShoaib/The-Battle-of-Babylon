using UnityEngine;

public class OrbProjectile : MonoBehaviour
{
    public bool boss_isRage = false;
    private CircleCollider2D col;

    public float speed = 13f;
    private float currentspeed;
    public float acceleration = 3f;
    private Vector3 direction;
    private bool isLaunched = false;

    public float startScale = 1f;
    public float growthRate = 1f;
    private Vector3 baseScale;

    void Start()
    {
        col = GetComponent<CircleCollider2D>();
    }

    public void Launch(Vector3 dir, bool Rage_throw)
    {
        direction = dir.normalized;
        currentspeed = speed;
        isLaunched = true;
        boss_isRage = Rage_throw;
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (!isLaunched) return;
        currentspeed += acceleration * Time.deltaTime;
        transform.position += direction * currentspeed * Time.deltaTime;

        if(boss_isRage)
        {
            col.radius += (growthRate * Time.deltaTime)/30;
            transform.localScale += Vector3.one * growthRate * Time.deltaTime;
        }
    }
}
