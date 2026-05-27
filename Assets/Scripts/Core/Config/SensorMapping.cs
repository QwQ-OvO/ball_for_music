using System;
using UnityEngine;
using InformationString.Core;

namespace InformationString.Core.Config
{
    [CreateAssetMenu(menuName = "InformationString/Config/SensorMapping")]
    public sealed class SensorMapping : ScriptableObject
    {
        [Header("Common")]
        [Tooltip("ADC 高于该值视为未连接/无方块。")]
        [Min(0)]
        [SerializeField] private int adcDisconnectedThreshold = 3700;

        [Tooltip("启动后需连续收到该帧数的全空帧，才允许确认方块/连接（抑制空板 ADC 噪声误触发）。")]
        [Min(0)]
        [SerializeField] private int startupVacantFramesRequired = 25;

        [Tooltip("同一槽位需连续稳定该帧数才确认类型（窗口滤波）。")]
        [Min(1)]
        [SerializeField] private int stableFramesForBlockId = 6;

        [Tooltip("槽位变为 empty/unknown 时，连续该帧数即释放稳定标签（应 ≤ 接入帧数）。")]
        [Min(1)]
        [SerializeField] private int stableFramesForBlockRelease = 1;

        [Tooltip("配对关系需连续稳定该帧数才确认为已连接（去抖）。")]
        [Min(1)]
        [SerializeField] private int stableFramesForConnection = 5;

        [Tooltip("links 清空时，连续该帧数即释放已确认连接（应 ≤ 接入帧数）。")]
        [Min(1)]
        [SerializeField] private int stableFramesForConnectionRelease = 1;

        [Header("Pairing (Legacy Delta — Mock 旧模式可选)")]
        [Tooltip("以 ADC 变化量判断“连线变化”的阈值（仅无 Link 数据时的回退逻辑）。")]
        [Min(0)]
        [SerializeField] private int pairingDeltaThreshold = 80;

        [Header("Info Side (i[9])")]
        [SerializeField] private BlockRange[] infoRanges;

        [Header("Rhythm Side (r[9])")]
        [SerializeField] private RhythmRange[] rhythmRanges;

        public int AdcDisconnectedThreshold => adcDisconnectedThreshold;
        public int StartupVacantFramesRequired => startupVacantFramesRequired;
        public int StableFramesForBlockId => stableFramesForBlockId;
        public int StableFramesForBlockRelease => stableFramesForBlockRelease;
        public int StableFramesForConnection => stableFramesForConnection;
        public int StableFramesForConnectionRelease => stableFramesForConnectionRelease;
        public int PairingDeltaThreshold => pairingDeltaThreshold;

        public BlockType MapInfo(int adc)
        {
            if (adc >= adcDisconnectedThreshold) return BlockType.None;
            if (infoRanges == null) return BlockType.None;
            for (var i = 0; i < infoRanges.Length; i++)
            {
                if (infoRanges[i].Min <= adc && adc <= infoRanges[i].Max) return infoRanges[i].Type;
            }
            return BlockType.None;
        }

        public RhythmType MapRhythm(int adc)
        {
            if (adc >= adcDisconnectedThreshold) return RhythmType.None;
            if (rhythmRanges == null) return RhythmType.None;
            for (var i = 0; i < rhythmRanges.Length; i++)
            {
                if (rhythmRanges[i].Min <= adc && adc <= rhythmRanges[i].Max) return rhythmRanges[i].Type;
            }
            return RhythmType.None;
        }

        /// <summary>
        /// Link 通道读到的阻值与节奏端方块使用同一套区间（线缆传来的是对侧 Rhythm 电阻编码）。
        /// </summary>
        public RhythmType MapRhythmFromLink(int linkAdc) => MapRhythm(linkAdc);

        public bool TryGetRepresentativeAdc(BlockType type, out int adc)
        {
            if (infoRanges != null)
            {
                for (var i = 0; i < infoRanges.Length; i++)
                {
                    if (infoRanges[i].Type != type) continue;
                    adc = infoRanges[i].Representative;
                    return true;
                }
            }
            adc = 0;
            return false;
        }

        public bool TryGetRepresentativeAdc(RhythmType type, out int adc)
        {
            if (rhythmRanges != null)
            {
                for (var i = 0; i < rhythmRanges.Length; i++)
                {
                    if (rhythmRanges[i].Type != type) continue;
                    adc = rhythmRanges[i].Representative;
                    return true;
                }
            }
            adc = 0;
            return false;
        }

