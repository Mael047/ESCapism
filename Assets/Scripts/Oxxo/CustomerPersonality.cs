using UnityEngine;

/// <summary>Rasgos especiales que puede tener un cliente.</summary>
public enum Quirk
{
    Ninguno,
    Apurado,         // poca paciencia, pero deja más propina si lo atiendes rápido
    Paciente,        // mucha paciencia
    Generoso,        // deja mucha más propina
    Tacano,          // casi no deja propina
    Ordenado,        // el bono por acomodar bien la bolsa vale el doble
    BilleteGrande,   // siempre paga con el billete más grande (cambio grande)
    PagaConMonedas   // siempre agrega monedas (cambio más difícil)
}

/// <summary>
/// Personalidad aleatoria del cliente. Va en la raíz del prefab del NPC, junto a Customer y NPCAppearance.
/// Cada vez que aparece un cliente, se generan sus valores al azar.
/// </summary>
public class CustomerPersonality : MonoBehaviour
{
    [Header("Variación de todos los clientes")]
    [Tooltip("Rango al azar del multiplicador de paciencia (x = mínimo, y = máximo).")]
    public Vector2 rangoPaciencia = new Vector2(0.85f, 1.15f);

    [Tooltip("Rango al azar del multiplicador de propina (x = mínimo, y = máximo).")]
    public Vector2 rangoPropina = new Vector2(0.8f, 1.2f);

    [Header("Rasgos especiales")]
    [Tooltip("Probabilidad de que el cliente tenga un rasgo especial.")]
    [Range(0f, 1f)] public float probabilidadRasgo = 0.5f;

    [Tooltip("Rasgos que pueden salir. Quita los que no quieras usar.")]
    public Quirk[] rasgosPosibles =
    {
        Quirk.Apurado, Quirk.Paciente, Quirk.Generoso, Quirk.Tacano,
        Quirk.Ordenado, Quirk.BilleteGrande, Quirk.PagaConMonedas
    };

    public Quirk Rasgo { get; private set; }
    public float MultiplicadorPaciencia { get; private set; } = 1f;
    public float MultiplicadorPropina { get; private set; } = 1f;
    public float MultiplicadorBonoBolsa { get; private set; } = 1f;
    /// <summary>Lo que dice al llegar (pista de su rasgo para el jugador). Vacío si no tiene.</summary>
    public string Saludo { get; private set; } = "";

    public bool PagaConBilleteGrande => Rasgo == Quirk.BilleteGrande;
    public bool PagaConMonedas => Rasgo == Quirk.PagaConMonedas;

    void Awake() => Generar();

    /// <summary>Genera valores y rasgo nuevos al azar.</summary>
    public void Generar()
    {
        MultiplicadorPaciencia = Random.Range(rangoPaciencia.x, rangoPaciencia.y);
        MultiplicadorPropina = Random.Range(rangoPropina.x, rangoPropina.y);
        MultiplicadorBonoBolsa = 1f;
        Saludo = "";

        Rasgo = Quirk.Ninguno;
        if (rasgosPosibles != null && rasgosPosibles.Length > 0 && Random.value < probabilidadRasgo)
            Rasgo = rasgosPosibles[Random.Range(0, rasgosPosibles.Length)];

        switch (Rasgo)
        {
            case Quirk.Apurado:
                MultiplicadorPaciencia *= 0.65f;
                MultiplicadorPropina *= 1.3f;
                Saludo = "¡Rápido, que tengo prisa!";
                break;
            case Quirk.Paciente:
                MultiplicadorPaciencia *= 1.5f;
                Saludo = "Con calma, no hay prisa.";
                break;
            case Quirk.Generoso:
                MultiplicadorPropina *= 1.75f;
                Saludo = "¡Hoy ando de buenas!";
                break;
            case Quirk.Tacano:
                MultiplicadorPropina *= 0.4f;
                Saludo = "Ni creas que dejo propina...";
                break;
            case Quirk.Ordenado:
                MultiplicadorBonoBolsa = 2f;
                Saludo = "Acomódame bien la bolsa, por favor.";
                break;
            case Quirk.BilleteGrande:
                Saludo = "Solo traigo billetes grandes.";
                break;
            case Quirk.PagaConMonedas:
                Saludo = "Le pago con morralla, ¿va?";
                break;
        }
    }
}