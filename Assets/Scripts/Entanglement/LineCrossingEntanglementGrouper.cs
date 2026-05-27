using System.Collections.Generic;
using InformationString.Core.Config;
using InformationString.Input;

namespace InformationString.Entanglement
{
    /// <summary>
    /// 左右 9 点连线图：L_i → R_j。两线几何交叉当 (i-m)*(j-n) &lt; 0。
    /// 交叉对构成无向图，连通分量即纠缠组（软件推测，非物理碰线检测）。
    /// </summary>
    public static class LineCrossingEntanglementGrouper
    {
        public static bool IsCross(int leftA, int rightA, int leftB, int rightB) =>
            (leftA - leftB) * (rightA - rightB) < 0;

        public static (List<EntangledGroup> groups, int level) BuildGroups(
            IReadOnlyList<ValidConnection> connections,
            EntanglementConfig config)
        {
            var groups = new List<EntangledGroup>();
            if (connections == null || connections.Count == 0)
                return (groups, 0);

            var gatedGlobalIndices = new List<int>();
            var leftVisual = new List<int>();
            var rightVisual = new List<int>();

            for (var i = 0; i < connections.Count; i++)
            {
                if (!connections[i].IsRhythmGated) continue;

                gatedGlobalIndices.Add(i);
                leftVisual.Add(MapVisualOrder(config, connections[i].LeftIndex, isInfo: true));
                rightVisual.Add(MapVisualOrder(config, connections[i].RightIndex, isInfo: false));
            }

            var n = gatedGlobalIndices.Count;
            if (n == 0)
                return (groups, 0);

            var parent = new int[n];
            for (var i = 0; i < n; i++) parent[i] = i;

            for (var a = 0; a < n; a++)
            {
                for (var b = a + 1; b < n; b++)
                {
                    if (!IsCross(leftVisual[a], rightVisual[a], leftVisual[b], rightVisual[b]))
                        continue;

                    Union(parent, a, b);
                }
            }

            var buckets = new Dictionary<int, List<int>>();
            for (var i = 0; i < n; i++)
            {
                var root = Find(parent, i);
                if (!buckets.TryGetValue(root, out var members))
                {
                    members = new List<int>();
                    buckets[root] = members;
                }

                members.Add(gatedGlobalIndices[i]);
            }

            var level = 0;
            foreach (var members in buckets.Values)
            {
                if (members.Count <= 1) continue;

                groups.Add(new EntangledGroup(members));
                level += members.Count * (members.Count - 1) / 2;
            }

            return (groups, level);
        }

        private static int MapVisualOrder(EntanglementConfig config, int slotIndex, bool isInfo)
        {
            if (config == null) return slotIndex;
            return isInfo ? config.MapInfoToVisualOrder(slotIndex) : config.MapRhythmToVisualOrder(slotIndex);
        }

        private static int Find(int[] parent, int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }

            return x;
        }

        private static void Union(int[] parent, int a, int b)
        {
            var ra = Find(parent, a);
            var rb = Find(parent, b);
            if (ra != rb) parent[rb] = ra;
        }
    }
}
