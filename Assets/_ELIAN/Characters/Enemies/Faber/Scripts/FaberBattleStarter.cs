using UnityEngine;

// Se activa al terminar el dialogo de presentacion de FABER y arranca el combate.
public class FaberBattleStarter : MonoBehaviour
{
    [SerializeField] private FaberController faber;

    private void OnEnable()
    {
        if (faber != null)
            faber.StartBattle();
    }
}
