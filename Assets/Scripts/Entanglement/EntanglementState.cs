using System.Collections.Generic;
using InformationString.Input;

namespace InformationString.Entanglement
{
    public sealed class EntanglementState
    {
        // ActiveConnections 顺序是计算时的快照；Groups.MemberIndices 指向该列表的索引（0..Count-1）。
        public List<ValidConnection> ActiveConnections { get; }
        public List<EntangledGroup> Groups { get; }
        public int Level { get; }

        public EntanglementState(
            List<ValidConnection> activeConnections,
            List<EntangledGroup> groups,
            int level)
        {
            ActiveConnections = activeConnections;
            Groups = groups;
            Level = level;
        }
    }

    public sealed class EntangledGroup
    {
        public List<int> MemberIndices { get; }

        public EntangledGroup(List<int> memberIndices)
        {
            MemberIndices = memberIndices;
        }
    }
}
