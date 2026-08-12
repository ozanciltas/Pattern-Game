using System;
using NUnit.Framework;
using PatternGame.Gameplay;

namespace PatternGame.Tests.EditMode
{
    [TestFixture]
    public sealed class WallApproachTests
    {
        const float SpawnDistance = 12f;
        const float ArrivalDistance = 0f;

        WallApproach approach;

        [SetUp]
        public void SetUp()
        {
            approach = new WallApproach(SpawnDistance, ArrivalDistance);
        }

        [Test]
        public void Constructor_RejectsASpawnDistanceBehindTheArrivalPoint()
        {
            Assert.Throws<ArgumentException>(() => new WallApproach(0f, 0f));
            Assert.Throws<ArgumentException>(() => new WallApproach(-1f, 0f));
        }

        [Test]
        public void NewApproach_WaitsAtTheSpawnDistance()
        {
            Assert.AreEqual(SpawnDistance, approach.Distance);
            Assert.IsFalse(approach.IsMoving);
            Assert.IsFalse(approach.HasArrived);
            Assert.AreEqual(0f, approach.NormalizedProgress);
        }

        [Test]
        public void Tick_DoesNothingBeforeLaunch()
        {
            approach.Tick(5f);

            Assert.AreEqual(SpawnDistance, approach.Distance);
            Assert.IsFalse(approach.HasArrived);
        }

        [Test]
        public void Launch_RejectsNonPositiveSpeeds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => approach.Launch(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => approach.Launch(-3f));
        }

        [Test]
        public void Tick_MovesTheWallBySpeedTimesDeltaTime()
        {
            approach.Launch(3f);
            approach.Tick(2f);

            Assert.That(approach.Distance, Is.EqualTo(SpawnDistance - 6f).Within(0.0001f));
            Assert.IsTrue(approach.IsMoving);
            Assert.IsFalse(approach.HasArrived);
        }

        [Test]
        public void Tick_IgnoresNonPositiveDeltaTime()
        {
            approach.Launch(3f);
            approach.Tick(0f);
            approach.Tick(-1f);

            Assert.AreEqual(SpawnDistance, approach.Distance);
        }

        [Test]
        public void Wall_StopsExactlyAtTheArrivalDistance()
        {
            approach.Launch(4f);
            approach.Tick(3f);

            Assert.AreEqual(ArrivalDistance, approach.Distance);
            Assert.IsTrue(approach.HasArrived);
            Assert.IsFalse(approach.IsMoving);
        }

        [Test]
        public void AHugeDeltaTimeCannotOvershootTheArrivalPoint()
        {
            approach.Launch(5f);
            approach.Tick(1000f);

            Assert.AreEqual(ArrivalDistance, approach.Distance);
            Assert.IsTrue(approach.HasArrived);
        }

        [Test]
        public void Tick_DoesNothingAfterArrival()
        {
            approach.Launch(100f);
            approach.Tick(1f);

            Assert.IsTrue(approach.HasArrived);

            approach.Tick(10f);

            Assert.AreEqual(ArrivalDistance, approach.Distance);
        }

        [Test]
        public void SmallStepsAndOneBigStepCoverTheSameGround()
        {
            var stepped = new WallApproach(SpawnDistance, ArrivalDistance);
            var single = new WallApproach(SpawnDistance, ArrivalDistance);

            stepped.Launch(2f);
            single.Launch(2f);

            for (int step = 0; step < 100; step++)
            {
                stepped.Tick(0.01f);
            }

            single.Tick(1f);

            Assert.That(stepped.Distance, Is.EqualTo(single.Distance).Within(0.001f));
        }

        [Test]
        public void NormalizedProgress_RunsFromZeroToOne()
        {
            approach.Launch(6f);

            Assert.AreEqual(0f, approach.NormalizedProgress);

            approach.Tick(1f);

            Assert.That(approach.NormalizedProgress, Is.EqualTo(0.5f).Within(0.0001f));

            approach.Tick(1f);

            Assert.AreEqual(1f, approach.NormalizedProgress);
        }

        [Test]
        public void Stop_HaltsTheWallWhereItIs()
        {
            approach.Launch(3f);
            approach.Tick(1f);
            approach.Stop();

            float distanceWhenStopped = approach.Distance;

            approach.Tick(10f);

            Assert.AreEqual(distanceWhenStopped, approach.Distance);
            Assert.IsFalse(approach.HasArrived);
        }

        [Test]
        public void Reset_SendsTheWallBackToTheSpawnDistance()
        {
            approach.Launch(50f);
            approach.Tick(1f);

            Assert.IsTrue(approach.HasArrived);

            approach.Reset();

            Assert.AreEqual(SpawnDistance, approach.Distance);
            Assert.IsFalse(approach.HasArrived);
            Assert.IsFalse(approach.IsMoving);
            Assert.AreEqual(0f, approach.Speed);
        }

        [Test]
        public void TravelTimeMatchesDistanceDividedBySpeed()
        {
            const float speed = 8f;
            const float timeStep = 1f / 120f;

            approach.Launch(speed);

            float elapsed = 0f;

            while (!approach.HasArrived && elapsed < 100f)
            {
                approach.Tick(timeStep);
                elapsed += timeStep;
            }

            float expected = (SpawnDistance - ArrivalDistance) / speed;

            Assert.IsTrue(approach.HasArrived);
            Assert.That(elapsed, Is.EqualTo(expected).Within(timeStep * 2f));
        }
    }
}
