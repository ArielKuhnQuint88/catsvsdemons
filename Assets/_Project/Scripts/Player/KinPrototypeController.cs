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

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            Vector2 input = Vector2.zero;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                input.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                input.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                input.x -= 1f;

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
    }
}
