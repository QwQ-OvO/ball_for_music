using System;

namespace InformationString.Input
{
    [Serializable]
    public readonly struct SensorLinkEntry : IEquatable<SensorLinkEntry>
    {
        public string InfoId { get; }
        public int RhythmSlot { get; }

        public SensorLinkEntry(string infoId, int rhythmSlot)
        {
            InfoId = infoId ?? string.Empty;
            RhythmSlot = rhythmSlot;
        }

        public bool Equals(SensorLinkEntry other) =>
            RhythmSlot == other.RhythmSlot &&
            string.Equals(InfoId, other.InfoId, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is SensorLinkEntry other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(InfoId, RhythmSlot);
    }
}
