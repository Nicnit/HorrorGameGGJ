using System;
using UnityEngine;

[DisallowMultipleComponent]
public class LightFlicker : MonoBehaviour
{
    [Header("Lights")]
    [Tooltip("Lights to flicker. Leave empty to auto-grab from children.")]
    [SerializeField] private Light[] lights;

    [Header("Timing")]
    [Tooltip("Average seconds between flicker events per light.")]
    [SerializeField] private Vector2 intervalRange = new Vector2(2.5f, 8.0f);

    [Tooltip("How long a typical flicker event lasts (seconds).")]
    [SerializeField] private Vector2 eventDurationRange = new Vector2(0.08f, 0.35f);

    [Tooltip("Extra random offset to desynchronize all lights at startup.")]
    [SerializeField] private Vector2 startOffsetRange = new Vector2(0f, 2.5f);

    [Header("Intensity Behavior")]
    [Tooltip("How much the intensity dips during normal flickers (0.1 = 10% dip).")]
    [Range(0f, 0.9f)]
    [SerializeField] private float normalDipMin = 0.08f;

    [Range(0f, 0.95f)]
    [SerializeField] private float normalDipMax = 0.22f;

    [Tooltip("Chance that a flicker event is a 'bad ballast' major dip.")]
    [Range(0f, 1f)]
    [SerializeField] private float majorEventChance = 0.10f;

    [Tooltip("How deep major dips are (as fraction of base intensity).")]
    [SerializeField] private Vector2 majorDipRange = new Vector2(0.35f, 0.75f);

    [Header("Stutter Pattern")]
    [Tooltip("How many micro-blips happen inside a flicker event.")]
    [SerializeField] private Vector2Int blipCountRange = new Vector2Int(2, 6);

    [Tooltip("How fast blips are inside an event (seconds).")]
    [SerializeField] private Vector2 blipTimeRange = new Vector2(0.015f, 0.07f);

    [Tooltip("Occasional tiny jitter even outside events (kept subtle).")]
    [Range(0f, 1f)]
    [SerializeField] private float ambientJitterAmount = 0.02f;

    [Header("Smoothing")]
    [Tooltip("Higher = smoother transitions (less harsh).")]
    [SerializeField] private float intensityLerpSpeed = 18f;

    [Header("Optional Audio/Emission")]
    [Tooltip("If true, we also flicker a material emission intensity via keyword _EMISSION on a Renderer array (optional).")]
    [SerializeField] private bool affectEmission = false;

    [SerializeField] private Renderer[] emissiveRenderers;

    [Tooltip("Multiplier applied to emission intensity (assumes HDR emission).")]
    [SerializeField] private float emissionMultiplier = 1f;

    // ------------------------

    private struct LightState
    {
        public Light light;
        public float baseIntensity;
        public float targetIntensity;

        // Scheduling
        public float nextEventTime;

        // Event data
        public bool inEvent;
        public float eventEndTime;
        public int blipsRemaining;
        public float nextBlipTime;
        public float dipAmount; // fraction of base intensity to dip

        // Per-light random
        public Unity.Mathematics.Random rng; // if you don't have math package, see note below
    }

    private LightState[] _states;

    private MaterialPropertyBlock _mpb;

    private void Awake()
    {
        if (lights == null || lights.Length == 0)
            lights = GetComponentsInChildren<Light>(includeInactive: true);

        if (lights == null || lights.Length == 0)
        {
            enabled = false;
            return;
        }

        _states = new LightState[lights.Length];

        // MaterialPropertyBlock for emission (optional)
        if (affectEmission)
            _mpb = new MaterialPropertyBlock();

        // Build per-light state with desynced schedules + stable seeds
        for (int i = 0; i < lights.Length; i++)
        {
            var l = lights[i];
            if (l == null) continue;

            // Stable-ish seed per light instance
            // Combine instance ID with index so duplicates still differ.
            uint seed = (uint)(Mathf.Abs(l.GetInstanceID()) + 17 * (i + 1));
            // Need non-zero seed for Unity.Mathematics.Random
            if (seed == 0) seed = 1;

            var rng = new Unity.Mathematics.Random(seed);

            float baseIntensity = Mathf.Max(0f, l.intensity);

            float startOffset = rng.NextFloat(startOffsetRange.x, startOffsetRange.y);

            _states[i] = new LightState
            {
                light = l,
                baseIntensity = baseIntensity,
                targetIntensity = baseIntensity,
                rng = rng,
                nextEventTime = Time.time + startOffset + rng.NextFloat(intervalRange.x, intervalRange.y),
                inEvent = false,
                eventEndTime = 0f,
                blipsRemaining = 0,
                nextBlipTime = 0f,
                dipAmount = 0f
            };
        }
    }

