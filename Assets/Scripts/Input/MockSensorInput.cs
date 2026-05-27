using System;
using System.Collections.Generic;
using InformationString.Core;
using InformationString.Core.Config;
using UnityEngine;

namespace InformationString.Input
{
    public sealed class MockSensorInput : MonoBehaviour, ISensorInput
    {
        public bool IsConnected { get; private set; }
        public event Action<SensorFrame> OnFrameReceived;

        [Header("Config")]
        [SerializeField] private SystemConfig systemConfig;

        [Header("Mock Blocks")]
        [SerializeField] private BlockType[] infoBlocks = new BlockType[4];
        [SerializeField] private RhythmType[] rhythmBlocks = new RhythmType[4];

        [Header("Mock Cables")]
        [SerializeField] private List<Cable> cables = new List<Cable>();

        [Header("Mock Output")]
        [Min(1f)]
        [SerializeField] private float mockHz = 50f;

        private float accumulator;
        private bool running;

        public void Initialize(SystemConfig config)
        {
            systemConfig = config;
        }

        public void StartReading()
        {
            running = true;
            IsConnected = true;
            accumulator = 0f;
        }

        public void StopReading()
        {
            running = false;
            IsConnected = false;
        }

        private void Update()
        {
            if (!running) return;
            if (systemConfig == null) return;

            accumulator += Time.unscaledDeltaTime;
            var interval = 1f / Mathf.Max(1f, mockHz);
            while (accumulator >= interval)
            {
                accumulator -= interval;
                EmitFrame();
            }
        }

        [ContextMenu("Randomize Cables")]
        private void RandomizeCables()
        {
            cables.Clear();
            var slots = Mathf.Max(1, systemConfig != null ? systemConfig.ExpectedSlotsPerSide : 4);
            var count = UnityEngine.Random.Range(1, Mathf.Min(3, slots + 1));
            for (var i = 0; i < count; i++)
            {
                cables.Add(new Cable
                {
                    LeftIndex = UnityEngine.Random.Range(0, slots),
                    RightIndex = UnityEngine.Random.Range(0, slots),
                });
            }
        }

        private void EmitFrame()
        {
            var slots = Mathf.Max(1, systemConfig.ExpectedSlotsPerSide);
            var info = new string[slots];
            var rhythm = new string[slots];

            for (var i = 0; i < slots; i++)
            {
                info[i] = BlockAt(infoBlocks, i);
                rhythm[i] = RhythmAt(rhythmBlocks, i);
            }

            var links = BuildLinks(slots, info);
            OnFrameReceived?.Invoke(new SensorFrame(info, rhythm, links, AudioSettings.dspTime));
        }

        private string BlockAt(BlockType[] blocks, int index)
        {
            if (blocks == null || index >= blocks.Length) return IdentityMapping.Empty;
            return IdentityMapping.ToInfoLabel(blocks[index]);
        }

        private string RhythmAt(RhythmType[] blocks, int index)
        {
            if (blocks == null || index >= blocks.Length) return IdentityMapping.Empty;
            return IdentityMapping.ToRhythmLabel(blocks[index]);
        }

        private SensorLinkEntry[] BuildLinks(int slots, string[] infoLabels)
        {
            var result = new List<SensorLinkEntry>();

            for (var c = 0; c < cables.Count; c++)
            {
                var left = Mathf.Clamp(cables[c].LeftIndex, 0, slots - 1);
                var right = Mathf.Clamp(cables[c].RightIndex, 0, slots - 1);

                if (infoBlocks != null && left < infoBlocks.Length && infoBlocks[left] == BlockType.None)
                    continue;
                if (rhythmBlocks != null && right < rhythmBlocks.Length && rhythmBlocks[right] == RhythmType.None)
                    continue;

                var infoId = infoLabels[left];
                if (infoId == IdentityMapping.Empty || infoId == IdentityMapping.Unknown)
                    continue;

                result.Add(new SensorLinkEntry(infoId, right));
            }

            return result.ToArray();
        }

        [Serializable]
        public struct Cable
        {
            public int LeftIndex;
            public int RightIndex;
        }
    }
}
