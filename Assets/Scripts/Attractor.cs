using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attractor : MonoBehaviour
{
    [SerializeField] private float attractionRadius = 2;
    [SerializeField] private float attractionForce = 1;

    public void FixedUpdate()
    {
        foreach (Collider theCollider in Physics.OverlapSphere(transform.position, attractionRadius))
        {
            // calculate direction from target to me
            Vector3 forceDirection = transform.position - theCollider.transform.position;

            // apply force on target towards me
            var rb = theCollider.GetComponent<Rigidbody>();
            if (rb) rb.AddForce(forceDirection.normalized * attractionForce * Time.fixedDeltaTime);
        }
    }
}
