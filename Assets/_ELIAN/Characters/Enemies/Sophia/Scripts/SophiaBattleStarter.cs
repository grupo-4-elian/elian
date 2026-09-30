using UnityEngine;

public class SophiaBattleStarter : MonoBehaviour
{
    [SerializeField] private SophiaController sophia;

    private void OnEnable()
    {
        if (sophia != null)
            sophia.StartBattle();
    }
}
