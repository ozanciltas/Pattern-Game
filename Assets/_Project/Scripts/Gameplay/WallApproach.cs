using System;
using UnityEngine;

namespace PatternGame.Gameplay
{
    public sealed class WallApproach
    {
        readonly float spawnDistance;
        readonly float arrivalDistance;

        float distance;
        float speed;
        bool isMoving;
        bool hasArrived;

        public WallApproach(float spawnDistance, float arrivalDistance)
        {
            if (spawnDistance <= arrivalDistance)
            {
                throw new ArgumentException(
                    $"Spawn distance ({spawnDistance}) must be greater than arrival distance ({arrivalDistance}).",
                    nameof(spawnDistance));
            }

            this.spawnDistance = spawnDistance;
            this.arrivalDistance = arrivalDistance;

            Reset();
        }

        public float SpawnDistance => spawnDistance;

        public float ArrivalDistance => arrivalDistance;

        public float Distance => distance;

        public float Speed => speed;

        public bool IsMoving => isMoving;

        public bool HasArrived => hasArrived;

        public float NormalizedProgress
        {
            get
            {
                float span = spawnDistance - arrivalDistance;

                return Mathf.Clamp01((spawnDistance - distance) / span);
            }
        }

        public void Reset()
        {
            distance = spawnDistance;
            speed = 0f;
            isMoving = false;
            hasArrived = false;
        }

        public void Launch(float approachSpeed)
        {
            if (approachSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(approachSpeed),
                    $"Approach speed must be positive but was {approachSpeed}.");
            }

            speed = approachSpeed;
            isMoving = true;
            hasArrived = false;
        }

        public void Stop()
        {
            isMoving = false;
        }

        public void Tick(float deltaTime)
        {
            if (!isMoving || deltaTime <= 0f)
            {
                return;
            }

            distance -= speed * deltaTime;

            if (distance <= arrivalDistance)
            {
                distance = arrivalDistance;
                isMoving = false;
                hasArrived = true;
            }
        }
    }
}
