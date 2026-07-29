namespace CatsVsDemons.Defense
{
    public enum DefenseType
    {
        Lantern,
        Bonsai
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
            return Selected == DefenseType.Bonsai ? 15 : 10;
        }

        public static string GetDisplayName()
        {
            return Selected == DefenseType.Bonsai
                ? "Bonsai"
                : "Lanterna";
        }
    }
}
