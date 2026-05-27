using UnityEngine;

namespace InformationString.Core.Config
{
    [CreateAssetMenu(menuName = "InformationString/Config/EntanglementConfig")]
    public sealed class EntanglementConfig : ScriptableObject
    {
        [Tooltip("当 Level 小于该阈值时使用拍级量化，否则使用小节级量化。")]
        [Min(0)]
        [SerializeField] private int beatQuantizeLevelThreshold = 5;

        [Tooltip("可选：槽位索引 → 左列视觉顺序（0=最上/左）。留空则 slot 索引即视觉顺序。")]
        [SerializeField] private int[] infoSlotVisualOrder;

        [Tooltip("可选：槽位索引 → 右列视觉顺序（0=最上/左）。留空则 slot 索引即视觉顺序。")]
        [SerializeField] private int[] rhythmSlotVisualOrder;

        public int BeatQuantizeLevelThreshold => beatQuantizeLevelThreshold;

        public int MapInfoToVisualOrder(int slotIndex)
        {
            if (infoSlotVisualOrder == null || slotIndex < 0 || slotIndex >= infoSlotVisualOrder.Length)
                return slotIndex;
            return infoSlotVisualOrder[slotIndex];
        }

        public int MapRhythmToVisualOrder(int slotIndex)
        {
            if (rhythmSlotVisualOrder == null || slotIndex < 0 || slotIndex >= rhythmSlotVisualOrder.Length)
                return slotIndex;
            return rhythmSlotVisualOrder[slotIndex];
        }
    }
}
