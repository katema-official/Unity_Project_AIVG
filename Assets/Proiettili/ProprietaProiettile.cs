using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProprietaProiettile : MonoBehaviour
{
    public Vector3 directionToGo;
    public float Speed;
    public float DimensionFactor;

    // Start is called before the first frame update
    void Start()
    {
        transform.localScale = Vector3.one * DimensionFactor;
    }

    public void create(Vector3 dir)
    {
        directionToGo = dir;
    }

    // Update is called once per frame
    void Update()
    {
        if(directionToGo != null && Speed > 0)
        {
            transform.Translate(directionToGo * Speed);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        print(collision.gameObject.name);
        if(collision.gameObject.name == "PrimoCubetto" || collision.gameObject.name == "PrimoCubetto(Clone)")
        {
            Debug.Log("Colpito!");
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
    }

}
