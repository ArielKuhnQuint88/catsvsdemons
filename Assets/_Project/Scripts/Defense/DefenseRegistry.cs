using System.Collections.Generic;
using UnityEngine;

namespace CatsVsDemons.Defense
{
    public static class DefenseRegistry
    {
        private static readonly HashSet<DefenseHealth> Active = new();
        public static int Count => Active.Count;
        public static void Register(DefenseHealth defense) => Active.Add(defense);
        public static void Unregister(DefenseHealth defense) => Active.Remove(defense);

        public static DefenseHealth FindNearest(Vector3 position, float range)
        {
            DefenseHealth nearest = null;
            float nearestSqr = range * range;
            foreach (DefenseHealth defense in Active)
            {
                if (defense == null || defense.IsDestroyed || !defense.isActiveAndEnabled)
                    continue;
                float sqr = (defense.transform.position - position).sqrMagnitude;
                if (sqr <= nearestSqr)
                {
                    nearest = defense;
                    nearestSqr = sqr;
                }
            }
            return nearest;
        }

        public static void HealInRange(Vector3 position, float range, int amount)
        {
            float rangeSqr = range * range;
            foreach (DefenseHealth defense in Active)
            {
                if (defense != null && !defense.IsDestroyed &&
                    defense.isActiveAndEnabled &&
                    (defense.transform.position - position).sqrMagnitude <= rangeSqr)
                    defense.Heal(amount);
            }
        }
    }
}
