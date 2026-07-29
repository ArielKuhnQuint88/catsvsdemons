using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatsVsDemons.Enemies
{
    public sealed class EnemyPathFollower : MonoBehaviour
    {
        [SerializeField] private string pathName = "Path_Left";
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float waypointDistance = 0.15f;

        private readonly List<Transform> waypoints = new();
        private int currentWaypoint;
        private bool reachedDestination;

        public void Configure(string newPathName)
        {
            pathName = newPathName;
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
                    reachedDestination = true;
                    Debug.Log($"{name} reached the house.");
                }

                return;
            }

            Vector3 direction = offset.normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                rotationSpeed * Time.deltaTime
            );
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
