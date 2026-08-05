using CatsVsDemons.Player;
using System.Collections.Generic;
using UnityEngine;

namespace CatsVsDemons.CameraSystem
{
    public sealed class CameraModeController : MonoBehaviour
    {
        [SerializeField] private float eyeHeight = 1.45f;
        [SerializeField] private float forwardOffset = 0.3f;
        [SerializeField] private float followSpeed = 14f;

        private Camera gameCamera;
        private Transform kin;
        private bool firstPerson;
        private Material runtimeSky;

        private void Start()
        {
            gameCamera = GetComponent<Camera>();
            ConfigureEnvironment();
            firstPerson = PlayerPrefs.GetInt("CameraMode", 0) == 1;

            if (!firstPerson)
            {
                return;
            }

            KinHealth kinHealth =
                Object.FindFirstObjectByType<KinHealth>();

            if (kinHealth == null)
            {
                Debug.LogError("Kin was not found for first-person mode.");
                return;
            }

            kin = kinHealth.transform;
            gameCamera.orthographic = false;
            gameCamera.fieldOfView = 68f;
            gameCamera.nearClipPlane = 0.08f;

            foreach (Renderer renderer in
                kin.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = false;
            }
        }

        private void ConfigureEnvironment()
        {
            gameCamera.clearFlags = CameraClearFlags.Skybox;

            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null && skyShader.isSupported)
            {
                runtimeSky = new Material(skyShader)
                {
                    name = "Runtime Japanese Twilight Sky",
                    hideFlags = HideFlags.HideAndDontSave
                };
                runtimeSky.SetColor(
                    "_SkyTint",
                    new Color(0.32f, 0.46f, 0.67f)
                );
                runtimeSky.SetColor(
                    "_GroundColor",
                    new Color(0.13f, 0.19f, 0.2f)
                );
                runtimeSky.SetFloat("_AtmosphereThickness", 0.72f);
                runtimeSky.SetFloat("_SunSize", 0.045f);
                runtimeSky.SetFloat("_SunSizeConvergence", 4.5f);
                runtimeSky.SetFloat("_Exposure", 1.08f);
                RenderSettings.skybox = runtimeSky;
            }

            RepairUnsupportedMaterials();
        }

        private static void RepairUnsupportedMaterials()
        {
            Shader fallback = Shader.Find("Universal Render Pipeline/Lit");
            if (fallback == null)
            {
                return;
            }

            Dictionary<Material, Material> repaired = new();
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(
                FindObjectsSortMode.None
            );
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int index = 0; index < materials.Length; index++)
                {
                    Material source = materials[index];
                    if (source == null)
                    {
                        continue;
                    }

                    if (source.shader != null && source.shader.isSupported)
                    {
                        continue;
                    }

                    if (!repaired.TryGetValue(source, out Material replacement))
                    {
                        Color color = source.HasProperty("_Color")
                            ? source.color
                            : Color.white;
                        replacement = new Material(fallback)
                        {
                            name = $"{source.name}_URP_Repair",
                            color = color,
                            hideFlags = HideFlags.HideAndDontSave
                        };
                        repaired[source] = replacement;
                    }

                    materials[index] = replacement;
                    changed = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                }
            }
        }

        private void OnDestroy()
        {
            if (runtimeSky != null)
            {
                Destroy(runtimeSky);
            }
        }

        private void LateUpdate()
        {
            if (!firstPerson || kin == null)
            {
                return;
            }

            Vector3 targetPosition =
                kin.position +
                Vector3.up * eyeHeight +
                kin.forward * forwardOffset;

            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                followSpeed * Time.deltaTime
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                kin.rotation,
                followSpeed * Time.deltaTime
            );
        }
    }
}
