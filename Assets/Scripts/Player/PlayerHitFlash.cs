using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerHitFlash : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float flashDuration = 0.12f;

    [Header("Parpadeo durante la invulnerabilidad")]
    [SerializeField] private bool blink = true;
    [SerializeField] private float blinkInterval = 0.08f;

    [Header("Curación")]
    [SerializeField] private Material healMaterial;
    [SerializeField] private float healFlashDuration = 0.15f;

    private PlayerHealth health;
    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Material[][] hurtMaterials;
    private Material[][] healMaterials;
    private Coroutine flashRoutine;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();

        // Solo mallas: excluye partículas y trails
        var meshes = new List<Renderer>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r is SkinnedMeshRenderer || r is MeshRenderer) meshes.Add(r);
        }
        renderers = meshes.ToArray();

        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterials;
        }

        hurtMaterials = BuildReplacement(flashMaterial);
        healMaterials = BuildReplacement(healMaterial);
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Healed += OnHealed;
        health.Died += OnDied;
        health.Revived += OnRevived;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Healed -= OnHealed;
        health.Died -= OnDied;
        SetMaterials(originalMaterials);
        health.Revived -= OnRevived;
    }

    private void OnDamaged(int amount)
    {
        if (flashMaterial == null) return;
        Restart(HurtRoutine());
    }

    private void OnHealed(int amount)
    {
        // El parpadeo de daño tiene prioridad: no lo interrumpimos
        if (healMaterial == null || flashRoutine != null) return;
        Restart(FlashOnce(healMaterials, healFlashDuration));
    }

    private void OnDied()
    {
        if (flashMaterial == null) return;
        Restart(FlashOnce(hurtMaterials, flashDuration));
    }

    private void Restart(IEnumerator routine)
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(routine);
    }

    private IEnumerator HurtRoutine()
    {
        SetMaterials(hurtMaterials);
        yield return new WaitForSeconds(flashDuration);
        SetMaterials(originalMaterials);

        if (blink)
        {
            bool on = false;
            while (Time.time < health.InvulnerableUntil && !health.IsDead)
            {
                on = !on;
                SetMaterials(on ? hurtMaterials : originalMaterials);
                yield return new WaitForSeconds(blinkInterval);
            }
            SetMaterials(originalMaterials);
        }

        flashRoutine = null;
    }

    private IEnumerator FlashOnce(Material[][] materials, float duration)
    {
        SetMaterials(materials);
        yield return new WaitForSeconds(duration);
        SetMaterials(originalMaterials);
        flashRoutine = null;
    }

    private Material[][] BuildReplacement(Material material)
    {
        var result = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            result[i] = new Material[originalMaterials[i].Length];
            for (int m = 0; m < result[i].Length; m++)
            {
                result[i][m] = material;
            }
        }
        return result;
    }

    private void SetMaterials(Material[][] materials)
    {
        if (materials == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].sharedMaterials = materials[i];
        }
    }

    private void OnRevived()
    {
        if (flashMaterial == null) return;
        Restart(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        bool on = false;
        while (Time.time < health.InvulnerableUntil && !health.IsDead)
        {
            on = !on;
            SetMaterials(on ? hurtMaterials : originalMaterials);
            yield return new WaitForSeconds(blinkInterval);
        }
        SetMaterials(originalMaterials);
        flashRoutine = null;
    }
}