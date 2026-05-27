#if UNITY_EDITOR
using System.Collections.Generic;
using InformationString.Core;
using InformationString.Input;
using UnityEditor;
using UnityEngine;

namespace InformationString.Entanglement.Editor
{
    public static class LineCrossingEntanglementGrouperTests
    {
        [MenuItem("InformationString/Run Line Crossing Entanglement Tests")]
        public static void RunAll()
        {
            TestIsCrossUserExample();
            TestParallelNoCross();
            TestTransitiveChain();
            TestSustainedExcluded();
            Debug.Log("[LineCrossingEntanglementGrouperTests] All checks passed.");
        }

        private static void TestIsCrossUserExample()
        {
            Assert(LineCrossingEntanglementGrouper.IsCross(0, 4, 2, 1), "L1→R5 vs L3→R2 crosses");
            Assert(!LineCrossingEntanglementGrouper.IsCross(0, 1, 2, 4), "L1→R2 vs L3→R5 no cross");

            var connections = new List<ValidConnection>
            {
                Gated(0, 4),
                Gated(2, 1),
            };

            var (groups, level) = LineCrossingEntanglementGrouper.BuildGroups(connections, null);
            Assert(groups.Count == 1, "crossing pair one group");
            Assert(groups[0].MemberIndices.Count == 2, "group size 2");
            Assert(level == 1, "level 1");
        }

        private static void TestParallelNoCross()
        {
            var connections = new List<ValidConnection>
            {
                Gated(0, 1),
                Gated(2, 4),
            };

            var (groups, level) = LineCrossingEntanglementGrouper.BuildGroups(connections, null);
            Assert(groups.Count == 0, "parallel lines no entangled groups");
            Assert(level == 0, "level 0");
        }

        private static void TestTransitiveChain()
        {
            // A=(0,4) x B=(2,1); B=(2,1) x C=(1,5); A x C false → still one component via B
            var connections = new List<ValidConnection>
            {
                Gated(0, 4),
                Gated(2, 1),
                Gated(1, 5),
            };

            Assert(LineCrossingEntanglementGrouper.IsCross(0, 4, 2, 1), "A x B");
            Assert(LineCrossingEntanglementGrouper.IsCross(2, 1, 1, 5), "B x C");
            Assert(!LineCrossingEntanglementGrouper.IsCross(0, 4, 1, 5), "A x C false");

            var (groups, level) = LineCrossingEntanglementGrouper.BuildGroups(connections, null);
            Assert(groups.Count == 1, "transitive one group");
            Assert(groups[0].MemberIndices.Count == 3, "group size 3");
            Assert(level == 3, "level C(3,2)");
        }

        private static void TestSustainedExcluded()
        {
            var connections = new List<ValidConnection>
            {
                new ValidConnection(0, -1, BlockType.Rain, RhythmType.None),
                Gated(0, 4),
                Gated(2, 1),
            };

            var (groups, level) = LineCrossingEntanglementGrouper.BuildGroups(connections, null);
            Assert(groups.Count == 1, "sustained ignored");
            Assert(level == 1, "level from gated only");
        }

        private static ValidConnection Gated(int left, int right) =>
            new ValidConnection(left, right, BlockType.Rain, RhythmType.Pulse);

        private static void Assert(bool condition, string label)
        {
            if (!condition)
                throw new System.InvalidOperationException($"Assertion failed: {label}");
        }
    }
}
#endif
