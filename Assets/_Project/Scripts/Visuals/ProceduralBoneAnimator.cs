using CatsVsDemons.Player;
using UnityEngine;

namespace CatsVsDemons.Visuals
{
    [DefaultExecutionOrder(40)]
    public sealed class ProceduralBoneAnimator : MonoBehaviour
    {
        private Transform[] bones;
        private Quaternion[] restRotations;
        private Vector3[] restPositions;
        private Transform movementRoot;
        private KinPrototypeController locomotion;
        private Vector3 previousPosition;
        private float walkPhase, attackTimer, motionBlend;
        public void Initialize(Transform[] rigBones) => bones = rigBones;

        private void Start()
        {
            movementRoot = transform.parent;
            previousPosition = movementRoot != null ? movementRoot.position : transform.position;
            locomotion = movementRoot != null ? movementRoot.GetComponent<KinPrototypeController>() : null;
            if (bones == null) return;
            restRotations = new Quaternion[bones.Length]; restPositions = new Vector3[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            { restRotations[i] = bones[i].localRotation; restPositions[i] = bones[i].localPosition; }
        }
        public void TriggerAttack() => attackTimer = KinPoseMath.AttackDuration;
        private void LateUpdate()
        {
            if (bones == null || restRotations == null || movementRoot == null) return;
            Vector3 movement = movementRoot.position - previousPosition; previousPosition = movementRoot.position; movement.y = 0;
            if (Time.timeScale <= 0 || Time.deltaTime <= 0) return;
            ResetBones();
            float speed = locomotion != null ? locomotion.PlanarVelocity.magnitude : movement.magnitude / Time.deltaTime;
            float signed = Vector3.Dot(movementRoot.forward, locomotion != null ? locomotion.PlanarVelocity : movement / Time.deltaTime);
            if (Mathf.Abs(signed) < speed * .15f) signed = speed; // Lateral steps still have a readable gait.
            motionBlend = Mathf.MoveTowards(motionBlend, Mathf.Clamp01(speed / 4), Time.deltaTime * 9);
            walkPhase = KinPoseMath.AdvanceGait(walkPhase, signed, Time.deltaTime);
            AnimateWalk();
            if (attackTimer > 0) AnimateAttack();
        }
        private void AnimateWalk()
        {
            float swing = Mathf.Sin(walkPhase) * 25 * motionBlend;
            Rotate(4, swing * .5f, 0, 0); Rotate(5, swing * .22f, 0, 0);
            Rotate(6, -swing * .5f, 0, 0); Rotate(7, -swing * .22f, 0, 0);
            Rotate(8, -swing, 0, 0); Rotate(9, Mathf.Max(0, swing) * .55f, 0, 0);
            Rotate(10, swing, 0, 0); Rotate(11, Mathf.Max(0, -swing) * .55f, 0, 0);
            // The authored mesh is Z-up: torso yaw/lean must use its rest coordinate system.
            Rotate(1, 0, Mathf.Sin(walkPhase) * 2 * motionBlend, 0);
            Rotate(2, 0, -Mathf.Sin(walkPhase) * 2.5f * motionBlend, 0);
        }
        private void AnimateAttack()
        {
            attackTimer = Mathf.Max(0, attackTimer - Time.deltaTime);
            float progress = 1 - attackTimer / KinPoseMath.AttackDuration;
            float strike = KinPoseMath.Swing(progress), recovery = KinPoseMath.Recovery(progress);
            // Every offset vanishes at the end; there is no permanently raised attack arm.
            Rotate(2, 0, -strike * 8, recovery * 14);
            Rotate(6, -strike * 92, strike * 24, -strike * 18);
            Rotate(7, -strike * 72, 0, strike * 20);
            Rotate(4, recovery * 16, 0, strike * 8);
            // Walking remains on legs 8-11 during the entire swing.
        }
        private void ResetBones()
        {
            for (int i = 0; i < bones.Length; i++)
            { bones[i].localRotation = restRotations[i]; bones[i].localPosition = restPositions[i]; }
        }
        private void Rotate(int index, float x, float y, float z)
        {
            if (index < 0 || index >= bones.Length) return;
            bones[index].localRotation = restRotations[index] * Quaternion.Euler(x, y, z);
        }
    }
}
