using CatsVsDemons.Player;
using UnityEngine;

namespace CatsVsDemons.Enemies
{
    public sealed class EnemyContactDamage : MonoBehaviour
    {
        [SerializeField] private int damage = 10;
        [SerializeField] private float attackDistance = 1.3f;
        [SerializeField] private float attackInterval = 1f;

        private KinHealth kin;
        private float attackTimer;

        private void Awake()
        {
            kin = Object.FindFirstObjectByType<KinHealth>();
        }

        private void Update()
        {
            if (kin == null || kin.IsDown)
            {
                kin = Object.FindFirstObjectByType<KinHealth>();
                return;
            }

            attackTimer -= Time.deltaTime;

            if (attackTimer > 0f)
            {
                return;
            }

            if (Vector3.Distance(transform.position, kin.transform.position)
                > attackDistance)
            {
                return;
            }

            kin.TakeDamage(damage);
            attackTimer = attackInterval;
            Debug.Log($"Demon hit Kin for {damage} damage.");
        }
    }
}
