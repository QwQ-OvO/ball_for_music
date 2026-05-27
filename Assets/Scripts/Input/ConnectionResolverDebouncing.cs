using System;
using System.Collections.Generic;
using InformationString.Core;

namespace InformationString.Input
{
    /// <summary>
    /// Shared debounce helpers for <see cref="ConnectionResolver"/> and Editor tests.
    /// </summary>
    public static class ConnectionResolverDebouncing
    {
        public static bool IsVacantLabel(string label)
        {
            if (string.IsNullOrEmpty(label)) return true;
            return label == IdentityMapping.Empty || label == IdentityMapping.Unknown;
        }

        public static bool AreAllSlotsVacant(string[] infoSlots, string[] rhythmSlots)
        {
            if (infoSlots == null || rhythmSlots == null) return false;
            if (infoSlots.Length == 0 || infoSlots.Length != rhythmSlots.Length) return false;

            for (var i = 0; i < infoSlots.Length; i++)
            {
                if (!IsVacantLabel(infoSlots[i])) return false;
                if (!IsVacantLabel(rhythmSlots[i])) return false;
            }

            return true;
        }

        /// <summary>
        /// Tracks consecutive all-vacant frames at startup. Returns true once warmup is complete.
        /// Only increments on vacant frames; non-vacant frames leave the counter unchanged.
        /// </summary>
        public static bool TryAdvanceStartupWarmup(
            bool allSlotsVacant,
            ref int consecutiveAllVacantFrames,
            ref bool warmupComplete,
            int requiredVacantFrames)
        {
            if (warmupComplete) return true;
            if (requiredVacantFrames <= 0)
            {
                warmupComplete = true;
                return true;
            }

            if (allSlotsVacant)
                consecutiveAllVacantFrames++;

            if (consecutiveAllVacantFrames >= requiredVacantFrames)
                warmupComplete = true;

            return warmupComplete;
        }

        public static bool HasAcquiredNonVacantLabel(string[] stableLabels)
        {
            if (stableLabels == null) return false;
            for (var i = 0; i < stableLabels.Length; i++)
            {
                if (!IsVacantLabel(stableLabels[i])) return true;
            }

            return false;
        }

        public static void UpdateStableLabel(
            string candidate,
            ref string lastCandidate,
            ref int stableCount,
            ref string stableValue,
            int acquireFrames,
            int releaseFrames)
        {
            candidate ??= IdentityMapping.Empty;

            if (string.Equals(candidate, lastCandidate, StringComparison.Ordinal)) stableCount++;
            else stableCount = 1;

            lastCandidate = candidate;

            var required = IsVacantLabel(candidate) ? releaseFrames : acquireFrames;
            if (stableCount >= required)
                stableValue = candidate;
        }

        public static void UpdateStableLinks(
            IReadOnlyList<SensorLinkEntry> frameLinks,
            List<SensorLinkEntry> lastCandidateLinks,
            List<SensorLinkEntry> confirmedLinks,
            ref int linkStableCount,
            int acquireFrames,
            int releaseFrames)
        {
            var current = new List<SensorLinkEntry>();
            if (frameLinks != null)
            {
                for (var i = 0; i < frameLinks.Count; i++)
                    current.Add(frameLinks[i]);
            }

            if (LinksEqual(current, lastCandidateLinks))
                linkStableCount++;
            else
            {
                linkStableCount = 1;
                lastCandidateLinks.Clear();
                for (var i = 0; i < current.Count; i++)
                    lastCandidateLinks.Add(current[i]);
            }

            var required = current.Count == 0 ? releaseFrames : acquireFrames;
            if (linkStableCount >= required)
            {
                confirmedLinks.Clear();
                for (var i = 0; i < current.Count; i++)
                    confirmedLinks.Add(current[i]);
            }
        }

        public static bool LinksEqual(List<SensorLinkEntry> a, List<SensorLinkEntry> b)
        {
            if (a.Count != b.Count) return false;
            for (var i = 0; i < a.Count; i++)
            {
                if (!a[i].Equals(b[i])) return false;
            }

            return true;
        }
    }
}
