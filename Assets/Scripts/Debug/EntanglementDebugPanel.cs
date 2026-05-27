using System;
using System.Collections.Generic;
using InformationString.Audio;
using InformationString.Core;
using InformationString.Entanglement;
using InformationString.Input;
using UnityEngine;

namespace InformationString.DebugUtils
{
    /// <summary>
    /// 无硬件时手动注入 ValidConnections，并用 RhythmGrid 手动推进量化点。
    /// Sustained 测试：RightIndex=-1, Rhythm=None；Rhythm-gated：RightIndex>=0 且 Rhythm!=None。
    /// </summary>
    public sealed class EntanglementDebugPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EntanglementEngine engine;
        [SerializeField] private RhythmGrid rhythmGrid;

        [Header("Mock Connections")]
        [SerializeField] private List<MockConnection> connections = new List<MockConnection>();

        [ContextMenu("Raise Connections")]
        public void RaiseConnections()
        {
            if (engine == null)
            {
                Debug.LogWarning("[Debug] Missing EntanglementEngine reference.");
                return;
            }

            var list = new List<ValidConnection>(connections.Count);
            for (var i = 0; i < connections.Count; i++)
            {
                var c = connections[i];
                list.Add(new ValidConnection(c.LeftIndex, c.RightIndex, c.SoundType, c.Rhythm));
            }

            GameEvents.RaiseValidConnections(list);
            var (groups, level) = LineCrossingEntanglementGrouper.BuildGroups(list, null);
            Debug.Log($"[Debug] Raised {list.Count} connections. Entangled groups={groups.Count}, level={level}.");
        }

        [ContextMenu("Tick Beat")]
        public void TickBeat()
        {
            if (rhythmGrid == null)
            {
                Debug.LogWarning("[Debug] Missing RhythmGrid reference.");
                return;
            }

            rhythmGrid.DebugTickBeat();
        }

        [Serializable]
        private struct MockConnection
        {
            [Range(0, 8)] public int LeftIndex;
            [Tooltip("-1 = Sustained (info-only). 0-8 = rhythm slot for gated mode.")]
            [Range(-1, 8)] public int RightIndex;
            public BlockType SoundType;
            public RhythmType Rhythm;
        }
    }
}
