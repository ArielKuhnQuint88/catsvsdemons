using CatsVsDemons.Visuals;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatsVsDemons.Player
{
    [DefaultExecutionOrder(200)]
    public sealed class KinFootstepTrail : MonoBehaviour
    {
        private sealed class Step { public Transform Root; public float Born; }
        private readonly RaycastHit[] groundHits = new RaycastHit[12];
        private KinEquipmentView owner;
        private bool active, left;
        private Mesh mesh;
        private Transform trail;
        private Step[] pool;
        private int next;
        private Vector3 lastPosition;
        private float distance;
        private const float Spacing = 0.48f, Lifetime = 1.5f;

        public void Configure(KinEquipmentView view, bool useSteps, Material material)
        {
            owner = view;
            if (active == useSteps && (!useSteps || trail != null)) return;
            Clear(); active = useSteps; lastPosition = transform.position; distance = 0;
            if (!active) return;
            mesh = KinEquipmentModels.BuildFootprint();
            trail = new GameObject("Kin Magic Footprints").transform;
            pool = new Step[Application.isMobilePlatform ? 8 : 12];
            for (int i = 0; i < pool.Length; i++)
            {
                GameObject stamp = new("Golden paw footprint " + i, typeof(MeshFilter), typeof(MeshRenderer));
                stamp.layer = gameObject.layer; stamp.transform.SetParent(trail, false);
                stamp.GetComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = stamp.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                stamp.SetActive(false); pool[i] = new Step { Root = stamp.transform, Born = -10 };
            }
        }

        private void LateUpdate()
        {
            if (!active || owner == null || pool == null) return;
            // Scaled time keeps paused tutorials, the shop and background transitions still.
            if (Time.deltaTime <= 0) { lastPosition = transform.position; return; }
            Vector3 move = transform.position - lastPosition; move.y = 0;
            lastPosition = transform.position;
            if (move.magnitude < 3f) distance += move.magnitude;
            else distance = 0; // Teleports must not draw a trail across the garden.
            if (distance >= Spacing && move.sqrMagnitude > 0.0001f)
            {
                distance %= Spacing; left = !left;
                Step step = pool[next]; next = (next + 1) % pool.Length;
                Vector3 foot = owner.FootPosition(left);
                int count = Physics.RaycastNonAlloc(foot + Vector3.up * 0.7f, Vector3.down, groundHits, 2f,
                    ~0, QueryTriggerInteraction.Ignore);
                float closest = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    RaycastHit hit = groundHits[i];
                    if (hit.collider == null || hit.collider.transform.IsChildOf(transform) || hit.normal.y < 0.6f || hit.distance >= closest) continue;
                    closest = hit.distance; foot = hit.point;
                }
                if (!float.IsPositiveInfinity(closest))
                {
                    step.Root.position = foot + Vector3.up * 0.018f;
                    step.Root.rotation = Quaternion.LookRotation(move.normalized, Vector3.up) * Quaternion.Euler(90, 0, 0);
                    step.Born = Time.time; step.Root.gameObject.SetActive(true);
                }
            }
            foreach (Step step in pool)
            {
                float life = (Time.time - step.Born) / Lifetime;
                step.Root.gameObject.SetActive(life < 1 && life >= 0);
                float pulse = Mathf.Sin(Mathf.Clamp01(life * 3f) * Mathf.PI * 0.5f);
                step.Root.localScale = Vector3.one * (2.2f * pulse * (1f - Mathf.Clamp01((life - 0.55f) / 0.45f)));
            }
        }

        public void Clear()
        {
            active = false; pool = null; next = 0; distance = 0;
            if (trail != null) { trail.gameObject.SetActive(false); Destroy(trail.gameObject); trail = null; }
            if (mesh != null) { Destroy(mesh); mesh = null; }
        }

        private void OnDisable() { if (trail != null) trail.gameObject.SetActive(false); }
        private void OnEnable() { if (trail != null) trail.gameObject.SetActive(true); lastPosition = transform.position; }
        private void OnDestroy() => Clear();
    }
}
