using CatsVsDemons.Economy;
using UnityEngine;

namespace CatsVsDemons.Defense
{
    public sealed class BuildSpot : MonoBehaviour
    {
        [SerializeField] private bool isOccupied;

        private Wallet wallet;

        private void Awake()
        {
            wallet = Object.FindFirstObjectByType<Wallet>();
        }

        private void OnMouseDown()
        {
            if (isOccupied)
            {
                Debug.Log("Este ponto já possui uma defesa.");
                return;
            }

            if (wallet == null)
            {
                wallet = Object.FindFirstObjectByType<Wallet>();
            }

            int cost = TowerBuildSelection.GetCost();

            if (wallet == null || !wallet.TrySpend(cost))
            {
                Debug.Log("Moedas insuficientes para construir.");
                return;
            }

            isOccupied = true;

            switch (TowerBuildSelection.Selected)
            {
                case DefenseType.Bonsai:
                    CreateBonsai();
                    break;
                case DefenseType.Portal:
                    CreatePortal();
                    break;
                default:
                    CreateLantern();
                    break;
            }

            Debug.Log(
                $"{TowerBuildSelection.GetDisplayName()} construído por {cost} moedas."
            );

            Renderer spotRenderer = GetComponent<Renderer>();
            if (spotRenderer != null)
            {
                spotRenderer.enabled = false;
            }
        }

        public void ClearDefense()
        {
            if (!isOccupied)
            {
                return;
            }

            for (int index = transform.childCount - 1; index >= 0; index--)
            {
                Destroy(transform.GetChild(index).gameObject);
            }

            isOccupied = false;

            Renderer spotRenderer = GetComponent<Renderer>();
            if (spotRenderer != null)
            {
                spotRenderer.enabled = true;
            }
        }

        private void CreateLantern()
        {
            GameObject tower = CreateRoot("LanternTower_Prototype");

            CreatePart(
                "Base", PrimitiveType.Cylinder, tower.transform,
                new Vector3(0f, 0.35f, 0f),
                new Vector3(0.7f, 0.35f, 0.7f),
                new Color(0.18f, 0.12f, 0.1f)
            );
            CreatePart(
                "Pillar", PrimitiveType.Cube, tower.transform,
                new Vector3(0f, 1.05f, 0f),
                new Vector3(0.22f, 1.1f, 0.22f),
                new Color(0.3f, 0.18f, 0.12f)
            );
            CreatePart(
                "Lantern", PrimitiveType.Cube, tower.transform,
                new Vector3(0f, 1.75f, 0f),
                new Vector3(0.75f, 0.75f, 0.75f),
                new Color(1f, 0.35f, 0.05f)
            );

            tower.AddComponent<TowerAttack>();
        }

        private void CreateBonsai()
        {
            GameObject bonsai = CreateRoot("Bonsai_Prototype");

            CreatePart(
                "Pot", PrimitiveType.Cylinder, bonsai.transform,
                new Vector3(0f, 0.3f, 0f),
                new Vector3(0.85f, 0.3f, 0.85f),
                new Color(0.45f, 0.18f, 0.08f)
            );
            CreatePart(
                "Trunk", PrimitiveType.Cylinder, bonsai.transform,
                new Vector3(0f, 1f, 0f),
                new Vector3(0.25f, 0.75f, 0.25f),
                new Color(0.3f, 0.14f, 0.05f)
            );
            CreatePart(
                "Leaves", PrimitiveType.Sphere, bonsai.transform,
                new Vector3(0f, 1.75f, 0f),
                new Vector3(1.5f, 0.9f, 1.2f),
                new Color(0.1f, 0.65f, 0.18f)
            );

            bonsai.AddComponent<BonsaiHealing>();
        }

        private void CreatePortal()
        {
            GameObject portal = CreateRoot("Portal_Prototype");

            Color stone = new Color(0.18f, 0.32f, 0.38f);
            Color glow = new Color(0.05f, 0.85f, 1f);

            CreatePart(
                "LeftPillar", PrimitiveType.Cube, portal.transform,
                new Vector3(-0.75f, 1.15f, 0f),
                new Vector3(0.3f, 2.3f, 0.45f),
                stone
            );
            CreatePart(
                "RightPillar", PrimitiveType.Cube, portal.transform,
                new Vector3(0.75f, 1.15f, 0f),
                new Vector3(0.3f, 2.3f, 0.45f),
                stone
            );
            CreatePart(
                "TopBeam", PrimitiveType.Cube, portal.transform,
                new Vector3(0f, 2.35f, 0f),
                new Vector3(2.2f, 0.3f, 0.55f),
                stone
            );
            CreatePart(
                "PortalGlow", PrimitiveType.Cube, portal.transform,
                new Vector3(0f, 1.2f, 0.08f),
                new Vector3(1.15f, 1.8f, 0.08f),
                glow
            );

            portal.AddComponent<PortalTransport>();
        }

        private GameObject CreateRoot(string objectName)
        {
            GameObject root = new GameObject(objectName);
            root.transform.SetParent(transform);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            return root;
        }

        private static void CreatePart(
            string partName,
            PrimitiveType primitive,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = partName;
            part.transform.SetParent(parent);
            part.transform.localPosition = position;
            part.transform.localScale = scale;

            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider != null)
            {
                Destroy(partCollider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.color = color;
            renderer.material = material;
        }
    }
}
