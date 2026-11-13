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

// Finales del juego
public enum EndingType
{
    None = 0,
    Good = 1,        // Final 1: completó las oleadas con el ánimo bien
    Stressed = 2,    // Final 2: completó las oleadas con estrés o depresión por encima de 50
    Collapse = 3     // Final 3: estrés y depresión en 80 o más al terminar un día o una noche
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Escenas")]
    [SerializeField] private string dayScene = "Nombre_Escena_Dia";
    [SerializeField] private string nightScene = "Nombre_Escena_Noche";

    [Header("BUcle")]
    [SerializeField] private int maxCycles = 2;

    [Header("Finales")]
    [SerializeField] private string endingScene = "Finales";
    [Tooltip("Final 2 si estrés o depresión están POR ENCIMA de este valor al completar las oleadas")]
    [SerializeField] private int stressedThreshold = 50;
    [Tooltip("Final 3 si estrés Y depresión llegan a este valor al terminar un día o una noche")]
    [SerializeField] private int collapseThreshold = 80;
    [Tooltip("Qué final sale si muere en la última noche sin completar las oleadas")]
    [SerializeField] private EndingType endingOnLastDeath = EndingType.Collapse;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    public GameState CurrentState { get; private set; } = GameState.Day;
    public int CurrentCycle { get; private set; } = 1;
    public RunData Data { get; private set; } = new RunData();
    public EndingType Ending { get; private set; } = EndingType.None;

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

        // Final 3: terminó el día con estrés y depresión al límite
        if (IsCollapsed())
        {
            TriggerEnding(EndingType.Collapse);
            return;
        }

        ChangeState(GameState.Night);
        SceneTransition.Load(nightScene, TransitionSide.Night);
    }

    // Llamar al morir o al sobrevivir la noche
    public void EndNight(bool playerDied)
    {
        if (CurrentState != GameState.Night) return;

        Debug.Log($"[GameManager] Fin de la noche {CurrentCycle}: {(playerDied ? "MURIÓ" : "COMPLETÓ LAS OLEADAS")} " +
                  $"(oleada alcanzada {Data.nightWaveReached}). Haz clic en este mensaje para ver quién lo llamó.");
        ChangeState(GameState.NightEnd);
        Data.activeTemporaryUpgrades.Clear();

        // Final 3: terminó la noche con estrés y depresión al límite (gane o pierda)
        if (IsCollapsed())
        {
            TriggerEnding(EndingType.Collapse);
            return;
        }

        // Completó las oleadas: final 1 o 2 según el ánimo
        if (!playerDied)
        {
            bool stressed = Data.stress > stressedThreshold || Data.depression > stressedThreshold;
            TriggerEnding(stressed ? EndingType.Stressed : EndingType.Good);
            return;
        }

        // Murió en el último día: final
        if (CurrentCycle >= maxCycles)
        {
            TriggerEnding(endingOnLastDeath);
            return;
        }

        // Murió: despierta en el día siguiente
        CurrentCycle++;
        ChangeState(GameState.Day);
        UnlockCursor();
        SceneTransition.Load(dayScene, TransitionSide.Day);
    }

    private bool IsCollapsed()
    {
        return Data.stress >= collapseThreshold && Data.depression >= collapseThreshold;
    }

    private void TriggerEnding(EndingType ending)
    {
        Ending = ending;
        ChangeState(GameState.Ending);
        UnlockCursor();

        Debug.Log($"[GameManager] Final {(int)ending} ({ending}). Estrés {Data.stress} | Depresión {Data.depression}");
        SceneTransition.Load(endingScene, TransitionSide.Night);
    }

    // Para volver a jugar desde cero (lo usa la pantalla de finales)
    public void ResetRun()
    {
        Data = new RunData();
        CurrentCycle = 1;
        Ending = EndingType.None;
        ChangeState(GameState.Day);
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

        // Las teclas de debug solo funcionan con Shift, para no presionarlas sin querer
        if (!Keyboard.current.shiftKey.isPressed) return;

        // Atajos temporales para probar el bucle sin gameplay
        if (Keyboard.current.nKey.wasPressedThisFrame) StartNight();
        if (Keyboard.current.mKey.wasPressedThisFrame) EndNight(playerDied: true);
        if (Keyboard.current.bKey.wasPressedThisFrame) EndNight(playerDied: false);
        // [V] subir estrés y depresión a 85 para probar el final 3
        if (Keyboard.current.vKey.wasPressedThisFrame) { Data.stress = 85; Data.depression = 85; }
    }

    private void OnGUI()
    {
        if (!showDebug) return;

        GUI.Label(new Rect(10, 10, 400, 25), $"Ciclo: {CurrentCycle}/{maxCycles}  |  Estado: {CurrentState}");
        GUI.Label(new Rect(10, 30, 600, 25),
    $"Dinero: {Data.money}  |  Estrés: {Data.stress}  |  Depresión: {Data.depression}  |  Días buenos: {Data.goodDaysStreak}");
        GUI.Label(new Rect(10, 50, 400, 25), "Shift + [N] Noche  [M] Morir  [B] Completar noche  [V] Ánimo 85");
    }

    //Dinero de la jornada de trabajo_DIA
    // Suma dinero a la partida (en centavos: 8474 = $84.74)
    public void AddMoney(int amount)
    {
        Data.money += amount;
    }
}
