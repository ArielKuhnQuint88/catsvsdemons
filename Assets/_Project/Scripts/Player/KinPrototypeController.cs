using CatsVsDemons.Visuals;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatsVsDemons.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class KinPrototypeController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private Vector2 horizontalBounds = new Vector2(-19f, 19f);
        [SerializeField] private Vector2 verticalBounds = new Vector2(-14f, 14f);

        private CharacterController characterController;
        private Camera mainCamera;
        private bool mobileDragActive;
        private Vector2 mobileDragOrigin;
        private Vector2 mobileDragPosition;
        private Vector2 mobileInput;
        private Texture2D joystickCircle;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            mainCamera = Camera.main;
            joystickCircle = CreateCircleTexture(128);

            RuntimeModelVisuals.Attach(
                transform,
                "Models/Kin",
                2.2f,
                -1f,
                new Color(0.95f, 0.92f, 0.88f)
            );
        }

        private void Update()
        {
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

            if (input.sqrMagnitude < 0.01f)
            {
                return;
            }

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

            Vector3 direction = (forward * input.y + right * input.x).normalized;
            characterController.SimpleMove(direction * moveSpeed);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                rotationSpeed * Time.deltaTime
            );

            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x, horizontalBounds.x, horizontalBounds.y);
            position.z = Mathf.Clamp(position.z, verticalBounds.x, verticalBounds.y);
            transform.position = position;
        }

        private void ReadMobileInput()
        {
            mobileInput = Vector2.zero;
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                mobileDragActive = false;
                return;
            }

            bool secondFingerPressed =
                touchscreen.touches.Count > 1 &&
                touchscreen.touches[1].press.isPressed;
            if (secondFingerPressed)
            {
                mobileDragActive = false;
                return;
            }

            Vector2 position = touchscreen.primaryTouch.position.ReadValue();
            if (touchscreen.primaryTouch.press.wasPressedThisFrame &&
                position.x < Screen.width * 0.42f &&
                position.y < Screen.height * 0.55f)
            {
                mobileDragActive = true;
                mobileDragOrigin = position;
                mobileDragPosition = position;
            }

            if (touchscreen.primaryTouch.press.wasReleasedThisFrame)
            {
                mobileDragActive = false;
            }

            if (mobileDragActive && touchscreen.primaryTouch.press.isPressed)
            {
                mobileDragPosition = position;
                float range = Mathf.Max(50f, Screen.height * 0.12f);
                mobileInput = Vector2.ClampMagnitude(
                    (position - mobileDragOrigin) / range,
                    1f
                );
            }
        }

        private void OnGUI()
        {
            if (!Application.isMobilePlatform || !mobileDragActive ||
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
            GUI.color = new Color(0.05f, 0.08f, 0.1f, 0.42f);
            DrawJoystickCircle(guiOrigin, outerRadius);
            GUI.color = new Color(1f, 0.78f, 0.18f, 0.82f);
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
