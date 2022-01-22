using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnCubes : MonoBehaviour
{

    public GameObject whatToSpwan;
    public float rate;
    public float areaToSpawn;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Spawner());
    }

    IEnumerator Spawner()
    {
        while(whatToSpwan != null)
        {
            Instantiate(whatToSpwan, GetComponent<Transform>().position + Random.insideUnitSphere * areaToSpawn, Quaternion.identity);
            yield return new WaitForSeconds(1f);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
