using UnityEngine;
using System.Collections;

public class AoEZone : MonoBehaviour
{
    public float delay = 1.5f; // time before explosion
    public GameObject explosionPrefab;

    void Start()
    {
        StartCoroutine(ExplodeRoutine());
    }

    IEnumerator ExplodeRoutine()
    {
        // wait (warning time)
        yield return new WaitForSeconds(delay);

        // create explosion
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // remove warning circle
        Destroy(gameObject);
    }
}