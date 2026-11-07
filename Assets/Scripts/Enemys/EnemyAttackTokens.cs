using UnityEngine;

public static class EnemyAttackTokens
{
    public static int MaxAttackers = 2;

    private static int current;

    // Reinicia el contador cada vez que se da Play en el editor
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => current = 0;

    public static bool TryTake()
    {
        if (current >= MaxAttackers) return false;
        current++;
        return true;
    }

    public static void Release()
    {
        current = Mathf.Max(0, current - 1);
    }
}