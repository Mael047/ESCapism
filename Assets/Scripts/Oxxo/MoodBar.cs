using TMPro;
using UnityEngine;

/// <summary>
/// Muestra el estrés o la depresión del jugador (0 a 100) en una SegmentedBar.
/// Se actualiza sola cada vez que MoodTracker cambia los valores.
/// Va en la raíz de la barra, junto a SegmentedBar.
/// </summary>
[RequireComponent(typeof(SegmentedBar))]
public class MoodBar : MonoBehaviour
{
    /// <summary>Qué valor del ánimo muestra la barra (va dentro de MoodBar para no chocar con otros enums del proyecto).</summary>
    public enum Tipo { Estres, Depresion }

    [Tooltip("Qué valor muestra esta barra.")]
    public Tipo valor = Tipo.Estres;

    [Tooltip("El MoodTracker de la escena. Si se deja vacío, lo busca solo.")]
    public MoodTracker tracker;

    [Tooltip("Valor máximo (MoodTracker los limita a 100).")]
    public int maximo = 100;

    [Tooltip("Texto opcional para ver el número (por ejemplo \"45\").")]
    public TMP_Text textoValor;

    SegmentedBar barra;

    void Awake()
    {
        barra = GetComponent<SegmentedBar>();
        if (tracker == null) tracker = FindAnyObjectByType<MoodTracker>();
    }

    void OnEnable()
    {
        if (tracker != null) tracker.Changed += Refrescar;
        Refrescar();
    }

    void OnDisable()
    {
        if (tracker != null) tracker.Changed -= Refrescar;
    }

    /// <summary>Lee el valor actual de los datos de la partida y actualiza la barra.</summary>
    public void Refrescar()
    {
        var datos = PermanentUpgrades.Data;
        if (datos == null || barra == null) return;

        int actual = valor == Tipo.Estres ? datos.stress : datos.depression;
        barra.SetValor(actual / (float)Mathf.Max(1, maximo));

        if (textoValor != null) textoValor.text = actual.ToString();
    }
}