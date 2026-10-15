using UnityEngine;

namespace CatsVsDemons.Defense
{
    public enum DefenseType
    {
        Lantern,
        Bonsai,
        Portal
    }

    public static class TowerBuildSelection
    {
        public static event System.Action<DefenseType> SelectionChanged;
        public static DefenseType Selected { get; private set; } =
            DefenseType.Lantern;
        public static bool AwaitingPlacement { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            Selected = DefenseType.Lantern;
            AwaitingPlacement = false;
        }

        public static void Select(DefenseType type)
        {
            Selected = type;
            AwaitingPlacement = true;
            SelectionChanged?.Invoke(type);
        }

        public static void CancelPlacement() => AwaitingPlacement = false;
        public static void CompletePlacement() => AwaitingPlacement = false;

        public static int GetCost() => GetCost(Selected);

        public static int GetCost(DefenseType type)
        {
            switch (type)
            {
                case DefenseType.Bonsai:
                    return 15;
                case DefenseType.Portal:
                    return 10;
                default:
                    return 10;
            }
        }

        public static string GetDisplayName() => GetDisplayName(Selected);

        public static string GetDisplayName(DefenseType type)
        {
            switch (type)
            {
                case DefenseType.Bonsai:
                    return "Bonsai";
                case DefenseType.Portal:
                    return "Portal";
                default:
                    return "Lanterna";
            }
        }

        public static string GetDescription(DefenseType type) => type switch
        {
            DefenseType.Bonsai => "Cura Kin; perto dele, também repara defesas.",
            DefenseType.Portal => "Teleporte automático entre dois ou mais portais.",
            _ => "Desacelera demônios; perto de Kin, lança fogo."
        };
    }
}
