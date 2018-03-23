using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnImpact : MonoBehaviour
{

    private PlaySounds playSounds;
    private Renderer rend;

    private void Start()
    {
        var gameManager = GameObject.Find("GameManager");
        rend = this.gameObject.GetComponent<Renderer>();
        playSounds = gameManager.GetComponent<PlaySounds>();
    }

    void OnCollisionEnter(Collision col)
    {
        if (!rend.isVisible)
        {
            return;
        }

        if (col.gameObject.tag == "Floor")
        {
            playSounds.PlayFloorSound();
        }
        else if (col.gameObject.tag == "Ball")
        {
            playSounds.PlayBallSound();
        }
    }
}
