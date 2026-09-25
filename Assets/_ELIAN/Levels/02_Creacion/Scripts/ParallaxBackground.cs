using UnityEngine;

// Fondo panoramico con paralaje: un unico fondo mas corto que el nivel
// se desplaza mas lento que la camara y cubre todo el recorrido, de la
// entrada (izquierda del fondo) al nucleo de FABER (derecha del fondo).
[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxBackground : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private float levelLeftEdge = 0f;
    [SerializeField] private float levelRightEdge = 120f;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (cam == null)
            cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (cam == null || sr.sprite == null)
            return;

        float halfView = cam.orthographicSize * cam.aspect;
        float width = sr.bounds.size.x;
        float camMin = levelLeftEdge + halfView;
        float camMax = levelRightEdge - halfView;
        float camX = Mathf.Clamp(cam.transform.position.x, camMin, camMax);

        float x;
        if (camMax - camMin <= 0.01f || width <= halfView * 2f)
        {
            x = camX;
        }
        else
        {
            // 0 = borde izquierdo del fondo alineado con la vista; 1 = borde derecho.
            float t = Mathf.InverseLerp(camMin, camMax, camX);
            float leftAligned = camX - halfView + width / 2f;
            float rightAligned = camX + halfView - width / 2f;
            x = Mathf.Lerp(leftAligned, rightAligned, t);
        }

        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }
}
