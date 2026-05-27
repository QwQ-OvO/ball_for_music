using System;
using UnityEngine;

namespace InformationString.Core.Config
{
    [CreateAssetMenu(menuName = "InformationString/Config/EntanglementRhythmLibrary")]
    public sealed class EntanglementRhythmLibrary : ScriptableObject
    {
        [SerializeField] private SongRhythmEntry[] entries;

        public bool TryGetConfig(int groupSize, out SongRhythmConfig config)
        {
            if (entries != null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    if (entries[i].GroupSize != groupSize) continue;
                    config = entries[i].Config;
                    return config.MemberAssignment != null && config.MemberAssignment.Length > 0;
                }
            }

            config = default;
            return false;
        }
    }

    [Serializable]
    public struct SongRhythmConfig
    {
        public string SongName;
        public int BeatsPerBar;
        public int SubdivisionsPerBeat;
        public float TargetBPM;
        public int[] MemberAssignment;
    }

    [Serializable]
    public struct SongRhythmEntry
    {
        public int GroupSize;
        public SongRhythmConfig Config;
    }
}

