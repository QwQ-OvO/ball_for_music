using System.Collections.Generic;
using InformationString.Core;
using InformationString.Core.Config;
using InformationString.Entanglement;
using InformationString.Input;
using UnityEngine;

namespace InformationString.Audio
{
    public sealed class MusicSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RhythmGrid rhythmGrid;
        [SerializeField] private AudioSourcePool pool;
        [SerializeField] private AudioEffectController effects;
        [SerializeField] private AudioConfig audioConfig;
        [SerializeField] private PatternLibrary patternLibrary;
        [SerializeField] private EntanglementRhythmLibrary entanglementRhythmLibrary;

        [Header("Scheduling")]
        [Min(0.1f)]
        [SerializeField] private float lookaheadBeats = 2f;

        [Min(0f)]
        [SerializeField] private float startLeadSeconds = 0.05f;

        private readonly RhythmMixer mixer = new RhythmMixer();
        private readonly Dictionary<int, AudioSource> sourcesByLeftIndex = new Dictionary<int, AudioSource>();
        private readonly Dictionary<int, bool> gatedByLeftIndex = new Dictionary<int, bool>();

        private EntanglementState lastEntanglement;
        private double nextScheduledTime;
        private int nextScheduledStep;
        private bool schedulerInitialized;

        private void OnEnable()
        {
            GameEvents.OnEntanglementCalculated += HandleEntanglement;
        }

        private void OnDisable()
        {
            GameEvents.OnEntanglementCalculated -= HandleEntanglement;
            StopAllSources();
        }

        private void Start()
        {
            if (rhythmGrid != null && audioConfig != null)
            {
                rhythmGrid.SetBPM(audioConfig.DefaultBpm, 0);
            }
        }

        private void Update()
        {
            if (rhythmGrid == null || pool == null || effects == null || audioConfig == null || patternLibrary == null) return;
            if (lastEntanglement == null) return;

            if (!schedulerInitialized)
            {
                nextScheduledTime = rhythmGrid.GetNextQuantizedTime(QuantizeResolution.SubBeat);
                nextScheduledStep = rhythmGrid.CurrentSubBeatInBar;
                schedulerInitialized = true;
            }

            var now = AudioSettings.dspTime;
            var lookaheadSeconds = lookaheadBeats * rhythmGrid.SecondsPerBeat;
            var horizon = now + lookaheadSeconds;
            var stepSeconds = rhythmGrid.SecondsPerBeat / rhythmGrid.SubdivisionsPerBeat;
            var stepsPerBar = rhythmGrid.BeatsPerBar * rhythmGrid.SubdivisionsPerBeat;

            while (nextScheduledTime < horizon)
            {
                ScheduleStep(nextScheduledTime, nextScheduledStep, stepsPerBar);
                nextScheduledTime += stepSeconds;
                nextScheduledStep = (nextScheduledStep + 1) % stepsPerBar;
            }
        }

        private void HandleEntanglement(EntanglementState state)
        {
            var connections = state?.ActiveConnections;
            if (connections == null || connections.Count == 0)
            {
                StopAllSources();
                lastEntanglement = null;
                schedulerInitialized = false;
                return;
            }

            lastEntanglement = state;
            SyncSourcesToConnections(connections);
            ApplyModeTransitions(connections);

            ApplyGlobalTempoFromEntanglement(state);

            if (rhythmGrid != null)
            {
                nextScheduledTime = rhythmGrid.GetNextQuantizedTime(QuantizeResolution.SubBeat);
                nextScheduledStep = rhythmGrid.CurrentSubBeatInBar;
                schedulerInitialized = true;
            }
        }

        private void SyncSourcesToConnections(List<ValidConnection> connections)
        {
            var connByLeft = new Dictionary<int, ValidConnection>();
            for (var i = 0; i < connections.Count; i++)
                connByLeft[connections[i].LeftIndex] = connections[i];

            var toRemove = new List<int>();
            foreach (var kvp in sourcesByLeftIndex)
            {
                if (!connByLeft.ContainsKey(kvp.Key)) toRemove.Add(kvp.Key);
            }

            for (var i = 0; i < toRemove.Count; i++)
            {
                var left = toRemove[i];
                if (sourcesByLeftIndex.TryGetValue(left, out var src))
                {
                    effects.Clear(src);
                    pool.Return(src);
                }

                sourcesByLeftIndex.Remove(left);
                gatedByLeftIndex.Remove(left);
            }

            foreach (var kvp in connByLeft)
            {
                var left = kvp.Key;
                var conn = kvp.Value;

                if (!audioConfig.TryGetClip(conn.SoundType, out var clip))
                {
                    Debug.LogWarning($"[MusicSystem] No AudioClip mapped for {conn.SoundType} in AudioConfig.", this);
                    continue;
                }

                if (sourcesByLeftIndex.TryGetValue(left, out var existing))
                {
                    if (existing.clip != clip)
                    {
                        existing.clip = clip;
                        existing.loop = true;
                        existing.PlayScheduled(AudioSettings.dspTime + startLeadSeconds);
                    }
                    continue;
                }

                var src = pool.Get();
                if (src == null) continue;

                src.clip = clip;
                src.loop = true;
                src.volume = 0f;
                src.PlayScheduled(AudioSettings.dspTime + startLeadSeconds);
                sourcesByLeftIndex[left] = src;
                gatedByLeftIndex[left] = conn.IsRhythmGated;

                if (conn.IsRhythmGated)
                    continue;

                effects.ScheduleVolume(
                    src,
                    AudioSettings.dspTime + startLeadSeconds,
                    audioConfig.ConnectionVolume,
                    audioConfig.FadeInSeconds);
            }
        }

