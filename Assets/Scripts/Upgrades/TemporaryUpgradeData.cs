using UnityEngine;

[CreateAssetMenu(fileName = "Temp_", menuName = "Upgrades/Temporary Upgrade")]
public class TemporaryUpgradeData : ScriptableObject
{
    public string id = "T01";
    public string displayName = "Puño de Algodón";
    [Tooltip("Opcional: si se deja vacío, usa el ícono de su categoría")]
    public Sprite icon;

    public UpgradeCategory category;
    public UpgradeTier tier;

    [Tooltip("Precio en unidades de noche")]
    public float price = 1f;
    [Tooltip("Bono (0.05 = +5%)")]
    public float effect = 0.05f;

    // Usa el mismo texto que la permanente de su categoría ("de daño con el martillo", etc.)
    public string GetEffectLine(UpgradeSettings settings)
    {
        PermanentUpgradeData categoryData = settings.Get(category);
        string text = categoryData != null ? categoryData.effectText : "";
        return $"+{effect * 100f:0}% {text}";
    }

    public Sprite GetIcon(UpgradeSettings settings)
    {
        if (icon != null) return icon;
        PermanentUpgradeData categoryData = settings.Get(category);
        return categoryData != null ? categoryData.icon : null;
    }
}