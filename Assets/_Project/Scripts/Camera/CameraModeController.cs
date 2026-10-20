using CatsVsDemons.Player;
using CatsVsDemons.Waves;
using CatsVsDemons.Defense;
using CatsVsDemons.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatsVsDemons.CameraSystem
{
    [DefaultExecutionOrder(200)]
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
        private float baseIsometricDistance;
        private float previousPinchDistance;
        private Vector2 previousTouchCenter;
        private bool twoFingerGestureActive;
        private Transform tutorialTarget;
        private float tutorialZoom = 0.84f;
        private float tutorialBlend;
        private Vector3 renderedTarget;
        private float renderedDistance;
        private readonly Dictionary<Renderer, bool> hiddenRenderers = new();
        private KinCombatVisual combatVisual;
        private float nextVisibilityCheck;
        private float lookYaw, lookPitch;
        private int lookFinger = -1;
        private Vector2 lookPrevious;
        public bool IsFirstPerson => firstPerson;
        public bool CanSwitchView => gameCamera != null && kin != null && gameCamera.isActiveAndEnabled && isActiveAndEnabled && Time.timeScale > 0 &&
            !KinGuideOverlay.IsOpen && !KinGuideOverlay.ConsumedInputThisFrame;
        public event System.Action<bool> ViewChanged;

        private void Start()
        {
            gameCamera = GetComponent<Camera>();
            if (gameCamera == null) return;
            gameCamera.enabled = true; gameCamera.cullingMask = ~0;
            gameCamera.rect = new Rect(0, 0, 1, 1); gameCamera.targetTexture = null; gameCamera.ResetProjectionMatrix();
            ConfigureEnvironment(); ConfigureIsometricCamera();
            KinHealth health = Object.FindFirstObjectByType<KinHealth>();
            if (health != null) kin = health.transform;
            firstPerson = false;
            if (PlayerPrefs.GetInt("CameraMode", 0) == 1 && kin != null) ChangeView(true);
        }

        public bool ToggleView()
        {
            if (!CanSwitchView) return false;
            ChangeView(!firstPerson); return true;
        }

        private void ChangeView(bool requested)
        {
            if (requested == firstPerson) return;
            if (kin == null) return;
            TowerBuildSelection.CancelPlacement();
            firstPerson = requested; twoFingerGestureActive = false; lookFinger = -1;
            tutorialTarget = null; tutorialBlend = 0;
            if (firstPerson)
            {
                lookYaw = kin.eulerAngles.y; lookPitch = 0;
                gameCamera.orthographic = false; gameCamera.fieldOfView = 68;
                gameCamera.nearClipPlane = .045f; gameCamera.farClipPlane = 300;
                EnsureCombatVisual();
                RefreshKinVisibility();
                UpdateFirstPersonPose(true);
            }
            else
            {
                RestoreKinVisibility();
                gameCamera.orthographic = false;
                gameCamera.fieldOfView = Application.isMobilePlatform ? 44 : 46;
                gameCamera.nearClipPlane = .3f; gameCamera.farClipPlane = 300;
                renderedTarget = mobileTarget; renderedDistance = mobileDistance;
                transform.position = mobileTarget + mobileViewDirection * mobileDistance;
                transform.rotation = Quaternion.LookRotation(mobileTarget - transform.position, Vector3.up);
            }
            EnsureCombatVisual();
            combatVisual?.SetFirstPerson(firstPerson, gameCamera);
            PlayerPrefs.SetInt("CameraMode", firstPerson ? 1 : 0); PlayerPrefs.Save();
            ViewChanged?.Invoke(firstPerson);
        }

        private void EnsureCombatVisual()
        {
            if (kin == null || combatVisual != null) return;
            combatVisual = kin.GetComponent<KinCombatVisual>();
            if (combatVisual == null) combatVisual = kin.gameObject.AddComponent<KinCombatVisual>();
        }

        private void RefreshKinVisibility()
        {
            if (kin == null) return;
            // Renderer.enabled continues to belong to the rig/fallback and equipment systems.
            // forceRenderingOff hides the self model without destroying their visibility state.
            foreach (Renderer renderer in kin.GetComponentsInChildren<Renderer>(true))
            {
                if (!hiddenRenderers.ContainsKey(renderer)) hiddenRenderers.Add(renderer, renderer.forceRenderingOff);
                renderer.forceRenderingOff = true;
            }
            nextVisibilityCheck = Time.unscaledTime + .2f;
        }

        private void RestoreKinVisibility()
        {
            foreach (var entry in hiddenRenderers)
                if (entry.Key != null) entry.Key.forceRenderingOff = entry.Value;
            hiddenRenderers.Clear();
        }

        public Vector3 WorldToIsometricViewport(Vector3 world)
        {
            if (gameCamera == null || mobileDistance <= 0) return gameCamera != null ? gameCamera.WorldToViewportPoint(world) : Vector3.zero;
            Vector3 at = mobileTarget + mobileViewDirection * mobileDistance;
            Quaternion orientation = Quaternion.LookRotation(mobileTarget - at, Vector3.up);
            Vector3 local = Quaternion.Inverse(orientation) * (world - at);
            float halfHeight = Mathf.Max(.0001f, Mathf.Abs(local.z) * Mathf.Tan((Application.isMobilePlatform ? 44 : 46) * Mathf.Deg2Rad * .5f));
            return new Vector3(.5f + local.x / (2 * halfHeight * Mathf.Max(.1f, gameCamera.aspect)), .5f + local.y / (2 * halfHeight), local.z);
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
            gameCamera.fieldOfView = Application.isMobilePlatform ? 44f : 46f;
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

            mobileTarget = target;
            mobileViewDirection =
                (transform.position - mobileTarget).normalized;
            mobileDistance = Vector3.Distance(
                transform.position,
                mobileTarget
            );
            baseIsometricDistance = mobileDistance;
            renderedTarget = mobileTarget;
            renderedDistance = mobileDistance;
        }

        public void FocusTutorial(Transform target, float zoom = 0.84f)
        {
            if (firstPerson || target == null) return;
            tutorialTarget = target;
            tutorialZoom = Mathf.Clamp(zoom, 0.75f, 1f);
        }

        public void ClearTutorialFocus() => tutorialTarget = null;

        public void SetPhaseZoom(int phase)
        {
            if (gameCamera == null || baseIsometricDistance <= 0f)
            {
                return;
            }

            int scenario = CampaignProgress.GetScenarioIndex(phase);
            int season = CampaignProgress.GetSeasonIndex(phase);
            float multiplier = 1f + scenario * 0.08f + season * 0.04f;
            mobileDistance = baseIsometricDistance * multiplier;
            if (firstPerson) return;
            transform.position =
                mobileTarget + mobileViewDirection * mobileDistance;
            transform.rotation = Quaternion.LookRotation(
                mobileTarget - transform.position,
                Vector3.up
            );
        }

        private void Update()
        {
            if (Time.timeScale <= 0 || KinGuideOverlay.IsOpen || KinGuideOverlay.ConsumedInputThisFrame)
            { twoFingerGestureActive = false; lookFinger = -1; return; }
            if (firstPerson) ReadFirstPersonLook();
            else if (Application.isMobilePlatform) ReadTwoFingerCameraGesture();
        }

        private void ReadFirstPersonLook()
        {
            if (kin == null || TowerBuildSelection.AwaitingPlacement) { lookFinger = -1; return; }
            Vector2 delta = Vector2.zero;
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed && !mouse.rightButton.wasPressedThisFrame &&
                !GameUiInput.IsOverControl(mouse.position.ReadValue())) delta = mouse.delta.ReadValue() * .16f;
            Touchscreen screen = Touchscreen.current;
            if (screen != null)
            {
                bool found = false;
                foreach (var touch in screen.touches)
                {
                    if (!touch.press.isPressed) continue;
                    Vector2 at = touch.position.ReadValue(); int id = touch.touchId.ReadValue();
                    if (lookFinger < 0 && touch.press.wasPressedThisFrame && at.x > Screen.width * .52f && !GameUiInput.IsOverControl(at))
                    { lookFinger = id; lookPrevious = at; }
                    if (id != lookFinger) continue;
                    delta += (at - lookPrevious) * (90f / Mathf.Max(360, Screen.height)); lookPrevious = at; found = true; break;
                }
                if (!found) lookFinger = -1;
            }
            lookYaw += delta.x; lookPitch = Mathf.Clamp(lookPitch - delta.y, -58, 65);
            kin.rotation = Quaternion.Euler(0, lookYaw, 0);
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
                // Continue from the view currently on screen when the player takes over.
                if (tutorialTarget != null || tutorialBlend > 0.001f)
                {
                    mobileTarget = renderedTarget;
                    mobileDistance = renderedDistance;
                    tutorialTarget = null;
                    tutorialBlend = 0f;
                }
                twoFingerGestureActive = true;
                previousPinchDistance = pinchDistance;
                previousTouchCenter = touchCenter;
                return;
            }

            float pinchDelta = pinchDistance - previousPinchDistance;
            mobileDistance = Mathf.Clamp(
                mobileDistance - pinchDelta * 0.025f,
                20f,
                60f
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
            RestoreKinVisibility();
            combatVisual?.SetFirstPerson(false, null);
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
            if (Time.timeScale <= 0) return;
            if (kin == null)
            {
                KinHealth health = Object.FindFirstObjectByType<KinHealth>();
                if (health != null) kin = health.transform;
            }
            if (!firstPerson) { UpdateTutorialFraming(); return; }
            if (kin == null) return;
            if (Time.unscaledTime >= nextVisibilityCheck) RefreshKinVisibility();
            UpdateFirstPersonPose(false);
        }

        private void UpdateFirstPersonPose(bool snap)
        {
            EnsureCombatVisual();
            Vector3 eye = combatVisual != null ? combatVisual.EyePosition : kin.position + Vector3.up * eyeHeight;
            Vector3 target = eye + kin.forward * Mathf.Min(forwardOffset, .18f);
            Quaternion rotation = Quaternion.Euler(lookPitch, lookYaw, 0);
            float amount = snap ? 1 : 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target, amount);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, amount);
        }

        private void UpdateTutorialFraming()
        {
            if (gameCamera == null || baseIsometricDistance <= 0 || Time.timeScale <= 0) return;
            float smoothing = 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime);
            tutorialBlend = Mathf.Lerp(tutorialBlend, tutorialTarget != null ? 1 : 0, smoothing);
            Vector3 focus = tutorialTarget != null ? tutorialTarget.position : mobileTarget;
            focus.y = mobileTarget.y;
            Vector3 target = Vector3.Lerp(mobileTarget, focus, tutorialBlend);
            float distance = Mathf.Lerp(mobileDistance, Mathf.Max(22f, mobileDistance * tutorialZoom), tutorialBlend);
            renderedTarget = Vector3.Lerp(renderedTarget, target, smoothing);
            renderedDistance = Mathf.Lerp(renderedDistance, distance, smoothing);
            transform.position = renderedTarget + mobileViewDirection * renderedDistance;
            transform.rotation = Quaternion.LookRotation(renderedTarget - transform.position, Vector3.up);
        }
    }
}