        private void ApplyModeTransitions(List<ValidConnection> connections)
        {
            if (rhythmGrid == null || effects == null || audioConfig == null) return;

            var stepTime = rhythmGrid.GetNextQuantizedTime(QuantizeResolution.SubBeat);

            for (var i = 0; i < connections.Count; i++)
            {
                var conn = connections[i];
                var left = conn.LeftIndex;
                if (!sourcesByLeftIndex.TryGetValue(left, out var src) || src == null) continue;

                gatedByLeftIndex.TryGetValue(left, out var wasGated);
                var isGated = conn.IsRhythmGated;
                if (isGated == wasGated) continue;

                effects.Clear(src);
                if (isGated)
                    src.volume = 0f;
                else
                    effects.ScheduleVolume(src, stepTime, audioConfig.ConnectionVolume, audioConfig.FadeInSeconds);

                gatedByLeftIndex[left] = isGated;
            }
        }

        private void StopAllSources()
        {
            foreach (var kvp in sourcesByLeftIndex)
            {
                if (kvp.Value == null) continue;
                effects.Clear(kvp.Value);
                pool.Return(kvp.Value);
            }
            sourcesByLeftIndex.Clear();
            gatedByLeftIndex.Clear();
        }

        private void ScheduleStep(double stepTime, int stepIndexInBar, int stepsPerBar)
        {
            if (lastEntanglement == null) return;
            if (lastEntanglement.ActiveConnections == null) return;

            var patternsByConnectionIndex = BuildEffectivePatterns();

            for (var i = 0; i < lastEntanglement.ActiveConnections.Count; i++)
            {
                var conn = lastEntanglement.ActiveConnections[i];
                if (!sourcesByLeftIndex.TryGetValue(conn.LeftIndex, out var src) || src == null) continue;

                if (!conn.IsRhythmGated)
                    continue;

                if (!patternsByConnectionIndex.TryGetValue(i, out var pattern) || pattern == null || pattern.Length == 0) continue;

                var open = pattern[stepIndexInBar % pattern.Length];
                var target = open ? audioConfig.ConnectionVolume : 0f;
                var duration = open ? audioConfig.FadeInSeconds : audioConfig.FadeOutSeconds;
                effects.ScheduleVolume(src, stepTime, target, duration);
            }
        }

        private void ApplyGlobalTempoFromEntanglement(EntanglementState state)
        {
            if (rhythmGrid == null || audioConfig == null) return;

            var maxGroupSize = 0;
            if (state?.Groups != null)
            {
                for (var i = 0; i < state.Groups.Count; i++)
                {
                    var count = state.Groups[i]?.MemberIndices?.Count ?? 0;
                    if (count > maxGroupSize) maxGroupSize = count;
                }
            }

            if (maxGroupSize <= 1)
            {
                rhythmGrid.SetTimeSignature(4, 4);
                rhythmGrid.SetBPM(audioConfig.DefaultBpm, 4);
                return;
            }

            if (entanglementRhythmLibrary != null &&
                entanglementRhythmLibrary.TryGetConfig(maxGroupSize, out var config))
            {
                rhythmGrid.SetTimeSignature(config.BeatsPerBar, config.SubdivisionsPerBeat);
                rhythmGrid.SetBPM(config.TargetBPM, 4);
            }
        }

        private Dictionary<int, bool[]> BuildEffectivePatterns()
        {
            var result = new Dictionary<int, bool[]>();
            var connections = lastEntanglement.ActiveConnections;

            for (var i = 0; i < connections.Count; i++)
            {
                if (!connections[i].IsRhythmGated) continue;

                if (!patternLibrary.TryGetPattern(connections[i].Rhythm, out var baseSteps))
                {
                    Debug.LogWarning($"[MusicSystem] No PatternLibrary entry for {connections[i].Rhythm}.", this);
                    continue;
                }
                if (baseSteps == null || baseSteps.Length == 0)
                {
                    Debug.LogWarning($"[MusicSystem] Empty pattern steps for {connections[i].Rhythm}.", this);
                    continue;
                }
                result[i] = baseSteps;
            }

            if (lastEntanglement.Groups == null) return result;

            for (var g = 0; g < lastEntanglement.Groups.Count; g++)
            {
                var group = lastEntanglement.Groups[g];
                if (group?.MemberIndices == null || group.MemberIndices.Count <= 1) continue;

                var patterns = new List<bool[]>();
                var memberIndices = new List<int>();

                for (var m = 0; m < group.MemberIndices.Count; m++)
                {
                    var idx = group.MemberIndices[m];
                    if (!result.TryGetValue(idx, out var p) || p == null) continue;
                    patterns.Add(p);
                    memberIndices.Add(idx);
                }

                if (patterns.Count <= 1) continue;

                var groupSize = group.MemberIndices.Count;
                bool[][] interleaved;
                if (entanglementRhythmLibrary != null &&
                    groupSize >= 2 &&
                    entanglementRhythmLibrary.TryGetConfig(groupSize, out var config))
                {
                    interleaved = mixer.Interleave(patterns, config.MemberAssignment);
                }
                else
                {
                    interleaved = mixer.Interleave(patterns);
                }

                for (var m = 0; m < interleaved.Length; m++)
                {
                    result[memberIndices[m]] = interleaved[m];
                }
            }

            return result;
        }
    }
}
