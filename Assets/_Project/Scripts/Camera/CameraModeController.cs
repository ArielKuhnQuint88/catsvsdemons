using CatsVsDemons.Player;
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

        private void Start()
        {
            gameCamera = GetComponent<Camera>();
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
