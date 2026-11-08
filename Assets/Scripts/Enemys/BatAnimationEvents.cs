using UnityEngine;

public class BatAnimationEvents : MonoBehaviour
{
    private BatAI ai;

    private void Awake()
    {
        ai = GetComponentInParent<BatAI>();
    }

    // Se escribe en el Animation Event del clip de ataque
    public void LungeStart() => ai.OnLungeEvent();
}