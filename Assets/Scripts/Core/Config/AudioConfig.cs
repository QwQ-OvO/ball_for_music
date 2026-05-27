using System;
using UnityEngine;
using InformationString.Core;

namespace InformationString.Core.Config
{
    [CreateAssetMenu(menuName = "InformationString/Config/AudioConfig")]
    public sealed class AudioConfig : ScriptableObject
    {
        [Header("Tempo")]
        [Min(1f)]
        [SerializeField] private float defaultBpm = 90f;

        [SerializeField] private Vector2 bpmRange = new Vector2(60f, 140f);

        [Header("Mix")]
        [Range(0f, 1f)]
        [SerializeField] private float connectionVolume = 1f;

        [Header("Gating")]
        [Min(0f)]
        [SerializeField] private float fadeInSeconds = 0.02f;

        [Min(0f)]
        [SerializeField] private float fadeOutSeconds = 0.05f;

        [Header("Sound Clips")]
        [SerializeField] private SoundClipMapping[] soundClips;

        public float DefaultBpm => Mathf.Clamp(defaultBpm, bpmRange.x, bpmRange.y);
        public Vector2 BpmRange => bpmRange;
        public float ConnectionVolume => connectionVolume;
        public float FadeInSeconds => fadeInSeconds;
        public float FadeOutSeconds => fadeOutSeconds;

        public bool TryGetClip(BlockType soundType, out AudioClip clip)
        {
            if (soundClips != null)
            {
                for (var i = 0; i < soundClips.Length; i++)
                {
                    if (soundClips[i].SoundType != soundType) continue;
                    clip = soundClips[i].Clip;
                    return clip != null;
                }
            }

            clip = null;
            return false;
        }

        [Serializable]
        public struct SoundClipMapping
        {
            public BlockType SoundType;
            public AudioClip Clip;
        }
    }
}