        public bool TryGetInfoAdcBounds(BlockType type, out int min, out int max)
        {
            if (infoRanges != null)
            {
                for (var i = 0; i < infoRanges.Length; i++)
                {
                    if (infoRanges[i].Type != type) continue;
                    min = infoRanges[i].Min;
                    max = infoRanges[i].Max;
                    return true;
                }
            }
            min = 0;
            max = 0;
            return false;
        }

        public bool TryGetRhythmAdcBounds(RhythmType type, out int min, out int max)
        {
            if (rhythmRanges != null)
            {
                for (var i = 0; i < rhythmRanges.Length; i++)
                {
                    if (rhythmRanges[i].Type != type) continue;
                    min = rhythmRanges[i].Min;
                    max = rhythmRanges[i].Max;
                    return true;
                }
            }
            min = 0;
            max = 0;
            return false;
        }

        /// <summary>
        /// 按实机 1k / 2.2k / 4.7k / 10k 四档阻值填入区间（基于 DualMuxSensorInput 实测）。
        /// 前 4 种类型可用；其余 5 种需在有对应方块后再标定。
        /// </summary>
        public void ApplyHardwareResistorCalibration()
        {
            adcDisconnectedThreshold = 3700;
            startupVacantFramesRequired = 25;
            stableFramesForBlockId = 6;
            stableFramesForBlockRelease = 1;
            stableFramesForConnection = 5;
            stableFramesForConnectionRelease = 1;
            pairingDeltaThreshold = 80;

            infoRanges = new[]
            {
                new BlockRange { Type = BlockType.Rain, Min = 180, Max = 380, Representative = 270 },
                new BlockRange { Type = BlockType.News, Min = 480, Max = 680, Representative = 570 },
                new BlockRange { Type = BlockType.Heartbeat, Min = 980, Max = 1280, Representative = 1130 },
                new BlockRange { Type = BlockType.Traffic, Min = 1680, Max = 2000, Representative = 1865 },
                UncalibratedBlockRange(BlockType.Typing),
                UncalibratedBlockRange(BlockType.Birds),
                UncalibratedBlockRange(BlockType.Static),
                UncalibratedBlockRange(BlockType.Wind),
                UncalibratedBlockRange(BlockType.Crowd),
            };

            rhythmRanges = new[]
            {
                new RhythmRange { Type = RhythmType.Pulse, Min = 150, Max = 320, Representative = 210 },
                new RhythmRange { Type = RhythmType.Swing, Min = 520, Max = 660, Representative = 590 },
                new RhythmRange { Type = RhythmType.March, Min = 1050, Max = 1210, Representative = 1135 },
                new RhythmRange { Type = RhythmType.Waltz, Min = 1780, Max = 1920, Representative = 1865 },
                UncalibratedRhythmRange(RhythmType.Shuffle),
                UncalibratedRhythmRange(RhythmType.Syncopation),
                UncalibratedRhythmRange(RhythmType.Triplet),
                UncalibratedRhythmRange(RhythmType.Polyrhythm),
                UncalibratedRhythmRange(RhythmType.Freeform),
            };
        }

#if UNITY_EDITOR
        [UnityEngine.ContextMenu("Apply Hardware Resistor Calibration")]
        private void ApplyHardwareResistorCalibrationContextMenu()
        {
            ApplyHardwareResistorCalibration();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        private static BlockRange UncalibratedBlockRange(BlockType type) =>
            new BlockRange { Type = type, Min = 9999, Max = 9998, Representative = 9999 };

        private static RhythmRange UncalibratedRhythmRange(RhythmType type) =>
            new RhythmRange { Type = type, Min = 9999, Max = 9998, Representative = 9999 };

        [Serializable]
        public struct BlockRange
        {
            public BlockType Type;
            public int Min;
            public int Max;
            [Tooltip("Mock 输出用代表值（应落在 Min/Max 内）。")]
            public int Representative;
        }

        [Serializable]
        public struct RhythmRange
        {
            public RhythmType Type;
            public int Min;
            public int Max;
            [Tooltip("Mock 输出用代表值（应落在 Min/Max 内）。")]
            public int Representative;
        }
    }
}