    private void Update()
    {
        float t = Time.time;

        for (int i = 0; i < _states.Length; i++)
        {
            ref var s = ref _states[i];
            if (s.light == null) continue;

            // Small ambient jitter: super subtle, slow-ish
            float ambient = 1f + (Mathf.PerlinNoise(i * 13.37f, t * 0.25f) - 0.5f) * 2f * ambientJitterAmount;

            // Start a new event?
            if (!s.inEvent && t >= s.nextEventTime)
            {
                BeginEvent(ref s, t);
            }

            // Advance event blips
            if (s.inEvent)
            {
                if (t >= s.eventEndTime)
                {
                    EndEvent(ref s, t);
                }
                else if (t >= s.nextBlipTime && s.blipsRemaining > 0)
                {
                    // Toggle intensity target between dipped and near-normal for that "stutter"
                    // We vary blip depth slightly each time.
                    float blipVariance = s.rng.NextFloat(0.85f, 1.10f);
                    bool goDim = (s.blipsRemaining % 2 == 0);

                    float dimTarget = s.baseIntensity * (1f - Mathf.Clamp01(s.dipAmount * blipVariance));
                    float normalTarget = s.baseIntensity * s.rng.NextFloat(0.92f, 1.02f);

                    s.targetIntensity = goDim ? dimTarget : normalTarget;

                    s.blipsRemaining--;
                    s.nextBlipTime = t + s.rng.NextFloat(blipTimeRange.x, blipTimeRange.y);
                }
            }
            else
            {
                // No event: hang around near base intensity, slightly imperfect
                s.targetIntensity = s.baseIntensity * s.rng.NextFloat(0.985f, 1.01f);
            }

            // Apply ambient on top
            float finalTarget = s.targetIntensity * ambient;

            // Smooth it so it feels like real electricity / ballast rather than a hard toggle
            s.light.intensity = Mathf.Lerp(s.light.intensity, finalTarget, 1f - Mathf.Exp(-intensityLerpSpeed * Time.deltaTime));

            // Optional emission flicker for meshes (e.g., light fixtures)
            if (affectEmission && emissiveRenderers != null && emissiveRenderers.Length > 0)
                ApplyEmission(i, s.light.intensity, s.baseIntensity);
        }
    }

    private void BeginEvent(ref LightState s, float now)
    {
        s.inEvent = true;

        bool major = s.rng.NextFloat(0f, 1f) < majorEventChance;

        float dip = major
            ? s.rng.NextFloat(majorDipRange.x, majorDipRange.y)
            : s.rng.NextFloat(normalDipMin, normalDipMax);

        s.dipAmount = dip;

        int blips = s.rng.NextInt(blipCountRange.x, blipCountRange.y + 1);
        // Ensure at least 2 so it "flickers" and not just one pop
        s.blipsRemaining = Mathf.Max(2, blips);

        float dur = s.rng.NextFloat(eventDurationRange.x, eventDurationRange.y);
        if (major) dur *= s.rng.NextFloat(1.2f, 2.2f);

        s.eventEndTime = now + dur;
        s.nextBlipTime = now; // immediate first blip
    }

    private void EndEvent(ref LightState s, float now)
    {
        s.inEvent = false;

        // Settle near base
        s.targetIntensity = s.baseIntensity * s.rng.NextFloat(0.93f, 1.02f);

        // Schedule next event with random interval; major events tend to be rarer
        float interval = s.rng.NextFloat(intervalRange.x, intervalRange.y);
        if (s.rng.NextFloat(0f, 1f) < 0.15f)
            interval *= s.rng.NextFloat(1.1f, 1.8f); // occasional long quiet stretch

        s.nextEventTime = now + interval;
    }

    private void ApplyEmission(int lightIndex, float currentIntensity, float baseIntensity)
    {
        // This is intentionally simple: it applies a scalar multiplier to _EmissionColor.
        // Works best if your emissive material uses HDR emission and _EmissionColor exists.

        if (_mpb == null) return;
        if (emissiveRenderers == null) return;

        // Map intensity ratio (0..?) to (0..1..)
        float ratio = (baseIntensity <= 0.0001f) ? 1f : (currentIntensity / baseIntensity);
        ratio = Mathf.Clamp(ratio, 0f, 1.25f);

        // Spread mapping across renderers if you have fewer renderers than lights
        int rIndex = lightIndex % emissiveRenderers.Length;
        var r = emissiveRenderers[rIndex];
        if (r == null) return;

        r.GetPropertyBlock(_mpb);

        // If you already set a specific emission color per material, you might want to multiply it.
        // Here we just set intensity multiplier on existing color if present.
        Color emission = Color.white * (ratio * emissionMultiplier);
        _mpb.SetColor("_EmissionColor", emission);

        r.SetPropertyBlock(_mpb);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        intervalRange.x = Mathf.Max(0.05f, intervalRange.x);
        intervalRange.y = Mathf.Max(intervalRange.x, intervalRange.y);

        eventDurationRange.x = Mathf.Max(0.01f, eventDurationRange.x);
        eventDurationRange.y = Mathf.Max(eventDurationRange.x, eventDurationRange.y);

        blipTimeRange.x = Mathf.Max(0.005f, blipTimeRange.x);
        blipTimeRange.y = Mathf.Max(blipTimeRange.x, blipTimeRange.y);

        majorDipRange.x = Mathf.Clamp01(majorDipRange.x);
        majorDipRange.y = Mathf.Clamp(majorDipRange.y, majorDipRange.x, 0.98f);

        normalDipMin = Mathf.Clamp01(normalDipMin);
        normalDipMax = Mathf.Clamp(normalDipMax, normalDipMin, 0.95f);

        intensityLerpSpeed = Mathf.Max(1f, intensityLerpSpeed);
    }
#endif
}
