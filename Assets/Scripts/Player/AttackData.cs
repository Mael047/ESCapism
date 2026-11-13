using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "Attack", menuName = "Combat/Attack Data")]
public class AttackData : ScriptableObject
{
    [Header("Animación")]
    public string stateName = "Attack1";
    public AnimationClip clip;
    public float transitionDuration = 0.1f;

    [Header("Daño")]
    public int damage = 10;

    [Header("Mejoras")]
    [Tooltip("Golpe final del combo: recibe el bono de Remate")]
    public bool isFinisher;

    [Header("Tiempos 0 = inicio , 1 = final")]
    [Tooltip("Encadenar el siguiente golpe si hay inputs guardados")]
    [Range(0f, 1f)] public float cancelPoint = 0.5f;
    [Range(0f, 1f)] public float exitPoint = 0.9f;

    [Header("Movimiento durante el ataque")]
    public float moveSpeed = 0f;
    public AnimationCurve moveCurve = AnimationCurve.Constant(0f, 1f, 0f);

    [Header("Onda Expansiva")]
    public int shockwaveDamage = 15;

    [Header("Feedback al conectar")]
    [Tooltip("Fuerza del temblor de cámara al golpear")]
    public float hitShake = 0.15f;
    [Tooltip("Segundos de congelamiento al golpear (0 = sin congelar)")]
    public float hitStop = 0f;
    [Tooltip("Temblor al golpear el suelo con la onda expansiva")]
    public float shockwaveShake = 0f;

    [Min(0.1f)] public float animationSpeed = 1f;

    [Header("Sonido")]
    [Tooltip("El golpe al aire (suena cuando se activa el hitbox)")]
    public SoundData swingSound;
    [Tooltip("Al conectar con un enemigo")]
    public SoundData hitSound;
    [Tooltip("Al golpear el suelo con la onda expansiva")]
    public SoundData shockwaveSound;

    public float Duration => clip != null ? clip.length / animationSpeed : 1f;
}
