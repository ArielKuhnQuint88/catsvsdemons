using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatsVsDemons.Visuals
{
    /// <summary>Scene-owned atmosphere; no per-frame material allocations or realtime reflections.</summary>
    public sealed class GardenVisualPolish : MonoBehaviour
    {
        private readonly Dictionary<Renderer, Material> waterMaterials = new();
        private ParticleSystem atmosphere;
        private Material particleMaterial;
        private Texture2D particleTexture;
        private Volume volume;
        private VolumeProfile profile;
        private int scenario = -1;
        private int season = -1;

        public void Apply(int scenarioIndex, int seasonIndex, Color ground)
        {
            if (Application.isMobilePlatform) Application.targetFrameRate = 60;
            bool lowMemory = Application.isMobilePlatform &&
                SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 3072;
            Camera camera = Camera.main;
            if (camera != null)
            {
                UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = !lowMemory;
                data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                data.antialiasingQuality = AntialiasingQuality.Medium;
            }
            EnsureVolume(lowMemory);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = scenarioIndex == 2
                ? new Color(0.38f, 0.40f, 0.63f) : new Color(0.62f, 0.70f, 0.82f);
            RenderSettings.ambientEquatorColor = Color.Lerp(ground, new Color(0.46f, 0.40f, 0.36f), 0.7f);
            RenderSettings.ambientGroundColor = new Color(0.17f, 0.19f, 0.22f);
            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                foreach (Light light in GetComponentsInChildren<Light>(true))
                    if (light.type == LightType.Directional) { sun = light; break; }
            }
            if (sun == null)
            {
                foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (light.type == LightType.Directional) { sun = light; break; }
            }
            if (sun != null)
            {
                RenderSettings.sun = sun;
                sun.shadows = lowMemory ? LightShadows.Hard : LightShadows.Soft;
                sun.shadowStrength = 0.72f;
                sun.color = scenarioIndex == 2 ? new Color(0.74f, 0.78f, 1f)
                    : seasonIndex == 2 ? new Color(1f, 0.79f, 0.57f)
                    : new Color(1f, 0.91f, 0.78f);
                sun.intensity = scenarioIndex == 2 ? 0.95f : 1.15f;
            }
            ApplyWater();
            if (scenario != scenarioIndex || season != seasonIndex)
            {
                scenario = scenarioIndex;
                season = seasonIndex;
                ConfigureParticles(lowMemory);
            }
        }

        private void EnsureVolume(bool lowMemory)
        {
            if (volume != null) return;
            GameObject root = new("Garden Color and Light");
            root.transform.SetParent(transform, false);
            volume = root.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 5f;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Runtime Garden Grading";
            volume.sharedProfile = profile;
            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.contrast.Override(8f);
            color.saturation.Override(6f);
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.active = !lowMemory;
            bloom.intensity.Override(Application.isMobilePlatform ? 0.18f : 0.32f);
            bloom.threshold.Override(1.1f);
            bloom.scatter.Override(0.55f);
            bloom.highQualityFiltering.Override(false);
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.1f);
            vignette.smoothness.Override(0.45f);
        }

        private void ApplyWater()
        {
            Shader shader = Shader.Find("CatsVsDemons/GardenWater");
            if (shader == null) return;
            // Scenario landmarks are rebuilt between seasons. Release their old materials.
            List<Renderer> removed = new();
            foreach (KeyValuePair<Renderer, Material> entry in waterMaterials)
                if (entry.Key == null) { Destroy(entry.Value); removed.Add(entry.Key); }
            foreach (Renderer renderer in removed) waterMaterials.Remove(renderer);

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                string name = renderer.gameObject.name;
                if (name != "Water" && name != "DeepWater" && name != "GroveStream" &&
                    !name.StartsWith("StreamWater")) continue;
                Color color = renderer.sharedMaterial != null ? renderer.sharedMaterial.color
                    : new Color(0.08f, 0.48f, 0.6f);
                if (!waterMaterials.TryGetValue(renderer, out Material water))
                {
                    water = new Material(shader) { name = "Animated Garden Water" };
                    waterMaterials.Add(renderer, water);
                }
                water.SetColor("_BaseColor", color);
                water.SetColor("_HighlightColor", Color.Lerp(color, new Color(0.74f, 0.94f, 1f), 0.65f));
                renderer.sharedMaterial = water;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void ConfigureParticles(bool lowMemory)
        {
            if (atmosphere == null)
            {
                GameObject root = new("Seasonal Garden Particles");
                root.transform.SetParent(transform, false);
                root.transform.position = new Vector3(0, 8f, 0);
                atmosphere = root.AddComponent<ParticleSystem>();
                atmosphere.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) return;
                particleMaterial = new Material(shader) { name = "Garden Petals" };
                particleTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[32 * 32];
                for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float distance = new Vector2((x - 15.5f) / 15f, (y - 15.5f) / 15f).magnitude;
                    pixels[y * 32 + x] = new Color(1, 1, 1, Mathf.Clamp01((1f - distance) * 8f));
                }
                particleTexture.SetPixels(pixels);
                particleTexture.Apply(false, true);
                particleMaterial.SetTexture("_BaseMap", particleTexture);
                particleMaterial.SetFloat("_Surface", 1f);
                particleMaterial.SetFloat("_Blend", 0f);
                particleMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                particleMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                particleMaterial.SetFloat("_ZWrite", 0f);
                particleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                particleMaterial.renderQueue = (int)RenderQueue.Transparent;
                ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = particleMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            if (particleMaterial == null) return;
            atmosphere.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = atmosphere.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = lowMemory ? 36 : Application.isMobilePlatform ? 72 : 140;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(season == 3 ? 0.09f : 0.12f, 0.22f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = season == 3 ? new Color(0.9f, 0.96f, 1f, 0.8f)
                : scenario == 2 ? new Color(0.58f, 0.76f, 1f, 0.65f)
                : season == 2 ? new Color(1f, 0.62f, 0.2f, 0.85f)
                : new Color(1f, 0.7f, 0.82f, 0.8f);
            var emission = atmosphere.emission;
            emission.rateOverTime = lowMemory ? 3f : Application.isMobilePlatform ? 6f : 12f;
            var shape = atmosphere.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(44f, 2f, 38f);
            var velocity = atmosphere.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0.2f, 0.7f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.9f, -0.5f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.25f);
            var rotation = atmosphere.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            var colorOverLife = atmosphere.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f) }, new[] {
                new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.12f),
                new GradientAlphaKey(1, 0.75f), new GradientAlphaKey(0, 1) });
            colorOverLife.color = gradient;
            atmosphere.Play();
        }

        private void OnDestroy()
        {
            foreach (Material material in waterMaterials.Values) Destroy(material);
            if (particleMaterial != null) Destroy(particleMaterial);
            if (particleTexture != null) Destroy(particleTexture);
            if (profile != null)
            {
                foreach (VolumeComponent component in profile.components) Destroy(component);
                Destroy(profile);
            }
        }
    }
}
