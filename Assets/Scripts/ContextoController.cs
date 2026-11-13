using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pantalla de Contexto de ESCapism: fondo negro + texto + efectos + audio.
/// Cada "beat" es una escena o corte.
/// Las imágenes son opcionales: asigna un Sprite en el campo "fondo" de un beat.
/// </summary>
public class ContextoController : MonoBehaviour
{
    public enum Aparicion { FadeFrases, MaquinaDeEscribir, Instantanea }

    [System.Serializable]
    public class Beat
    {
        public string nombre;

        [Header("Texto")]
        [TextArea(2, 5)] public string[] frases;
        public Aparicion aparicion = Aparicion.FadeFrases;
        public TMP_FontAsset fuente;          // vacío = usa la fuente del TMP
        public Color color = Color.white;
        public float tamano = 48f;
        public float fadeFrase = 1f;          // FadeFrases: duración del fade de cada frase
        public float letrasPorSegundo = 30f;  // MaquinaDeEscribir
        public float pausaEntreFrases = 0.8f;

        [Header("Tiempos")]
        public float silencioAntes = 0f;      // negro y silencio antes de empezar
        public float mantener = 1.5f;         // tiempo con el texto completo
        public float fadeSalida = 0.5f;

        [Header("Efectos")]
        public bool flash;
        public Color colorFlash = Color.white;
        public float temblor = 0f;            // en píxeles
        public float zoomFinal = 1f;          // 1 = sin zoom; >1 acerca; <1 aleja
        public float duracionZoom = 8f;
        public float glitchEnSegundo = -1f;   // -1 = sin glitch
        public bool pulso;                    // latido lento del texto
        public Sprite fondo;                  // opcional: imagen detrás del texto

        [Header("Audio")]
        public AudioClip musica;
        public bool detenerMusica;
        public float pitchMusica = 1f;        // se aplica solo si este beat cambia la música
        public AudioClip ambiente;
        public bool detenerAmbiente;
        public AudioClip sfx;
    }

    [Header("Referencias")]
    public TMP_Text texto;
    public CanvasGroup grupoTexto;
    public Image fondo;          // Image a pantalla completa (opcional)
    public Image flashImg;       // Image a pantalla completa, blanca, encima de todo (opcional)
    public AudioSource musicaSrc;
    public AudioSource ambienteSrc;
    public AudioSource sfxSrc;
    public Button botonComenzar; // opcional
    public Button botonOmitir;   // opcional

    [Header("Ritmo de lectura (global)")]
    [Tooltip("1 = tiempos del beat tal cual. 1.3 = 30% más lento. Afecta fades, pausas, tiempo en pantalla y velocidad de escritura.")]
    [Range(0.5f, 2.5f)] public float lentitud = 1.3f;
    public bool permitirZoom = false;   // si está apagado, ignora Zoom Final de los beats
    public bool permitirPulso = false;  // si está apagado, ignora Pulso de los beats

    [Header("Fade de audio (global)")]
    public float fadeOutMusica = 2f;     // 0 = corte seco
    public float fadeOutAmbiente = 1.5f; // 0 = corte seco

    [Header("Guion")]
    public List<Beat> beats = new List<Beat>();

    [Header("Al terminar")]
    public UnityEvent alTerminar; // aquí conecta la carga del tutorial

    RectTransform rt;
    Vector2 posBase;
    Vector2 glitchOffset;
    bool terminado;

    void Reset() { CargarGuion(); }

    void Start()
    {
        rt = texto.rectTransform;
        posBase = rt.anchoredPosition;
        texto.text = "";
        grupoTexto.alpha = 0f;
        if (flashImg) flashImg.color = new Color(1, 1, 1, 0);
        if (fondo) fondo.color = new Color(1, 1, 1, 0);
        if (botonComenzar)
        {
            botonComenzar.gameObject.SetActive(false);
            botonComenzar.onClick.AddListener(Terminar);
        }
        // Omitir funciona aunque no haya botón Comenzar
        if (botonOmitir) botonOmitir.onClick.AddListener(Terminar);
        StartCoroutine(Secuencia());
    }

    IEnumerator Secuencia()
    {
        foreach (var b in beats) yield return Reproducir(b);

        if (botonComenzar) botonComenzar.gameObject.SetActive(true);
        else Terminar();
    }

