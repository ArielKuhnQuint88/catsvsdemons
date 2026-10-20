using CatsVsDemons.Player;
using UnityEngine;

namespace CatsVsDemons.Visuals
{
    [DefaultExecutionOrder(20)]
    public sealed class ProceduralModelAnimator : MonoBehaviour
    {
        private Transform movementRoot;
        private Vector3 previousRootPosition;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private Vector3 restScale;
        private Vector3 groundPivot;
        private float groundHeight = -1f;
        private float walkPhase, idlePhase, attackTimer, movementBlend;
        private KinPrototypeController locomotion;
        private ProceduralBoneAnimator boneAnimator;
        private CharacterMotionProfile profile;

        public void Configure(string modelPath, float bottomOffset)
        {
            profile = CharacterMotionMath.ProfileFor(modelPath);
            groundHeight = bottomOffset;
        }

        private void Start()
        {
            movementRoot = transform.parent;
            if (movementRoot == null) { enabled = false; return; }
            locomotion = movementRoot.GetComponent<KinPrototypeController>();
            boneAnimator = GetComponent<ProceduralBoneAnimator>();
            previousRootPosition = movementRoot.position;
            restPosition = transform.localPosition;
            restRotation = transform.localRotation;
            restScale = transform.localScale;
            groundPivot = new Vector3(0f, groundHeight, 0f) - restPosition;
            idlePhase = Time.time * 1.8f + movementRoot.position.x * .7f + movementRoot.position.z * .4f;
        }

        public void TriggerAttack()
        {
            attackTimer = KinPoseMath.AttackDuration;
            if (boneAnimator == null) boneAnimator = GetComponent<ProceduralBoneAnimator>();
            if (boneAnimator != null) boneAnimator.TriggerAttack();
        }

        private void LateUpdate()
        {
            if (movementRoot == null) return;
            Vector3 displacement = movementRoot.position - previousRootPosition;
            previousRootPosition = movementRoot.position;
            if (Time.timeScale <= 0f || Time.deltaTime <= 0f) return;
            displacement.y = 0f;
            float speed = locomotion != null ? locomotion.PlanarVelocity.magnitude :
                displacement.magnitude / Time.deltaTime;
            // A spawn/teleport is not a running stride.
            if (speed > 20f) speed = 0f;
            float signed = locomotion != null ? Vector3.Dot(movementRoot.forward, locomotion.PlanarVelocity) : speed;
            if (Mathf.Abs(signed) < speed * .15f) signed = speed;
            if (speed <= .05f) signed = 0f;
            walkPhase = KinPoseMath.AdvanceGait(walkPhase, signed, Time.deltaTime);
            movementBlend = Mathf.MoveTowards(movementBlend, Mathf.Clamp01(speed / 4f), Time.deltaTime * 9f);
            idlePhase += Time.deltaTime * 1.8f;

            bool attacking = attackTimer > 0f;
            attackTimer = Mathf.Max(0f, attackTimer - Time.deltaTime);
            float progress = 1f - attackTimer / KinPoseMath.AttackDuration;
            CharacterBodyPose pose = CharacterMotionMath.Evaluate(profile, walkPhase,
                idlePhase, movementBlend, attacking, progress);
            Quaternion bodyRotation = Quaternion.Euler(pose.Euler.x, pose.Euler.y, pose.Euler.z);
            // Apply the pose in the Y-up movement root, before the imported Z-up
            // rest rotation. Rotate around the feet instead of orbiting the mesh origin.
            transform.localPosition = restPosition + pose.Offset + groundPivot - bodyRotation * groundPivot;
            transform.localRotation = bodyRotation * restRotation;
            // Keep every authored proportion. No squash/stretch on faces or silhouettes.
            transform.localScale = restScale;
        }
    }
}
