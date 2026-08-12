using CatsVsDemons.Player;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
        private Vector3 mobileTarget = new Vector3(0f, 1.5f, 3f);
        private Vector3 mobileViewDirection;
        private float mobileDistance;
        private float previousPinchDistance;
        private Vector2 previousTouchCenter;
        private bool twoFingerGestureActive;

        private void Start()
        {
            gameCamera = GetComponent<Camera>();
            firstPerson = !Application.isMobilePlatform &&
                PlayerPrefs.GetInt("CameraMode", 0) == 1;
            gameCamera.enabled = true;
            gameCamera.cullingMask = ~0;
            gameCamera.rect = new Rect(0f, 0f, 1f, 1f);
            gameCamera.targetTexture = null;
            gameCamera.ResetProjectionMatrix();
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
            if (Application.isMobilePlatform)
            {
                QualitySettings.SetQualityLevel(0, true);
            }

            gameCamera.orthographic = false;
            gameCamera.fieldOfView = Application.isMobilePlatform ? 40f : 42f;
            gameCamera.nearClipPlane = 0.3f;
            gameCamera.farClipPlane = 300f;

            Vector3 target = new Vector3(0f, 1.5f, 3f);
            transform.SetParent(null, true);
            transform.position = Application.isMobilePlatform
                ? new Vector3(0f, 18f, -29f)
                : new Vector3(0f, 22f, -38f);
            transform.rotation = Quaternion.LookRotation(
                target - transform.position,
                Vector3.up
            );

            if (Application.isMobilePlatform)
            {
                mobileTarget = target;
                mobileViewDirection =
                    (transform.position - mobileTarget).normalized;
                mobileDistance = Vector3.Distance(
                    transform.position,
                    mobileTarget
                );
            }
        }

        private void Update()
        {
            if (!Application.isMobilePlatform || firstPerson)
            {
                return;
            }

            ReadTwoFingerCameraGesture();
        }

        private void ReadTwoFingerCameraGesture()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null || touchscreen.touches.Count < 2)
            {
                twoFingerGestureActive = false;
                return;
            }

            var first = touchscreen.touches[0];
            var second = touchscreen.touches[1];
            if (!first.press.isPressed || !second.press.isPressed)
            {
                twoFingerGestureActive = false;
                return;
            }

            Vector2 firstPosition = first.position.ReadValue();
            Vector2 secondPosition = second.position.ReadValue();
            float pinchDistance = Vector2.Distance(
                firstPosition,
                secondPosition
            );
            Vector2 touchCenter = (firstPosition + secondPosition) * 0.5f;

            if (!twoFingerGestureActive)
            {
                twoFingerGestureActive = true;
                previousPinchDistance = pinchDistance;
                previousTouchCenter = touchCenter;
                return;
            }

            float pinchDelta = pinchDistance - previousPinchDistance;
            mobileDistance = Mathf.Clamp(
                mobileDistance - pinchDelta * 0.025f,
                20f,
                43f
            );

            Vector2 panDelta = touchCenter - previousTouchCenter;
            Vector3 cameraRight = Vector3.ProjectOnPlane(
                transform.right,
                Vector3.up
            ).normalized;
            Vector3 cameraForward = Vector3.ProjectOnPlane(
                transform.forward,
                Vector3.up
            ).normalized;
            float panScale = mobileDistance / Mathf.Max(600f, Screen.height);
            mobileTarget -= cameraRight * panDelta.x * panScale;
            mobileTarget -= cameraForward * panDelta.y * panScale;
            mobileTarget.x = Mathf.Clamp(mobileTarget.x, -10f, 10f);
            mobileTarget.z = Mathf.Clamp(mobileTarget.z, -7f, 11f);

            transform.position =
                mobileTarget + mobileViewDirection * mobileDistance;
            transform.rotation = Quaternion.LookRotation(
                mobileTarget - transform.position,
                Vector3.up
            );

            previousPinchDistance = pinchDistance;
            previousTouchCenter = touchCenter;
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
