using System;
using UnityEngine;
using UnityEngine.Audio;

// Ajustes del jugador guardados entre partidas (PlayerPrefs).
// Los usan el menú de opciones, la pausa y la cámara.
public static class GameSettings
{
    public const string MusicParameter = "MusicVolume";   // parámetros expuestos del MainMixer
    public const string SfxParameter = "SFXVolume";

    private const string MusicKey = "Vol_Music";
    private const string SfxKey = "Vol_SFX";
    private const string MouseKey = "Sens_Mouse";
    private const string StickKey = "Sens_Stick";
    private const string FullscreenKey = "Fullscreen";

    public const float MinSensitivity = 0.2f;
    public const float MaxSensitivity = 3f;

    public static event Action Changed;

    private static AudioMixer mixer;
    private static bool loaded;
    private static float music, sfx, mouse, stick;
    private static bool fullscreen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        loaded = false;
        mixer = null;
        Changed = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyOnStart()
    {
        Load();
        ApplyDisplay();
    }

    private static void Load()
    {
        if (loaded) return;
        loaded = true;
        music = PlayerPrefs.GetFloat(MusicKey, 0.7f);
        sfx = PlayerPrefs.GetFloat(SfxKey, 0.8f);
        mouse = PlayerPrefs.GetFloat(MouseKey, 1f);
        stick = PlayerPrefs.GetFloat(StickKey, 1f);
        fullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
    }

    // ---------- Valores ----------

    public static float MusicVolume
    {
        get { Load(); return music; }
        set { music = Mathf.Clamp01(value); Save(MusicKey, music); ApplyAudio(); Changed?.Invoke(); }
    }

    public static float SfxVolume
    {
        get { Load(); return sfx; }
        set { sfx = Mathf.Clamp01(value); Save(SfxKey, sfx); ApplyAudio(); Changed?.Invoke(); }
    }

    // Multiplicador de la sensibilidad del mouse (1 = la del CameraLook)
    public static float MouseSensitivity
    {
        get { Load(); return mouse; }
        set { mouse = Mathf.Clamp(value, MinSensitivity, MaxSensitivity); Save(MouseKey, mouse); Changed?.Invoke(); }
    }

    // Multiplicador de la sensibilidad del control
    public static float StickSensitivity
    {
        get { Load(); return stick; }
        set { stick = Mathf.Clamp(value, MinSensitivity, MaxSensitivity); Save(StickKey, stick); Changed?.Invoke(); }
    }

    public static bool Fullscreen
    {
        get { Load(); return fullscreen; }
        set
        {
            fullscreen = value;
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            PlayerPrefs.Save();
            ApplyDisplay();
            Changed?.Invoke();
        }
    }

    // ---------- Aplicar ----------

    // Lo llaman la pausa y las opciones (tienen la referencia al mixer)
    public static void RegisterMixer(AudioMixer audioMixer)
    {
        if (audioMixer != null) mixer = audioMixer;
        ApplyAudio();
    }

    public static void ApplyAudio()
    {
        Load();
        bool musicOk = mixer != null && mixer.SetFloat(MusicParameter, ToDecibels(music));
        if (!musicOk && MusicManager.Instance != null) MusicManager.Instance.SetVolume(music);
        if (mixer != null) mixer.SetFloat(SfxParameter, ToDecibels(sfx));
    }

    private static void ApplyDisplay()
    {
        Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
    }

    // Slider 0-1 → decibeles del mixer (-80 = silencio, 0 = normal)
    public static float ToDecibels(float value)
    {
        return value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
    }

    private static void Save(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
    }
}
