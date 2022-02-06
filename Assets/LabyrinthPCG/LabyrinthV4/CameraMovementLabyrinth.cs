using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class CameraMovementLabyrinth : MonoBehaviour
{
    public float RotationSpeed = 5;
    public float XMovementSpeedFraction = 5;
    public float ZMovementSpeedFraction = 5;
    public float YMovementSpeed = 0.1f;

    void Update()
    {
        //Source: https://gist.github.com/seferciogluecce/32c468b4392393f4f394a33a4a3e3c6a
        if (Input.GetMouseButton(0))
        {

            transform.eulerAngles += RotationSpeed * new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0);
        }

        //Source: https://answers.unity.com/questions/352235/moving-camera-with-wasd.html
        float xAxisValue = Input.GetAxis("Horizontal");
        float zAxisValue = Input.GetAxis("Vertical");
        transform.Translate(new Vector3(xAxisValue / XMovementSpeedFraction, 0.0f, zAxisValue / ZMovementSpeedFraction));

        //To move up and down
        if (Input.GetKey(KeyCode.Space))
        {
            transform.Translate(0, YMovementSpeed / 10, 0);
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            transform.Translate(0, -YMovementSpeed / 10, 0);
        }


    }
}