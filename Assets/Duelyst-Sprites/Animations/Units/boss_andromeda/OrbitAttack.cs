using UnityEngine;
using System.Collections.Generic;

public class OrbManager : MonoBehaviour
{
    public Boss_stats boss;
    public Transform player;

    public Transform boss_transform;
    public GameObject orbPrefab1;
    public GameObject orbPrefab2;

    public int orbCount = 5;
    public float radius = 2f;
    public float speed = 8f;

    public bool isActive = false;
    public float duration = 10f;
    private float timer = 0f; 

    private List<Transform> orbs = new List<Transform>();
    private List<Transform> orbs2 = new List<Transform>();
    private float angle;

    Vector3 GetPosition(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;

        return boss_transform.position + new Vector3(
            Mathf.Cos(rad) * radius,
            Mathf.Sin(rad) * radius,
            0
        );
    }

    public void Start_OrbAttack()
    {
        if (boss.currentPhase == BossPhase.Rage)
        {
            orbCount = 8;
        }
        else
        {
            orbCount = 5;
        }

        for (int i = 0; i < orbCount; i++)
        {
            float a = (360f / orbCount) * i;
            Vector3 pos = GetPosition(a);

            var orb1 = Instantiate(orbPrefab1, pos, Quaternion.identity);
            orbs.Add(orb1.transform);
            var orb2 = Instantiate(orbPrefab2, pos, Quaternion.identity);
            orbs2.Add(orb2.transform);
        }
    }

    public void Activate()
    {
        if(isActive) { return; }
        
        Start_OrbAttack();
        isActive = true;
        timer = 0f;
    }

    public void Deactivate()
    {
        isActive = false;

        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i] != null)
            {
                Destroy(orbs[i].gameObject);
                Destroy(orbs2[i].gameObject);
            }
        }
        orbs.Clear();
        orbs2.Clear();
    }

    public void ThrowBalls()
    {
        for (int i = 0; i < orbCount; i++)
        {
            if (orbs[i] == null) continue;

            Vector3 dir = (player.position - orbs[i].position).normalized;
            dir += new Vector3(Random.Range(-0.14f, 0.14f), Random.Range(-0.14f, 0.14f), 0);

            var proj1 = orbs[i].GetComponent<OrbProjectile>(); // calls the script attached to the orb prefab
            var proj2 = orbs2[i].GetComponent<OrbProjectile>(); // twice because orbs are 2 effects layered. Both are thrown as one

            if (proj1 != null)
            {
                bool Rage_throw = false;
                if(boss.currentPhase == BossPhase.Rage)
                {
                    Rage_throw = true;
                }         
                proj1.Launch(dir, Rage_throw);
                proj2.Launch(dir, Rage_throw);
            }
        }
        isActive = false;
        orbs.Clear();
        orbs2.Clear();
    }

    void Update()
    {
        if (boss.queenisDead && isActive)
        {
            Deactivate();
        }
        if (!isActive) return;

        timer += Time.deltaTime;

        angle += speed * Time.deltaTime;

        for (int i = 0; i < orbs.Count; i++)
        {
            float a = angle + (360f / orbCount) * i;
            orbs[i].position = GetPosition(a);
            orbs2[i].position = GetPosition(a);
        }

        if (timer >= duration)
        {
            if (boss.currentPhase == BossPhase.Normal) Deactivate();
            else if (boss.currentPhase == BossPhase.Aggressive) ThrowBalls();
            else if (boss.currentPhase == BossPhase.Rage) ThrowBalls();

        }
    }
}