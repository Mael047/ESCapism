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

    [Header("Tiempos 0 = inicio , 1 = final")]
    [Tooltip("Encadenar el siguiente golpe si hay inputs guardados")]
    [Range(0f, 1f)] public float cancelPoint = 0.5f;
    [Range(0f, 1f)] public float exitPoint = 0.9f;

    [Header("Movimiento durante el ataque")]
    public float moveSpeed = 0f;
    public AnimationCurve moveCurve = AnimationCurve.Constant(0f, 1f, 0f);

    [Header("Onda Expansiva")]
    public int shockwaveDamage = 15;

    [Min(0.1f)] public float animationSpeed = 1f;

    public float Duration => clip != null ? clip.length / animationSpeed : 1f;
}
