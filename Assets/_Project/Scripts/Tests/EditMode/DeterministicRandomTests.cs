using System;
using NUnit.Framework;
using PatternGame.Core.Randomness;

namespace PatternGame.Tests.EditMode
{
    [TestFixture]
    public sealed class DeterministicRandomTests
    {
        const int SmallRangeBound = 20;

        [Test]
        public void SameSeed_ProducesIdenticalSequences()
        {
            var first = new DeterministicRandom(12345);
            var second = new DeterministicRandom(12345);

            for (int draw = 0; draw < 10000; draw++)
            {
                Assert.AreEqual(first.NextUInt(), second.NextUInt(), $"Sequences diverged at draw {draw}.");
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            var first = new DeterministicRandom(1);
            var second = new DeterministicRandom(2);

            int identicalDraws = 0;

            for (int draw = 0; draw < 1000; draw++)
            {
                if (first.NextUInt() == second.NextUInt())
                {
                    identicalDraws++;
                }
            }

            Assert.Less(identicalDraws, 5, "Two different seeds produced suspiciously similar sequences.");
        }

        [Test]
        public void Seed_IsExposedForReproduction()
        {
            Assert.AreEqual(4242, new DeterministicRandom(4242).Seed);
            Assert.AreEqual(0, new DeterministicRandom(0).Seed);
        }

        [Test]
        public void ZeroSeed_DoesNotCollapseIntoADeadState()
        {
            var random = new DeterministicRandom(0);

            bool sawNonZero = false;

            for (int draw = 0; draw < 1000; draw++)
            {
                if (random.NextUInt() != 0u)
                {
                    sawNonZero = true;
                    break;
                }
            }

            Assert.IsTrue(sawNonZero, "Seed 0 collapsed the generator into an all-zero state.");
        }

        [Test]
        public void NextUInt_NeverReturnsZero()
        {
            var random = new DeterministicRandom(987654321);

            for (int draw = 0; draw < 200000; draw++)
            {
                if (random.NextUInt() == 0u)
                {
                    Assert.Fail($"Generator reached the dead zero state at draw {draw}.");
                }
            }
        }

        [Test]
        public void NextUInt_DoesNotRepeatEarly()
        {
            var random = new DeterministicRandom(555);
            uint firstValue = random.NextUInt();

            for (int draw = 0; draw < 200000; draw++)
            {
                if (random.NextUInt() == firstValue)
                {
                    Assert.Fail($"Sequence cycled after only {draw + 1} draws.");
                }
            }
        }

        [Test]
        public void NextInt_StaysWithinTheRequestedBound()
        {
            var random = new DeterministicRandom(31337);

            for (int draw = 0; draw < 100000; draw++)
            {
                int value = random.NextInt(7);

                if (value < 0 || value >= 7)
                {
                    Assert.Fail($"NextInt(7) returned {value} at draw {draw}.");
                }
            }
        }

        [Test]
        public void NextInt_WithBoundOfOne_AlwaysReturnsZero()
        {
            var random = new DeterministicRandom(8);

            for (int draw = 0; draw < 1000; draw++)
            {
                Assert.AreEqual(0, random.NextInt(1));
            }
        }

        [Test]
        public void NextInt_StaysWithinAnExplicitRange()
        {
            var random = new DeterministicRandom(99);

            for (int draw = 0; draw < 100000; draw++)
            {
                int value = random.NextInt(-5, 5);

                if (value < -5 || value >= 5)
                {
                    Assert.Fail($"NextInt(-5, 5) returned {value} at draw {draw}.");
                }
            }
        }

        [Test]
        public void NextInt_ReachesEveryValueInASmallRange()
        {
            var random = new DeterministicRandom(2024);
            var seen = new bool[SmallRangeBound];

            for (int draw = 0; draw < 10000; draw++)
            {
                seen[random.NextInt(SmallRangeBound)] = true;
            }

            for (int value = 0; value < SmallRangeBound; value++)
            {
                Assert.IsTrue(seen[value], $"Value {value} never appeared.");
            }
        }

        [Test]
        public void NextInt_IsCloseToUniform()
        {
            const int bucketCount = 4;
            const int drawCount = 400000;
            const int expectedPerBucket = drawCount / bucketCount;

            var random = new DeterministicRandom(7);
            var buckets = new int[bucketCount];

            for (int draw = 0; draw < drawCount; draw++)
            {
                buckets[random.NextInt(bucketCount)]++;
            }

            for (int bucket = 0; bucket < bucketCount; bucket++)
            {
                float deviation = Math.Abs(buckets[bucket] - expectedPerBucket) / (float)expectedPerBucket;

                Assert.Less(
                    deviation,
                    0.02f,
                    $"Bucket {bucket} received {buckets[bucket]} draws, expected around {expectedPerBucket}.");
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void NextInt_ThrowsForNonPositiveBounds(int exclusiveMaximum)
        {
            var random = new DeterministicRandom(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt(exclusiveMaximum));
        }

        [Test]
        public void NextInt_ThrowsForAnEmptyRange()
        {
            var random = new DeterministicRandom(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt(5, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt(5, 4));
        }

        [Test]
        public void NextFloat_StaysInsideTheUnitInterval()
        {
            var random = new DeterministicRandom(3);

            for (int draw = 0; draw < 200000; draw++)
            {
                float value = random.NextFloat();

                if (value < 0f || value >= 1f)
                {
                    Assert.Fail($"NextFloat returned {value} at draw {draw}.");
                }
            }
        }

        [Test]
        public void NextFloat_AveragesAroundOneHalf()
        {
            var random = new DeterministicRandom(3);
            double total = 0d;

            const int drawCount = 200000;

            for (int draw = 0; draw < drawCount; draw++)
            {
                total += random.NextFloat();
            }

            double mean = total / drawCount;

            Assert.That(mean, Is.EqualTo(0.5d).Within(0.01d));
        }

        [Test]
        public void NextChance_WithZeroProbability_IsAlwaysFalse()
        {
            var random = new DeterministicRandom(17);

            for (int draw = 0; draw < 100000; draw++)
            {
                Assert.IsFalse(random.NextChance(0f));
            }
        }

        [Test]
        public void NextChance_WithFullProbability_IsAlwaysTrue()
        {
            var random = new DeterministicRandom(17);

            for (int draw = 0; draw < 100000; draw++)
            {
                Assert.IsTrue(random.NextChance(1f));
            }
        }

        [Test]
        public void NextChance_ApproximatesTheGivenProbability()
        {
            var random = new DeterministicRandom(64);
            int hits = 0;

            const int drawCount = 200000;

            for (int draw = 0; draw < drawCount; draw++)
            {
                if (random.NextChance(0.25f))
                {
                    hits++;
                }
            }

            double ratio = hits / (double)drawCount;

            Assert.That(ratio, Is.EqualTo(0.25d).Within(0.01d));
        }
    }
}
