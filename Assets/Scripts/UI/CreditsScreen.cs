using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Pantalla de créditos. Escribe el texto en el Inspector.
// Formato:
//   # Título        → encabezado grande y rosa (arriba, centrado)
//   ## Sección      → en negrita; cada sección va completa a una columna
//   (línea vacía)   → espacio
// Si hay columnas asignadas, las secciones se reparten solas entre ellas (izquierda → derecha).
// También acepta etiquetas de TextMeshPro como <b>, <i> o <color=#F33283>.
// Crea la escena con: Tools → Escenas → Crear escena de Créditos
public class CreditsScreen : MonoBehaviour
{
    [Header("Texto")]
    [Tooltip("Para las líneas que empiezan con # (opcional)")]
    [SerializeField] private TMP_Text headerText;
    [Tooltip("Columnas de izquierda a derecha. Vacío = todo en Credits Text")]
    [SerializeField] private TMP_Text[] columns;
    [Tooltip("Qué tanto cabe en cada columna, en el mismo orden (la del centro es más baja por los personajes)")]
    [SerializeField] private float[] columnWeights = { 1f, 0.6f, 1f };
    [Tooltip("Se usa si no hay columnas")]
    [SerializeField] private TMP_Text creditsText;
    [TextArea(12, 40)]
    [SerializeField] private string credits =
        "# MLT Games Studio\n" +
        "\n" +
        "## Programación\n" +
        "Nombre Apellido\n" +
        "\n" +
        "## Arte e ilustración\n" +
        "Nombre Apellido\n" +
        "\n" +
        "## Diseño y narrativa\n" +
        "Nombre Apellido\n" +
        "\n" +
        "## Música y sonido\n" +
        "Nombre Apellido\n" +
        "\n" +
        "## Agradecimientos\n" +
        "Fuente Pixelify Sans (SIL Open Font License)";

    [SerializeField] private Color titleColor = new Color(0.953f, 0.196f, 0.514f);   // #F33283
    [SerializeField] private float titleScale = 1.4f;
    [SerializeField] private float subtitleScale = 1.1f;

    [Header("Personajes")]
    [Tooltip("Imagen con los personajes (se mueve suavemente arriba y abajo)")]
    [SerializeField] private RectTransform characters;
    [SerializeField] private float bobPixels = 8f;
    [SerializeField] private float bobSpeed = 1.5f;

    [Header("Volver")]
    [SerializeField] private string mainMenuScene = "Portada";
    [Tooltip("Activo: vuelve con la pantalla de carga. Inactivo: cambia de escena directo")]
    [SerializeField] private bool useTransition = false;

    private Vector2 charactersBase;

    private void Start()
    {
        Apply();
        if (characters != null) charactersBase = characters.anchoredPosition;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
    }

    // Para ver los cambios del texto en el editor sin dar Play
    private void OnValidate()
    {
        Apply();
    }

    private void Apply()
    {
        if (columns == null || columns.Length == 0)
        {
            if (creditsText != null) creditsText.text = Format(credits);
            return;
        }
        Distribute();
    }

    // Separa el texto en encabezado (#) y secciones (##), y reparte las secciones en las columnas
    private void Distribute()
    {
        var header = new StringBuilder();
        var sections = new List<StringBuilder>();
        StringBuilder current = null;

        foreach (string raw in credits.Replace("\r", "").Split('\n'))
        {
            string line = raw.TrimEnd();
            if (line.StartsWith("# "))
            {
                header.AppendLine(line);
            }
            else if (line.StartsWith("## "))
            {
                current = new StringBuilder();
                current.AppendLine(line);
                sections.Add(current);
            }
            else if (current != null)
            {
                current.AppendLine(line);
            }
            else if (line.Length > 0)
            {
                header.AppendLine(line);   // texto antes de la primera sección
            }
        }

        if (headerText != null) headerText.text = Format(header.ToString().Trim());

        // Texto y número de líneas de cada sección
        var blocks = new List<string>();
        var lines = new List<int>();
        int totalLines = 0;
        foreach (StringBuilder b in sections)
        {
            string text = b.ToString().Trim();
            blocks.Add(text);
            int n = text.Split('\n').Length + 1;
            lines.Add(n);
            totalLines += n;
        }

        float totalWeight = 0f;
        for (int c = 0; c < columns.Length; c++) totalWeight += Weight(c);

        var output = new StringBuilder[columns.Length];
        for (int c = 0; c < columns.Length; c++) output[c] = new StringBuilder();

        int column = 0;
        float filled = 0f;
        for (int i = 0; i < blocks.Count; i++)
        {
            float target = totalLines * Weight(column) / totalWeight;
            int remainingSections = blocks.Count - i;
            int remainingColumns = columns.Length - column - 1;

            // Pasa a la siguiente columna si esta ya se llenó, o si hace falta para no dejar columnas vacías
            bool full = filled + lines[i] / 2f > target;
            bool needed = remainingSections <= remainingColumns;
            if (filled > 0f && column < columns.Length - 1 && (full || needed))
            {
                column++;
                filled = 0f;
            }

            if (output[column].Length > 0) output[column].Append("\n\n");
            output[column].Append(blocks[i]);
            filled += lines[i];
        }

        for (int c = 0; c < columns.Length; c++)
            if (columns[c] != null) columns[c].text = Format(output[c].ToString());
    }

    private float Weight(int column)
    {
        return columnWeights != null && column < columnWeights.Length ? Mathf.Max(0.1f, columnWeights[column]) : 1f;
    }

    private void Update()
    {
        // Movimiento suave en pasos de "píxel" para que combine con el pixel art
        if (characters != null && bobPixels > 0f)
        {
            float offset = Mathf.Round(Mathf.Sin(Time.time * bobSpeed) * bobPixels / 4f) * 4f;
            characters.anchoredPosition = charactersBase + new Vector2(0f, offset);
        }

        Keyboard k = Keyboard.current;
        Gamepad g = Gamepad.current;
        if ((k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.buttonEast.wasPressedThisFrame))
            BackToMenu();
    }

    private string Format(string raw)
    {
        string hex = ColorUtility.ToHtmlStringRGB(titleColor);
        var sb = new StringBuilder();

        foreach (string line in raw.Replace("\r", "").Split('\n'))
        {
            if (line.StartsWith("## "))
                sb.Append($"<size={subtitleScale * 100f:0}%><b>{line.Substring(3)}</b></size>");
            else if (line.StartsWith("# "))
                sb.Append($"<size={titleScale * 100f:0}%><color=#{hex}><b>{line.Substring(2)}</b></color></size>");
            else
                sb.Append(line);
            sb.Append('\n');
        }
        return sb.ToString().TrimEnd('\n');
    }

    // Botón Volver
    public void BackToMenu()
    {
        if (SceneTransition.IsTransitioning) return;

        if (useTransition) SceneTransition.Load(mainMenuScene);
        else SceneManager.LoadScene(mainMenuScene);
    }
}
