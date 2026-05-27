using System.Collections.Generic;
using InformationString.Core;
using InformationString.Core.Config;
using InformationString.Core.System;
using UnityEngine;

namespace InformationString.Input
{
    public sealed class SensorInputManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private SystemConfig systemConfig;

        [Header("Input Sources")]
        [SerializeField] private MockSensorInput mockInput;

        private SerialSensorInput serialInput;
        private ISensorInput active;

        private readonly object gate = new object();
        private readonly Queue<SensorFrame> pendingFrames = new Queue<SensorFrame>();
        private bool loggedFirstFrame;

        private void Awake()
        {
            serialInput = new SerialSensorInput();
            serialInput.OnFrameReceived += EnqueueFrame;
            serialInput.OnDisconnected += HandleSerialDisconnected;

            if (mockInput != null)
            {
                mockInput.OnFrameReceived += EnqueueFrame;
            }
        }

        private void OnEnable()
        {
            StartActive();
        }

        private void OnDisable()
        {
            StopActive();
        }

        private void Update()
        {
            DrainFrames();
        }

        private void StartActive()
        {
            GameEvents.RaiseSystemState(SystemState.Initializing);

            if (systemConfig == null)
            {
                GameEvents.RaiseSystemState(SystemState.Error);
                Debug.LogError("[SensorInputManager] Missing SystemConfig.", this);
                return;
            }

            if (!string.IsNullOrWhiteSpace(systemConfig.SerialPortName))
            {
                serialInput.Initialize(systemConfig);
                serialInput.StartReading();
                active = serialInput;
                GameEvents.RaiseSystemState(SystemState.Running);
                Debug.Log(
                    $"[SensorInputManager] Serial active on {systemConfig.SerialPortName} @ {systemConfig.SerialBaudRate} baud.",
                    this);
                return;
            }

            FallbackToMock("SerialPortName not set.");
        }

        private void StopActive()
        {
            if (active == null) return;
            active.StopReading();
            active = null;
            loggedFirstFrame = false;
            GameEvents.RaiseSystemState(SystemState.Stopped);
        }

        private void FallbackToMock(string reason)
        {
            if (mockInput == null)
            {
                GameEvents.RaiseSystemState(SystemState.Error);
                Debug.LogError($"[SensorInputManager] Cannot fallback to Mock: missing MockSensorInput. ({reason})", this);
                return;
            }

            mockInput.Initialize(systemConfig);
            mockInput.StartReading();
            active = mockInput;

            GameEvents.RaiseSystemState(SystemState.Degraded);
            Debug.LogWarning($"[SensorInputManager] Using Mock input. ({reason})", this);
        }

        private void HandleSerialDisconnected(string message)
        {
            GameEvents.RaiseSerialDisconnected(message);

            // 先停止串口线程再降级，避免重复读取占用端口。
            serialInput.StopReading();

            if (systemConfig != null && systemConfig.AutoFallbackToMock)
            {
                FallbackToMock(message);
            }
            else
            {
                GameEvents.RaiseSystemState(SystemState.Error);
            }
        }

        private void EnqueueFrame(SensorFrame frame)
        {
            lock (gate) pendingFrames.Enqueue(frame);
        }

        private void DrainFrames()
        {
            while (true)
            {
                SensorFrame frame;
                lock (gate)
                {
                    if (pendingFrames.Count == 0) break;
                    frame = pendingFrames.Dequeue();
                }

                GameEvents.RaiseSensorFrame(frame);

                if (!loggedFirstFrame)
                {
                    loggedFirstFrame = true;
                    Debug.Log("[SensorInputManager] First sensor frame received — input link active.", this);
                }
            }
        }
    }
}
