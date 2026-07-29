using CatsVsDemons.Enemies;
using UnityEngine;

namespace CatsVsDemons.Defense
{
    public sealed class TowerAttack : MonoBehaviour
    {
        [SerializeField] private float range = 7f;
        [SerializeField] private float kinActivationRange = 5f;
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private int damage = 5;
        [SerializeField] private float slowMultiplier = 0.45f;

        private float attackTimer;
        private float beamTimer;
        private LineRenderer beam;
        private Transform kin;

        private void Awake()
        {
            FindKin();
            CreateBeam();
        }

        private void Update()
        {
            UpdateBeam();

            if (!IsKinNearby())
            {
                return;
            }

            ApplyLanternSlow();

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

        private bool IsKinNearby()
        {
            if (kin == null)
            {
                FindKin();
            }

            return kin != null &&
                Vector3.Distance(transform.position, kin.position)
                    <= kinActivationRange;
        }

        private void FindKin()
        {
            GameObject player = GameObject.FindWithTag("Player");

            if (player != null)
            {
                kin = player.transform;
                return;
            }

            GameObject playerGroup = GameObject.Find("Game/Player");
            if (playerGroup != null && playerGroup.transform.childCount > 0)
            {
                kin = playerGroup.transform.GetChild(0);
                return;
            }

            GameObject kinObject = GameObject.Find("Kin_Prototype");
            if (kinObject == null)
            {
                kinObject = GameObject.Find("Kin");
            }

            kin = kinObject != null ? kinObject.transform : null;
        }

        private void ApplyLanternSlow()
        {
            EnemyPathFollower[] enemies =
                Object.FindObjectsByType<EnemyPathFollower>(
                    FindObjectsSortMode.None
                );

            foreach (EnemyPathFollower enemy in enemies)
            {
                if (Vector3.Distance(
                    transform.position,
                    enemy.transform.position
                ) <= range)
                {
                    enemy.ApplySlow(slowMultiplier, 0.2f);
                }
            }
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

        private void CreateBeam()
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

        private void UpdateBeam()
        {
            if (beamTimer <= 0f)
            {
                return;
            }

            beamTimer -= Time.deltaTime;
            if (beamTimer <= 0f)
            {
                beam.enabled = false;
            }
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

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, kinActivationRange);
        }
    }
}
