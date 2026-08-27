using System.Collections.Generic;
using CatsVsDemons.Player;
using UnityEngine;

namespace CatsVsDemons.Defense
{
    public sealed class PortalTransport : MonoBehaviour
    {
        private static readonly List<PortalTransport> Portals = new();
        private static float nextTeleportTime;

        [SerializeField] private float activationDistance = 1.5f;
        [SerializeField] private float teleportCooldown = 1.5f;
        [SerializeField] private float exitDistance = 2.2f;

        private KinHealth kin;

        private void OnEnable()
        {
            if (!Portals.Contains(this))
            {
                Portals.Add(this);
            }

            FindKin();
        }

        private void OnDisable()
        {
            Portals.Remove(this);
        }

        private void Update()
        {
            if (Portals.Count < 2 || Time.time < nextTeleportTime)
            {
                return;
            }

            if (kin == null)
            {
                FindKin();
            }

            if (kin == null || kin.IsDown)
            {
                return;
            }

            if (Vector3.Distance(transform.position, kin.transform.position)
                > activationDistance)
            {
                return;
            }

            int currentIndex = Portals.IndexOf(this);
            int destinationIndex = (currentIndex + 1) % Portals.Count;
            PortalTransport destination = Portals[destinationIndex];

            Vector3 exitDirection =
                (destination.transform.position - transform.position).normalized;

            if (exitDirection.sqrMagnitude < 0.1f)
            {
                exitDirection = Vector3.forward;
            }

            Vector3 exitPosition =
                destination.transform.position +
                exitDirection * exitDistance;

            exitPosition.y = kin.transform.position.y;
            KinPrototypeController controller =
                kin.GetComponent<KinPrototypeController>();
            if (controller != null)
                controller.TeleportTo(exitPosition);
            else
                kin.transform.position = exitPosition;
            nextTeleportTime = Time.time + teleportCooldown;
            CatsVsDemons.Feedback.GameFeedback.PlayPortal();

            Debug.Log("Kin atravessou um portal.");
        }

        private void FindKin()
        {
            kin = Object.FindFirstObjectByType<KinHealth>();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, activationDistance);
        }
    }
}
