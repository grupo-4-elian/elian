using System.Collections;
using UnityEngine;

// Capsula de fabricacion de FABER: se abre, arma un robot y lo libera.
public class FabricationPod : MonoBehaviour
{
    [SerializeField] private FrameAnimator anim;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float sectionMinX = 80f;
    [SerializeField] private float sectionMaxX = 118f;

    private GameObject lastSpawned;
    private bool working;

    public bool CanFabricate => !working && lastSpawned == null;

    public void Fabricate()
    {
        if (!CanFabricate)
            return;
        StartCoroutine(Run());
    }

    // FABER cayo: la capsula deja de fabricar aunque estuviera a medias.
    public void Stop()
    {
        StopAllCoroutines();
        working = true;
        if (anim != null)
            anim.Play("idle", true);
    }

    private IEnumerator Run()
    {
        working = true;
        anim.Play("build", true);
        yield return new WaitForSeconds(anim.LengthOf("build"));

        if (enemyPrefab != null)
        {
            lastSpawned = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
            FactoryEnemy e = lastSpawned.GetComponent<FactoryEnemy>();
            if (e != null) e.SetSectionBounds(sectionMinX, sectionMaxX);
        }

        anim.Play("idle", true);
        working = false;
    }
}
