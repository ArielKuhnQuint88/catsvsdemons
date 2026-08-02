using System.Collections.Generic;
using UnityEngine;

namespace CatsVsDemons.Visuals
{
    public static class RuntimeModelVisuals
    {
        private static readonly Dictionary<string, Material> Materials = new();

        public static bool Attach(
            Transform parent,
            string resourcePath,
            float targetHeight,
            float bottomOffset,
            Color color,
            bool hideExisting = true)
        {
            GameObject model = Resources.Load<GameObject>(resourcePath);
            if (model == null)
            {
                Debug.LogWarning($"3D model not found: {resourcePath}", parent);
                return false;
            }

            if (hideExisting)
            {
                foreach (Renderer renderer in
                    parent.GetComponentsInChildren<Renderer>(true))
                {
                    if (IsPrototypeRenderer(parent, renderer.transform))
                    {
                        renderer.enabled = false;
                    }
                }
            }

            GameObject visual = Object.Instantiate(model, parent);
            visual.name = "GameplayModel";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            visual.transform.localScale = Vector3.one;

            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            {
                Object.Destroy(collider);
            }

            Bounds bounds = CalculateBounds(visual);
            if (bounds.size.y > 0.001f)
            {
                float scale = targetHeight / bounds.size.y;
                visual.transform.localScale = Vector3.one * scale;
            }

            bounds = CalculateBounds(visual);
            float desiredBottom = parent.TransformPoint(
                new Vector3(0f, bottomOffset, 0f)
            ).y;
            visual.transform.position +=
                Vector3.up * (desiredBottom - bounds.min.y);

            Bounds modelBounds = GetModelBounds(visual);
            Material material = GetMaterial(
                resourcePath,
                color,
                modelBounds
            );
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                renderer.sharedMaterial = material;
            }

            visual.AddComponent<ProceduralModelAnimator>();
            return true;
        }

        private static bool IsPrototypeRenderer(
            Transform root,
            Transform candidate)
        {
            if (candidate == root)
            {
                return true;
            }

            string objectName = candidate.name;
            return objectName == "Horn_Left" ||
                objectName == "Horn_Right" ||
                objectName == "Golden_Sword" ||
                objectName == "Red_Belt";
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static Bounds GetModelBounds(GameObject root)
        {
            MeshFilter filter = root.GetComponentInChildren<MeshFilter>();
            return filter != null && filter.sharedMesh != null
                ? filter.sharedMesh.bounds
                : new Bounds(Vector3.zero, Vector3.one);
        }

        private static Material GetMaterial(
            string key,
            Color fallbackColor,
            Bounds bounds)
        {
            if (Materials.TryGetValue(key, out Material material))
            {
                return material;
            }

            Shader shader = Shader.Find(
                "CatsVsDemons/StylizedModel"
            );

            if (shader == null)
            {
                shader = Shader.Find(
                    "Universal Render Pipeline/Lit"
                );
                material = new Material(shader)
                {
                    name = $"Runtime_{key.Replace('/', '_')}",
                    color = fallbackColor
                };
                Materials.Add(key, material);
                return material;
            }

            Color baseColor = fallbackColor;
            Color secondaryColor = Color.white;
            Color accentColor = fallbackColor;
            Color rimColor = new Color(1f, 0.75f, 0.42f);
            float topStart = 0.64f;
            float topEnd = 0.76f;
            float accentCenter = 0.43f;
            float accentWidth = 0.09f;

            switch (key)
            {
                case "Models/Kin":
                    baseColor = new Color(0.06f, 0.23f, 0.24f);
                    secondaryColor =
                        new Color(0.98f, 0.94f, 0.86f);
                    accentColor =
                        new Color(0.72f, 0.055f, 0.04f);
                    rimColor =
                        new Color(1f, 0.72f, 0.38f);
                    accentCenter = 0.4f;
                    accentWidth = 0.1f;
                    break;

                case "Models/DemonPoerix":
                    baseColor =
                        new Color(0.63f, 0.36f, 0.16f);
                    secondaryColor =
                        new Color(0.95f, 0.72f, 0.38f);
                    accentColor =
                        new Color(0.86f, 0.18f, 0.08f);
                    rimColor =
                        new Color(1f, 0.58f, 0.2f);
                    break;

                case "Models/DemonSono":
                    baseColor =
                        new Color(0.23f, 0.08f, 0.5f);
                    secondaryColor =
                        new Color(0.72f, 0.2f, 0.92f);
                    accentColor =
                        new Color(0.95f, 0.28f, 0.72f);
                    rimColor =
                        new Color(0.45f, 0.75f, 1f);
                    break;

                case "Models/DemonFlamurk":
                    baseColor =
                        new Color(0.95f, 0.16f, 0.025f);
                    secondaryColor =
                        new Color(1f, 0.72f, 0.04f);
                    accentColor =
                        new Color(1f, 0.9f, 0.42f);
                    rimColor =
                        new Color(1f, 0.45f, 0.08f);
                    topStart = 0.7f;
                    topEnd = 0.84f;
                    break;

                case "Models/Bonsai":
                    baseColor =
                        new Color(0.3f, 0.12f, 0.035f);
                    secondaryColor =
                        new Color(0.08f, 0.56f, 0.14f);
                    accentColor =
                        new Color(0.62f, 0.2f, 0.07f);
                    rimColor =
                        new Color(0.45f, 1f, 0.4f);
                    topStart = 0.27f;
                    topEnd = 0.43f;
                    accentCenter = 0.12f;
                    accentWidth = 0.12f;
                    break;

                case "Models/StoneLantern":
                    baseColor =
                        new Color(0.25f, 0.31f, 0.34f);
                    secondaryColor =
                        new Color(0.55f, 0.67f, 0.7f);
                    accentColor =
                        new Color(1f, 0.48f, 0.06f);
                    rimColor =
                        new Color(1f, 0.68f, 0.25f);
                    accentCenter = 0.58f;
                    accentWidth = 0.07f;
                    break;
            }

            material = new Material(shader)
            {
                name = $"Runtime_{key.Replace('/', '_')}"
            };
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_SecondaryColor", secondaryColor);
            material.SetColor("_AccentColor", accentColor);
            material.SetColor("_RimColor", rimColor);
            material.SetFloat("_MinHeight", bounds.min.z);
            material.SetFloat("_MaxHeight", bounds.max.z);
            material.SetFloat("_TopStart", topStart);
            material.SetFloat("_TopEnd", topEnd);
            material.SetFloat("_AccentCenter", accentCenter);
            material.SetFloat("_AccentWidth", accentWidth);
            material.SetFloat("_RimStrength", 0.3f);

            Materials.Add(key, material);
            return material;
        }
    }
}
