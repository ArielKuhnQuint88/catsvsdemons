using UnityEngine;

namespace CatsVsDemons.Visuals
{
    public sealed class ProceduralBoneAnimator : MonoBehaviour
    {
        private Transform[] bones;
        private Quaternion[] restRotations;
        private Transform movementRoot;
        private Vector3 previousPosition;
        private float walkPhase;
        private float attackTimer;
        private const float AttackDuration = 0.32f;

        public void Initialize(Transform[] rigBones)
        {
            bones = rigBones;
        }

        private void Start()
        {
            movementRoot = transform.parent;
            previousPosition = movementRoot != null
                ? movementRoot.position
                : transform.position;

            if (bones == null)
            {
                return;
            }

            restRotations = new Quaternion[bones.Length];
            for (int index = 0; index < bones.Length; index++)
            {
                restRotations[index] = bones[index].localRotation;
            }
        }

        public void TriggerAttack()
        {
            attackTimer = AttackDuration;
        }

        private void LateUpdate()
        {
            if (bones == null ||
                restRotations == null ||
                movementRoot == null)
            {
                return;
            }

            Vector3 movement =
                movementRoot.position - previousPosition;
            previousPosition = movementRoot.position;
            movement.y = 0f;

            ResetBones();

            if (attackTimer > 0f)
            {
                AnimateAttack();
                return;
            }

            if (movement.sqrMagnitude > 0.000001f)
            {
                AnimateWalk();
            }
        }

        private void AnimateWalk()
        {
            walkPhase += Time.deltaTime * 9f;
            float swing = Mathf.Sin(walkPhase) * 24f;
            float counterSwing = -swing;

            Rotate(4, swing * 0.72f, 0f, 0f);
            Rotate(5, swing * 0.35f, 0f, 0f);
            Rotate(6, counterSwing * 0.72f, 0f, 0f);
            Rotate(7, counterSwing * 0.35f, 0f, 0f);

            Rotate(8, counterSwing, 0f, 0f);
            Rotate(9, Mathf.Max(0f, swing) * 0.55f, 0f, 0f);
            Rotate(10, swing, 0f, 0f);
            Rotate(11, Mathf.Max(0f, counterSwing) * 0.55f, 0f, 0f);

            Rotate(1, 0f, 0f, Mathf.Sin(walkPhase) * 3f);
            Rotate(2, 0f, 0f, -Mathf.Sin(walkPhase) * 4f);
        }

        private void AnimateAttack()
        {
            attackTimer -= Time.deltaTime;
            float progress = 1f -
                Mathf.Clamp01(attackTimer / AttackDuration);
            float strike = Mathf.Sin(progress * Mathf.PI);
            float recoil = Mathf.Sin(progress * Mathf.PI * 2f);

            Rotate(2, 0f, strike * 18f, -strike * 10f);
            Rotate(6, -35f - strike * 80f, strike * 28f, -strike * 25f);
            Rotate(7, -50f - strike * 65f, 0f, strike * 30f);
            Rotate(4, recoil * 18f, 0f, strike * 8f);
            Rotate(0, 0f, recoil * 8f, 0f);
        }

        private void ResetBones()
        {
            for (int index = 0; index < bones.Length; index++)
            {
                bones[index].localRotation = Quaternion.Slerp(
                    bones[index].localRotation,
                    restRotations[index],
                    Time.deltaTime * 18f
                );
            }
        }

        private void Rotate(
            int index,
            float x,
            float y,
            float z)
        {
            if (index < 0 || index >= bones.Length)
            {
                return;
            }

            bones[index].localRotation =
                restRotations[index] *
                Quaternion.Euler(x, y, z);
        }
    }
}
