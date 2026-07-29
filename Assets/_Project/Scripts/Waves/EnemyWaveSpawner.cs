using System.Collections;
using CatsVsDemons.Enemies;
using CatsVsDemons.House;
using UnityEngine;

namespace CatsVsDemons.Waves
{
    public sealed class EnemyWaveSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyTemplate;
        [SerializeField] private int totalWaves = 3;
        [SerializeField] private int firstWaveEnemies = 5;
        [SerializeField] private int enemiesAddedPerWave = 2;
        [SerializeField] private float spawnInterval = 1.2f;
        [SerializeField] private float timeBetweenWaves = 4f;
        [SerializeField] private string[] pathNames =
        {
            "Path_Left",
            "Path_Right",
            "Path_Bottom"
        };

        private HouseHealth houseHealth;
        private Transform enemiesRoot;

        public int CurrentWave { get; private set; }
        public int TotalWaves => totalWaves;

        public event System.Action<int, int> WaveStarted;
        public event System.Action Victory;

        public void Initialize(GameObject template, Transform root)
        {
            enemyTemplate = template;
            enemiesRoot = root;
        }

        private void Start()
        {
            houseHealth = Object.FindFirstObjectByType<HouseHealth>();

            if (enemiesRoot == null)
            {
                GameObject root = GameObject.Find("Game/Enemies");
                enemiesRoot = root != null ? root.transform : null;
            }

            if (enemyTemplate == null || enemiesRoot == null)
            {
                Debug.LogError(
                    "WaveSpawner needs an enemy template and Enemies root.",
                    this
                );
                enabled = false;
                return;
            }

            StartCoroutine(RunWaves());
        }

        private IEnumerator RunWaves()
        {
            yield return new WaitForSeconds(1f);

            for (int wave = 1; wave <= totalWaves; wave++)
            {
                if (HouseWasDestroyed())
                {
                    yield break;
                }

                CurrentWave = wave;
                WaveStarted?.Invoke(CurrentWave, totalWaves);

                int enemyCount =
                    firstWaveEnemies +
                    ((wave - 1) * enemiesAddedPerWave);

                Debug.Log(
                    $"Wave {wave}/{totalWaves}: {enemyCount} enemies."
                );

                for (int i = 0; i < enemyCount; i++)
                {
                    if (HouseWasDestroyed())
                    {
                        yield break;
                    }

                    SpawnEnemy(i);
                    yield return new WaitForSeconds(spawnInterval);
                }

                yield return new WaitUntil(
                    () => HouseWasDestroyed() || CountActiveEnemies() == 0
                );

                if (wave < totalWaves && !HouseWasDestroyed())
                {
                    yield return new WaitForSeconds(timeBetweenWaves);
                }
            }

            if (!HouseWasDestroyed())
            {
                Debug.Log("Victory: all test waves were completed.");
                Victory?.Invoke();
            }
        }

        private void SpawnEnemy(int enemyIndex)
        {
            GameObject enemy = Instantiate(enemyTemplate, enemiesRoot);

            string selectedPath =
                pathNames[Random.Range(0, pathNames.Length)];

            EnemyPathFollower follower =
                enemy.GetComponent<EnemyPathFollower>();

            if (follower != null)
            {
                follower.Configure(selectedPath);
            }

            ConfigureDemon(enemy, enemyIndex);

            if (enemy.GetComponent<EnemyContactDamage>() == null)
            {
                enemy.AddComponent<EnemyContactDamage>();
            }

            enemy.SetActive(true);
        }

        private void ConfigureDemon(GameObject enemy, int enemyIndex)
        {
            int type = (enemyIndex + CurrentWave - 1) % 3;
            string demonName;
            int health;
            int reward;
            float speed;
            int houseDamage;
            Color color;

            switch (type)
            {
                case 1:
                    demonName = "Poeirix";
                    health = 20;
                    reward = 4;
                    speed = 4.2f;
                    houseDamage = 8;
                    color = new Color(0.78f, 0.68f, 0.48f);
                    break;
                case 2:
                    demonName = "Flamur";
                    health = 50;
                    reward = 8;
                    speed = 2.1f;
                    houseDamage = 20;
                    color = new Color(1f, 0.22f, 0.04f);
                    break;
                default:
                    demonName = "Sonegron";
                    health = 30;
                    reward = 5;
                    speed = 3f;
                    houseDamage = 10;
                    color = new Color(0.38f, 0.12f, 0.68f);
                    break;
            }

            enemy.name = $"{demonName}_{enemyIndex + 1:00}";

            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.Configure(health, reward);
            }

            EnemyPathFollower follower =
                enemy.GetComponent<EnemyPathFollower>();

            if (follower != null)
            {
                follower.ConfigureMovement(speed, houseDamage);
            }

            Renderer renderer = enemy.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        private int CountActiveEnemies()
        {
            EnemyPathFollower[] activeEnemies =
                Object.FindObjectsByType<EnemyPathFollower>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            return activeEnemies.Length;
        }

        private bool HouseWasDestroyed()
        {
            return houseHealth != null && houseHealth.IsDestroyed;
        }
    }
}
