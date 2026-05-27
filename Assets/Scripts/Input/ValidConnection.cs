using System;
using InformationString.Core;

namespace InformationString.Input
{
    [Serializable]
    public readonly struct ValidConnection : IEquatable<ValidConnection>
    {
        public int LeftIndex { get; }
        public int RightIndex { get; }
        public BlockType SoundType { get; }
        public RhythmType Rhythm { get; }

        public ValidConnection(int leftIndex, int rightIndex, BlockType soundType, RhythmType rhythm)
        {
            LeftIndex = leftIndex;
            RightIndex = rightIndex;
            SoundType = soundType;
            Rhythm = rhythm;
        }

        /// <summary>
        /// True when a stable link exists and the rhythm slot has a block (pattern gating applies).
        /// RightIndex == -1 means info-only sustained playback.
        /// </summary>
        public bool IsRhythmGated => RightIndex >= 0 && Rhythm != RhythmType.None;

        public bool Equals(ValidConnection other) =>
            LeftIndex == other.LeftIndex &&
            RightIndex == other.RightIndex &&
            SoundType == other.SoundType &&
            Rhythm == other.Rhythm;

        public override bool Equals(object obj) => obj is ValidConnection other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(LeftIndex, RightIndex, (int)SoundType, (int)Rhythm);

        public override string ToString() => $"{LeftIndex}->{RightIndex} ({SoundType}/{Rhythm})";
    }
}
