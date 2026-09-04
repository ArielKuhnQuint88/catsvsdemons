using System.Collections;
using System.Collections.Generic;
using CatsVsDemons.Defense;
using CatsVsDemons.Economy;
using CatsVsDemons.Enemies;
using CatsVsDemons.House;
using UnityEngine;
using UnityEngine.SceneManagement;
using CatsVsDemons.UI;

namespace CatsVsDemons.Waves
{
    public sealed class EnemyWaveSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyTemplate;
        [SerializeField] private int totalPhases = CampaignProgress.TotalPhaseCount;
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
        private Wallet wallet;
        private Transform enemiesRoot;
        private PhaseEnvironmentController phaseEnvironment;
        private int sceneFirstPhase = 1;
        private int sceneLastPhase = 4;
        private string nextScenarioScene;
        private bool continueFromIntermission;

        public int CurrentPhase { get; private set; }
        public int TotalPhases => totalPhases;
        public int CurrentWave { get; private set; }
        public int TotalWaves => totalWaves;
        public int StartingPhase => sceneFirstPhase;
        public bool IsInIntermission { get; private set; }

        public event System.Action<int, int> PhaseStarted;
        public event System.Action<int, int> WaveStarted;
        public event System.Action<int, int> PreparationChanged;
        public event System.Action PreparationEnded;
        public event System.Action<int, int> IntermissionStarted;
        public event System.Action Victory;

        public void Initialize(GameObject template, Transform root)
        {
            enemyTemplate = template;
            enemiesRoot = root;
        }

        private void Awake()
        {
            ConfigureCampaignScene();

            // The scene keeps the three authored routes, while the environment
            // controller pushes their entrances beyond the active camera. Add it
            // here so a demon always spawns where its route begins: off-screen.
            GameObject gameRoot = GameObject.Find("Game");
            if (gameRoot == null)
            {
                return;
            }

            phaseEnvironment =
                gameRoot.GetComponent<PhaseEnvironmentController>();
            if (phaseEnvironment == null)
            {
                phaseEnvironment =
                    gameRoot.AddComponent<PhaseEnvironmentController>();
            }
        }

        private void Start()
        {
            houseHealth = Object.FindFirstObjectByType<HouseHealth>();
            wallet = Object.FindFirstObjectByType<Wallet>();
            if (wallet != null && CampaignProgress.HasStoredCoinsForScene(
                SceneManager.GetActiveScene().name))
            {
                wallet.SetCoins(CampaignProgress.StoredCoins);
            }

            HouseIntermissionController intermission =
                GetComponent<HouseIntermissionController>();
            if (intermission == null)
            {
                intermission = gameObject.AddComponent<HouseIntermissionController>();
            }
            intermission.Initialize(this);

            if (enemiesRoot == null)
            {
                GameObject root = GameObject.Find("Game/Enemies");
                enemiesRoot = root != null ? root.transform : null;
            }

            RefreshAvailablePaths();
            phaseEnvironment?.ApplyPhase(StartingPhase, totalPhases);

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
            yield return new WaitUntil(() => !TutorialDirector.BlockWaves);

            for (int phase = sceneFirstPhase; phase <= sceneLastPhase; phase++)
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

                float phaseMultiplier = 1f + (phase - 1) * 0.22f;

                Debug.Log(
                    $"Phase {phase}/{totalPhases}: enemy multiplier x" +
                    $"{phaseMultiplier:0.00}."
                );

                yield return StartCoroutine(
                    RunPhaseWaves(phaseMultiplier)
                );

                if (phase < totalPhases && !HouseWasDestroyed())
                {
                    yield return StartCoroutine(RunIntermission(phase));

                    if (CampaignProgress.IsScenarioTransitionAfter(phase))
                    {
                        LoadNextScenario(phase);
                        yield break;
                    }
                }
            }

            if (!HouseWasDestroyed() && sceneLastPhase >= totalPhases)
            {
                Debug.Log("Victory: all phases were completed.");
                Victory?.Invoke();
            }
        }

        private void ConfigureCampaignScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            CampaignScenario scenario =
                CampaignProgress.GetScenarioForScene(sceneName);

            totalPhases = CampaignProgress.TotalPhaseCount;
            sceneFirstPhase = CampaignProgress.GetStartingPhase(sceneName);
            sceneLastPhase = scenario.LastPhase;
            nextScenarioScene = CampaignProgress.GetNextSceneName(sceneName);
        }

        private void LoadNextScenario(int completedPhase)
        {
            if (string.IsNullOrEmpty(nextScenarioScene))
            {
                Debug.LogError(
                    $"No next scenario was configured after phase " +
                    $"{completedPhase}.",
                    this
                );
                return;
            }

            int coins = wallet != null ? wallet.Coins : 0;
            CampaignProgress.StoreScenarioTransition(completedPhase, coins);
            Time.timeScale = 1f;
            SceneManager.LoadScene(nextScenarioScene);
        }

        public void ContinueFromHouse()
        {
            if (IsInIntermission)
            {
                continueFromIntermission = true;
            }
        }

        private IEnumerator RunIntermission(int completedPhase)
        {
            if (IntermissionStarted == null)
            {
                Debug.LogWarning(
                    "House intermission controller was not found; " +
                    "continuing to the next phase.",
                    this
                );
                yield break;
            }

            IsInIntermission = true;
            continueFromIntermission = false;
            CurrentWave = 0;
            IntermissionStarted.Invoke(completedPhase, totalPhases);

            yield return new WaitUntil(
                () => continueFromIntermission || HouseWasDestroyed()
            );

            IsInIntermission = false;
            continueFromIntermission = false;
        }

        private IEnumerator RunPhaseWaves(float phaseMultiplier)
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

                int enemyCount = Mathf.CeilToInt(
                    baseEnemyCount * phaseMultiplier
                );

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

        private void RefreshAvailablePaths()
        {
            GameObject pathsObject = GameObject.Find("Game/Paths");
            if (pathsObject == null)
            {
                return;
            }

            List<string> discovered = new();
            foreach (Transform path in pathsObject.transform)
            {
                if (path.name.StartsWith("Path_"))
                {
                    discovered.Add(path.name);
                }
            }

            if (discovered.Count > 0)
            {
                pathNames = discovered.ToArray();
                Debug.Log($"WaveSpawner found {pathNames.Length} enemy routes.", this);
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
            return EnemyRegistry.Count;
        }

        private bool HouseWasDestroyed()
        {
            return houseHealth != null && houseHealth.IsDestroyed;
        }
    }
}
