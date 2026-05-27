using System.Collections.Generic;
using InformationString.Audio;
using InformationString.Core;
using InformationString.Core.Config;
using InformationString.Input;
using UnityEngine;

namespace InformationString.Entanglement
{
    public sealed class EntanglementEngine : MonoBehaviour
    {
        [SerializeField] private EntanglementConfig config;
        [SerializeField] private RhythmGrid rhythmGrid;

        private readonly object gate = new object();
        private List<ValidConnection> pendingConnections;
        private bool hasPending;

        private EntanglementState currentState;

        private void OnEnable()
        {
            if (config == null)
            {
                Debug.LogError("[EntanglementEngine] Missing EntanglementConfig reference.", this);
                enabled = false;
                return;
            }

            if (rhythmGrid == null)
            {
                Debug.LogError("[EntanglementEngine] Missing RhythmGrid reference.", this);
                enabled = false;
                return;
            }

            GameEvents.OnValidConnectionsUpdated += HandleValidConnectionsUpdated;
            rhythmGrid.OnBeat += HandleBeatTick;
            rhythmGrid.OnBar += HandleBarTick;
        }

        private void OnDisable()
        {
            GameEvents.OnValidConnectionsUpdated -= HandleValidConnectionsUpdated;
            if (rhythmGrid == null) return;
            rhythmGrid.OnBeat -= HandleBeatTick;
            rhythmGrid.OnBar -= HandleBarTick;
        }

        private void HandleValidConnectionsUpdated(List<ValidConnection> connections)
        {
            lock (gate)
            {
                pendingConnections = connections == null ? null : new List<ValidConnection>(connections);
                hasPending = true;
            }

            // 连接变化后立即重算，避免仅依赖 OnBeat 导致 Mock 测试长时间无声。
            TryCalculateOnQuantizedTick();
        }

        private void HandleBeatTick()
        {
            var threshold = config.BeatQuantizeLevelThreshold;
            var currentLevel = currentState?.Level ?? 0;
            if (currentLevel >= threshold) return; // 高纠缠：仅在小节头重算
            TryCalculateOnQuantizedTick();
        }

        private void HandleBarTick()
        {
            var threshold = config.BeatQuantizeLevelThreshold;
            var currentLevel = currentState?.Level ?? 0;
            if (currentLevel < threshold) return; // 低纠缠：拍级重算即可，小节头不重复算
            TryCalculateOnQuantizedTick();
        }

        private void TryCalculateOnQuantizedTick()
        {
            List<ValidConnection> snapshot;
            lock (gate)
            {
                if (!hasPending) return;
                hasPending = false;
                snapshot = pendingConnections;
                pendingConnections = null;
            }

            if (snapshot == null) snapshot = new List<ValidConnection>();

            var (groups, level) = LineCrossingEntanglementGrouper.BuildGroups(snapshot, config);
            currentState = new EntanglementState(snapshot, groups, level);
            GameEvents.RaiseEntanglement(currentState);
        }
    }
}
