using CatsVsDemons.Visuals;
using CatsVsDemons.CameraSystem;
using CatsVsDemons.Defense;
using UnityEngine.InputSystem.Controls;
using CatsVsDemons.House;
using CatsVsDemons.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatsVsDemons.Player
{
    [DefaultExecutionOrder(-30)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class KinPrototypeController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float gravity = -28f;
        [SerializeField] private Vector2 horizontalBounds = new Vector2(-19f, 19f);
        [SerializeField] private Vector2 verticalBounds = new Vector2(-14f, 14f);

        private CharacterController characterController;
        private Camera mainCamera;
        private CameraModeController cameraMode;
        private KinPrototypeAttack attack;
        private int movementFinger = -1;
        public Vector3 PlanarVelocity { get; private set; }
        public Vector3 LocalMotion => transform.InverseTransformDirection(PlanarVelocity) / Mathf.Max(.1f, moveSpeed);
        public bool IsMovingBackwards => LocalMotion.z < -.12f;
        private bool mobileDragActive;
        private Vector2 mobileDragOrigin;
        private Vector2 mobileDragPosition;
        private Vector2 mobileInput;
        private Texture2D joystickCircle;
        private Vector3 lastSafePosition;
        private float nextGroundCheck;
        private float verticalVelocity;
        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            mainCamera = Camera.main;
            attack = GetComponent<KinPrototypeAttack>();
            joystickCircle = CreateCircleTexture(128);
            lastSafePosition = transform.position;
            EnsureSafetyFloor();

            RuntimeModelVisuals.Attach(
                transform,
                "Models/Kin",
                2.2f,
                -1f,
                new Color(0.95f, 0.92f, 0.88f)
            );
            KinShopLoadout loadout = GetComponent<KinShopLoadout>();
            if (loadout == null) loadout = gameObject.AddComponent<KinShopLoadout>();
            loadout.Apply(HouseShopSave.GetEquipped(HouseShopCategory.Clothes),
                HouseShopSave.GetEquipped(HouseShopCategory.Accessories));
            if (GetComponent<KinCombatVisual>() == null) gameObject.AddComponent<KinCombatVisual>();
        }

        private void Update()
        {
            if (Time.timeScale <= 0 || KinGuideOverlay.IsOpen || KinGuideOverlay.ConsumedInputThisFrame)
            { PlanarVelocity = Vector3.zero; mobileDragActive = false; movementFinger = -1; return; }
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            if (!characterController.enabled)
                characterController.enabled = true;

            ValidateGroundPosition();
            if (mainCamera == null) mainCamera = Camera.main;
            if (cameraMode == null && mainCamera != null) cameraMode = mainCamera.GetComponent<CameraModeController>();
            ReadMobileInput();
            Keyboard keyboard = Keyboard.current;
            Vector2 input = mobileInput;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                    input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                    input.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                    input.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                    input.x -= 1f;
            }

            input = Vector2.ClampMagnitude(input, 1f);

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            Vector3 forward = mainCamera != null
                ? Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized
                : Vector3.forward;

            Vector3 right = mainCamera != null
                ? Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up).normalized
                : Vector3.right;

            if (cameraMode == null && mainCamera != null) cameraMode = mainCamera.GetComponent<CameraModeController>();
            if (attack == null) attack = GetComponent<KinPrototypeAttack>();
            bool firstPerson = cameraMode != null && cameraMode.IsFirstPerson;
            bool combat = attack != null && attack.HasCombatTarget;
            Vector3 direction = KinMotionMath.Direction(input, forward, right, transform.forward, transform.right, firstPerson, combat);
            float speed = moveSpeed * (input.y < -.05f ? .85f : 1f);
            PlanarVelocity = KinMotionMath.ApproachVelocity(PlanarVelocity, direction, speed, Time.deltaTime);

            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity = Mathf.Max(verticalVelocity + gravity * Time.deltaTime,
                    gravity);

            Vector3 beforeMovement = transform.position;
            Vector3 motion = PlanarVelocity;
            motion.y = verticalVelocity;
            CollisionFlags flags = characterController.Move(motion * Time.deltaTime);
            if ((flags & CollisionFlags.Below) != 0)
                verticalVelocity = -2f;

            Vector3 position = transform.position;
            Vector3 correction = new Vector3(
                Mathf.Clamp(position.x, horizontalBounds.x, horizontalBounds.y) -
                    position.x,
                0f,
                Mathf.Clamp(position.z, verticalBounds.x, verticalBounds.y) -
                    position.z
            );
            if (correction.sqrMagnitude > 0.0001f)
                characterController.Move(correction);

            if (KinMotionMath.TurnWithMovement(input, firstPerson, combat) && direction.sqrMagnitude > .01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction, Vector3.up),
                    1f - Mathf.Exp(-rotationSpeed * Time.deltaTime)
                );
            }

            // Animation reads actual displacement, including walls and bounds, instead of input alone.
            Vector3 actual = transform.position - beforeMovement; actual.y = 0;
            if (Time.deltaTime > 0) PlanarVelocity = actual / Time.deltaTime;
            if (characterController.isGrounded) RememberSafePosition();
        }

        public void TeleportTo(Vector3 position)
        {
            Vector3 safeDestination = FindGroundedPosition(position,
                out Vector3 grounded) ? grounded : lastSafePosition;
            characterController.enabled = false;
            transform.position = safeDestination;
            characterController.enabled = true;
            verticalVelocity = -2f;
            PlanarVelocity = Vector3.zero;
            lastSafePosition = safeDestination;
        }

        private void ValidateGroundPosition()
        {
            if (transform.position.y < -0.9f)
            {
                RecoverFromFall();
                return;
            }

            if (Time.time < nextGroundCheck)
                return;

            nextGroundCheck = Time.time + 0.2f;
            RememberSafePosition();
        }

        private void RememberSafePosition()
        {
            Vector3 origin = transform.position + Vector3.up * 2f;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                5f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );
            float nearestDistance = float.MaxValue;
            RaycastHit groundHit = default;
            bool foundGround = false;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = groundHits[index];
                if (hit.collider == null ||
                    hit.collider.transform.IsChildOf(transform) ||
                    hit.normal.y < 0.55f ||
                    hit.distance >= nearestDistance)
                    continue;
                nearestDistance = hit.distance;
                groundHit = hit;
                foundGround = true;
            }

            if (foundGround)
            {
                Vector3 candidate = transform.position;
                float standingOffset =
                    characterController.height * 0.5f -
                    characterController.center.y +
                    characterController.skinWidth;
                candidate.y = groundHit.point.y + standingOffset;
                lastSafePosition = candidate;
            }
        }

        private void RecoverFromFall()
        {
            characterController.enabled = false;
            transform.position = lastSafePosition;
            characterController.enabled = true;
            verticalVelocity = -2f;
            Debug.LogWarning("Kin saiu do terreno e foi reposicionado.", this);
        }

        private bool FindGroundedPosition(Vector3 position, out Vector3 grounded)
        {
            Vector3 origin = new Vector3(position.x,
                Mathf.Max(position.y + 8f, 12f), position.z);
            int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down,
                groundHits, 30f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = float.MaxValue;
            grounded = position;
            bool found = false;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = groundHits[index];
                if (hit.collider == null ||
                    hit.collider.transform.IsChildOf(transform) ||
                    hit.normal.y < 0.55f || hit.distance >= nearestDistance)
                    continue;
                nearestDistance = hit.distance;
                float standingOffset = characterController.height * 0.5f -
                    characterController.center.y + characterController.skinWidth;
                grounded = hit.point + Vector3.up * standingOffset;
                found = true;
            }
            return found;
        }

        private void EnsureSafetyFloor()
        {
            const string floorName = "Kin Safety Floor";
            if (GameObject.Find(floorName) != null) return;
            GameObject floor = new(floorName);
            floor.hideFlags = HideFlags.HideInHierarchy;
            float centerX = (horizontalBounds.x + horizontalBounds.y) * 0.5f;
            float centerZ = (verticalBounds.x + verticalBounds.y) * 0.5f;
            floor.transform.position = new Vector3(centerX, -0.45f, centerZ);
            BoxCollider collider = floor.AddComponent<BoxCollider>();
            collider.size = new Vector3(
                horizontalBounds.y - horizontalBounds.x + 4f,
                0.4f,
                verticalBounds.y - verticalBounds.x + 4f
            );
        }

        private void ReadMobileInput()
        {
            mobileInput = Vector2.zero;
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null || TowerBuildSelection.AwaitingPlacement)
            { mobileDragActive = false; movementFinger = -1; return; }
            if (cameraMode == null && mainCamera != null) cameraMode = mainCamera.GetComponent<CameraModeController>();
            int fingers = 0;
            foreach (var touch in touchscreen.touches) if (touch.press.isPressed) fingers++;
            if (fingers > 1 && (cameraMode == null || !cameraMode.IsFirstPerson))
            { mobileDragActive = false; movementFinger = -1; return; }
            TouchControl active = null;
            foreach (var touch in touchscreen.touches)
                if (touch.press.isPressed && touch.touchId.ReadValue() == movementFinger) { active = touch; break; }
            if (active == null)
            {
                mobileDragActive = false; movementFinger = -1;
                foreach (var touch in touchscreen.touches)
                {
                    if (!touch.press.wasPressedThisFrame) continue;
                    Vector2 at = touch.position.ReadValue();
                    if (at.x >= Screen.width * .42f || at.y >= Screen.height * .55f || GameUiInput.IsOverControl(at)) continue;
                    movementFinger = touch.touchId.ReadValue(); mobileDragOrigin = mobileDragPosition = at;
                    mobileDragActive = true; active = touch; break;
                }
            }
            if (active == null) return;
            mobileDragPosition = active.position.ReadValue();
            mobileInput = Vector2.ClampMagnitude((mobileDragPosition - mobileDragOrigin) / Mathf.Max(50, Screen.height * .12f), 1);
        }

        private void OnGUI()
        {
            if (!Application.isMobilePlatform || Time.timeScale <= 0f || KinGuideOverlay.IsOpen || !mobileDragActive ||
                joystickCircle == null)
            {
                return;
            }

            float outerRadius = Mathf.Max(54f, Screen.height * 0.085f);
            float knobRadius = outerRadius * 0.38f;
            Vector2 guiOrigin = new Vector2(
                mobileDragOrigin.x,
                Screen.height - mobileDragOrigin.y
            );
            Vector2 guiPosition = new Vector2(
                mobileDragPosition.x,
                Screen.height - mobileDragPosition.y
            );
            Vector2 offset = Vector2.ClampMagnitude(
                guiPosition - guiOrigin,
                outerRadius
            );
            Color previous = GUI.color;
            GUI.color = new Color(.045f, .13f, .10f, .6f);
            DrawJoystickCircle(guiOrigin, outerRadius);
            GUI.color = new Color(RuntimeUiFactory.Gold.r, RuntimeUiFactory.Gold.g, RuntimeUiFactory.Gold.b, .62f);
            GUI.DrawTexture(new Rect(guiOrigin.x - outerRadius, guiOrigin.y - outerRadius, outerRadius * 2, outerRadius * 2),
                GameUiArt.Ring.texture, ScaleMode.StretchToFill, true);
            GUI.color = RuntimeUiFactory.Paper;
            DrawJoystickCircle(guiOrigin + offset, knobRadius + 2);
            GUI.color = RuntimeUiFactory.Jade;
            DrawJoystickCircle(guiOrigin + offset, knobRadius);
            GUI.color = previous;
        }

        private void DrawJoystickCircle(Vector2 center, float radius)
        {
            GUI.DrawTexture(
                new Rect(center.x - radius, center.y - radius,
                    radius * 2f, radius * 2f),
                joystickCircle,
                ScaleMode.StretchToFill,
                true
            );
        }

        private static Texture2D CreateCircleTexture(int size)
        {
            Texture2D texture = new Texture2D(
                size, size, TextureFormat.RGBA32, false
            )
            {
                name = "MobileJoystickCircle",
                hideFlags = HideFlags.HideAndDontSave
            };
            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float radius = center - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    byte alpha = dx * dx + dy * dy <= radius * radius
                        ? (byte)255
                        : (byte)0;
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void OnDestroy()
        {
            if (joystickCircle != null)
            {
                Destroy(joystickCircle);
            }
        }
    }
}
