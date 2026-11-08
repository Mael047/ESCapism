using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyHitFlash : MonoBehaviour
{
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float flashDuration = 0.08f;

    private EnemyHealth health;
    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Material[][] flashMaterials;
    private Coroutine flashRoutine;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();

        var meshes = new System.Collections.Generic.List<Renderer>();
        foreach(Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r is SkinnedMeshRenderer || r is MeshRenderer) meshes.Add(r);
        }
        renderers = meshes.ToArray();

        originalMaterials = new Material[renderers.Length][];
        flashMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterials;
            flashMaterials[i] = new Material[originalMaterials[i].Length];
            for(int m = 0; m < flashMaterials[i].Length; m++)
            {
                flashMaterials[i][m] = flashMaterial;
            }
        }

    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Died += Flash;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= Flash;
        SetFlash(false);
    }

    private void OnDamaged(int amount) => Flash();

    public void Flash()
    {
        if (flashMaterial == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        SetFlash(true);
        yield return new WaitForSeconds(flashDuration);
        SetFlash(false);
        flashRoutine = null;
    }

    private void SetFlash(bool on)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].sharedMaterials = on ? flashMaterials[i] : originalMaterials[i];
        }
    }
}
