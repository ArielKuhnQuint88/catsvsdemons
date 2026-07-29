using CatsVsDemons.Economy;
using UnityEngine;

namespace CatsVsDemons.Defense
{
    public sealed class BuildSpot : MonoBehaviour
    {
        [SerializeField] private int towerCost = 10;
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
                Debug.Log("Este ponto já possui uma torre.");
                return;
            }

            if (wallet == null)
            {
                wallet = Object.FindFirstObjectByType<Wallet>();
            }

            if (wallet == null || !wallet.TrySpend(towerCost))
            {
                Debug.Log("Moedas insuficientes para construir a torre.");
                return;
            }

            isOccupied = true;
            CreateTower();
            Debug.Log($"Torre construída por {towerCost} moedas.");
        }

        private void CreateTower()
        {
            GameObject tower = new GameObject("LanternTower_Prototype");
            tower.transform.SetParent(transform);
            tower.transform.localPosition = Vector3.zero;
            tower.transform.localRotation = Quaternion.identity;

            GameObject basePart = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePart.name = "Base";
            basePart.transform.SetParent(tower.transform);
            basePart.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            basePart.transform.localScale = new Vector3(0.7f, 0.35f, 0.7f);
            RemoveCollider(basePart);
            SetColor(basePart, new Color(0.18f, 0.12f, 0.1f));

            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Pillar";
            pillar.transform.SetParent(tower.transform);
            pillar.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            pillar.transform.localScale = new Vector3(0.22f, 1.1f, 0.22f);
            RemoveCollider(pillar);
            SetColor(pillar, new Color(0.3f, 0.18f, 0.12f));

            GameObject lantern = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lantern.name = "Lantern";
            lantern.transform.SetParent(tower.transform);
            lantern.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            lantern.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            RemoveCollider(lantern);
            SetColor(lantern, new Color(1f, 0.35f, 0.05f));

            tower.AddComponent<TowerAttack>();

            Renderer spotRenderer = GetComponent<Renderer>();
            if (spotRenderer != null)
            {
                spotRenderer.enabled = false;
            }
        }

        private static void RemoveCollider(GameObject part)
        {
            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider != null)
            {
                Destroy(partCollider);
            }
        }

        private static void SetColor(GameObject part, Color color)
        {
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
