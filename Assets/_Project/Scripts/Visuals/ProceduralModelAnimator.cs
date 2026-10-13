using UnityEngine;

namespace CatsVsDemons.Visuals
{
    [DefaultExecutionOrder(20)]
    public sealed class ProceduralModelAnimator : MonoBehaviour
    {
        [SerializeField] private float walkFrequency = 9f;
        [SerializeField] private float walkBounce = 0.035f;
        [SerializeField] private float walkTilt = 3.5f;
        [SerializeField] private float attackDuration = 0.32f;

        private Transform movementRoot;
        private Vector3 previousRootPosition;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private Vector3 restScale;
        private float walkPhase;
        private float attackTimer;
        private CatsVsDemons.Player.KinPrototypeController locomotion;

        private void Start()
        {
            movementRoot = transform.parent;
            locomotion = movementRoot != null ? movementRoot.GetComponent<CatsVsDemons.Player.KinPrototypeController>() : null;
            previousRootPosition = movementRoot.position;
            restPosition = transform.localPosition;
            restRotation = transform.localRotation;
            restScale = transform.localScale;
        }

        public void TriggerAttack()
        {
            attackTimer = attackDuration;

            ProceduralBoneAnimator boneAnimator =
                GetComponent<ProceduralBoneAnimator>();
            if (boneAnimator != null)
            {
                boneAnimator.TriggerAttack();
            }
        }

        private void LateUpdate()
        {
            if (movementRoot == null || Time.timeScale <= 0 || Time.deltaTime <= 0)
            {
                return;
            }

            Vector3 displacement =
                movementRoot.position - previousRootPosition;
            previousRootPosition = movementRoot.position;
            displacement.y = 0f;

            if (attackTimer > 0f)
            {
                AnimateAttack();
                return;
            }

            float speed = displacement.magnitude /
                Mathf.Max(Time.deltaTime, 0.0001f);

            if (speed > 0.05f)
            {
                float signed = locomotion != null ? Vector3.Dot(movementRoot.forward, locomotion.PlanarVelocity) : speed;
                walkPhase += Time.deltaTime * walkFrequency * (signed < -.1f ? -1 : 1) * Mathf.Clamp(speed / 5, .3f, 1.3f);
                float step = Mathf.Sin(walkPhase);
                float bounce = Mathf.Abs(step) * walkBounce;

                transform.localPosition =
                    restPosition + Vector3.up * bounce;
                transform.localRotation =
                    restRotation *
                    Quaternion.Euler(
                        step * 2f,
                        0f,
                        step * walkTilt
                    );
                transform.localScale = restScale;
            }
            else
            {
                transform.localPosition = Vector3.Lerp(
                    transform.localPosition,
                    restPosition,
                    Time.deltaTime * 12f
                );
                transform.localRotation = Quaternion.Slerp(
                    transform.localRotation,
                    restRotation,
                    Time.deltaTime * 12f
                );
                transform.localScale = Vector3.Lerp(
                    transform.localScale,
                    restScale,
                    Time.deltaTime * 12f
                );
            }
        }

        private void AnimateAttack()
        {
            attackTimer -= Time.deltaTime;
            float progress = 1f -
                Mathf.Clamp01(attackTimer / attackDuration);
            float strike = Mathf.Sin(progress * Mathf.PI);
            float twist = Mathf.Sin(progress * Mathf.PI * 2f);

            transform.localPosition =
                restPosition +
                Vector3.forward * (strike * 0.12f) +
                Vector3.up * (strike * 0.12f);
            transform.localRotation =
                restRotation *
                Quaternion.Euler(
                    -strike * 10f,
                    twist * 12f,
                    -strike * 5f
                );
            transform.localScale = Vector3.Scale(
                restScale,
                new Vector3(
                    1f + strike * 0.025f,
                    1f - strike * 0.025f,
                    1f + strike * 0.025f
                )
            );
        }
    }
}
