using System;
using System.Collections.Generic;
using UnityEngine;

namespace InformationString.Audio
{
    public sealed class AudioEffectController : MonoBehaviour
    {
        private sealed class FadePlan
        {
            public double StartTime;
            public float StartVolume;
            public float TargetVolume;
            public float Duration;
        }

        private readonly Dictionary<AudioSource, Queue<FadePlan>> plansBySource = new Dictionary<AudioSource, Queue<FadePlan>>();
        private readonly Dictionary<AudioSource, float> lastPlannedVolume = new Dictionary<AudioSource, float>();

        public void Clear(AudioSource src)
        {
            if (src == null) return;
            plansBySource.Remove(src);
            lastPlannedVolume.Remove(src);
        }

        public void ScheduleVolume(AudioSource src, double startTime, float targetVolume, float durationSeconds)
        {
            if (src == null) return;

            if (!plansBySource.TryGetValue(src, out var queue))
            {
                queue = new Queue<FadePlan>();
                plansBySource[src] = queue;
                lastPlannedVolume[src] = src.volume;
            }

            var startVolume = lastPlannedVolume[src];
            queue.Enqueue(new FadePlan
            {
                StartTime = startTime,
                StartVolume = startVolume,
                TargetVolume = targetVolume,
                Duration = Mathf.Max(0f, durationSeconds),
            });
            lastPlannedVolume[src] = targetVolume;
        }

        private void Update()
        {
            var now = UnityEngine.AudioSettings.dspTime;
            foreach (var kvp in plansBySource)
            {
                var src = kvp.Key;
                var queue = kvp.Value;
                if (src == null || queue == null || queue.Count == 0) continue;

                while (queue.Count > 0)
                {
                    var plan = queue.Peek();
                    if (now < plan.StartTime) break;

                    var elapsed = now - plan.StartTime;
                    if (plan.Duration <= 0.0001f)
                    {
                        src.volume = plan.TargetVolume;
                        queue.Dequeue();
                        continue;
                    }

                    var t = Mathf.Clamp01((float)(elapsed / plan.Duration));
                    src.volume = Mathf.Lerp(plan.StartVolume, plan.TargetVolume, t);

                    if (t >= 1f)
                    {
                        queue.Dequeue();
                        continue;
                    }

                    break;
                }
            }
        }
    }
}
