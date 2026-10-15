using CatsVsDemons.Enemies;
using CatsVsDemons.Visuals;
using UnityEngine;

namespace CatsVsDemons.Player
{
    [DefaultExecutionOrder(-20)]
    public sealed class KinPrototypeAttack : MonoBehaviour
    {
        [SerializeField] private int damage = 10;
        [SerializeField] private float attackRange = 2.5f;
        [SerializeField] private float cooldown = 0.4f;
        [SerializeField] private float rotationSpeed = 14f;

        private float nextAttackTime;
        private KinEnergy energy;
        private int baseDamage;

        public int Damage => damage;
        public EnemyHealth CurrentTarget { get; private set; }
        public bool HasCombatTarget => CurrentTarget != null && !CurrentTarget.IsDead && CurrentTarget.isActiveAndEnabled;
        public event System.Action AttackStarted;
        private CatsVsDemons.CameraSystem.CameraModeController cameraMode;

        private void Awake()
        {
            baseDamage = Mathf.Max(1, damage);
            energy = GetComponent<KinEnergy>();
            if (energy == null) energy = gameObject.AddComponent<KinEnergy>();
            if (GetComponent<KinSpecialAttack>() == null)
                gameObject.AddComponent<KinSpecialAttack>();
        }

        public void SetShopDamageBonus(int bonus)
        {
            damage = baseDamage + Mathf.Max(0, bonus);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            EnemyHealth target = FindNearestTarget();
            CurrentTarget = target;
            if (target == null)
            {
                return;
            }

            Vector3 direction = target.transform.position - transform.position;
            direction.y = 0f;
            if (cameraMode == null && Camera.main != null) cameraMode = Camera.main.GetComponent<CatsVsDemons.CameraSystem.CameraModeController>();
            if ((cameraMode == null || !cameraMode.IsFirstPerson) && direction.sqrMagnitude > .01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    1f - Mathf.Exp(-rotationSpeed * Time.deltaTime)
                );
            }

            if (Time.time >= nextAttackTime)
            {
                Attack(target);
            }
        }

        private void Attack(EnemyHealth target)
        {
            nextAttackTime = Time.time + cooldown;
            AttackStarted?.Invoke();

            ProceduralModelAnimator modelAnimator =
                GetComponentInChildren<ProceduralModelAnimator>();
            if (modelAnimator != null)
            {
                modelAnimator.TriggerAttack();
            }

            if (target != null && !target.IsDead)
            {
                target.TakeDamage(damage);
                energy.Add(8f);
            }
        }

        private EnemyHealth FindNearestTarget()
        {
            return EnemyRegistry.FindNearest(transform.position, attackRange);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
