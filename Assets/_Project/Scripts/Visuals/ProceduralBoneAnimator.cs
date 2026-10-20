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
        private float walkPhase, idlePhase, attackTimer, motionBlend;
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
            if (speed > 20f) speed = 0f;
            float signed = Vector3.Dot(movementRoot.forward, locomotion != null ? locomotion.PlanarVelocity : movement / Time.deltaTime);
            if (Mathf.Abs(signed) < speed * .15f) signed = speed; // Lateral steps still have a readable gait.
            if (speed <= .05f) signed = 0f;
            motionBlend = Mathf.MoveTowards(motionBlend, Mathf.Clamp01(speed / 4), Time.deltaTime * 9);
            walkPhase = KinPoseMath.AdvanceGait(walkPhase, signed, Time.deltaTime);
            idlePhase += Time.deltaTime * 1.8f;
            AnimateWalk();
            if (attackTimer > 0) AnimateAttack();
        }
        private void AnimateWalk()
        {
            float step = Mathf.Sin(walkPhase);
            float swing = step * 24f * motionBlend;
            Rotate(4, swing * .42f, 0, 0); Rotate(5, swing * .16f, 0, 0);
            Rotate(6, -swing * .42f, 0, 0); Rotate(7, -swing * .16f, 0, 0);
            Rotate(8, -swing, 0, 0); Rotate(9, Mathf.Max(0, step) * Mathf.Max(0, step) * 30f * motionBlend, 0, 0);
            Rotate(10, swing, 0, 0); Rotate(11, Mathf.Max(0, -step) * Mathf.Max(0, -step) * 30f * motionBlend, 0, 0);
            // Imported bones are Z-up: Z twists the torso, X leans it forward.
            float breath = Mathf.Sin(idlePhase) * .45f;
            Rotate(1, -motionBlend * 1.6f + breath, 0, step * 1.7f * motionBlend);
            Rotate(2, 0, 0, -step * 2.2f * motionBlend);
            Rotate(3, motionBlend * .7f - breath * .5f, 0, step * .45f * motionBlend);
        }
        private void AnimateAttack()
        {
            attackTimer = Mathf.Max(0, attackTimer - Time.deltaTime);
            float progress = 1 - attackTimer / KinPoseMath.AttackDuration;
            float strike = KinPoseMath.Swing(progress), prepare = KinPoseMath.Anticipation(progress);
            // Add the strike to locomotion. Preparation and recovery return smoothly
            // to the current walking pose, rather than freezing then snapping to rest.
            AddRotation(2, -strike * 2f, 0, prepare * 6f - strike * 10f);
            AddRotation(6, prepare * 24f - strike * 78f, strike * 12f, -strike * 12f);
            AddRotation(7, prepare * 14f - strike * 46f, 0, strike * 12f);
            AddRotation(4, prepare * 7f - strike * 15f, 0, strike * 6f);
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
        private void AddRotation(int index, float x, float y, float z)
        {
            if (index < 0 || index >= bones.Length) return;
            bones[index].localRotation *= Quaternion.Euler(x, y, z);
        }
    }
}
