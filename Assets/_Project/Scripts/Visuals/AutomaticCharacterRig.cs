using System.Collections.Generic;
using UnityEngine;

namespace CatsVsDemons.Visuals
{
    public sealed class AutomaticCharacterRig : MonoBehaviour
    {
        private static readonly Dictionary<Mesh, Mesh> RiggedMeshes = new();
        private static bool simpleAnimationForSession;
        private MeshRenderer originalRenderer;
        private SkinnedMeshRenderer rigRenderer;
        private ProceduralBoneAnimator boneAnimator;
        private bool awaitingFirstRender;
        private int missedVisibleFrames;

        public bool IsUsingRig => rigRenderer != null && rigRenderer.enabled &&
            originalRenderer != null && !originalRenderer.enabled;

        public string FallbackReason { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSessionMode() => simpleAnimationForSession = false;

        public static void EnableSimpleAnimationForSession() => simpleAnimationForSession = true;

        private void Awake()
        {
            try { BuildRig(); }
            catch (System.Exception exception)
            {
                UseOriginalRenderer("Falha ao configurar o rig: " + exception.Message);
            }
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
            originalRenderer = sourceRenderer;
            // House previews clone a configured model. Reuse that skeleton rather than
            // building a second rig on top of the copied renderer and equipment.
            Transform existingRig = filter.transform.Find("RiggedModel");
            if (existingRig != null && existingRig.TryGetComponent(out SkinnedMeshRenderer existing) &&
                existing.sharedMesh != null && existing.bones.Length > 0 && existing.bones.Length == existing.sharedMesh.bindposes.Length)
            {
                rigRenderer = existing;
                return;
            }
            if (simpleAnimationForSession)
            {
                FallbackReason = "Animação simples selecionada para esta sessão.";
                return;
            }

            Mesh source = filter.sharedMesh;
            if (!source.isReadable)
            {
                FallbackReason = "Malha sem Read/Write; usando animação do modelo original.";
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
            float bottom = bounds.min.z;
            bool kinProfile = false;
            foreach (Material material in sourceRenderer.sharedMaterials)
                if (material != null && material.name.IndexOf("_Kin", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    kinProfile = true;

            Transform pelvis = CreateBone(
                "Rig_Pelvis", meshRoot, meshRoot,
                new Vector3(center.x, center.y, bottom + height * (kinProfile ? 0.235f : 0.4f))
            );
            Transform spine = CreateBone(
                "Rig_Spine", pelvis, meshRoot,
                new Vector3(center.x, center.y, bottom + height * (kinProfile ? 0.33f : 0.53f))
            );
            Transform chest = CreateBone(
                "Rig_Chest", spine, meshRoot,
                new Vector3(center.x, center.y, bottom + height * (kinProfile ? 0.407f : 0.67f))
            );
            Transform head = CreateBone(
                "Rig_Head", chest, meshRoot,
                new Vector3(center.x, center.y, bottom + height * (kinProfile ? 0.69f : 0.82f))
            );

            Transform leftUpperArm = CreateBone(
                "Rig_LeftUpperArm", chest, meshRoot,
                new Vector3(
                    center.x - width * 0.24f,
                    center.y,
                    bottom + height * (kinProfile ? 0.40f : 0.66f)
                )
            );
            Transform leftForearm = CreateBone(
                "Rig_LeftForearm", leftUpperArm, meshRoot,
                new Vector3(
                    center.x - width * 0.4f,
                    center.y,
                    bottom + height * (kinProfile ? 0.29f : 0.57f)
                )
            );
            Transform rightUpperArm = CreateBone(
                "Rig_RightUpperArm", chest, meshRoot,
                new Vector3(
                    center.x + width * 0.24f,
                    center.y,
                    bottom + height * (kinProfile ? 0.40f : 0.66f)
                )
            );
            Transform rightForearm = CreateBone(
                "Rig_RightForearm", rightUpperArm, meshRoot,
                new Vector3(
                    center.x + width * 0.4f,
                    center.y,
                    bottom + height * (kinProfile ? 0.29f : 0.57f)
                )
            );

            Transform leftThigh = CreateBone(
                "Rig_LeftThigh", pelvis, meshRoot,
                new Vector3(
                    center.x - width * 0.13f,
                    center.y,
                    bottom + height * (kinProfile ? 0.16f : 0.36f)
                )
            );
            Transform leftShin = CreateBone(
                "Rig_LeftShin", leftThigh, meshRoot,
                new Vector3(
                    center.x - width * 0.13f,
                    center.y,
                    bottom + height * (kinProfile ? 0.07f : 0.17f)
                )
            );
            Transform rightThigh = CreateBone(
                "Rig_RightThigh", pelvis, meshRoot,
                new Vector3(
                    center.x + width * 0.13f,
                    center.y,
                    bottom + height * (kinProfile ? 0.16f : 0.36f)
                )
            );
            Transform rightShin = CreateBone(
                "Rig_RightShin", rightThigh, meshRoot,
                new Vector3(
                    center.x + width * 0.13f,
                    center.y,
                    bottom + height * (kinProfile ? 0.07f : 0.17f)
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

            if (!RiggedMeshes.TryGetValue(source, out Mesh riggedMesh) ||
                riggedMesh == null || riggedMesh.bindposes.Length != bones.Length)
            {
                // Rebuild readable vertex streams for skinning instead of reusing
                // the imported static mesh's compressed/optimized buffer layout.
                riggedMesh = CreateRigMesh(source);

                BoneWeight[] weights =
                    CreateWeights(source.vertices, bounds, kinProfile);
                Matrix4x4[] bindPoses = new Matrix4x4[bones.Length];

                for (int index = 0; index < bones.Length; index++)
                {
                    bindPoses[index] =
                        bones[index].worldToLocalMatrix *
                        meshRoot.localToWorldMatrix;
                }

                riggedMesh.bindposes = bindPoses;
                riggedMesh.boneWeights = weights;
                riggedMesh.RecalculateBounds();
                RiggedMeshes[source] = riggedMesh;
            }

            if (riggedMesh.bindposes.Length != bones.Length ||
                riggedMesh.boneWeights.Length != source.vertexCount)
            {
                // Keep the original mesh visible if an imported/cached rig is invalid.
                UseOriginalRenderer("Auto-rig inválido; usando a malha original de " + source.name);
                Object.Destroy(pelvis.gameObject);
                return;
            }

            GameObject rigObject = new("RiggedModel")
            {
                layer = filter.gameObject.layer
            };
            rigObject.transform.SetParent(meshRoot, false);
            SkinnedMeshRenderer skinned =
                rigObject.AddComponent<SkinnedMeshRenderer>();
            rigRenderer = skinned;
            skinned.enabled = false;
            // Unity validates bones against bindposes when sharedMesh is assigned.
            // Assign the skeleton first, then publish the fully configured mesh.
            skinned.bones = bones;
            // The mesh and its culling bounds use meshRoot's coordinate space.
            // A translated pelvis would offset the bounds from the model.
            skinned.rootBone = meshRoot;
            skinned.sharedMaterials = sourceRenderer.sharedMaterials;
            skinned.sharedMesh = riggedMesh;
            skinned.updateWhenOffscreen = false;
            skinned.localBounds = riggedMesh.bounds;
            Bounds animatedBounds = skinned.localBounds;
            animatedBounds.Expand(animatedBounds.size * 0.4f);
            skinned.localBounds = animatedBounds;
            skinned.shadowCastingMode = sourceRenderer.shadowCastingMode;
            skinned.receiveShadows = sourceRenderer.receiveShadows;

            if (!ValidateRestPose(skinned, source, out string reason))
            {
                UseOriginalRenderer(reason);
                Object.Destroy(rigObject);
                Object.Destroy(pelvis.gameObject);
                return;
            }

            skinned.enabled = true;
            // Keep the real model visible until Unity marks the rig visible.
            // Bones/weights alone do not prove the model is ready for rendering.
            awaitingFirstRender = true;

            boneAnimator =
                gameObject.AddComponent<ProceduralBoneAnimator>();
            boneAnimator.Initialize(bones);
            boneAnimator.enabled = false;
        }

        private void LateUpdate()
        {
            if (!awaitingFirstRender || originalRenderer == null || rigRenderer == null)
                return;
            // Respect first-person mode and other systems deliberately hiding Kin.
            if (!originalRenderer.enabled || !rigRenderer.enabled)
            {
                awaitingFirstRender = false;
                return;
            }
            if (rigRenderer.isVisible)
            {
                originalRenderer.enabled = false;
                boneAnimator.enabled = true;
                awaitingFirstRender = false;
            }
            else if (originalRenderer.isVisible)
            {
                if (++missedVisibleFrames >= 6)
                    UseOriginalRenderer("O renderer do rig não chegou à câmera; mantendo o modelo original.");
            }
            else missedVisibleFrames = 0;
        }

        public void UseOriginalRenderer(string reason = "Modo de animação simples ativado.")
        {
            awaitingFirstRender = false;
            FallbackReason = reason;
            if (rigRenderer != null) rigRenderer.enabled = false;
            if (boneAnimator != null) boneAnimator.enabled = false;
            if (originalRenderer != null) originalRenderer.enabled = true;
            Debug.LogWarning("[Visual] " + gameObject.name + ": " + reason, this);
        }

        private static Mesh CreateRigMesh(Mesh source)
        {
            Mesh mesh = new()
            {
                name = source.name + "_AutoRigged",
                indexFormat = source.indexFormat,
                vertices = source.vertices
            };
            Vector3[] normals = source.normals;
            if (normals.Length == source.vertexCount) mesh.normals = normals;
            Vector4[] tangents = source.tangents;
            if (tangents.Length == source.vertexCount) mesh.tangents = tangents;
            Color32[] colors = source.colors32;
            if (colors.Length == source.vertexCount) mesh.colors32 = colors;
            List<Vector4> uv = new();
            for (int channel = 0; channel < 8; channel++)
            {
                uv.Clear();
                source.GetUVs(channel, uv);
                if (uv.Count == source.vertexCount) mesh.SetUVs(channel, uv);
            }
            mesh.subMeshCount = source.subMeshCount;
            for (int submesh = 0; submesh < source.subMeshCount; submesh++)
                mesh.SetIndices(source.GetIndices(submesh), source.GetTopology(submesh), submesh, false);
            if (normals.Length != source.vertexCount) mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static bool ValidateRestPose(SkinnedMeshRenderer renderer, Mesh source, out string reason)
        {
            Mesh baked = new();
            try
            {
                renderer.BakeMesh(baked, true);
                Vector3[] actual = baked.vertices;
                Vector3[] expected = source.vertices;
                if (actual.Length != expected.Length || actual.Length == 0)
                {
                    reason = "O rig não produziu a geometria do personagem.";
                    return false;
                }
                float tolerance = Mathf.Max(source.bounds.size.magnitude * 0.01f, 0.001f);
                float toleranceSquared = tolerance * tolerance;
                for (int index = 0; index < actual.Length; index++)
                {
                    Vector3 point = actual[index];
                    if (float.IsNaN(point.x) || float.IsInfinity(point.x) ||
                        float.IsNaN(point.y) || float.IsInfinity(point.y) ||
                        float.IsNaN(point.z) || float.IsInfinity(point.z) ||
                        (point - expected[index]).sqrMagnitude > toleranceSquared)
                    {
                        reason = "A posição inicial do rig deformou ou deslocou a malha; mantendo o modelo original.";
                        return false;
                    }
                }
                reason = null;
                return true;
            }
            catch (System.Exception exception)
            {
                reason = "Falha na geometria do rig: " + exception.Message;
                return false;
            }
            finally
            {
                Object.Destroy(baked);
            }
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
            Bounds bounds,
            bool kinProfile = false)
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

                if (kinProfile)
                {
                    // Kin's head occupies the upper half; his torso and limbs are short.
                    // Human proportions would bind his cheeks to arm bones.
                    if (z > 0.455f) SetWeight(ref weight, 3, 2, 0.97f);
                    else if (z > 0.22f && absoluteX > 0.225f)
                    {
                        bool left = x < 0, hand = absoluteX > 0.35f || z < 0.30f;
                        SetWeight(ref weight, left ? (hand ? 5 : 4) : (hand ? 7 : 6), 2, 0.9f);
                    }
                    else if (z < 0.245f)
                        SetWeight(ref weight, x < 0 ? (z < 0.11f ? 9 : 8) : (z < 0.11f ? 11 : 10), 0, 0.9f);
                    else if (z > 0.37f) SetWeight(ref weight, 2, 1, 0.82f);
                    else if (z > 0.28f) SetWeight(ref weight, 1, 0, 0.8f);
                    else SetWeight(ref weight, 0, 1, 0.85f);
                    weights[index] = weight;
                    continue;
                }

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
