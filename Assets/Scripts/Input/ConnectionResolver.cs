using System;
using System.Collections.Generic;
using InformationString.Core;
using InformationString.Core.Config;
using UnityEngine;

namespace InformationString.Input
{
    public sealed class ConnectionResolver : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private SensorMapping sensorMapping;

        [Header("Debug")]
        [SerializeField] private bool debugLog;

        private string[] stableInfoLabels;
        private string[] stableRhythmLabels;
        private BlockType[] stableInfo;
        private RhythmType[] stableRhythm;

        private string[] lastCandidateInfoLabels;
        private string[] lastCandidateRhythmLabels;
        private int[] infoStableCounts;
        private int[] rhythmStableCounts;

        private readonly List<SensorLinkEntry> lastCandidateLinks = new List<SensorLinkEntry>();
        private readonly List<SensorLinkEntry> confirmedLinks = new List<SensorLinkEntry>();
        private int linkStableCount;

        private List<ValidConnection> lastPublished = new List<ValidConnection>();

        private int consecutiveAllVacantFrames;
        private bool startupWarmupComplete;

        private string[] lastFrameInfoLabels;
        private string[] lastFrameRhythmLabels;

        private void OnEnable()
        {
            if (sensorMapping == null)
            {
                Debug.LogError("[ConnectionResolver] Missing SensorMapping reference.", this);
                enabled = false;
                return;
            }

            GameEvents.OnSensorFrameReceived += HandleFrame;
            GameEvents.OnSerialDisconnected += HandleSerialDisconnected;
        }

        private void OnDisable()
        {
            GameEvents.OnSensorFrameReceived -= HandleFrame;
            GameEvents.OnSerialDisconnected -= HandleSerialDisconnected;
        }

        private void HandleSerialDisconnected(string _)
        {
            ResetStartupWarmup();
            ResetStableState();
        }

        private void HandleFrame(SensorFrame frame)
        {
            if (frame.InfoSlots == null || frame.RhythmSlots == null) return;
            if (frame.InfoSlots.Length != frame.RhythmSlots.Length) return;
            if (frame.InfoSlots.Length == 0) return;

            var slots = frame.InfoSlots.Length;
            EnsureBuffers(slots);

            lastFrameInfoLabels = frame.InfoSlots;
            lastFrameRhythmLabels = frame.RhythmSlots;

            var allVacant = ConnectionResolverDebouncing.AreAllSlotsVacant(frame.InfoSlots, frame.RhythmSlots);

            if (allVacant)
            {
                ConnectionResolverDebouncing.TryAdvanceStartupWarmup(
                    true,
                    ref consecutiveAllVacantFrames,
                    ref startupWarmupComplete,
                    sensorMapping.StartupVacantFramesRequired);

                ResetStableState();
                return;
            }

            UpdateStableFromFrame(frame, slots);

            ConnectionResolverDebouncing.UpdateStableLinks(
                frame.Links,
                lastCandidateLinks,
                confirmedLinks,
                ref linkStableCount,
                sensorMapping.StableFramesForConnection,
                sensorMapping.StableFramesForConnectionRelease);

            if (!startupWarmupComplete)
            {
                ConnectionResolverDebouncing.TryAdvanceStartupWarmup(
                    false,
                    ref consecutiveAllVacantFrames,
                    ref startupWarmupComplete,
                    sensorMapping.StartupVacantFramesRequired);

                if (!startupWarmupComplete &&
                    HasAcquiredNonVacantBlock(slots))
                {
                    startupWarmupComplete = true;
                    if (debugLog)
                    {
                        Debug.Log(
                            "[ConnectionResolver] Startup warmup completed via sustained block signal.",
                            this);
                    }
                }

                if (!startupWarmupComplete)
                {
                    ForcePublishEmptyConnections();
                    return;
                }
            }

            PublishValidConnections(slots);
        }

        private void UpdateStableFromFrame(SensorFrame frame, int slots)
        {
            for (var i = 0; i < slots; i++)
            {
                ConnectionResolverDebouncing.UpdateStableLabel(
                    frame.InfoSlots[i],
                    ref lastCandidateInfoLabels[i],
                    ref infoStableCounts[i],
                    ref stableInfoLabels[i],
                    sensorMapping.StableFramesForBlockId,
                    sensorMapping.StableFramesForBlockRelease);

                ConnectionResolverDebouncing.UpdateStableLabel(
                    frame.RhythmSlots[i],
                    ref lastCandidateRhythmLabels[i],
                    ref rhythmStableCounts[i],
                    ref stableRhythmLabels[i],
                    sensorMapping.StableFramesForBlockId,
                    sensorMapping.StableFramesForBlockRelease);

                stableInfo[i] = IdentityMapping.ParseInfo(stableInfoLabels[i]);
                stableRhythm[i] = IdentityMapping.ParseRhythm(stableRhythmLabels[i]);
            }
        }

