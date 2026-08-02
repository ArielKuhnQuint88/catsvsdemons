using System.Collections.Generic;
using UnityEngine;

namespace CatsVsDemons.Visuals
{
    public sealed class AutomaticCharacterRig : MonoBehaviour
    {
        private static readonly Dictionary<Mesh, Mesh> RiggedMeshes = new();

        private void Awake()
        {
            BuildRig();
        }

        private void BuildRig()
        {
            MeshFilter filter = GetComponentInChildren<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                return;
            }

            MeshRenderer sourceRenderer =
                filter.GetComponent<MeshRenderer>();
            if (sourceRenderer == null)
            {
                return;
            }

            Mesh source = filter.sharedMesh;
            if (!source.isReadable)
            {
                Debug.LogWarning(
                    $"Mesh {source.name} needs Read/Write enabled. " +
                    "Run Tools > Cats vs Demons > Prepare Automatic Rigs.",
                    this
                );
                return;
            }

            Transform meshRoot = filter.transform;
            Bounds bounds = source.bounds;
            Vector3 center = bounds.center;
            float height = Mathf.Max(bounds.size.z, 0.001f);
            float width = Mathf.Max(bounds.size.x, 0.001f);
            float depth = Mathf.Max(bounds.size.y, 0.001f);
            float bottom = bounds.min.z;

            Transform pelvis = CreateBone(
                "Rig_Pelvis", meshRoot, meshRoot,
                new Vector3(center.x, center.y, bottom + height * 0.4f)
            );
            Transform spine = CreateBone(
                "Rig_Spine", pelvis, meshRoot,
                new Vector3(center.x, center.y, bottom + height * 0.53f)
            );
            Transform chest = CreateBone(
                "Rig_Chest", spine, meshRoot,
                new Vector3(center.x, center.y, bottom + height * 0.67f)
            );
            Transform head = CreateBone(
                "Rig_Head", chest, meshRoot,
                new Vector3(center.x, center.y, bottom + height * 0.82f)
            );

            Transform leftUpperArm = CreateBone(
                "Rig_LeftUpperArm", chest, meshRoot,
                new Vector3(
                    center.x - width * 0.24f,
                    center.y,
                    bottom + height * 0.66f
                )
            );
            Transform leftForearm = CreateBone(
                "Rig_LeftForearm", leftUpperArm, meshRoot,
                new Vector3(
                    center.x - width * 0.4f,
                    center.y,
                    bottom + height * 0.57f
                )
            );
            Transform rightUpperArm = CreateBone(
                "Rig_RightUpperArm", chest, meshRoot,
                new Vector3(
                    center.x + width * 0.24f,
                    center.y,
                    bottom + height * 0.66f
                )
            );
            Transform rightForearm = CreateBone(
                "Rig_RightForearm", rightUpperArm, meshRoot,
                new Vector3(
                    center.x + width * 0.4f,
                    center.y,
                    bottom + height * 0.57f
                )
            );

            Transform leftThigh = CreateBone(
                "Rig_LeftThigh", pelvis, meshRoot,
                new Vector3(
                    center.x - width * 0.13f,
                    center.y,
                    bottom + height * 0.36f
                )
            );
            Transform leftShin = CreateBone(
                "Rig_LeftShin", leftThigh, meshRoot,
                new Vector3(
                    center.x - width * 0.13f,
                    center.y,
                    bottom + height * 0.17f
                )
            );
            Transform rightThigh = CreateBone(
                "Rig_RightThigh", pelvis, meshRoot,
                new Vector3(
                    center.x + width * 0.13f,
                    center.y,
                    bottom + height * 0.36f
                )
            );
            Transform rightShin = CreateBone(
                "Rig_RightShin", rightThigh, meshRoot,
                new Vector3(
                    center.x + width * 0.13f,
                    center.y,
                    bottom + height * 0.17f
                )
            );

            Transform[] bones =
            {
                pelvis,
                spine,
                chest,
                head,
                leftUpperArm,
                leftForearm,
                rightUpperArm,
                rightForearm,
                leftThigh,
                leftShin,
                rightThigh,
                rightShin
            };

