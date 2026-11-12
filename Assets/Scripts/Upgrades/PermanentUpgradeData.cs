using UnityEngine;

[CreateAssetMenu(fileName = "Perm_", menuName = "Upgrades/Permanent Upgrade")]
public class PermanentUpgradeData : ScriptableObject
{
    public UpgradeCategory category;
    public string displayName = "Fuerza";
    public Sprite icon;
    public Sprite backGround;

    [Tooltip("Efecto de cada nivel (0.12 = +12%)")]
    public float effectPerLevel = 0.12f;
    [Tooltip("Texto después del porcentaje en la carta")]
    public string effectText = "de daño con el martillo";

    [Tooltip("Precio del nivel n = P × este valor × n")]
    public float priceMultiplier = 0.5f;

    [Tooltip("Qué estado emocional reduce esta mejora en la noche")]
    public MoodStat affectedBy = MoodStat.Ninguna;

    public string GetEffectLine() => $"+{effectPerLevel * 100f:0}% {effectText}";
}