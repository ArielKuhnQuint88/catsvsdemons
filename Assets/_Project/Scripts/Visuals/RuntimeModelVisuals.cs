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
                foreach (Renderer renderer in parent.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = false;
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

            Material material = GetMaterial(resourcePath, color);
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                renderer.sharedMaterial = material;
            }

            visual.AddComponent<ProceduralModelAnimator>();
            return true;
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

        private static Material GetMaterial(string key, Color color)
        {
            if (Materials.TryGetValue(key, out Material material))
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = $"Runtime_{key.Replace('/', '_')}",
                color = color
            };
            Materials.Add(key, material);
            return material;
        }
    }
}
