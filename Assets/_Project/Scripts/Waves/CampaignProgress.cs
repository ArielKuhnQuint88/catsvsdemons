using UnityEngine;

namespace CatsVsDemons.Waves
{
    public struct CampaignScenario
    {
        public string SceneName { get; }
        public string DisplayName { get; }
        public int FirstPhase { get; }
        public int LastPhase { get; }

        public CampaignScenario(
            string sceneName,
            string displayName,
            int firstPhase,
            int lastPhase)
        {
            SceneName = sceneName;
            DisplayName = displayName;
            FirstPhase = firstPhase;
            LastPhase = lastPhase;
        }
    }

    /// <summary>
    /// Keeps the campaign state while Unity swaps between the three authored
    /// scenario scenes. Shop unlocks already live in PlayerPrefs; this class
    /// only carries the current chapter and the wallet for the active run.
    /// </summary>
    public static class CampaignProgress
    {
        public const int TotalPhaseCount = 12;

        private static readonly string[] Seasons =
        {
            "Primavera",
            "Verão",
            "Outono",
            "Inverno"
        };

        private static readonly CampaignScenario[] Scenarios =
        {
            new CampaignScenario(
                "Game",
                "Jardim das Cerejeiras",
                1,
                4
            ),
            new CampaignScenario(
                "Game_BambooGrove",
                "Bosque de Bambu",
                5,
                8
            ),
            new CampaignScenario(
                "Game_EclipseSanctuary",
                "Santuário do Eclipse",
                9,
                12
            )
        };

        public static bool IsCampaignActive { get; private set; }
        public static bool HasStoredCoins { get; private set; }
        public static int StoredCoins { get; private set; }
        public static int NextPhase { get; private set; } = 1;

        public static void BeginNewCampaign()
        {
            IsCampaignActive = true;
            HasStoredCoins = false;
            StoredCoins = 0;
            NextPhase = 1;
        }

        public static CampaignScenario GetScenarioForScene(string sceneName)
        {
            foreach (CampaignScenario scenario in Scenarios)
            {
                if (scenario.SceneName == sceneName)
                {
                    return scenario;
                }
            }

            return Scenarios[0];
        }

        public static CampaignScenario GetScenarioForPhase(int phase)
        {
            int scenarioIndex = GetScenarioIndex(phase);
            return Scenarios[scenarioIndex];
        }

        public static int GetStartingPhase(string sceneName)
        {
            CampaignScenario scenario = GetScenarioForScene(sceneName);
            if (!IsCampaignActive)
            {
                IsCampaignActive = true;
                HasStoredCoins = false;
                StoredCoins = 0;
                NextPhase = scenario.FirstPhase;
                return NextPhase;
            }

            if (NextPhase < scenario.FirstPhase || NextPhase > scenario.LastPhase)
            {
                return scenario.FirstPhase;
            }

            return NextPhase;
        }

        public static void StoreScenarioTransition(int completedPhase, int coins)
        {
            IsCampaignActive = true;
            HasStoredCoins = true;
            StoredCoins = Mathf.Max(0, coins);
            NextPhase = Mathf.Clamp(completedPhase + 1, 1, TotalPhaseCount);
        }

        public static bool HasStoredCoinsForScene(string sceneName)
        {
            CampaignScenario scenario = GetScenarioForScene(sceneName);
            return HasStoredCoins &&
                NextPhase >= scenario.FirstPhase &&
                NextPhase <= scenario.LastPhase;
        }

        public static bool IsScenarioTransitionAfter(int phase)
        {
            CampaignScenario scenario = GetScenarioForPhase(phase);
            return phase == scenario.LastPhase && phase < TotalPhaseCount;
        }

        public static string GetNextSceneName(string sceneName)
        {
            CampaignScenario current = GetScenarioForScene(sceneName);
            for (int index = 0; index < Scenarios.Length - 1; index++)
            {
                if (Scenarios[index].SceneName == current.SceneName)
                {
                    return Scenarios[index + 1].SceneName;
                }
            }

            return string.Empty;
        }

        public static int GetSeasonIndex(int phase)
        {
            return (Mathf.Clamp(phase, 1, TotalPhaseCount) - 1) % 4;
        }

        public static string GetSeasonName(int phase)
        {
            return Seasons[GetSeasonIndex(phase)];
        }

        public static int GetScenarioIndex(int phase)
        {
            return Mathf.Clamp(
                (Mathf.Clamp(phase, 1, TotalPhaseCount) - 1) / 4,
                0,
                Scenarios.Length - 1
            );
        }

        public static string GetPhaseTitle(int phase)
        {
            CampaignScenario scenario = GetScenarioForPhase(phase);
            return $"{scenario.DisplayName} — {GetSeasonName(phase)}";
        }
    }
}
