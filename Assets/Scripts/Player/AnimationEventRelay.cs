using UnityEngine;

//Puente para las animaciones entre el Animator y el PlayerCombat

public class AnimationEventRelay : MonoBehaviour
{
    private PlayerCombat combat;
    private PlayerAudio playerAudio;

    private void Awake()
    {
        combat  = GetComponentInParent<PlayerCombat>();
        playerAudio = GetComponentInParent<PlayerAudio>();
    }

    // Evento de animación en los pasos de la animación de caminar/correr
    public void Footstep()
    {
        if (playerAudio != null) playerAudio.Footstep();
    }

    public void HitboxOn(int index) => combat.HitboxOn(index);
    public void HitboxOff(int index) => combat.HitboxOff(index);
    public void TrailOn() => combat.TrailOn();
    public void TrailOff() => combat.TrailOff();
    public void Shockwave() => combat.SpawnShockwave();

}
