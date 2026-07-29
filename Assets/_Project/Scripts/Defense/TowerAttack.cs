using CatsVsDemons.Enemies;
using UnityEngine;

namespace CatsVsDemons.Defense
{
    public sealed class TowerAttack : MonoBehaviour
    {
        [SerializeField] private float range = 7f;
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private int damage = 5;

        private float attackTimer;
        private float beamTimer;
        private LineRenderer beam;

        private void Awake()
        {
            beam = gameObject.AddComponent<LineRenderer>();
            beam.positionCount = 2;
            beam.startWidth = 0.12f;
            beam.endWidth = 0.04f;
            beam.startColor = new Color(1f, 0.75f, 0.1f);
            beam.endColor = new Color(1f, 0.15f, 0.02f);
            beam.useWorldSpace = true;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            beam.material = new Material(shader);
            beam.enabled = false;
        }

        private void Update()
        {
            if (beamTimer > 0f)
            {
                beamTimer -= Time.deltaTime;
                if (beamTimer <= 0f)
                {
                    beam.enabled = false;
                }
            }

            attackTimer -= Time.deltaTime;
            if (attackTimer > 0f)
            {
                return;
            }

            EnemyHealth target = FindNearestTarget();
            if (target == null)
            {
                return;
            }

            attackTimer = attackInterval;
            target.TakeDamage(damage);
            ShowBeam(target.transform);
        }

        private EnemyHealth FindNearestTarget()
        {
            EnemyHealth[] enemies =
                Object.FindObjectsByType<EnemyHealth>(
                    FindObjectsSortMode.None
                );

            EnemyHealth nearest = null;
            float nearestDistance = range;

            foreach (EnemyHealth enemy in enemies)
            {
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    transform.position,
                    enemy.transform.position
                );

                if (distance <= nearestDistance)
                {
                    nearest = enemy;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private void ShowBeam(Transform target)
        {
            beam.SetPosition(
                0,
                transform.position + Vector3.up * 1.75f
            );
            beam.SetPosition(
                1,
                target.position + Vector3.up * 0.7f
            );
            beam.enabled = true;
            beamTimer = 0.12f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.45f, 0.05f);
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
