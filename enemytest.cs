using System.Collections;
using UnityEngine;

public class DummyEnemy : MonoBehaviour
{
    SpriteRenderer sr;
    Color original;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        original = sr.color;
    }

    void TakeDamage(int damage)
    {
        Debug.Log("Hit! damage = " + damage);
        StopAllCoroutines();
        StartCoroutine(Flash());
    }

    IEnumerator Flash()
    {
        sr.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        sr.color = original;
    }
}