using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnBalls : MonoBehaviour
{
    [SerializeField] private GameObject ball = default;
    [SerializeField] private Material[] ballMaterials = default;

    [SerializeField] private float spawnRate = 5.0f;
    [SerializeField] private float spawnInitialDelay = 5.0f;
    [SerializeField] private float spawnHeight = 50.0f;

    [SerializeField] private Vector3 floorCenter = new Vector3(0, 0, 0);
    [SerializeField] private float floorRadius = 50.0f;
    [SerializeField] private int spawnOddsPercent = 50;

    void Start()
    {
        InvokeRepeating("SpawnBall", spawnInitialDelay, spawnRate);
    }

    void SpawnBall()
    {
        if (Random.Range(0, 100) > spawnOddsPercent)
        {
            return;
        }

        Material newMat = ballMaterials[Random.Range(0, ballMaterials.Length)];

        Vector3 pos = GetRandomSpawnPosition();
        GameObject newBall = Instantiate(ball, pos, Quaternion.identity);
        newBall.GetComponent<Renderer>().material = newMat;
    }

    Vector3 GetRandomSpawnPosition()
    {
        Vector3 center = new Vector3(floorCenter.x, floorCenter.y + spawnHeight, floorCenter.z);
        Vector3 pos = GameUtils.RandomPointWithinCircle(center, floorRadius);
        return pos;
    }
}
