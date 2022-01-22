using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BumpUp : MonoBehaviour
{

    public float push = 300f;
    public float fadeTime = 10f;
    public float updateAlphaFrequency = 0.05F;    //aggiorno l'alpha questo numero di volte al secondo
    private bool fadeStarted = false;
    void Start()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("LOL!!!");
        Rigidbody rb = GetComponent<Rigidbody>();

        rb.AddForce(collision.gameObject.transform.up * push);
    }

    private void OnMouseDown()
    {
        if (!fadeStarted) {
            fadeStarted = true;
            StartCoroutine(Fade());
        }
    }

    IEnumerator Fade()
    {
        Material m = GetComponent<Renderer>().material;
        Color c;
        Debug.Log(updateAlphaFrequency + "/" + fadeTime + " = " + 1/(fadeTime/updateAlphaFrequency));
        for(float f = 1f; f >= 0; f -= 1 / (fadeTime / updateAlphaFrequency))
        {
            c = m.color;
            c.a = f;
            m.color = c;
            Debug.Log("Alpha value = " + c.a);
            yield return new WaitForSeconds(updateAlphaFrequency);
        }
        c = m.color;
        c.a = 1f;
        m.color = c;
        Destroy(gameObject);
    }

    void Update()
    {
        
    }
}
