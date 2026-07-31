using System;
using System.Collections.Generic;
using CatsVsDemons.House;
using CatsVsDemons.Visuals;
using UnityEngine;

namespace CatsVsDemons.Enemies
{
    public sealed class EnemyPathFollower : MonoBehaviour
    {
        [SerializeField] private string pathName = "Path_Left";
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float waypointDistance = 0.15f;
        [SerializeField] private int houseDamage = 10;

        private readonly List<Transform> waypoints = new();
        private int currentWaypoint;
        private bool reachedDestination;
        private float speedMultiplier = 1f;
        private float slowTimer;

        private void Awake()
        {
            string[] models =
            {
                "Models/DemonPoerix",
                "Models/DemonSono",
                "Models/DemonFlamurk"
            };
            Color[] colors =
            {
                new Color(0.82f, 0.68f, 0.42f),
                new Color(0.42f, 0.2f, 0.75f),
                new Color(1f, 0.28f, 0.06f)
            };

            int variant = UnityEngine.Random.Range(0, models.Length);
            RuntimeModelVisuals.Attach(
                transform,
                models[variant],
                1.9f,
                -1f,
                colors[variant]
            );
        }

        public void Configure(string newPathName)
        {
            pathName = newPathName;
        }

        public void ConfigureMovement(float speed, int damage)
        {
            moveSpeed = Mathf.Max(0.1f, speed);
            houseDamage = Mathf.Max(0, damage);
        }

        public void SetHouseDamage(int damage)
        {
            houseDamage = Mathf.Max(0, damage);
        }

        public void ApplySlow(float multiplier, float duration)
        {
            speedMultiplier = Mathf.Min(
                speedMultiplier,
                Mathf.Clamp(multiplier, 0.1f, 1f)
            );
            slowTimer = Mathf.Max(slowTimer, duration);
        }

        private void Start()
        {
            LoadPath();

            if (waypoints.Count == 0)
            {
                enabled = false;
                return;
            }

            transform.position = GroundPosition(waypoints[0].position);
            currentWaypoint = 1;
        }

        private void Update()
        {
            UpdateSlow();

            if (reachedDestination || currentWaypoint >= waypoints.Count)
            {
                return;
            }

            Vector3 target = GroundPosition(waypoints[currentWaypoint].position);
            Vector3 offset = target - transform.position;
            offset.y = 0f;

            if (offset.magnitude <= waypointDistance)
            {
                currentWaypoint++;

                if (currentWaypoint >= waypoints.Count)
                {
                    ReachHouse();
                }

                return;
            }

            Vector3 direction = offset.normalized;
            transform.position +=
                direction * moveSpeed * speedMultiplier * Time.deltaTime;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                rotationSpeed * Time.deltaTime
            );
        }

        private void UpdateSlow()
        {
            if (slowTimer <= 0f)
            {
                speedMultiplier = 1f;
                return;
            }

            slowTimer -= Time.deltaTime;
        }

        private void ReachHouse()
        {
            reachedDestination = true;

            HouseHealth house =
                UnityEngine.Object.FindFirstObjectByType<HouseHealth>();

            if (house != null)
            {
                house.TakeDamage(houseDamage);
            }
            else
            {
                Debug.LogWarning("HouseHealth was not found.", this);
            }

            Destroy(gameObject);
        }

        private void LoadPath()
        {
            waypoints.Clear();

            GameObject path = GameObject.Find($"Game/Paths/{pathName}");
            if (path == null)
            {
                Debug.LogError($"Path not found: {pathName}", this);
                return;
            }

            foreach (Transform child in path.transform)
            {
                if (child.name.StartsWith("Joint_", StringComparison.Ordinal))
                {
                    waypoints.Add(child);
                }
            }

            waypoints.Sort((a, b) =>
                string.CompareOrdinal(a.name, b.name)
            );

            if (waypoints.Count < 2)
            {
                Debug.LogError($"Path {pathName} needs at least two joints.", this);
            }
        }

        private Vector3 GroundPosition(Vector3 point)
        {
            point.y = 1f;
            return point;
        }
    }
}
