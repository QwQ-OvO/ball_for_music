using System.Collections.Generic;
using UnityEngine;

namespace InformationString.Audio
{
    public sealed class AudioSourcePool : MonoBehaviour
    {
        [Min(1)]
        [SerializeField] private int poolSize = 32;

        [SerializeField] private AudioSource audioSourcePrefab;

        private readonly Stack<AudioSource> available = new Stack<AudioSource>();
        private readonly HashSet<AudioSource> inUse = new HashSet<AudioSource>();

        private void Awake()
        {
            Prewarm();
        }

        private void Prewarm()
        {
            if (audioSourcePrefab == null)
            {
                var go = new GameObject("PooledAudioSource");
                go.transform.SetParent(transform);
                audioSourcePrefab = go.AddComponent<AudioSource>();
                audioSourcePrefab.playOnAwake = false;
                audioSourcePrefab.loop = true;
                audioSourcePrefab.spatialBlend = 0f;
            }

            while (available.Count + inUse.Count < poolSize)
            {
                var src = Instantiate(audioSourcePrefab, transform);
                src.name = $"AudioSource_{available.Count + inUse.Count}";
                src.playOnAwake = false;
                src.loop = true;
                src.spatialBlend = 0f;
                src.volume = 0f;
                src.Stop();
                available.Push(src);
            }
        }

        public AudioSource Get()
        {
            if (available.Count == 0) Prewarm();
            if (available.Count == 0) return null;

            var src = available.Pop();
            inUse.Add(src);
            return src;
        }

        public void Return(AudioSource src)
        {
            if (src == null) return;
            if (!inUse.Remove(src)) return;

            src.clip = null;
            src.volume = 0f;
            src.Stop();
            available.Push(src);
        }
    }
}
