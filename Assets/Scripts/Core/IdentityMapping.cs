using System;

namespace InformationString.Core
{
    /// <summary>
    /// ESP32 语义字符串 ↔ Unity 枚举。与固件 ID_RANGES / HARDWARE_MIGRATION_PLAN.md 同步。
    /// </summary>
    public static class IdentityMapping
    {
        public const string Empty = "empty";
        public const string Unknown = "unknown";

        public static BlockType ParseInfo(string label)
        {
            if (string.IsNullOrEmpty(label)) return BlockType.None;
            if (label == Empty || label == Unknown) return BlockType.None;

            if (label.Length >= 5 && label.StartsWith("Info", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(label.Substring(4), out var n))
            {
                return n switch
                {
                    1 => BlockType.Rain,
                    2 => BlockType.News,
                    3 => BlockType.Heartbeat,
                    4 => BlockType.Traffic,
                    5 => BlockType.Typing,
                    6 => BlockType.Birds,
                    7 => BlockType.Static,
                    8 => BlockType.Wind,
                    9 => BlockType.Crowd,
                    _ => BlockType.None,
                };
            }

            return BlockType.None;
        }

        public static RhythmType ParseRhythm(string label)
        {
            if (string.IsNullOrEmpty(label)) return RhythmType.None;
            if (label == Empty || label == Unknown) return RhythmType.None;

            if (label.Length >= 7 && label.StartsWith("Rhythm", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(label.Substring(6), out var n))
            {
                return n switch
                {
                    1 => RhythmType.Pulse,
                    2 => RhythmType.Swing,
                    3 => RhythmType.March,
                    4 => RhythmType.Waltz,
                    5 => RhythmType.Shuffle,
                    6 => RhythmType.Syncopation,
                    7 => RhythmType.Triplet,
                    8 => RhythmType.Polyrhythm,
                    9 => RhythmType.Freeform,
                    _ => RhythmType.None,
                };
            }

            return RhythmType.None;
        }

        public static string ToInfoLabel(BlockType type) =>
            type switch
            {
                BlockType.Rain => "Info1",
                BlockType.News => "Info2",
                BlockType.Heartbeat => "Info3",
                BlockType.Traffic => "Info4",
                BlockType.Typing => "Info5",
                BlockType.Birds => "Info6",
                BlockType.Static => "Info7",
                BlockType.Wind => "Info8",
                BlockType.Crowd => "Info9",
                _ => Empty,
            };

        public static string ToRhythmLabel(RhythmType type) =>
            type switch
            {
                RhythmType.Pulse => "Rhythm1",
                RhythmType.Swing => "Rhythm2",
                RhythmType.March => "Rhythm3",
                RhythmType.Waltz => "Rhythm4",
                RhythmType.Shuffle => "Rhythm5",
                RhythmType.Syncopation => "Rhythm6",
                RhythmType.Triplet => "Rhythm7",
                RhythmType.Polyrhythm => "Rhythm8",
                RhythmType.Freeform => "Rhythm9",
                _ => Empty,
            };
    }
}
