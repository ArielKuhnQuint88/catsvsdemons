using CatsVsDemons.Visuals;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatsVsDemons.Player
{
    /// <summary>Readable white paw and golden katana in the garden, plus a camera-local first-person hand.</summary>
    [DefaultExecutionOrder(180)]
    public sealed class KinCombatVisual : MonoBehaviour
    {
        private Transform fitRoot, meshRoot, gardenGrip, firstPersonGrip;
        private MeshFilter source;
        private MeshRenderer original;
        private SkinnedMeshRenderer skinned;
        private AutomaticCharacterRig rig;
        private Transform wrist;
        private Matrix4x4 wristBindpose;
        private KinPrototypeAttack attack;
        private KinPrototypeController movement;
        private KinEquipmentView equipment;
        private Material material;
        private Mesh handMesh;
        private MeshRenderer gardenRenderer, firstPersonRenderer;
        private bool firstPerson;
        private float attackTimer, nextModelCheck;
        private string outfit;
        private static readonly Vector3 RestGrip = new(.278f, .292f, .076f);
        public bool HasGardenHand => gardenGrip != null;
        public bool HasFirstPersonHand => firstPersonGrip != null;
        public Vector3 EyePosition => fitRoot != null ? fitRoot.TransformPoint(new Vector3(0, .695f, .14f)) : transform.position + Vector3.up * .6f;
        private void Awake()
        {
            attack = GetComponent<KinPrototypeAttack>(); movement = GetComponent<KinPrototypeController>(); equipment = GetComponent<KinEquipmentView>();
            EnsureModel();
        }
        private void OnEnable() { BindAttack(); if (firstPersonGrip != null) firstPersonGrip.gameObject.SetActive(firstPerson); }
        private void BindAttack()
        {
            KinPrototypeAttack current = GetComponent<KinPrototypeAttack>();
            if (current == attack && attack != null) { attack.AttackStarted -= TriggerAttack; attack.AttackStarted += TriggerAttack; return; }
            if (attack != null) attack.AttackStarted -= TriggerAttack;
            attack = current; if (attack != null) attack.AttackStarted += TriggerAttack;
        }
        private void OnDisable()
        {
            if (attack != null) attack.AttackStarted -= TriggerAttack;
            if (firstPersonGrip != null) firstPersonGrip.gameObject.SetActive(false);
        }
        public void TriggerAttack() => attackTimer = KinPoseMath.AttackDuration;
        private void EnsureModel()
        {
            if (fitRoot != null) return;
            Transform model = transform.Find("GameplayModel");
            if (model == null) return;
            source = model.GetComponentInChildren<MeshFilter>(true);
            if (source == null || source.sharedMesh == null) return;
            meshRoot = source.transform; original = source.GetComponent<MeshRenderer>();
            rig = model.GetComponent<AutomaticCharacterRig>(); skinned = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinned != null && skinned.sharedMesh != null)
            {
                for (int i = 0; i < skinned.bones.Length; i++)
                    if (skinned.bones[i] != null && skinned.bones[i].name == "Rig_RightForearm" && i < skinned.sharedMesh.bindposes.Length)
                    { wrist = skinned.bones[i]; wristBindpose = skinned.sharedMesh.bindposes[i]; break; }
            }
            Bounds bounds = source.sharedMesh.bounds;
            fitRoot = new GameObject("Kin Combat Hand Fit").transform; fitRoot.SetParent(meshRoot, false);
            fitRoot.localPosition = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z);
            fitRoot.localRotation = Quaternion.Euler(90, 0, 0); fitRoot.localScale = Vector3.one * Mathf.Max(.001f, bounds.size.z);
            gardenGrip = CreateGrip("Kin attacking paw and golden katana", fitRoot, out gardenRenderer);
            gardenGrip.localPosition = RestGrip;
            UpdateOutfit();
        }
        private Transform CreateGrip(string name, Transform parent, out MeshRenderer renderer)
        {
            GameObject root = new(name, typeof(MeshFilter), typeof(MeshRenderer)); root.transform.SetParent(parent, false);
            root.layer = parent.gameObject.layer; renderer = root.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GetMaterial(); renderer.shadowCastingMode = ShadowCastingMode.On;
            return root.transform;
        }
        private Material GetMaterial()
        {
            if (material != null) return material;
            Shader shader = Shader.Find("CatsVsDemons/StylizedModel") ?? Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader) { name = "Kin white paw and gold katana", hideFlags = HideFlags.HideAndDontSave };
            material.SetColor("_BaseColor", Color.white); material.SetFloat("_UseVertexColor", 1);
            material.SetFloat("_UseHeightPalette", 0); material.SetFloat("_UseBaseMap", 0); material.SetFloat("_AmbientFloor", .28f);
            Texture2D detail = Resources.Load<Texture2D>("Textures/Characters/Kin_SurfaceDetail");
            if (detail != null) material.SetTexture("_DetailMap", detail);
            material.SetFloat("_DetailStrength", .18f); material.SetFloat("_DetailTiling", 10);
            return material;
        }
        public static Mesh BuildHand(string outfitId)
        {
            KinEquipmentModels.GetPalette(outfitId, out Color sleeve, out Color secondary, out Color dark, out Color accent);
            KinEquipmentGeometry g = new(); Color fur = new(.98f, .94f, .87f), gold = new(.93f, .73f, .27f);
            g.Sleeve(new Vector3(0, -.12f, -.018f), new Vector3(0, -.031f, 0), .047f, .036f, sleeve);
            g.Ring(new Vector3(0, -.036f, 0), .039f, .034f, .004f, accent, .55f, 0, true);
            g.Ellipsoid(Vector3.zero, new Vector3(.043f, .035f, .041f), fur, 0, 0, 10, 5);
            for (int i = 0; i < 3; i++) g.Ellipsoid(new Vector3((i - 1) * .018f, .012f, .026f), new Vector3(.011f, .016f, .018f), fur, 0, 0, 8, 4);
            g.Tube(new[] { new Vector3(0, -.028f, .013f), new Vector3(0, .052f, .013f) }, .009f, dark, .25f);
            for (int i = 0; i < 4; i++) g.Ring(new Vector3(0, -.012f + i * .016f, .013f), .011f, .011f, .002f, gold, .85f, 0, true);
            g.Ellipsoid(new Vector3(0, .057f, .013f), new Vector3(.048f, .006f, .028f), gold, .9f, 0, 12, 4);
            // Solid two-sided blade with a curved tip and brighter sharpened edge.
            Vector3[] centers = { new(0, .065f, .013f), new(.014f, .18f, .013f), new(.025f, .3f, .013f), new(.046f, .386f, .013f) };
            g.Panel(centers, new[] { .021f, .020f, .015f, .001f }, gold, .004f, .85f);
            g.Tube(new[] { new Vector3(.0115f, .07f, .017f), new Vector3(.025f, .18f, .017f), new Vector3(.0335f, .3f, .017f), new Vector3(.0465f, .386f, .015f) }, .0025f, new Color(1, .91f, .62f), .9f, .08f);
            return g.Build("Kin sculpted attacking paw and katana");
        }
        private void UpdateOutfit()
        {
            string current = equipment != null ? equipment.OutfitId : "samurai_vermelho";
            if (handMesh != null && outfit == current) return;
            outfit = current; Mesh old = handMesh; handMesh = BuildHand(current);
            if (gardenGrip != null) gardenGrip.GetComponent<MeshFilter>().sharedMesh = handMesh;
            if (firstPersonGrip != null) firstPersonGrip.GetComponent<MeshFilter>().sharedMesh = handMesh;
            if (old != null) Destroy(old);
        }
        public void SetFirstPerson(bool value, Camera camera)
        {
            firstPerson = value; EnsureModel();
            if (value && camera != null && firstPersonGrip == null)
            {
                firstPersonGrip = CreateGrip("Kin First Person Hand", camera.transform, out firstPersonRenderer);
                firstPersonRenderer.shadowCastingMode = ShadowCastingMode.Off;
                firstPersonRenderer.receiveShadows = false;
                firstPersonGrip.GetComponent<MeshFilter>().sharedMesh = handMesh;
            }
            if (firstPersonGrip != null) firstPersonGrip.gameObject.SetActive(value && isActiveAndEnabled);
        }
        private void LateUpdate()
        {
            if (fitRoot == null && Time.unscaledTime >= nextModelCheck)
            { nextModelCheck = Time.unscaledTime + .3f; EnsureModel(); BindAttack(); }
            if (attack == null) BindAttack();
            if (fitRoot == null || Time.timeScale <= 0 || Time.deltaTime <= 0) return;
            if (equipment == null) equipment = GetComponent<KinEquipmentView>();
            UpdateOutfit();
            attackTimer = Mathf.Max(0, attackTimer - Time.deltaTime);
            float p = 1 - attackTimer / KinPoseMath.AttackDuration;
            float strike = attackTimer > 0 ? KinPoseMath.Swing(p) : 0;
            bool animated = rig != null && rig.IsUsingRig && wrist != null;
            Matrix4x4 delta = animated ? fitRoot.worldToLocalMatrix * wrist.localToWorldMatrix * wristBindpose * meshRoot.worldToLocalMatrix * fitRoot.localToWorldMatrix : Matrix4x4.identity;
            gardenGrip.localPosition = delta.MultiplyPoint3x4(RestGrip);
            gardenGrip.localRotation = animated ? delta.rotation : Quaternion.Euler(-strike * 105, strike * 34, -strike * 20);
            gardenGrip.localScale = delta.lossyScale;
            gardenRenderer.enabled = (original != null && original.enabled) || (skinned != null && skinned.enabled);
            if (firstPerson && firstPersonGrip != null)
            {
                float speed = movement != null ? Mathf.Clamp01(movement.PlanarVelocity.magnitude / 7) : 0;
                float bob = Mathf.Sin(Time.time * 9) * .008f * speed;
                firstPersonGrip.localPosition = new Vector3(.23f - strike * .13f, -.17f + bob + strike * .05f, .47f + strike * .10f);
                firstPersonGrip.localRotation = Quaternion.Euler(9 - strike * 68, -12 + strike * 43, -18 + strike * 65);
                firstPersonGrip.localScale = Vector3.one * .88f;
            }
        }
        private void OnDestroy()
        {
            if (attack != null) attack.AttackStarted -= TriggerAttack;
            if (fitRoot != null) Destroy(fitRoot.gameObject);
            if (firstPersonGrip != null) Destroy(firstPersonGrip.gameObject);
            if (handMesh != null) Destroy(handMesh); if (material != null) Destroy(material);
        }
    }
}
