#if UNITY_EDITOR
using System.Collections.Generic;
using InformationString.Core;
using InformationString.Input;
using UnityEditor;
using UnityEngine;

namespace InformationString.Input.Editor
{
    public static class ConnectionResolverReleaseTests
    {
        private const int AcquireFrames = 3;
        private const int ReleaseFrames = 1;
        private const int HardwareAcquireFrames = 6;
        private const int StartupVacantFrames = 25;

        [MenuItem("InformationString/Run Connection Resolver Release Tests")]
        public static void RunAll()
        {
            TestReleaseFasterThanAcquire();
            TestNoiseInterruptedRelease();
            TestAllSlotsVacantDetection();
            TestLinkReleaseClearsConfirmed();
            TestStartupWarmupBlocksEarlyAcquire();
            TestStartupWarmupAllowsAcquireAfterReady();
            TestStartupWarmupBootstrapsFromSustainedBlock();
            Debug.Log("[ConnectionResolverReleaseTests] All checks passed.");
        }

        private static void TestReleaseFasterThanAcquire()
        {
            var last = string.Empty;
            var count = 0;
            var stable = IdentityMapping.Empty;

            for (var i = 0; i < AcquireFrames; i++)
                ConnectionResolverDebouncing.UpdateStableLabel(
                    "Info1", ref last, ref count, ref stable, AcquireFrames, ReleaseFrames);

            Assert(stable == "Info1", "acquire Info1");

            ConnectionResolverDebouncing.UpdateStableLabel(
                IdentityMapping.Empty, ref last, ref count, ref stable, AcquireFrames, ReleaseFrames);

            Assert(stable == IdentityMapping.Empty, "one empty frame releases");
            Assert(ParseStableInfo(stable) == BlockType.None, "released maps to None");
        }

        private static void TestNoiseInterruptedRelease()
        {
            var last = string.Empty;
            var count = 0;
            var stable = IdentityMapping.Empty;

            for (var i = 0; i < AcquireFrames; i++)
                ConnectionResolverDebouncing.UpdateStableLabel(
                    "Info1", ref last, ref count, ref stable, AcquireFrames, ReleaseFrames);

            ConnectionResolverDebouncing.UpdateStableLabel(
                IdentityMapping.Empty, ref last, ref count, ref stable, AcquireFrames, ReleaseFrames);
            Assert(stable == IdentityMapping.Empty, "first empty releases");

            ConnectionResolverDebouncing.UpdateStableLabel(
                IdentityMapping.Unknown, ref last, ref count, ref stable, AcquireFrames, ReleaseFrames);
            Assert(stable == IdentityMapping.Unknown, "unknown also releases with releaseFrames=1");

            ConnectionResolverDebouncing.UpdateStableLabel(
                IdentityMapping.Empty, ref last, ref count, ref stable, AcquireFrames, ReleaseFrames);
            Assert(stable == IdentityMapping.Empty, "empty after unknown still vacant");
        }

        private static void TestAllSlotsVacantDetection()
        {
            var info = new[] { IdentityMapping.Empty, IdentityMapping.Unknown };
            var rhythm = new[] { "empty", IdentityMapping.Empty };

            Assert(ConnectionResolverDebouncing.AreAllSlotsVacant(info, rhythm), "all vacant");
            Assert(!ConnectionResolverDebouncing.AreAllSlotsVacant(
                new[] { "Info1", IdentityMapping.Empty },
                rhythm), "one occupied info");
        }

        private static void TestLinkReleaseClearsConfirmed()
        {
            var lastCandidate = new List<SensorLinkEntry>();
            var confirmed = new List<SensorLinkEntry> { new SensorLinkEntry("Info1", 0) };
            var linkStableCount = AcquireFrames;

            ConnectionResolverDebouncing.UpdateStableLinks(
                System.Array.Empty<SensorLinkEntry>(),
                lastCandidate,
                confirmed,
                ref linkStableCount,
                AcquireFrames,
                ReleaseFrames);

            Assert(confirmed.Count == 0, "empty links release clears confirmed");
        }

        private static void TestStartupWarmupBlocksEarlyAcquire()
        {
            var consecutive = 0;
            var complete = false;

            for (var i = 0; i < StartupVacantFrames - 1; i++)
            {
                Assert(
                    !ConnectionResolverDebouncing.TryAdvanceStartupWarmup(
                        true, ref consecutive, ref complete, StartupVacantFrames),
                    $"vacant frame {i + 1} should not complete warmup");
            }

            ConnectionResolverDebouncing.TryAdvanceStartupWarmup(
                false, ref consecutive, ref complete, StartupVacantFrames);
            Assert(consecutive == StartupVacantFrames - 1, "non-vacant frame preserves vacant progress");
            Assert(!complete, "warmup still incomplete before final vacant frame");

            ConnectionResolverDebouncing.TryAdvanceStartupWarmup(
                true, ref consecutive, ref complete, StartupVacantFrames);
            Assert(complete, "final vacant frame completes warmup");
        }

        private static void TestStartupWarmupAllowsAcquireAfterReady()
        {
            var consecutive = 0;
            var complete = false;

            for (var i = 0; i < StartupVacantFrames; i++)
            {
                ConnectionResolverDebouncing.TryAdvanceStartupWarmup(
                    true, ref consecutive, ref complete, StartupVacantFrames);
            }

            Assert(complete, "warmup completes after required vacant frames");

            var last = string.Empty;
            var count = 0;
            var stable = IdentityMapping.Empty;

            for (var i = 0; i < HardwareAcquireFrames; i++)
                ConnectionResolverDebouncing.UpdateStableLabel(
                    "Info1", ref last, ref count, ref stable, HardwareAcquireFrames, ReleaseFrames);

            Assert(stable == "Info1", "acquire Info1 after warmup with hardware debounce");
            Assert(ParseStableInfo(stable) == BlockType.Rain, "Info1 maps to Rain");
        }

        private static void TestStartupWarmupBootstrapsFromSustainedBlock()
        {
            var consecutive = 0;
            var complete = false;
            var last = string.Empty;
            var count = 0;
            var stable = IdentityMapping.Empty;

            for (var i = 0; i < HardwareAcquireFrames; i++)
            {
                ConnectionResolverDebouncing.UpdateStableLabel(
                    "Info1", ref last, ref count, ref stable, HardwareAcquireFrames, ReleaseFrames);
                ConnectionResolverDebouncing.TryAdvanceStartupWarmup(
                    false, ref consecutive, ref complete, StartupVacantFrames);
            }

            Assert(!complete, "warmup not done by vacant counter alone");
            Assert(
                ConnectionResolverDebouncing.HasAcquiredNonVacantLabel(new[] { stable }),
                "sustained block should be detectable during warmup");
        }

        private static BlockType ParseStableInfo(string label) =>
            IdentityMapping.ParseInfo(label);

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new UnityException($"[ConnectionResolverReleaseTests] {message}");
        }
    }
}
#endif
