using CatsVsDemons.Enemies;
using CatsVsDemons.Visuals;
using UnityEngine;

namespace CatsVsDemons.Player
{
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
            EnemyHealth target = FindNearestTarget();
            if (target == null)
            {
                return;
            }

            Vector3 direction = target.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    rotationSpeed * Time.deltaTime
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