   void Terminar()
{
    if (terminado) return;   // evita que Omitir y Continuar se disparen dos veces
    terminado = true;
    StopAllCoroutines();
    if (musicaSrc) musicaSrc.Stop();
    if (ambienteSrc) ambienteSrc.Stop();
    if (botonOmitir) botonOmitir.gameObject.SetActive(false);
    alTerminar.Invoke();
}
    IEnumerator Reproducir(Beat b)
    {
        float k = Mathf.Max(0.1f, lentitud);

        texto.text = "";
        texto.maxVisibleCharacters = 99999;
        grupoTexto.alpha = 0f;

        if (b.silencioAntes > 0f) yield return new WaitForSeconds(b.silencioAntes);

        AplicarAudio(b);
        if (b.flash && flashImg) StartCoroutine(Flash(b.colorFlash));
        StartCoroutine(Fondo(b.fondo));

        if (b.fuente) texto.font = b.fuente;
        texto.color = b.color;
        texto.fontSize = b.tamano;
        rt.localScale = Vector3.one;
        rt.anchoredPosition = posBase;
        grupoTexto.alpha = 1f;

        Coroutine fx = StartCoroutine(Efectos(b));

        string previo = "";
        if (b.frases != null)
        {
            for (int i = 0; i < b.frases.Length; i++)
            {
                string frase = b.frases[i];
                switch (b.aparicion)
                {
                    case Aparicion.FadeFrases:
                        yield return FadeFrase(previo, frase, b.fadeFrase * k);
                        break;
                    case Aparicion.MaquinaDeEscribir:
                        yield return Escribir(previo, frase, b.letrasPorSegundo / k);
                        break;
                    default:
                        texto.text = previo + frase;
                        break;
                }
                previo += frase + "\n";
                if (i < b.frases.Length - 1 && b.pausaEntreFrases > 0f)
                    yield return new WaitForSeconds(b.pausaEntreFrases * k);
            }
        }

        yield return new WaitForSeconds(b.mantener * k);

        // salida
        float t = 0f;
        while (t < b.fadeSalida)
        {
            t += Time.deltaTime;
            grupoTexto.alpha = 1f - Mathf.Clamp01(t / b.fadeSalida);
            yield return null;
        }
        grupoTexto.alpha = 0f;

        StopCoroutine(fx);
        glitchOffset = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.anchoredPosition = posBase;
    }

    // ---------- Texto ----------

