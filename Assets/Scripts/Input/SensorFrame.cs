using System;

namespace InformationString.Input
{
    [Serializable]
    public readonly struct SensorFrame
    {
        public string[] InfoSlots { get; }
        public string[] RhythmSlots { get; }
        public SensorLinkEntry[] Links { get; }
        public double Timestamp { get; }

        public SensorFrame(
            string[] infoSlots,
            string[] rhythmSlots,
            SensorLinkEntry[] links,
            double timestamp)
        {
            InfoSlots = infoSlots;
            RhythmSlots = rhythmSlots;
            Links = links ?? Array.Empty<SensorLinkEntry>();
            Timestamp = timestamp;
        }

        public int SlotCount => InfoSlots?.Length ?? 0;
    }
}
