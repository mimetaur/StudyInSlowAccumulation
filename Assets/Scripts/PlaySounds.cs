using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySounds : MonoBehaviour
{

    public AudioSource source;
    public AudioClip[] floorClips;
    public AudioClip[] ballClips;
    public float ballVolume = 0.5f;
    public float floorVolume = 1.0f;

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
