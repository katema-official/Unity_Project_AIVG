using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FSeek : FBoidComponent
{

    //to hold the current edge of the path we are traveling
    private int i = 0;

    //and the current target towards which we are heading
    private GNode target;

    private Vector3 toVector = Vector3.zero;
    private Vector3 seek;

    public override Vector3 GetDirection(Collider[] neighbors, int size)
    {

        target = FlockAStar.path[i].to;
        toVector.x = target.x;
        toVector.y = transform.position.y;
        toVector.z = target.z;

        //if I've reached the end (the very last "to" node of the path) then I have nothing to seek anymore
        if (i == FlockAStar.path.Length - 1  && (transform.position - toVector).magnitude <= FlockAStar.NodeReachedThreshold){
            return Vector3.zero;
        }

        //Let's see: are we close enough to the target node?
        //Yes: then the "to" node of the next edge is our target.
        //No: let's go to the current target.
        if ((transform.position - toVector).magnitude <= FlockAStar.NodeReachedThreshold)
        {
            i += 1;
            target = FlockAStar.path[i].to;
            toVector.x = target.x;
            toVector.y = transform.position.y;
            toVector.z = target.z;
            
        }

        seek = (toVector - transform.position).normalized;
        return seek * FlockAStar.SeekComponent;
    }

}
