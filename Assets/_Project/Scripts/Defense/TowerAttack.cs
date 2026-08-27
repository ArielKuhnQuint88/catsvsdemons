using CatsVsDemons.Enemies;
using CatsVsDemons.Player;
using UnityEngine;

namespace CatsVsDemons.Defense
{
    public sealed class TowerAttack : MonoBehaviour
    {
        [SerializeField] private float range = 7f;
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private int damage = 5;
        [SerializeField] private float slowMultiplier = 0.45f;
        [SerializeField] private float kinPowerRange = 5f;
        [SerializeField] private float poweredRangeMultiplier = 1.55f;
        [SerializeField] private int poweredDamage = 9;
        [SerializeField] private int fireWaveDamage = 8;
        [SerializeField] private float fireWaveInterval = 3.2f;

        private float attackTimer;
        private float beamTimer;
        private float fireWaveTimer;
        private float slowRefreshTimer;
        private LineRenderer beam;
        private KinHealth kin;
        private Light powerLight;
        public bool IsPowered { get; private set; }
        private float CurrentRange => IsPowered ?
            range * poweredRangeMultiplier : range;

        private void Awake()
        {
            CreateBeam();
            kin = Object.FindFirstObjectByType<KinHealth>();
            CreatePowerLight();
        }

        private void Update()
        {
            UpdateBeam();
            UpdateKinPower();
            slowRefreshTimer -= Time.deltaTime;
            if (slowRefreshTimer <= 0f)
            {
                ApplyLanternSlow();
                slowRefreshTimer = 0.15f;
            }

            if (IsPowered)
            {
                fireWaveTimer -= Time.deltaTime;
                if (fireWaveTimer <= 0f)
                {
                    ReleaseFireWave();
                    fireWaveTimer = fireWaveInterval;
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
            target.TakeDamage(IsPowered ? poweredDamage : damage);
            ShowBeam(target.transform);
        }

        private void ApplyLanternSlow()
        {
            var enemies = EnemyRegistry.GetInRange(transform.position, CurrentRange);
            foreach (EnemyHealth health in enemies)
            {
                EnemyPathFollower enemy = health.GetComponent<EnemyPathFollower>();
                if (enemy != null)
                    enemy.ApplySlow(IsPowered ? slowMultiplier * 0.72f :
                        slowMultiplier, 0.2f);
            }
        }

        private EnemyHealth FindNearestTarget()
        {
            return EnemyRegistry.FindNearest(transform.position, CurrentRange);
        }

        private void UpdateKinPower()
        {
            if (kin == null) kin = Object.FindFirstObjectByType<KinHealth>();
            bool powered = kin != null && !kin.IsDown &&
                (kin.transform.position - transform.position).sqrMagnitude <=
                kinPowerRange * kinPowerRange;
            if (powered == IsPowered) return;
            IsPowered = powered;
            if (powerLight != null) powerLight.enabled = powered;
        }

        private void ReleaseFireWave()
        {
            var targets = EnemyRegistry.GetInRange(transform.position, CurrentRange);
            for (int index = targets.Count - 1; index >= 0; index--)
                targets[index].TakeDamage(fireWaveDamage);
        }

        private void CreatePowerLight()
        {
            GameObject glow = new("Kin Power Glow");
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = Vector3.up * 2f;
            powerLight = glow.AddComponent<Light>();
            powerLight.type = LightType.Point;
            powerLight.color = new Color(1f, 0.34f, 0.04f);
            powerLight.range = 5f;
            powerLight.intensity = 2.3f;
            powerLight.enabled = false;
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
            Gizmos.DrawWireSphere(transform.position, CurrentRange);

        }
    }
}
