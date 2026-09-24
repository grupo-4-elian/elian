using UnityEngine;

// Zona de tunel de techo bajo: mientras Elian este adentro queda agachado
// y solo puede avanzar o retroceder (caminando y disparando), sin saltar.
//
// El trigger debe cubrir desde el suelo hasta el techo del tunel y
// sobresalir un poco a cada lado, para que Elian se agache antes de
// chocar con el techo y se levante recien cuando ya salio de abajo.
[RequireComponent(typeof(BoxCollider2D))]
public class CrawlZone : MonoBehaviour
{
    private PlayerController playerInside;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (playerInside != null || !other.CompareTag("Player"))
            return;

        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc == null)
            return;

        playerInside = pc;
        pc.EnterCrawlZone();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (playerInside == null || !other.CompareTag("Player"))
            return;

        if (other.GetComponentInParent<PlayerController>() != playerInside)
            return;

        playerInside.ExitCrawlZone();
        playerInside = null;
    }

    private void OnDisable()
    {
        if (playerInside != null)
        {
            playerInside.ExitCrawlZone();
            playerInside = null;
        }
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
            return;

        Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.35f);
        Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
    }
}
