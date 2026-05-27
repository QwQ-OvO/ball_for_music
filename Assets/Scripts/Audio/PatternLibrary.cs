using System;
using UnityEngine;
using InformationString.Core;

namespace InformationString.Audio
{
    [CreateAssetMenu(menuName = "InformationString/Config/PatternLibrary")]
    public sealed class PatternLibrary : ScriptableObject
    {
        [SerializeField] private RhythmPattern[] patterns;

        public bool TryGetPattern(RhythmType type, out bool[] steps)
        {
            if (patterns != null)
            {
                for (var i = 0; i < patterns.Length; i++)
                {
                    if (patterns[i].Type != type) continue;
                    steps = patterns[i].Steps;
                    return steps != null && steps.Length > 0;
                }
            }

            steps = null;
            return false;
        }

        [Serializable]
        public struct RhythmPattern
        {
            public RhythmType Type;
            public bool[] Steps;
        }
    }
}
