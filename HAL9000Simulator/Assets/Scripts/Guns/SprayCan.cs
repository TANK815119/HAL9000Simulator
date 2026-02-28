using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// SprayCan - Paints decals onto surfaces via raycast.
/// Attach this to a spray can GameObject that also has:
///   - A Rigidbody
///   - An AudioSource
///   - A VisualEffect component (assigned in Inspector)
///   - A child Transform named "Nozzle" (spray origin & forward direction)
///   - An InputData component (from the Firearm base class pattern)
/// </summary>
public class SprayCan : Firearm
{
    // ─── Inspector Fields ──────────────────────────────────────────────────────

    [Header("VFX & Audio")]
    [SerializeField] private VisualEffect sprayEffect;

    [Header("Paint Settings")]
    [Tooltip("Decal prefab to stamp onto surfaces. Must have a Projector or Decal Projector component.")]
    [SerializeField] private GameObject paintDecalPrefab;

    [Tooltip("Color of this spray can's paint.")]
    [SerializeField] private Color paintColor = Color.red;

    [Tooltip("Radius of the spray spread in world-space on the hit surface.")]
    [SerializeField] private float sprayDegrees = 0.15f;

    [Tooltip("How far the spray can reach (metres).")]
    [SerializeField] private float sprayRange = 1.5f;

    [Tooltip("How many decal stamps are placed per second while spraying.")]
    [SerializeField] private float stampsPerSecond = 12f;

    [Tooltip("Layers the spray can paint onto.")]
    [SerializeField] private LayerMask paintableLayers = ~0;

    // ─── Private State ─────────────────────────────────────────────────────────

    private bool spraying = false;
    private float stampTimer = 0f;

    // Pool of spawned decals (optional: cap max live decals to save memory)
    private readonly List<GameObject> spawnedDecals = new List<GameObject>();
    [SerializeField] private int maxDecals = 80;

    // ─── Unity Lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        gunBody = GetComponent<Rigidbody>();
        inputData = GetComponent<InputData>();
        audioSource = GetComponent<AudioSource>();

        sprayEffect.Stop();
    }

    void FixedUpdate()
    {
        if (TriggerSqueezed())
        {
            Spray();
        }

        if (TriggerReleased())
        {
            StopSpray();
        }
    }

    // ─── Core Spray Logic ──────────────────────────────────────────────────────

    private void Spray()
    {
        // Begin VFX / audio on first frame of trigger press
        if (!spraying)
        {
            BeginSpray();
        }

        // Apply recoil
        ApplyRecoil();

        // Throttle decal stamps to stampsPerSecond
        stampTimer += Time.deltaTime;
        float stampInterval = 1f / Mathf.Max(stampsPerSecond, 0.1f);

        while (stampTimer >= stampInterval)
        {
            stampTimer -= stampInterval;
            TryStampDecal();
        }
    }

    private void BeginSpray()
    {
        spraying = true;
        stampTimer = 0f;

        sprayEffect.Play();

        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    private void StopSpray()
    {
        if (!spraying) return;

        spraying = false;
        sprayEffect.Stop();

        if (audioSource != null)
            audioSource.Stop();
    }

    // ─── Decal Stamping ────────────────────────────────────────────────────────

    /// <summary>
    /// Casts a ray from the nozzle forward. If it hits a paintable surface,
    /// instantiates a decal at the hit point, slightly offset along the normal,
    /// with a random rotation and a small random offset within the spray radius.
    /// </summary>
    private void TryStampDecal()
    {
        if (paintDecalPrefab == null) return;

        Vector3 origin = recoilOrigin.position;
        Vector3 direction = gunBody.transform.right;
        Vector2 scatter = Random.insideUnitCircle * sprayDegrees;
        direction  = new Vector3(
            direction.x + scatter.x,
            direction.y + scatter.y,
            direction.z
        ).normalized;

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, sprayRange, paintableLayers))
            return;

        // Random scatter within spray radius (simulates aerosol spread)
        //Vector2 scatter = Random.insideUnitCircle * sprayRadius;
        //Vector3 tangent = Vector3.Cross(hit.normal, Vector3.up).normalized;
        //Vector3 bitangent = Vector3.Cross(hit.normal, tangent).normalized;
        //Vector3 spawnPos = hit.point
        //                  + hit.normal * 0.005f          // tiny offset to avoid z-fighting
        //                  + tangent * scatter.x
        //                  + bitangent * scatter.y;
        Vector3 spawnPos = hit.point + hit.normal * 0.005f;

        // Orient decal so it projects along the surface normal
        Quaternion spawnRot = Quaternion.LookRotation(-hit.normal)
                            * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        GameObject decal = Instantiate(paintDecalPrefab, spawnPos, spawnRot);

        // Parent to the hit object so decals move with it (e.g. moving props)
        decal.transform.SetParent(hit.collider.transform, worldPositionStays: true);

        // Tint the decal to this can's paint colour (works for URP Decal Projectors
        // that expose a "_BaseColor" property, or legacy Projector materials)
        ApplyColorToDecal(decal);

        // Pool management — remove oldest decal when over budget
        spawnedDecals.Add(decal);
        if (spawnedDecals.Count > maxDecals)
        {
            GameObject oldest = spawnedDecals[0];
            spawnedDecals.RemoveAt(0);
            if (oldest != null) Destroy(oldest);
        }
    }

    // ─── Colour Helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Tints the decal prefab instance. Supports:
    ///  • URP Decal Projector  (UniversalRenderPipeline.Decal component)
    ///  • Legacy Projector     (UnityEngine.Projector component)
    ///  • Any MeshRenderer     (fallback — sets _BaseColor / _Color)
    /// </summary>
    private void ApplyColorToDecal(GameObject decal)
    {
        // URP Decal Projector path
#if UNITY_2021_2_OR_NEWER
        var urpDecal = decal.GetComponentInChildren<UnityEngine.Rendering.Universal.DecalProjector>();
        if (urpDecal != null)
        {
            Material mat = new Material(urpDecal.material);  // instance so we don't pollute shared mat
            mat.color = paintColor;
            urpDecal.material = mat;
            return;
        }
#endif
        // Legacy Projector path
        var projector = decal.GetComponentInChildren<Projector>();
        if (projector != null && projector.material != null)
        {
            Material mat = new Material(projector.material);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", paintColor);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", paintColor);
            projector.material = mat;
            return;
        }

        // MeshRenderer fallback
        var rend = decal.GetComponentInChildren<MeshRenderer>();
        if (rend != null && rend.material != null)
        {
            if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", paintColor);
            else if (rend.material.HasProperty("_Color")) rend.material.SetColor("_Color", paintColor);
        }
    }

    // ─── Recoil ────────────────────────────────────────────────────────────────

    private void ApplyRecoil()
    {
        if (gunBody == null || recoilOrigin == null) return;

        float totalMass = gunBody.mass;
        Vector3 posteriorForce = -gunBody.transform.right * recoil * (gunBody.mass / totalMass);
        gunBody.AddForceAtPosition(posteriorForce * Time.deltaTime, recoilOrigin.position, ForceMode.Impulse);
    }

    // ─── Public API ────────────────────────────────────────────────────────────

    /// <summary>Change this can's colour at runtime.</summary>
    public void SetPaintColor(Color color)
    {
        paintColor = color;
    }

    // ─── Firearm Override ──────────────────────────────────────────────────────

    public override void TryLoadAmmo(Ammo ammo)
    {
        // Spray can has infinite paint — no ammo system needed.
        throw new System.NotImplementedException();
    }
}