using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawn : MonoBehaviour
{
    public int levelCount;
    public bool spawn = true;
    [SerializeField] GameObject[] patterns;
    [SerializeField] GameObject[] enemies;
    [SerializeField] GameObject enemySpawnPoint;
    [SerializeField] GameObject spawnPoint;
    void Update()
    {
        if (spawn == true)
        {
            StartCoroutine(spawnLevel());
            spawn = false;
        }        

    }
    IEnumerator spawnLevel()
    {
        yield return new WaitForSeconds(1.5f);
        Instantiate(patterns[levelCount], spawnPoint.transform.position, Quaternion.identity);
        Instantiate(enemies[levelCount], enemySpawnPoint.transform.position, Quaternion.identity);
    }
}
