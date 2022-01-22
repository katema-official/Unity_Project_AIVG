using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeColor : MonoBehaviour
{

    public Material myColor;
    private Material beginColor;

    // Start is called before the first frame update
    void Start()
    {
        beginColor = GetComponent<Material>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Material actualColor = GetComponent<Renderer>().material;

            if(actualColor.name.Replace(" (Instance)", "") != myColor.name)
            {
                actualColor = myColor;
            }
            else
            {
                actualColor = beginColor;
            }
            GetComponent<Renderer>().material = actualColor;

        }
        
    }
}
