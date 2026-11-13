using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuActions : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Pantalla de carga sin girar (ej. Portada → Contexto)
    public void LoadSceneWithTransition(string sceneName)
    {
        SceneTransition.Load(sceneName);
    }

    // Entra al día con la pantalla de carga (gira al lado del día)
    public void StartGame(string sceneName)
    {
        SceneTransition.Load(sceneName, TransitionSide.Day);
    }

    // Para el botón Opciones: arrastra el MenuOpciones de la escena al campo del evento
    public void OpenOptions(OptionsMenu options)
    {
        if (options != null) options.Open();
    }

    public void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}