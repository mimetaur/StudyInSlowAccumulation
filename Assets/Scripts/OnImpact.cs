using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnImpact : MonoBehaviour
{
    [SerializeField] private PlaySounds playSounds = default;
    [SerializeField] private bool onlyPlayIfVisible = false;
    private Renderer rend;

    private void Start()
    {
        var gameManager = GameObject.Find("GameManager");
        rend = this.gameObject.GetComponent<Renderer>();
    }

    void OnCollisionEnter(Collision col)
    {
        if (rend.isVisible == false && onlyPlayIfVisible == true)
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
