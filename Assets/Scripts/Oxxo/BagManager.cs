using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maneja varias bolsas: pedir otra bolsa y moverse entre ellas.
/// Conecta los botones de la UI a AgregarBolsa(), Siguiente() y Anterior().
/// </summary>
public class BagManager : MonoBehaviour
{
    [Tooltip("Prefab de la bolsa (un objeto con BagGrid).")]
    public BagGrid prefabBolsa;

    [Tooltip("Dónde aparece la bolsa (esquina inferior izquierda de la grilla).")]
    public Transform puntoBolsa;

    [Min(1)] public int maxBolsas = 10;

    /// <summary>(índice actual, total de bolsas). Útil para un texto tipo "Bolsa 2/3".</summary>
    public event Action<int, int> OnBolsaCambiada;

    readonly List<BagGrid> bolsas = new List<BagGrid>();
    int indice = -1;

    public BagGrid BolsaActual => indice >= 0 && indice < bolsas.Count ? bolsas[indice] : null;
    public IReadOnlyList<BagGrid> Bolsas => bolsas;
    public int TotalBolsas => bolsas.Count;
    public float TamanoCelda => prefabBolsa != null ? prefabBolsa.tamanoCelda : 1f;

    void Start()
    {
        if (bolsas.Count == 0) AgregarBolsa();
    }

    public void AgregarBolsa()
    {
        if (bolsas.Count >= maxBolsas) return;

        Vector3 pos = puntoBolsa != null ? puntoBolsa.position : transform.position;
        var bolsa = Instantiate(prefabBolsa, pos, Quaternion.identity, transform);
        bolsa.name = $"Bolsa {bolsas.Count + 1}";
        bolsas.Add(bolsa);
        Mostrar(bolsas.Count - 1);
    }

    public void Siguiente()
    {
        if (bolsas.Count > 0) Mostrar((indice + 1) % bolsas.Count);
    }

    public void Anterior()
    {
        if (bolsas.Count > 0) Mostrar((indice - 1 + bolsas.Count) % bolsas.Count);
    }

    void Mostrar(int i)
    {
        indice = i;
        // Las bolsas ocultas esconden también sus productos (son hijos de la bolsa)
        for (int k = 0; k < bolsas.Count; k++) bolsas[k].gameObject.SetActive(k == i);
        OnBolsaCambiada?.Invoke(indice, bolsas.Count);
    }

    // --- Datos para la puntuación ---

    public int BolsasUsadas
    {
        get
        {
            int n = 0;
            foreach (var b in bolsas) if (!b.EstaVacia) n++;
            return n;
        }
    }

    /// <summary>Celdas ocupadas / celdas totales, contando solo las bolsas usadas (0 a 1).</summary>
    public float OcupacionPromedio
    {
        get
        {
            int total = 0, ocupadas = 0;
            foreach (var b in bolsas)
            {
                if (b.EstaVacia) continue;
                total += b.TotalCeldas;
                ocupadas += b.CeldasOcupadas;
            }
            return total == 0 ? 0f : (float)ocupadas / total;
        }
    }

    /// <summary>Destruye todas las bolsas (con sus productos) y deja una vacía para el siguiente cliente.</summary>
    public void ReiniciarParaNuevoCliente()
    {
        foreach (var b in bolsas) Destroy(b.gameObject);
        bolsas.Clear();
        indice = -1;
        AgregarBolsa();
    }
}