using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

//Estados del juego 
public enum GameState
{
    Day,
    Night,
    NightEnd,
    Ending
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Escenas")]
    [SerializeField] private string dayScene = "Nombre_Escena_Dia";
    [SerializeField] private string nightScene = "Nombre_Escena_Noche";

    [Header("BUcle")]
    [SerializeField] private int maxCycles = 2;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    public GameState CurrentState { get; private set; } = GameState.Day;
    public int CurrentCycle { get; private set; } = 1;
    public RunData Data { get; private set; } = new RunData();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Llamar desde la tienda cuando el jugador termina el día
    public void StartNight()
    {
        if (CurrentState != GameState.Day) return;

        ChangeState(GameState.Night);
        SceneManager.LoadScene(nightScene);
    }

    // Llamar al morir o al sobrevivir la noche
    public void EndNight(bool playerDied)
    {
        if (CurrentState != GameState.Night) return;

        ChangeState(GameState.NightEnd);

        //Para cuando hayan finales o muertes
        if (playerDied)
        {
            // En caso de morir
        }

        // ¿Se cerró el último ciclo?
        if (CurrentCycle >= maxCycles)
        {
            EvaluateEndings();
            return;
        }

        CurrentCycle++;
        ChangeState(GameState.Day);
        SceneManager.LoadScene(dayScene);
    }

    private void EvaluateEndings()
    {
        ChangeState(GameState.Ending);
        // Para definir los finales en base a las estadisticas del personaje 
        Debug.Log("[GameManager] Fin del ciclo 2: evaluar finales");
    }

    private void ChangeState(GameState newState)
    {
        Debug.Log($"[GameManager] {CurrentState} → {newState} (Ciclo {CurrentCycle})");
        CurrentState = newState;
    }

    private void Update()
    {
        if (!showDebug || Keyboard.current == null) return;

        // Atajos temporales para probar el bucle sin gameplay
        if (Keyboard.current.nKey.wasPressedThisFrame) StartNight();
        if (Keyboard.current.mKey.wasPressedThisFrame) EndNight(playerDied: true);
        if (Keyboard.current.bKey.wasPressedThisFrame) EndNight(playerDied: false);
    }

    private void OnGUI()
    {
        if (!showDebug) return;

        GUI.Label(new Rect(10, 10, 400, 25), $"Ciclo: {CurrentCycle}/{maxCycles}  |  Estado: {CurrentState}");
        GUI.Label(new Rect(10, 30, 400, 25), $"Dinero: {Data.money}  |  Drops: {Data.drop}");
        GUI.Label(new Rect(10, 50, 400, 25), "[N] Noche  [M] Morir  [B] Sobrevivir");
    }




}
