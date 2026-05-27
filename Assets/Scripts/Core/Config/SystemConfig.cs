using UnityEngine;

namespace InformationString.Core.Config
{
    [CreateAssetMenu(menuName = "InformationString/Config/SystemConfig")]
    public sealed class SystemConfig : ScriptableObject
    {
        [Header("Serial")]
        [SerializeField] private string serialPortName;
        [Min(1)]
        [SerializeField] private int serialBaudRate = 115200;

        [Header("Input")]
        [Min(1)]
        [SerializeField] private int expectedSlotsPerSide = 9;

        [Tooltip("当串口断连/异常时，自动切换到 Mock。")]
        [SerializeField] private bool autoFallbackToMock = true;

        public string SerialPortName => serialPortName;
        public int SerialBaudRate => serialBaudRate;
        public int ExpectedSlotsPerSide => expectedSlotsPerSide;
        public bool AutoFallbackToMock => autoFallbackToMock;
    }
}
