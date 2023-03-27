using System;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    public class AudioTrigger : MonoBehaviour
    {
        [Header("Audio source parameters")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _audioClip;
        [Space(5)] 
        [Header("Audio Playback parameters")]
        [SerializeField][Range(0f,1f)]
        private float _volume = 0.5f;

        private void Awake()
        {
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.volume = _volume;
        }

        public void PlayAudio()
        {
            if(!_audioSource.isActiveAndEnabled)
                return;

            _audioSource.clip = _audioClip;
            _audioSource.Play();
        }
    }
}