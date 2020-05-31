using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySounds : MonoBehaviour
{
    [SerializeField] private AudioSource source = default;
    [SerializeField] private AudioClip[] floorClips = default;
    [SerializeField] private AudioClip[] ballClips = default;
    [Range(0, 1)] [SerializeField] private float ballVolume = 0.5f;
    [Range(0, 1)] [SerializeField] private float floorVolume = 1.0f;

    public void PlayFloorSound()
    {
        AudioClip clip = floorClips[Random.Range(0, floorClips.Length)];
        source.PlayOneShot(clip, floorVolume);
    }

    public void PlayBallSound()
    {
        AudioClip clip = ballClips[Random.Range(0, ballClips.Length)];
        source.PlayOneShot(clip, ballVolume);
    }
}
