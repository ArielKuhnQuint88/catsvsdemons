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
        public static DefenseType Selected { get; private set; } =
            DefenseType.Lantern;

        public static void Select(DefenseType type)
        {
            Selected = type;
        }

        public static int GetCost()
        {
            switch (Selected)
            {
                case DefenseType.Bonsai:
                    return 15;
                case DefenseType.Portal:
                    return 20;
                default:
                    return 10;
            }
        }

        public static string GetDisplayName()
        {
            switch (Selected)
            {
                case DefenseType.Bonsai:
                    return "Bonsai";
                case DefenseType.Portal:
                    return "Portal";
                default:
                    return "Lanterna";
            }
        }
    }
}
