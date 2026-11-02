using UnityEngine;

public enum Mood { Happy, Serious, Angry }

// Put this script on the root object of the NPC (or on the client prefab).
// When the NPC spawns, it picks a random skin, eyes, hair and outfit,
// and arrives Happy. The game can then make it Serious or Angry.
public class NPCAppearance : MonoBehaviour
{
    [Header("NPC Sprite Renderers")]
    public SpriteRenderer outfit;      // the "Outfit" object
    public SpriteRenderer face;        // the "Face" object
    public SpriteRenderer eyes;        // the "Eyes" object
    public SpriteRenderer expression;  // the "Expression" object
    public SpriteRenderer hair;        // the "Hair" object

    [Header("Random options (drag sprites from the npc folder)")]
    public Sprite[] outfits;     // vesti_1, vesti_2, vesti_3
    public Sprite[] faces;       // cabeza_1, cabeza_2
    public Sprite[] eyeOptions;  // ojos_1, ojos_2, ojos_3
    public Sprite[] hairs;       // pelo_1, pelo_2, pelo_3

    [Header("Expressions")]
    public Sprite happyExpression;    // expre_h
    public Sprite seriousExpression;  // expre_n
    public Sprite angryExpression;    // expre_e

    public Mood CurrentMood { get; private set; }

    void Awake()
    {
        Randomize();
        SetMood(Mood.Happy); // every client arrives happy
    }

    // Call this to give the NPC a new random look.
    public void Randomize()
    {
        SetRandom(outfit, outfits);
        SetRandom(face, faces);
        SetRandom(eyes, eyeOptions);
        SetRandom(hair, hairs);
    }

    // Set a specific mood: npc.SetMood(Mood.Angry);
    public void SetMood(Mood mood)
    {
        CurrentMood = mood;
        if (expression == null) return;

        switch (mood)
        {
            case Mood.Happy:   expression.sprite = happyExpression;   break;
            case Mood.Serious: expression.sprite = seriousExpression; break;
            case Mood.Angry:   expression.sprite = angryExpression;   break;
        }
    }

    // Something bad happened: Happy -> Serious -> Angry.
    public void WorsenMood()
    {
        if (CurrentMood == Mood.Happy) SetMood(Mood.Serious);
        else if (CurrentMood == Mood.Serious) SetMood(Mood.Angry);
    }

    // Something good happened: Angry -> Serious -> Happy.
    public void ImproveMood()
    {
        if (CurrentMood == Mood.Angry) SetMood(Mood.Serious);
        else if (CurrentMood == Mood.Serious) SetMood(Mood.Happy);
    }

    public bool IsAngry => CurrentMood == Mood.Angry;

    static void SetRandom(SpriteRenderer target, Sprite[] options)
    {
        if (target == null || options == null || options.Length == 0) return;
        target.sprite = options[Random.Range(0, options.Length)];
    }
}
