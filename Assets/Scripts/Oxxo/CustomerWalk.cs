using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Caminado del cliente al estilo "South Park": el cuerpo entero se balancea de un lado a otro
/// y da un saltito en cada paso, sin necesidad de dibujar cuadros de caminata.
/// Va en la RAÍZ del prefab del NPC (con el pivote en los pies, para que se meza desde abajo).
/// Customer lo usa solo: entra al aparecer, avanza en la fila y sale al irse.
/// </summary>
public class CustomerWalk : MonoBehaviour
{
    [Header("Recorrido")]
    [Tooltip("Lado por el que entra: 1 = derecha, -1 = izquierda.")]
    public float ladoEntrada = 1f;

    [Tooltip("Lado por el que sale: 1 = derecha, -1 = izquierda.")]
    public float ladoSalida = -1f;

    [Tooltip("Distancia que camina para entrar o salir de la pantalla (unidades del mundo).")]
    public float distancia = 8f;

    [Tooltip("Velocidad al caminar (unidades por segundo).")]
    public float velocidad = 4f;

    [Header("Caminado tipo South Park")]
    [Tooltip("Pasos por segundo.")]
    public float pasosPorSegundo = 3.5f;

    [Tooltip("Inclinación máxima hacia cada lado, en grados.")]
    public float inclinacion = 7f;

    [Tooltip("Altura del saltito en cada paso (unidades del mundo).")]
    public float saltito = 0.12f;

    [Tooltip("Segundos que tarda en quedarse quieto al llegar.")]
    public float duracionFrenado = 0.2f;

    public bool Caminando { get; private set; }

    Vector3 escalaBase;
    Vector3 posBase;
    float escalaActual = 1f;

    void Awake()
    {
        escalaBase = transform.localScale;
        posBase = transform.position;
    }

    /// <summary>Entra caminando desde fuera de la pantalla hasta su posición actual, con el tamaño indicado.</summary>
    public void Entrar(float escala, Action alLlegar)
    {
        escalaActual = escala;
        Vector3 destino = transform.position;
        Vector3 inicio = destino + Vector3.right * Mathf.Sign(ladoEntrada) * distancia;

        StopAllCoroutines();
        StartCoroutine(Recorrido(inicio, destino, escala, escala, alLlegar));
    }

    /// <summary>Camina hasta 'destino' (por ejemplo, avanzar en la fila) cambiando de tamaño poco a poco.</summary>
    public void MoverA(Vector3 destino, float escala, Action alLlegar)
    {
        StopAllCoroutines();
        StartCoroutine(Recorrido(posBase, destino, escalaActual, escala, alLlegar));
    }

    /// <summary>Sale caminando de la pantalla. Devuelve cuántos segundos tarda.</summary>
    public float Salir(Action alSalir)
    {
        Vector3 destino = posBase + Vector3.right * Mathf.Sign(ladoSalida) * distancia;

        StopAllCoroutines();
        StartCoroutine(Recorrido(posBase, destino, escalaActual, escalaActual, alSalir));
        return Duracion();
    }

    /// <summary>Segundos que tarda en recorrer la distancia completa.</summary>
    public float Duracion() => distancia / Mathf.Max(0.01f, velocidad) + duracionFrenado;

    IEnumerator Recorrido(Vector3 desde, Vector3 hasta, float escalaDesde, float escalaHasta, Action alTerminar)
    {
        Caminando = true;
        posBase = desde;
        float total = Vector3.Distance(desde, hasta);
        float fase = 0f;
        Aplicar(fase, 1f);

        // Camina meciéndose
        while ((posBase - hasta).sqrMagnitude > 0.0001f)
        {
            posBase = Vector3.MoveTowards(posBase, hasta, velocidad * Time.deltaTime);
            float avance = total > 0.0001f ? 1f - Vector3.Distance(posBase, hasta) / total : 1f;
            escalaActual = Mathf.Lerp(escalaDesde, escalaHasta, avance);

            fase += Time.deltaTime * pasosPorSegundo * Mathf.PI;
            Aplicar(fase, 1f);
            yield return null;
        }

        posBase = hasta;
        escalaActual = escalaHasta;

        // Frena: el balanceo se apaga suavemente
        for (float t = 0f; t < duracionFrenado; t += Time.deltaTime)
        {
            fase += Time.deltaTime * pasosPorSegundo * Mathf.PI;
            Aplicar(fase, 1f - t / duracionFrenado);
            yield return null;
        }

        Aplicar(0f, 0f);
        Caminando = false;
        alTerminar?.Invoke();
    }

    /// <summary>Inclina hacia un lado y luego al otro; sube un poco en cada inclinación.</summary>
    void Aplicar(float fase, float intensidad)
    {
        float s = Mathf.Sin(fase);
        float giro = s * inclinacion * intensidad;
        float salto = Mathf.Abs(s) * saltito * intensidad;

        transform.SetPositionAndRotation(posBase + Vector3.up * salto, Quaternion.Euler(0f, 0f, giro));
        transform.localScale = escalaBase * escalaActual;
    }
}