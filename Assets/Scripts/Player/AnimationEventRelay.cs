using UnityEngine;

//Puente para las animaciones entre el Animator y el PlayerCombat

public class AnimationEventRelay : MonoBehaviour
{
    private PlayerCombat combat;

    private void Awake()
    {
        combat  = GetComponentInParent<PlayerCombat>();
    }

    public void HitboxOn(int index) => combat.HitboxOn(index);
    public void HitboxOff(int index) => combat.HitboxOff(index);
    public void TrailOn() => combat.TrailOn();
    public void TrailOff() => combat.TrailOff();
}
