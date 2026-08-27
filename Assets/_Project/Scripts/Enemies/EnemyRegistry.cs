using System.Collections.Generic;
using UnityEngine;

namespace CatsVsDemons.Enemies
{
    public static class EnemyRegistry
    {
        private static readonly HashSet<EnemyHealth> Active = new();
        private static readonly List<EnemyHealth> Buffer = new();
        public static int Count => Active.Count;

        public static void Register(EnemyHealth enemy) => Active.Add(enemy);
        public static void Unregister(EnemyHealth enemy) => Active.Remove(enemy);

        public static EnemyHealth FindNearest(Vector3 position, float range)
        {
            EnemyHealth nearest = null;
            float nearestSqr = range * range;
            foreach (EnemyHealth enemy in Active)
            {
                if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled)
                    continue;
                float sqr = (enemy.transform.position - position).sqrMagnitude;
                if (sqr <= nearestSqr)
                {
                    nearest = enemy;
                    nearestSqr = sqr;
                }
            }
            return nearest;
        }

        public static IReadOnlyList<EnemyHealth> GetInRange(
            Vector3 position, float range)
        {
            Buffer.Clear();
            float rangeSqr = range * range;
            foreach (EnemyHealth enemy in Active)
            {
                if (enemy != null && !enemy.IsDead && enemy.isActiveAndEnabled &&
                    (enemy.transform.position - position).sqrMagnitude <= rangeSqr)
                    Buffer.Add(enemy);
            }
            return Buffer;
        }
    }
}
