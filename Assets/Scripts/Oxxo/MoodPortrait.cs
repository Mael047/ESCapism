using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cambia el retrato del personaje según su estrés (ansiedad) y depresión:
/// - Normal: los dos por debajo de la mitad.
/// - Triste: cualquiera de los dos llega a la mitad.
/// - Enojada: los dos pasan del 60.
/// Va en la Image del retrato (tu objeto "Emociones").
/// </summary>
[RequireComponent(typeof(Image))]
public class MoodPortrait : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite normal;
    public Sprite triste;
    public Sprite enojada;

    [Header("Cuándo cambia (valores de 0 a 100)")]
    [Tooltip("Se pone triste si la ansiedad O la depresión llegan a este valor.")]
    public int umbralTriste = 50;

    [Tooltip("Se enoja si la ansiedad Y la depresión pasan de este valor.")]
    public int umbralEnojada = 60;

    [Header("Referencias")]
    [Tooltip("El MoodTracker de la escena. Si se deja vacío, lo busca solo.")]
    public MoodTracker tracker;

    [Header("Animación")]
    [Tooltip("Hace un pequeño 'pop' al cambiar de expresión.")]
    public bool animar = true;

    Image imagen;
    Vector3 escalaBase;

    void Awake()
    {
        imagen = GetComponent<Image>();
        escalaBase = transform.localScale;
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
        transform.localScale = escalaBase;
    }

    /// <summary>Lee la ansiedad y la depresión y pone la expresión que corresponde.</summary>
    public void Refrescar()
    {
        var datos = PermanentUpgrades.Data;
        if (datos == null || imagen == null) return;

        int ansiedad = datos.stress;
        int depresion = datos.depression;

        Sprite elegido;
        if (ansiedad > umbralEnojada && depresion > umbralEnojada) elegido = enojada;
        else if (ansiedad >= umbralTriste || depresion >= umbralTriste) elegido = triste;
        else elegido = normal;

        if (elegido == null || imagen.sprite == elegido) return;

        imagen.sprite = elegido;
        if (animar && isActiveAndEnabled)
        {
            StopAllCoroutines();
            StartCoroutine(Pop());
        }
    }

    IEnumerator Pop()
    {
        const float duracion = 0.2f;
        for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.Sin(t / duracion * Mathf.PI);
            transform.localScale = escalaBase * (1f + 0.12f * k);
            yield return null;
        }
        transform.localScale = escalaBase;
    }
}