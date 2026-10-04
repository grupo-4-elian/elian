using UnityEngine;

// Cinta transportadora: mueve lo que se apoya encima (piezas, enemigos y
// tambien a Elian) hacia la boca de FABER. FABER puede invertirla.
[RequireComponent(typeof(SurfaceEffector2D))]
public class CintaTransportadora : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private SpriteRenderer beltRenderer;
    [SerializeField] private Sprite[] beltFrames;
    [SerializeField] private float frameRate = 10f;

    private SurfaceEffector2D effector;
    private int direction = 1;
    private float t;

    public int Direction => direction;

    private void Awake()
    {
        effector = GetComponent<SurfaceEffector2D>();
        Apply();
    }

    public void SetDirection(int dir)
    {
        direction = dir >= 0 ? 1 : -1;
        Apply();
    }

    private void Apply()
    {
        if (effector != null)
            effector.speed = speed * direction;
    }

    private void Update()
    {
        if (beltRenderer == null || beltFrames == null || beltFrames.Length == 0)
            return;

        t += Time.deltaTime * frameRate * direction;
        int i = ((int)Mathf.Floor(t) % beltFrames.Length + beltFrames.Length) % beltFrames.Length;
        beltRenderer.sprite = beltFrames[i];
    }
}
