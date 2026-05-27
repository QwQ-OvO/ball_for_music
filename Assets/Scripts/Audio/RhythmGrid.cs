using System;
using UnityEngine;

namespace InformationString.Audio
{
    public sealed class RhythmGrid : MonoBehaviour
    {
        public event Action OnBeat;
        public event Action OnBar;
        public event Action<int> OnSubBeat;

        [Header("Time Signature")]
        [Min(1)]
        [SerializeField] private int beatsPerBar = 4;

        [Min(1)]
        [SerializeField] private int subdivisionsPerBeat = 4;

        [Header("Tempo")]
        [Min(1f)]
        [SerializeField] private float bpm = 90f;

        [Min(0f)]
        [SerializeField] private float startDelaySeconds = 0.1f;

        [Header("BPM Transition")]
        [Tooltip("默认 SetBPM() 平滑过渡拍数（仅 Inspector 预览用，可在调用时覆盖）。")]
        [Min(0f)]
        [SerializeField] private float defaultBpmTransitionBeats = 4f;

        private double nextSubBeatDspTime;
        private int currentSubBeatInBar;
        private int currentBeatInBar;

        private bool started;

        private float transitionStartBpm;
        private float transitionTargetBpm;
        private double transitionStartDspTime;
        private double transitionDurationSeconds;
        private bool hasTransition;

        private int pendingBeatsPerBar;
        private int pendingSubdivisionsPerBeat;
        private bool hasPendingTimeSignature;

        public double BPM => bpm;
        public double SecondsPerBeat => 60.0 / BPM;
        public int BeatsPerBar => beatsPerBar;
        public int SubdivisionsPerBeat => subdivisionsPerBeat;

        public int CurrentBeatInBar => currentBeatInBar;
        public int CurrentSubBeatInBar => currentSubBeatInBar;

        private int SubBeatsPerBar => beatsPerBar * subdivisionsPerBeat;

        private void OnEnable()
        {
            ResetClock();
        }

        private void Update()
        {
            if (!started)
            {
                nextSubBeatDspTime = UnityEngine.AudioSettings.dspTime + startDelaySeconds;
                started = true;
            }

            var now = UnityEngine.AudioSettings.dspTime;

            if (hasTransition && transitionDurationSeconds > 0)
            {
                var t = (now - transitionStartDspTime) / transitionDurationSeconds;
                if (t >= 1.0)
                {
                    bpm = transitionTargetBpm;
                    hasTransition = false;
                }
                else if (t >= 0.0)
                {
                    bpm = Mathf.Lerp(transitionStartBpm, transitionTargetBpm, (float)t);
                }
            }

            var subBeatSeconds = SecondsPerBeat / subdivisionsPerBeat;
            while (now >= nextSubBeatDspTime)
            {
                TickSubBeat();
                nextSubBeatDspTime += subBeatSeconds;
            }
        }

        public double GetNextQuantizedTime(QuantizeResolution resolution)
        {
            var subBeatSeconds = SecondsPerBeat / subdivisionsPerBeat;
            var next = nextSubBeatDspTime;

            switch (resolution)
            {
                case QuantizeResolution.SubBeat:
                    return next;
                case QuantizeResolution.HalfBeat:
                    // 半拍 = 2 个 subBeat（当 subdivisionsPerBeat=4 时）
                    return AlignToMultiple(next, subBeatSeconds, 2);
                case QuantizeResolution.Beat:
                    return AlignToMultiple(next, subBeatSeconds, subdivisionsPerBeat);
                case QuantizeResolution.Bar:
                    return AlignToMultiple(next, subBeatSeconds, SubBeatsPerBar);
                default:
                    return next;
            }
        }

        public void SetBPM(double newBpm, double transitionBeats = 4)
        {
            var clamped = Mathf.Max(1f, (float)newBpm);
            if (Math.Abs(clamped - bpm) < 0.001f) return;

            transitionStartBpm = bpm;
            transitionTargetBpm = clamped;
            transitionStartDspTime = UnityEngine.AudioSettings.dspTime;

            var beats = Math.Max(0.0, transitionBeats);
            transitionDurationSeconds = beats * SecondsPerBeat;
            hasTransition = transitionDurationSeconds > 0.0;

            if (!hasTransition) bpm = transitionTargetBpm;
        }

        public void SetTimeSignature(int newBeatsPerBar, int newSubdivisionsPerBeat)
        {
            var nextBeats = Mathf.Max(1, newBeatsPerBar);
            var nextSubdivisions = Mathf.Max(1, newSubdivisionsPerBeat);

            if (nextBeats == beatsPerBar && nextSubdivisions == subdivisionsPerBeat) return;

            pendingBeatsPerBar = nextBeats;
            pendingSubdivisionsPerBeat = nextSubdivisions;
            hasPendingTimeSignature = true;
        }

        [ContextMenu("Preview: Set BPM (Smooth)")]
        private void PreviewSetBpmSmooth()
        {
            SetBPM(bpm, defaultBpmTransitionBeats);
        }

        [ContextMenu("Reset Clock")]
        public void ResetClock()
        {
            started = false;
            currentSubBeatInBar = 0;
            currentBeatInBar = 0;
            nextSubBeatDspTime = 0;
        }

        private void TickSubBeat()
        {
            OnSubBeat?.Invoke(currentSubBeatInBar);

            if (currentSubBeatInBar % subdivisionsPerBeat == 0)
            {
                OnBeat?.Invoke();
                // OnBar 语义：原实现为“当前小节最后一拍开始时触发”（而非新小节第一拍）。
                // 这会影响以 OnBar 作为量化点的订阅者（例如 EntanglementEngine）。
                if (currentBeatInBar == beatsPerBar - 1) OnBar?.Invoke();
                currentBeatInBar = (currentBeatInBar + 1) % beatsPerBar;
            }

            var lastSubBeatInBar = SubBeatsPerBar - 1;
            var isEndOfBar = currentSubBeatInBar >= lastSubBeatInBar;

            if (isEndOfBar)
            {
                currentSubBeatInBar = 0;
                currentBeatInBar = 0;

                if (hasPendingTimeSignature)
                {
                    beatsPerBar = pendingBeatsPerBar;
                    subdivisionsPerBeat = pendingSubdivisionsPerBeat;
                    hasPendingTimeSignature = false;
                }

                return;
            }

            currentSubBeatInBar++;
        }

        private static double AlignToMultiple(double next, double stepSeconds, int multiple)
        {
            if (multiple <= 1) return next;
            var now = UnityEngine.AudioSettings.dspTime;
            var stepsSinceNow = (next - now) / stepSeconds;
            var steps = Math.Ceiling(stepsSinceNow);
            var alignedSteps = Math.Ceiling(steps / multiple) * multiple;
            return now + alignedSteps * stepSeconds;
        }

        public void DebugTickBeat()
        {
            // 仅用于 Editor 手动验证（不依赖 dspTime）。
            TickSubBeat();
        }
    }

    public enum QuantizeResolution
    {
        Bar,
        Beat,
        HalfBeat,
        SubBeat
    }
}
