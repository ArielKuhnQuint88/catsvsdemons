using System.Collections;
using CatsVsDemons.Defense;
using CatsVsDemons.Enemies;
using CatsVsDemons.House;
using UnityEngine;

namespace CatsVsDemons.Waves
{
    public sealed class EnemyWaveSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyTemplate;
        [SerializeField] private int totalPhases = 3;
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

        public int CurrentPhase { get; private set; }
        public int TotalPhases => totalPhases;
        public int CurrentWave { get; private set; }
        public int TotalWaves => totalWaves;

        public event System.Action<int, int> PhaseStarted;
        public event System.Action<int, int> WaveStarted;
        public event System.Action<int, int> PreparationChanged;
        public event System.Action PreparationEnded;
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

            StartCoroutine(RunPhases());
        }

        private IEnumerator RunPhases()
        {
            for (int phase = 1; phase <= totalPhases; phase++)
            {
                if (HouseWasDestroyed())
                {
                    yield break;
                }

                CurrentPhase = phase;

                if (phase > 1)
                {
                    ClearAllDefenses();
                    yield return null;
                }

                PhaseStarted?.Invoke(CurrentPhase, totalPhases);

                int phaseMultiplier = 1 << (phase - 1);

                Debug.Log(
                    $"Phase {phase}/{totalPhases}: enemy multiplier x{phaseMultiplier}."
                );

                yield return StartCoroutine(
                    RunPhaseWaves(phaseMultiplier)
                );
            }

            if (!HouseWasDestroyed())
            {
                Debug.Log("Victory: all phases were completed.");
                Victory?.Invoke();
            }
        }

        private IEnumerator RunPhaseWaves(int phaseMultiplier)
        {
            for (int wave = 1; wave <= totalWaves; wave++)
            {
                if (HouseWasDestroyed())
                {
                    yield break;
                }

                float preparation =
                    wave == 1 ? 8f : timeBetweenWaves;

                yield return StartCoroutine(
                    RunPreparation(wave, preparation)
                );

                if (HouseWasDestroyed())
                {
                    yield break;
                }

                CurrentWave = wave;
                WaveStarted?.Invoke(CurrentWave, totalWaves);

                int baseEnemyCount =
                    firstWaveEnemies +
                    ((wave - 1) * enemiesAddedPerWave);

                int enemyCount = baseEnemyCount * phaseMultiplier;

                Debug.Log(
                    $"Phase {CurrentPhase}, wave {wave}/{totalWaves}: " +
                    $"{enemyCount} enemies."
                );

                for (int index = 0; index < enemyCount; index++)
                {
                    if (HouseWasDestroyed())
                    {
                        yield break;
                    }

                    SpawnEnemy(index);
                    yield return new WaitForSeconds(spawnInterval);
                }

                yield return new WaitUntil(
                    () => HouseWasDestroyed() || CountActiveEnemies() == 0
                );
            }
        }

        private IEnumerator RunPreparation(
            int nextWave,
            float duration)
        {
            int seconds = Mathf.CeilToInt(duration);

            while (seconds > 0)
            {
                if (HouseWasDestroyed())
                {
                    yield break;
                }

                PreparationChanged?.Invoke(nextWave, seconds);
                yield return new WaitForSeconds(1f);
                seconds--;
            }

            PreparationEnded?.Invoke();
        }

        private void ClearAllDefenses()
        {
            BuildSpot[] spots =
                Object.FindObjectsByType<BuildSpot>(
                    FindObjectsSortMode.None
                );

            foreach (BuildSpot spot in spots)
            {
                spot.ClearDefense();
            }

            Debug.Log(
                $"Phase {CurrentPhase}: {spots.Length} build spots cleared."
            );
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
            int type =
                (enemyIndex + CurrentWave + CurrentPhase - 2) % 3;

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

            enemy.name =
                $"F{CurrentPhase}_{demonName}_{enemyIndex + 1:00}";

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
