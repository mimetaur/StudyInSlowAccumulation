using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnBalls : MonoBehaviour
{
    [Header("Ball Prefab and Materials")]
    [SerializeField] private GameObject ball = default;
    [SerializeField] private Material[] ballMaterials = default;

    [Header("Spawn Locations")]
    [SerializeField] private Vector3 floorCenter = new Vector3(0, 0, 0);
    [SerializeField] private float spawnHeight = 50.0f;
    [SerializeField] private float floorRadius = 50.0f;

    [Header("Spawn Parameters")]
    [SerializeField] private float spawnRate = 5.0f;
    [SerializeField] private float spawnInitialDelay = 5.0f;
    [SerializeField] private int spawnOddsPercent = 50;

    [Header("Spawning Duration")]
    [SerializeField] private bool doCapSpawnDuration = false;
    [SerializeField] private float spawnDurationInMinutes = 60f;

    private bool doEndSpawn = false;
    private const float minutesToSeconds = 60f;

    void Start()
    {
        InvokeRepeating("SpawnBall", spawnInitialDelay, spawnRate);
        if (doCapSpawnDuration)
        {
            float spawnDurationInSeconds = spawnDurationInMinutes * minutesToSeconds;
            Debug.Log($"Num seconds til end spawn: {spawnDurationInSeconds}");
            Invoke("EndSpawning", spawnDurationInSeconds);
        }
    }

    private void EndSpawning()
    {
        doEndSpawn = true;
    }

    private void SpawnBall()
    {
        if (doEndSpawn)
        {
            Debug.Log("Not spawning a new ball");
        }
        else
        {
            if (Random.Range(0, 100) < spawnOddsPercent)
            {
                Material newMat = ballMaterials[Random.Range(0, ballMaterials.Length)];

                Vector3 pos = GetRandomSpawnPosition();
                GameObject newBall = Instantiate(ball, pos, Quaternion.identity);
                newBall.GetComponent<Renderer>().material = newMat;
            }
        }


    }

    Vector3 GetRandomSpawnPosition()
    {
        Vector3 center = new Vector3(floorCenter.x, floorCenter.y + spawnHeight, floorCenter.z);
        Vector3 pos = GameUtils.RandomPointWithinCircle(center, floorRadius);
        return pos;
    }
}
