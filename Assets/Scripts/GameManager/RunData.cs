using System;
using System.Collections.Generic;

[Serializable]
public class RunData
{
    // Economía (en centavos: 5000 = $50.00).
    public int money;
    public int drop;

    // Progreso de la noche
    public int nightWaveReached;

    // Niveles de las mejoras permanentes, indexados por UpgradeCategory
    public int[] permanentLevels = new int[4];

    public List<string> activeTemporaryUpgrades = new List<string>();

    // Estado emocional (0 a 100, se arrastra entre días)
    public int stress;
    public int depression;
}