    IEnumerator FadeFrase(string previo, string frase, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            int a = Mathf.RoundToInt(255f * Mathf.Clamp01(t / dur));
            texto.text = previo + "<alpha=#" + a.ToString("X2") + ">" + frase;
            yield return null;
        }
        texto.text = previo + frase;
    }

    IEnumerator Escribir(string previo, string frase, float cps)
    {
        texto.text = previo;
        texto.ForceMeshUpdate();
        int inicio = texto.textInfo.characterCount;

        texto.text = previo + frase;
        texto.ForceMeshUpdate();
        int total = texto.textInfo.characterCount;

        float espera = 1f / Mathf.Max(1f, cps);
        for (int i = inicio; i <= total; i++)
        {
            texto.maxVisibleCharacters = i;
            yield return new WaitForSeconds(espera);
        }
        texto.maxVisibleCharacters = 99999;
    }

    // ---------- Efectos ----------

    IEnumerator Efectos(Beat b)
    {
        float t = 0f;
        bool glitchHecho = false;
        while (true)
        {
            t += Time.deltaTime;

            float escala = 1f;
            if (permitirZoom && b.zoomFinal != 1f)
                escala = Mathf.Lerp(1f, b.zoomFinal, Mathf.Clamp01(t / Mathf.Max(0.01f, b.duracionZoom)));
            if (permitirPulso && b.pulso)
                escala *= 1f + 0.01f * Mathf.Sin(t * 3.5f);
            rt.localScale = Vector3.one * escala;

            Vector2 off = glitchOffset;
            if (b.temblor > 0f) off += Random.insideUnitCircle * b.temblor;
            rt.anchoredPosition = posBase + off;

            if (!glitchHecho && b.glitchEnSegundo >= 0f && t >= b.glitchEnSegundo)
            {
                glitchHecho = true;
                StartCoroutine(Glitch());
            }
            yield return null;
        }
    }

    IEnumerator Glitch()
    {
        Color original = texto.color;
        float fin = Time.time + 0.3f;
        while (Time.time < fin)
        {
            glitchOffset = new Vector2(Random.Range(-18f, 18f), Random.Range(-6f, 6f));
            texto.color = Random.value < 0.5f ? new Color(1f, 0.2f, 0.3f) : new Color(0.2f, 1f, 1f);
            yield return new WaitForSeconds(0.04f);
        }
        glitchOffset = Vector2.zero;
        texto.color = original;
    }

    IEnumerator Flash(Color c)
    {
        float t = 0f, dur = 0.35f;
        while (t < dur)
        {
            t += Time.deltaTime;
            flashImg.color = new Color(c.r, c.g, c.b, 1f - t / dur);
            yield return null;
        }
        flashImg.color = new Color(c.r, c.g, c.b, 0f);
    }

    IEnumerator Fondo(Sprite s)
    {
        if (!fondo) yield break;
        float desde = fondo.color.a;
        float hasta = 0f;
        if (s != null) { fondo.sprite = s; hasta = 1f; }
        float t = 0f, dur = 0.6f;
        while (t < dur)
        {
            t += Time.deltaTime;
            fondo.color = new Color(1, 1, 1, Mathf.Lerp(desde, hasta, t / dur));
            yield return null;
        }
        fondo.color = new Color(1, 1, 1, hasta);
    }

    // ---------- Audio ----------

    void AplicarAudio(Beat b)
    {
        if (musicaSrc)
        {
            if (b.detenerMusica)
            {
                if (b.musica || fadeOutMusica <= 0f) musicaSrc.Stop();
                else StartCoroutine(FadeOutYParar(musicaSrc, fadeOutMusica));
            }
            if (b.musica)
            {
                musicaSrc.clip = b.musica;
                musicaSrc.loop = true;
                musicaSrc.pitch = b.pitchMusica;
                musicaSrc.Play();
            }
        }
        if (ambienteSrc)
        {
            if (b.detenerAmbiente)
            {
                if (b.ambiente || fadeOutAmbiente <= 0f) ambienteSrc.Stop();
                else StartCoroutine(FadeOutYParar(ambienteSrc, fadeOutAmbiente));
            }
            if (b.ambiente)
            {
                ambienteSrc.clip = b.ambiente;
                ambienteSrc.loop = true;
                ambienteSrc.Play();
            }
        }
        if (sfxSrc && b.sfx) sfxSrc.PlayOneShot(b.sfx);
    }

    IEnumerator FadeOutYParar(AudioSource src, float dur)
    {
        float v0 = src.volume, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(v0, 0f, t / dur);
            yield return null;
        }
        src.Stop();
        src.volume = v0;
    }

    // ---------- Guion por defecto (solo para proyectos nuevos: BORRA fuentes y audios asignados) ----------

    [ContextMenu("Cargar guion por defecto")]
    void CargarGuion()
    {
        Color rosa = new Color(1f, 0.72f, 0.88f);
        Color gris = new Color(0.75f, 0.78f, 0.8f);
        Color frio = new Color(0.9f, 0.93f, 1f);

        beats = new List<Beat>
        {
            new Beat {
                nombre = "ESCENA 1: Mundo mágico",
                frases = new[] {
                    "Cuando cae la noche, te conviertes en chica mágica.",
                    "La academia brilla bajo las estrellas, tus amigas te esperan en la entrada",
                    "y cada monstruo que derrotas hace que el mundo entero te aplauda." },
                aparicion = Aparicion.FadeFrases, color = rosa, tamano = 46f,
                fadeFrase = 1.6f, pausaEntreFrases = 1.3f, mantener = 1.7f
            },
            new Beat {
                nombre = "CORTE A: Amanece",
                frases = new[] { "Amanece." },
                aparicion = Aparicion.Instantanea, color = Color.white, tamano = 64f,
                flash = true, colorFlash = Color.white, mantener = 1.0f, fadeSalida = 0.1f,
                detenerMusica = true, detenerAmbiente = true
            },
            new Beat {
                nombre = "ESCENA 2: Realidad",
                frases = new[] {
                    "Pero el sol sale, y con él vuelve todo lo demás.",
                    "Turnos interminables detrás de una caja, clientes que ni te miran, cuentas que no cuadran.",
                    "Aquí nadie te aplaude.",
                    "Nadie nota si estás cansada." },
                aparicion = Aparicion.MaquinaDeEscribir, color = gris, tamano = 38f,
                letrasPorSegundo = 22f, pausaEntreFrases = 0.9f, mantener = 1.5f,
                temblor = 1.5f
            },
            new Beat {
                nombre = "CORTE B: Llega la noche",
                frases = new[] { "Pero llega la noche." },
                aparicion = Aparicion.FadeFrases, color = rosa, tamano = 56f,
                silencioAntes = 1.0f, flash = true, colorFlash = new Color(1f, 0.6f, 0.85f),
                fadeFrase = 1.0f, mantener = 0.8f, detenerAmbiente = true
            },
            new Beat {
                nombre = "ESCENA 3: El anhelo",
                frases = new[] {
                    "Cada noche vuelves a la academia y todo vuelve a tener sentido.",
                    "Aquí eres alguien. Aquí te esperan.",
                    "Por eso sigues peleando, aunque el cuerpo ya no responda como antes." },
                aparicion = Aparicion.FadeFrases, color = rosa, tamano = 46f,
                fadeFrase = 1.6f, pausaEntreFrases = 1.3f, mantener = 1.7f,
                glitchEnSegundo = 4f, pitchMusica = 0.85f
            },
            new Beat {
                nombre = "CORTE C: El vacío",
                frases = new string[0],
                mantener = 2f, fadeSalida = 0.01f,
                detenerMusica = true, detenerAmbiente = true
            },
            new Beat {
                nombre = "ESCENA 4a: La pregunta",
                frases = new[] {
                    "Una vida te agota.",
                    "La otra te abraza.",
                    "Y cuanto más te abraza, más difícil es soltarla." },
                aparicion = Aparicion.FadeFrases, color = frio, tamano = 44f,
                fadeFrase = 1.6f, pausaEntreFrases = 1.3f, mantener = 1.3f
            },
            new Beat {
                nombre = "ESCENA 4b: La pregunta final",
                frases = new[] { "¿Hasta dónde llegarás por no despertar?" },
                aparicion = Aparicion.FadeFrases, color = rosa, tamano = 62f,
                silencioAntes = 2f, fadeFrase = 1.8f, mantener = 3f, fadeSalida = 1f
            },
            new Beat {
                nombre = "CIERRE → TUTORIAL",
                frases = new string[0],
                mantener = 1.5f, fadeSalida = 0.01f
            }
        };
    }
}