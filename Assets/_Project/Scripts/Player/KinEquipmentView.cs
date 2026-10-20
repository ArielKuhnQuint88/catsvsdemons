using System.Collections.Generic;
using CatsVsDemons.Visuals;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatsVsDemons.Player
{
    [DefaultExecutionOrder(150)]
    public sealed class KinEquipmentView : MonoBehaviour
    {
        private sealed class Binding
        {
            public Transform Part;
            public int Bone;
            public Vector3 Pivot;
            public float Sway;
        }

        private readonly List<Mesh> ownedMeshes = new();
        private readonly List<Binding> bindings = new();
        private readonly List<Renderer> renderers = new();
        private Transform fitRoot, meshRoot;
        private MeshRenderer original;
        private SkinnedMeshRenderer skinned;
        private AutomaticCharacterRig rig;
        private Transform[] rigBones;
        private Matrix4x4[] rigBindposes;
        private Material surface, glass;
        private KinFootstepTrail steps;
        private Vector3 previousPosition;
        private float speed;
        public string OutfitId { get; private set; }
        public string AccessoryId { get; private set; }
        public int PartCount => bindings.Count;
        public bool HasEquipment => fitRoot != null;

        public bool Apply(string outfitId, string accessoryId)
        {
            Transform model = transform.Find("GameplayModel");
            if (model == null) return false;
            MeshFilter source = model.GetComponentInChildren<MeshFilter>(true);
            if (source == null || source.sharedMesh == null) return false;
            ClearGeometry();
            meshRoot = source.transform;
            original = source.GetComponent<MeshRenderer>();
            rig = model.GetComponent<AutomaticCharacterRig>();
            skinned = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            rigBones = skinned != null ? skinned.bones : null;
            rigBindposes = skinned != null && skinned.sharedMesh != null ? skinned.sharedMesh.bindposes : null;
            Bounds bounds = source.sharedMesh.bounds;
            float height = Mathf.Max(bounds.size.z, 0.001f);
            fitRoot = new GameObject("Equipped Kin Wardrobe").transform;
            fitRoot.SetParent(meshRoot, false);
            fitRoot.localPosition = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z);
            fitRoot.localRotation = Quaternion.Euler(90f, 0, 0);
            fitRoot.localScale = Vector3.one * height;
            List<KinEquipmentPart> parts = KinEquipmentModels.BuildOutfit(outfitId);
            parts.AddRange(KinEquipmentModels.BuildAccessory(accessoryId));
            foreach (KinEquipmentPart part in parts) Install(part);
            OutfitId = outfitId; AccessoryId = accessoryId;
            if (steps == null) steps = gameObject.AddComponent<KinFootstepTrail>();
            steps.Configure(this, accessoryId == "passos_magicos", GetSurface());
            previousPosition = transform.position;
            UpdateBindings();
            return true;
        }

        private void Install(KinEquipmentPart template)
        {
            GameObject part = new(template.Name, typeof(MeshFilter), typeof(MeshRenderer));
            part.layer = meshRoot.gameObject.layer;
            part.transform.SetParent(fitRoot, false);
            part.transform.localPosition = template.Pivot;
            Mesh mesh = template.Mesh;
            if (template.Pivot.sqrMagnitude > 0)
            {
                Vector3[] vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++) vertices[i] -= template.Pivot;
                mesh.vertices = vertices; mesh.RecalculateBounds();
            }
            ownedMeshes.Add(mesh);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = template.Glass ? GetGlass() : GetSurface();
            renderer.shadowCastingMode = template.Glass ? ShadowCastingMode.Off : ShadowCastingMode.On;
            renderer.receiveShadows = !template.Glass;
            renderers.Add(renderer);
            bindings.Add(new Binding { Part = part.transform, Bone = FindBone(template.Anchor), Pivot = template.Pivot, Sway = template.Sway });
        }

        private int FindBone(KinEquipmentAnchor anchor)
        {
            if (skinned == null) return -1;
            string name = anchor switch
            {
                KinEquipmentAnchor.Head => "Rig_Head", KinEquipmentAnchor.Chest => "Rig_Chest",
                KinEquipmentAnchor.LeftArm => "Rig_LeftUpperArm", KinEquipmentAnchor.RightArm => "Rig_RightUpperArm",
                KinEquipmentAnchor.LeftHand => "Rig_LeftForearm", KinEquipmentAnchor.RightHand => "Rig_RightForearm",
                KinEquipmentAnchor.LeftLeg => "Rig_LeftThigh", KinEquipmentAnchor.RightLeg => "Rig_RightThigh",
                KinEquipmentAnchor.LeftFoot => "Rig_LeftShin", KinEquipmentAnchor.RightFoot => "Rig_RightShin",
                _ => "Rig_Pelvis"
            };
            Transform[] bones = rigBones;
            for (int i = 0; i < bones.Length; i++) if (bones[i] != null && bones[i].name == name) return i;
            return -1;
        }

        private Material GetSurface()
        {
            if (surface != null) return surface;
            Shader shader = Shader.Find("CatsVsDemons/StylizedModel");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            surface = new Material(shader) { name = "Kin sculpted equipment fabric gold and spirit", hideFlags = HideFlags.HideAndDontSave };
            surface.SetColor("_BaseColor", Color.white);
            surface.SetFloat("_UseVertexColor", 1); surface.SetFloat("_UseBaseMap", 0);
            surface.SetFloat("_UseHeightPalette", 0); surface.SetFloat("_AmbientFloor", 0.24f);
            surface.SetColor("_RimColor", new Color(0.82f, 0.88f, 0.91f)); surface.SetFloat("_RimStrength", 0.12f);
            Texture2D detail = Resources.Load<Texture2D>("Textures/Characters/Kin_SurfaceDetail");
            if (detail != null) surface.SetTexture("_DetailMap", detail);
            surface.SetFloat("_DetailStrength", 0.3f); surface.SetFloat("_DetailTiling", 10);
            return surface;
        }

        private Material GetGlass()
        {
            if (glass != null) return glass;
            glass = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Kin transparent blue lenses", hideFlags = HideFlags.HideAndDontSave };
            glass.SetColor("_BaseColor", new Color(0.15f, 0.55f, 0.85f, 0.22f));
            glass.SetFloat("_Surface", 1); glass.SetFloat("_Blend", 0); glass.SetFloat("_AlphaClip", 0);
            glass.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); glass.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            glass.SetFloat("_ZWrite", 0); glass.SetFloat("_Cull", (float)CullMode.Off);
            glass.SetFloat("_Smoothness", 0.95f); glass.SetFloat("_Metallic", 0);
            glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); glass.DisableKeyword("_ALPHATEST_ON");
            glass.SetOverrideTag("RenderType", "Transparent"); glass.renderQueue = (int)RenderQueue.Transparent;
            return glass;
        }

        private void LateUpdate()
        {
            if (fitRoot == null) return;
            if (Time.deltaTime > 0)
            {
                Vector3 movement = transform.position - previousPosition; movement.y = 0;
                speed = Mathf.Lerp(speed, movement.magnitude / Time.deltaTime, Mathf.Clamp01(Time.deltaTime * 12));
            }
            previousPosition = transform.position;
            UpdateBindings();
        }

        private void UpdateBindings()
        {
            bool animated = rig != null && rig.IsUsingRig && skinned != null && rigBones != null && rigBindposes != null;
            bool visible = (original != null && original.enabled) || (skinned != null && skinned.enabled);
            foreach (Renderer renderer in renderers) if (renderer != null) renderer.enabled = visible;
            Matrix4x4 normalizedToMesh = meshRoot.worldToLocalMatrix * fitRoot.localToWorldMatrix;
            Matrix4x4[] poses = animated ? rigBindposes : null;
            Transform[] bones = animated ? rigBones : null;
            foreach (Binding binding in bindings)
            {
                Matrix4x4 delta = Matrix4x4.identity;
                if (animated && binding.Bone >= 0 && binding.Bone < bones.Length && binding.Bone < poses.Length && bones[binding.Bone] != null)
                    delta = fitRoot.worldToLocalMatrix * bones[binding.Bone].localToWorldMatrix * poses[binding.Bone] * normalizedToMesh;
                binding.Part.localPosition = delta.MultiplyPoint3x4(binding.Pivot);
                float flutter = Mathf.Sin(Time.time * (2.2f + Mathf.Min(speed, 7) * 0.5f) + binding.Pivot.x * 11) *
                    Mathf.Abs(binding.Sway) * (1f + Mathf.Min(speed, 7) * 0.8f);
                Quaternion sway = Quaternion.Euler(flutter, binding.Sway < 0 ? -flutter : flutter, flutter * 0.25f);
                binding.Part.localRotation = delta.rotation * sway;
                binding.Part.localScale = delta.lossyScale;
            }
        }

        public Vector3 FootPosition(bool left) => fitRoot != null
            ? fitRoot.TransformPoint(new Vector3(left ? -0.12f : 0.12f, 0.016f, 0.04f)) : transform.position;

        private void ClearGeometry()
        {
            if (fitRoot != null) { fitRoot.gameObject.SetActive(false); Destroy(fitRoot.gameObject); }
            foreach (Mesh mesh in ownedMeshes) if (mesh != null) Destroy(mesh);
            ownedMeshes.Clear(); bindings.Clear(); renderers.Clear(); fitRoot = null;
        }

        private void OnDestroy()
        {
            if (steps != null) steps.Clear();
            ClearGeometry();
            if (surface != null) Destroy(surface);
            if (glass != null) Destroy(glass);
        }
    }
}
