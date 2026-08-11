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
        private Vector2 mobileInput;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            mainCamera = Camera.main;

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

            Vector2 position = touchscreen.primaryTouch.position.ReadValue();
            if (touchscreen.primaryTouch.press.wasPressedThisFrame &&
                position.x < Screen.width * 0.42f &&
                position.y < Screen.height * 0.55f)
            {
                mobileDragActive = true;
                mobileDragOrigin = position;
            }

            if (touchscreen.primaryTouch.press.wasReleasedThisFrame)
            {
                mobileDragActive = false;
            }

            if (mobileDragActive && touchscreen.primaryTouch.press.isPressed)
            {
                float range = Mathf.Max(50f, Screen.height * 0.12f);
                mobileInput = Vector2.ClampMagnitude(
                    (position - mobileDragOrigin) / range,
                    1f
                );
            }
        }

        private void OnGUI()
        {
            if (!Application.isMobilePlatform)
            {
                return;
            }

            Rect hint = new Rect(
                22f,
                Screen.height - 118f,
                150f,
                88f
            );
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(hint, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.white;
            GUI.Label(hint, "ARRASTE AQUI\nPARA MOVER", style);
            GUI.color = previous;
        }
    }
}
