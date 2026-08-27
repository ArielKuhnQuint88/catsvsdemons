using CatsVsDemons.Player;
using UnityEngine;

namespace CatsVsDemons.Defense
{
    public sealed class BonsaiHealing : MonoBehaviour
    {
        [SerializeField] private float healingRange = 5f;
        [SerializeField] private int healingAmount = 5;
        [SerializeField] private float healingInterval = 1f;
        [SerializeField] private float defenseHealingRange = 7f;
        [SerializeField] private int defenseHealingAmount = 4;

        private KinHealth kin;
        private float healingTimer;

        private void Awake()
        {
            kin = Object.FindFirstObjectByType<KinHealth>();
        }

        private void Update()
        {
            if (kin == null)
            {
                kin = Object.FindFirstObjectByType<KinHealth>();
                return;
            }

            healingTimer -= Time.deltaTime;

            if (healingTimer > 0f || kin.IsDown)
            {
                return;
            }

            if (Vector3.Distance(transform.position, kin.transform.position)
                > healingRange)
            {
                return;
            }

            kin.Heal(healingAmount);
            DefenseRegistry.HealInRange(
                transform.position, defenseHealingRange, defenseHealingAmount);
            CatsVsDemons.Feedback.GameFeedback.PlayHeal();
            healingTimer = healingInterval;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, healingRange);
            Gizmos.color = new Color(0.2f, 1f, 0.55f, 0.65f);
            Gizmos.DrawWireSphere(transform.position, defenseHealingRange);
        }
    }
}
