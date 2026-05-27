using System;
using System.Collections.Generic;

namespace InformationString.Audio
{
    public sealed class RhythmMixer
    {
        public bool[][] Interleave(List<bool[]> patterns, int[] memberAssignment)
        {
            if (patterns == null) throw new ArgumentNullException(nameof(patterns));
            if (patterns.Count == 0) return Array.Empty<bool[]>();

            for (var i = 0; i < patterns.Count; i++)
            {
                if (patterns[i] == null) throw new ArgumentException("patterns contains null", nameof(patterns));
                if (patterns[i].Length == 0)
                {
                    throw new ArgumentException("patterns contains empty pattern", nameof(patterns));
                }
            }

            if (memberAssignment == null || memberAssignment.Length == 0)
            {
                return Interleave(patterns);
            }

            var outputLength = memberAssignment.Length;
            var output = new bool[patterns.Count][];
            for (var i = 0; i < output.Length; i++) output[i] = new bool[outputLength];

            for (var step = 0; step < outputLength; step++)
            {
                var assigned = memberAssignment[step];
                if (assigned < 0 || assigned >= patterns.Count) continue;

                var pattern = patterns[assigned];
                output[assigned][step] = pattern[step % pattern.Length];
            }

            return output;
        }

        public bool[][] Interleave(List<bool[]> patterns)
        {
            if (patterns == null) throw new ArgumentNullException(nameof(patterns));
            if (patterns.Count == 0) return Array.Empty<bool[]>();

            var length = patterns[0]?.Length ?? 0;
            for (var i = 0; i < patterns.Count; i++)
            {
                if (patterns[i] == null) throw new ArgumentException("patterns contains null", nameof(patterns));
                if (patterns[i].Length != length)
                {
                    throw new ArgumentException("All patterns must have the same length.", nameof(patterns));
                }
            }

            var output = new bool[patterns.Count][];
            for (var i = 0; i < output.Length; i++) output[i] = new bool[length];

            for (var step = 0; step < length; step++)
            {
                var member = step % patterns.Count;
                output[member][step] = patterns[member][step];
            }

            return output;
        }
    }
}
