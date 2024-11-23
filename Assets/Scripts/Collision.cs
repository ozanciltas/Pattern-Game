using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collision : MonoBehaviour
{
    [SerializeField] GameObject failTxt;
    [SerializeField] GameObject[] particle;
    private void OnCollisionEnter(UnityEngine.Collision collision)
    {
        EnemySpawn esp = GameObject.Find("LevelControl").GetComponent<EnemySpawn>();
        if (collision.gameObject.tag == "TruePoint")
        {
            var parent = this.transform.parent.gameObject;
            var colParent = collision.transform.parent.gameObject;
            esp.levelCount++;
            esp.spawn = true;
            Destroy(parent);
            Destroy(colParent);
            var a = Instantiate(particle[0], transform.position, Quaternion.identity);
            var b = Instantiate(particle[1], colParent.transform.position, Quaternion.identity);
            Destroy(a, 1f);
            Destroy(b, 1f);
        }
        else
        {
            Time.timeScale = 0;
            failTxt.SetActive(true);
        }
    }

}
