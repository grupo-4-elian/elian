using UnityEngine;

// Boca de ensamblaje de FABER: todo lo que la cinta trae hasta aqui
// se le entrega a FABER para que intente ensamblarlo.
[RequireComponent(typeof(Collider2D))]
public class FaberIntake : MonoBehaviour
{
    [SerializeField] private FaberController faber;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Pieza pieza = other.GetComponentInParent<Pieza>();
        if (pieza == null || pieza.Consumed || faber == null)
            return;

        faber.OnItemReceived(pieza);
    }
}