            if (!RiggedMeshes.TryGetValue(source, out Mesh riggedMesh))
            {
                riggedMesh = Object.Instantiate(source);
                riggedMesh.name = source.name + "_AutoRigged";

                BoneWeight[] weights =
                    CreateWeights(source.vertices, bounds);
                Matrix4x4[] bindPoses = new Matrix4x4[bones.Length];

                for (int index = 0; index < bones.Length; index++)
                {
                    bindPoses[index] =
                        bones[index].worldToLocalMatrix *
                        meshRoot.localToWorldMatrix;
                }

                riggedMesh.boneWeights = weights;
                riggedMesh.bindposes = bindPoses;
                riggedMesh.RecalculateBounds();
                RiggedMeshes.Add(source, riggedMesh);
            }

            SkinnedMeshRenderer skinned =
                filter.gameObject.AddComponent<SkinnedMeshRenderer>();
            skinned.sharedMesh = riggedMesh;
            skinned.sharedMaterials = sourceRenderer.sharedMaterials;
            skinned.bones = bones;
            skinned.rootBone = pelvis;
            skinned.updateWhenOffscreen = false;
            skinned.localBounds = riggedMesh.bounds;

            sourceRenderer.enabled = false;

            ProceduralBoneAnimator animator =
                gameObject.AddComponent<ProceduralBoneAnimator>();
            animator.Initialize(bones);
        }

        private static Transform CreateBone(
            string name,
            Transform parent,
            Transform meshRoot,
            Vector3 meshLocalPosition)
        {
            GameObject boneObject = new GameObject(name);
            Transform bone = boneObject.transform;
            bone.SetParent(parent, true);
            bone.position = meshRoot.TransformPoint(meshLocalPosition);
            bone.rotation = meshRoot.rotation;
            bone.localScale = Vector3.one;
            return bone;
        }

        private static BoneWeight[] CreateWeights(
            Vector3[] vertices,
            Bounds bounds)
        {
            BoneWeight[] weights = new BoneWeight[vertices.Length];
            float width = Mathf.Max(bounds.size.x, 0.001f);
            float height = Mathf.Max(bounds.size.z, 0.001f);

            for (int index = 0; index < vertices.Length; index++)
            {
                Vector3 vertex = vertices[index];
                float x =
                    (vertex.x - bounds.center.x) / width;
                float z =
                    (vertex.z - bounds.min.z) / height;
                float absoluteX = Mathf.Abs(x);

                BoneWeight weight = new BoneWeight();

                if (z > 0.76f)
                {
                    SetWeight(ref weight, 3, 2, 0.9f);
                }
                else if (z > 0.44f && absoluteX > 0.24f)
                {
                    bool left = x < 0f;
                    bool forearm = absoluteX > 0.36f;
                    int arm = left
                        ? (forearm ? 5 : 4)
                        : (forearm ? 7 : 6);
                    SetWeight(ref weight, arm, 2, 0.88f);
                }
                else if (z < 0.43f)
                {
                    bool left = x < 0f;
                    bool shin = z < 0.22f;
                    int leg = left
                        ? (shin ? 9 : 8)
                        : (shin ? 11 : 10);
                    SetWeight(ref weight, leg, 0, 0.88f);
                }
                else if (z > 0.62f)
                {
                    SetWeight(ref weight, 2, 1, 0.8f);
                }
                else if (z > 0.5f)
                {
                    SetWeight(ref weight, 1, 2, 0.72f);
                }
                else
                {
                    SetWeight(ref weight, 0, 1, 0.8f);
                }

                weights[index] = weight;
            }

            return weights;
        }

        private static void SetWeight(
            ref BoneWeight weight,
            int primary,
            int secondary,
            float primaryWeight)
        {
            weight.boneIndex0 = primary;
            weight.weight0 = primaryWeight;
            weight.boneIndex1 = secondary;
            weight.weight1 = 1f - primaryWeight;
        }
    }
}
