using System;
using InformationString.Core.Config;

namespace InformationString.Input
{
    public interface ISensorInput
    {
        bool IsConnected { get; }
        event Action<SensorFrame> OnFrameReceived;

        void Initialize(SystemConfig config);
        void StartReading();
        void StopReading();
    }
}
