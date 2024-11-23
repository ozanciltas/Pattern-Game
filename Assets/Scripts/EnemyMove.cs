using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    float speed = 5.5f;
    void Update()
    {
        transform.Translate(Vector3.back * speed * Time.deltaTime);
    }
}
