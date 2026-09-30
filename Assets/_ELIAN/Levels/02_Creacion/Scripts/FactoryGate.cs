using System.Collections;
using UnityEngine;

// Compuerta industrial entre secciones del Nivel 2.
// El bloqueo real es el objeto "Gate_xx" (su collider), que el WaveController
// desactiva al eliminar la ultima oleada. Esta parte visual lo detecta y
// sube la persiana: la luz pasa de roja a verde y el paso queda libre.
public class FactoryGate : MonoBehaviour
{
    [SerializeField] private GameObject blocker;
    [SerializeField] private Transform shutter;
    [SerializeField] private SpriteRenderer lamp;
    [SerializeField] private Sprite lampOpen;
    [SerializeField] private float openDuration = 1.2f;
    [Tooltip("Cuanto sube la persiana (unidades); termina fuera de camara.")]
    [SerializeField] private float openHeight = 10f;

    private bool opened;

    private void Update()
    {
        if (opened || blocker == null || blocker.activeInHierarchy)
            return;

        opened = true;
        StartCoroutine(Open());
    }

    private IEnumerator Open()
    {
        if (lamp != null && lampOpen != null)
            lamp.sprite = lampOpen;
        Sfx.Play(Sfx.Descarga, 0.7f);

        if (shutter == null)
            yield break;

        Vector3 start = shutter.localPosition;
        float t = 0f;
        while (t < openDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / openDuration);
            shutter.localPosition = start + Vector3.up * openHeight * k;
            yield return null;
        }

        shutter.gameObject.SetActive(false);
    }
}
