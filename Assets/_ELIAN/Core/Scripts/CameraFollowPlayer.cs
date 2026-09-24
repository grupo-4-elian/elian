using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollowPlayer : MonoBehaviour
{
    [SerializeField] public GameObject Player;

    [Header("Bordes del nivel (recomendado)")]
    [Tooltip("Si esta activo, la camara calcula sus limites segun el ancho real de la pantalla, " +
             "asi nunca muestra nada fuera del fondo sin importar la resolucion.")]
    [SerializeField] private bool useLevelBounds = true;
    [Tooltip("Coordenada X del borde izquierdo del fondo.")]
    [SerializeField] private float levelLeftEdge = 0f;
    [Tooltip("Coordenada X del borde derecho del fondo.")]
    [SerializeField] private float levelRightEdge = 120f;

    [Header("Limites horizontales fijos (si no se usan los bordes del nivel)")]
    [SerializeField] private float minX = 10f;
    [SerializeField] private float maxX = 30f;

    [Header("Posicion vertical")]
    [SerializeField] private float fixedY = 3.1875f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (Player == null)
            return;

        float left = minX;
        float right = maxX;

        if (useLevelBounds && cam != null && cam.orthographic)
        {
            // Mitad del ancho visible, en unidades del mundo.
            float halfWidth = cam.orthographicSize * cam.aspect;
            left = levelLeftEdge + halfWidth;
            right = levelRightEdge - halfWidth;

            // Si la pantalla es mas ancha que el nivel entero, centramos.
            if (left > right)
                left = right = (levelLeftEdge + levelRightEdge) * 0.5f;
        }

        float cameraX = Mathf.Clamp(
            Player.transform.position.x,
            left,
            right
        );

        transform.position = new Vector3(
            cameraX,
            fixedY,
            transform.position.z
        );
    }
}
