using UnityEngine;

// Marca a un enemigo como jefe: el WaveManager avisa al HUD para mostrar su barra de vida.
public class BossMarker : MonoBehaviour
{
    [SerializeField] private string displayName = "Murciélago Rey";

    public string DisplayName => displayName;
}
