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
        Data.activeTemporaryUpgrades.Clear();

        // Completó las 10 oleadas: final
        if (!playerDied)
        {
            EvaluateEndings(completedAllWaves: true);
            return;
        }

        // Murió en el último día: final
        if (CurrentCycle >= maxCycles)
        {
            EvaluateEndings(completedAllWaves: false);
            return;
        }

        // Murió: despierta en el día siguiente
        CurrentCycle++;
        ChangeState(GameState.Day);
        UnlockCursor();
        SceneManager.LoadScene(dayScene);
    }

    private void EvaluateEndings(bool completedAllWaves)
    {
        ChangeState(GameState.Ending);
        UnlockCursor();

        // TODO SCRUM-24: elegir el final según completedAllWaves y Data.emotionalStats
        Debug.Log($"[GameManager] Evaluar finales. Completó todas las oleadas: {completedAllWaves}");
    }

    private void UnlockCursor()
    {
        // La escena de Noche bloquea el cursor; la tienda y los finales lo necesitan
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
        GUI.Label(new Rect(10, 50, 400, 25), "[N] Noche  [M] Morir  [B] Completar noche");
    }




}
