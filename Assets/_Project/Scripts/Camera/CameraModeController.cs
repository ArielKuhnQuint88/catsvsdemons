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
        private GameObject horizonGround;
        private Material horizonMaterial;

        private void Start()
        {
            gameCamera = GetComponent<Camera>();
            firstPerson = PlayerPrefs.GetInt("CameraMode", 0) == 1;
            ConfigureEnvironment();

            if (!firstPerson)
            {
                ConfigureIsometricCamera();
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
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color(0.28f, 0.48f, 0.68f);
            RenderSettings.skybox = null;

            Shader groundShader = Shader.Find("Universal Render Pipeline/Lit");
            if (groundShader != null)
            {
                horizonGround = GameObject.CreatePrimitive(PrimitiveType.Plane);
                horizonGround.name = "Runtime Horizon Ground";
                horizonGround.transform.position = new Vector3(0f, -0.22f, 20f);
                horizonGround.transform.localScale = new Vector3(18f, 1f, 18f);

                Collider groundCollider = horizonGround.GetComponent<Collider>();
                if (groundCollider != null)
                {
                    Destroy(groundCollider);
                }

                horizonMaterial = new Material(groundShader)
                {
                    name = "Runtime Garden Horizon",
                    color = new Color(0.12f, 0.28f, 0.16f),
                    hideFlags = HideFlags.HideAndDontSave
                };
                horizonGround.GetComponent<Renderer>().sharedMaterial =
                    horizonMaterial;
            }

            RepairUnsupportedMaterials();
        }

        private void ConfigureIsometricCamera()
        {
            gameCamera.orthographic = false;
            gameCamera.fieldOfView = 42f;
            gameCamera.nearClipPlane = 0.3f;
            gameCamera.farClipPlane = 300f;

            Vector3 target = new Vector3(0f, 1.5f, 3f);
            transform.position = new Vector3(0f, 22f, -38f);
            transform.rotation = Quaternion.LookRotation(
                target - transform.position,
                Vector3.up
            );
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

                    bool gardenMaterial = source.name.StartsWith("Garden_");
                    bool unsupported =
                        source.shader == null || !source.shader.isSupported;
                    if (!gardenMaterial && !unsupported)
                    {
                        continue;
                    }

                    if (!repaired.TryGetValue(source, out Material replacement))
                    {
                        Color color = source.HasProperty("_BaseColor")
                            ? source.GetColor("_BaseColor")
                            : source.HasProperty("_Color")
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
            if (horizonGround != null)
            {
                Destroy(horizonGround);
            }

            if (horizonMaterial != null)
            {
                Destroy(horizonMaterial);
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
