using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySounds : MonoBehaviour
{
    public AudioSource source;

    [Range(0, 1)]
    public float ballVolume = 0.5f;

    [Range(0, 1)]
    public float floorVolume = 1.0f;

    public AudioClip[] floorClips;
    public AudioClip[] ballClips;

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