        private bool HasAcquiredNonVacantBlock(int slots)
        {
            if (stableInfoLabels == null || stableRhythmLabels == null) return false;
            if (stableInfoLabels.Length < slots || stableRhythmLabels.Length < slots) return false;

            return ConnectionResolverDebouncing.HasAcquiredNonVacantLabel(stableInfoLabels) ||
                   ConnectionResolverDebouncing.HasAcquiredNonVacantLabel(stableRhythmLabels);
        }

        private void PublishValidConnections(int slots)
        {
            var next = new Dictionary<int, ValidConnection>();

            for (var i = 0; i < slots; i++)
            {
                if (stableInfo[i] == BlockType.None) continue;
                next[i] = new ValidConnection(i, -1, stableInfo[i], RhythmType.None);
            }

            if (confirmedLinks.Count > 0)
            {
                for (var i = 0; i < confirmedLinks.Count; i++)
                {
                    var link = confirmedLinks[i];
                    if (link.RhythmSlot < 0 || link.RhythmSlot >= slots) continue;

                    var leftIndex = FindInfoSlotForLabel(link.InfoId, slots);
                    if (leftIndex < 0) continue;
                    if (stableInfo[leftIndex] == BlockType.None) continue;

                    var sound = IdentityMapping.ParseInfo(link.InfoId);
                    if (sound == BlockType.None) continue;
                    if (stableInfo[leftIndex] != sound) continue;

                    var rhythm = stableRhythm[link.RhythmSlot];
                    if (rhythm == RhythmType.None) continue;

                    next[leftIndex] = new ValidConnection(leftIndex, link.RhythmSlot, sound, rhythm);
                }
            }

            var list = new List<ValidConnection>(next.Count);
            foreach (var kvp in next)
                list.Add(kvp.Value);

            PublishIfChanged(list);
        }

        private int FindInfoSlotForLabel(string infoLabel, int slots)
        {
            for (var i = 0; i < slots; i++)
            {
                if (string.Equals(stableInfoLabels[i], infoLabel, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private void PublishIfChanged(List<ValidConnection> next)
        {
            if (AreSame(next, lastPublished)) return;
            lastPublished = next;

            if (debugLog)
            {
                var message =
                    $"[ConnectionResolver] published connections={next.Count} stableInfo=[{FormatStableLabels()}]";
                if (next.Count > 0)
                {
                    message +=
                        $" rawInfo=[{FormatFrameLabels(lastFrameInfoLabels)}] rawRhythm=[{FormatFrameLabels(lastFrameRhythmLabels)}]";
                }

                Debug.Log(message, this);
            }

            GameEvents.RaiseValidConnections(next);
        }

        private string FormatStableLabels()
        {
            if (stableInfoLabels == null) return string.Empty;
            return string.Join(",", stableInfoLabels);
        }

        private static string FormatFrameLabels(string[] labels)
        {
            if (labels == null) return string.Empty;
            return string.Join(",", labels);
        }

        private void ForcePublishEmptyConnections()
        {
            PublishIfChanged(new List<ValidConnection>());
        }

        private void ResetStableState()
        {
            if (stableInfoLabels != null)
            {
                for (var i = 0; i < stableInfoLabels.Length; i++)
                {
                    stableInfoLabels[i] = IdentityMapping.Empty;
                    stableRhythmLabels[i] = IdentityMapping.Empty;
                    if (stableInfo != null) stableInfo[i] = BlockType.None;
                    if (stableRhythm != null) stableRhythm[i] = RhythmType.None;
                    if (lastCandidateInfoLabels != null) lastCandidateInfoLabels[i] = IdentityMapping.Empty;
                    if (lastCandidateRhythmLabels != null) lastCandidateRhythmLabels[i] = IdentityMapping.Empty;
                    if (infoStableCounts != null) infoStableCounts[i] = 0;
                    if (rhythmStableCounts != null) rhythmStableCounts[i] = 0;
                }
            }

            confirmedLinks.Clear();
            lastCandidateLinks.Clear();
            linkStableCount = 0;
            ForcePublishEmptyConnections();
        }

        private void ResetStartupWarmup()
        {
            consecutiveAllVacantFrames = 0;
            startupWarmupComplete = false;
        }

        private static bool AreSame(List<ValidConnection> a, List<ValidConnection> b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;

            var set = new HashSet<ValidConnection>(a);
            for (var i = 0; i < b.Count; i++)
            {
                if (!set.Contains(b[i])) return false;
            }

            return true;
        }

        private void EnsureBuffers(int slots)
        {
            if (stableInfo != null && stableInfo.Length == slots) return;

            stableInfoLabels = new string[slots];
            stableRhythmLabels = new string[slots];
            stableInfo = new BlockType[slots];
            stableRhythm = new RhythmType[slots];

            lastCandidateInfoLabels = new string[slots];
            lastCandidateRhythmLabels = new string[slots];
            infoStableCounts = new int[slots];
            rhythmStableCounts = new int[slots];

            for (var i = 0; i < slots; i++)
            {
                stableInfoLabels[i] = IdentityMapping.Empty;
                stableRhythmLabels[i] = IdentityMapping.Empty;
            }

            confirmedLinks.Clear();
            lastCandidateLinks.Clear();
            linkStableCount = 0;
            ResetStartupWarmup();
        }
    }
}
