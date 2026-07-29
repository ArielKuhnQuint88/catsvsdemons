using System.Collections.Generic;
using CatsVsDemons.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatsVsDemons.Player
{
    public sealed class KinPrototypeAttack : MonoBehaviour
    {
        [SerializeField] private int damage = 10;
        [SerializeField] private float attackRange = 2.5f;
        [SerializeField] private float attackAngle = 120f;
        [SerializeField] private float cooldown = 0.4f;

        private float nextAttackTime;

        private void Update()
        {
            bool mouseAttack =
                Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame;

            bool keyboardAttack =
                Keyboard.current != null &&
                Keyboard.current.spaceKey.wasPressedThisFrame;

            if ((mouseAttack || keyboardAttack) &&
                Time.time >= nextAttackTime)
            {
                Attack();
            }
        }

        private void Attack()
        {
            nextAttackTime = Time.time + cooldown;

            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                attackRange
            );

            HashSet<EnemyHealth> damagedEnemies = new();
            float minimumDot =
                Mathf.Cos(attackAngle * 0.5f * Mathf.Deg2Rad);

            foreach (Collider hit in hits)
            {
                EnemyHealth enemy =
                    hit.GetComponentInParent<EnemyHealth>();

                if (enemy == null ||
                    enemy.IsDead ||
                    damagedEnemies.Contains(enemy))
                {
                    continue;
                }

                Vector3 direction =
                    enemy.transform.position - transform.position;
                direction.y = 0f;

                if (direction.sqrMagnitude < 0.01f)
                {
                    continue;
                }

                float dot = Vector3.Dot(
                    transform.forward,
                    direction.normalized
                );

                if (dot < minimumDot)
                {
                    continue;
                }

                damagedEnemies.Add(enemy);
                enemy.TakeDamage(damage);
            }

            Debug.Log(
                damagedEnemies.Count > 0
                    ? $"Kin hit {damagedEnemies.Count} demon(s)."
                    : "Kin attacked but hit nothing."
            );
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
