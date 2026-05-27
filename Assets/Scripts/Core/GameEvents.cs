using System;
using System.Collections.Generic;
using InformationString.Core.System;
using InformationString.Entanglement;
using InformationString.Input;

namespace InformationString.Core
{
    public static class GameEvents
    {
        public static event Action<SensorFrame> OnSensorFrameReceived;
        public static event Action<List<ValidConnection>> OnValidConnectionsUpdated;
        public static event Action<EntanglementState> OnEntanglementCalculated;
        public static event Action<SystemState> OnSystemStateChanged;
        public static event Action<string> OnSerialDisconnected;

        public static void RaiseSensorFrame(SensorFrame frame) =>
            OnSensorFrameReceived?.Invoke(frame);

        public static void RaiseValidConnections(List<ValidConnection> connections) =>
            OnValidConnectionsUpdated?.Invoke(connections);

        public static void RaiseEntanglement(EntanglementState state) =>
            OnEntanglementCalculated?.Invoke(state);

        public static void RaiseSystemState(SystemState state) =>
            OnSystemStateChanged?.Invoke(state);

        public static void RaiseSerialDisconnected(string message) =>
            OnSerialDisconnected?.Invoke(message);
    }
}
