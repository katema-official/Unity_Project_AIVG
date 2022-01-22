using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnProjectile : MonoBehaviour
{

    public GameObject projectile;
    private ProprietaProiettile scriptForProjectile;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            GameObject actualProjectile = (GameObject) Instantiate(projectile, transform.position, Quaternion.identity);
            actualProjectile.GetComponent<ProprietaProiettile>().directionToGo = transform.forward;
        }

    }
}
