using System;
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
    private bool reached = false;

    //the path I want to follow
    public GEdge[] path = null;

    //a multiplication factor to be used on the time I expect it to take me to reach the target
    private float timeFactor = 1.1f;
    private IEnumerator recalculateCoroutine;
    private bool isCoroutineRunning = false;

    private FlockAStar c;
    private void Awake()
    {
        c = GameObject.Find("FlockEscape").GetComponent<FlockAStar>();
        path = c.path;
        target = path[i].to;
    }

    public override Vector3 GetDirection(Collider[] neighbors, int size)
    {
        
        toVector.x = target.x;
        toVector.y = transform.position.y;
        toVector.z = target.z;

        //if I've reached the end (the very last "to" node of the path) then I have nothing to seek anymore
        if (i == path.Length - 1 && (transform.position - toVector).magnitude <= FlockAStar.NodeReachedThreshold)
        {
            reached = true;
        }

        if (reached) return Vector3.zero;



        if (!isCoroutineRunning)
        {
            recalculateCoroutine = myCoroutine((toVector - transform.position).magnitude);
            StartCoroutine(recalculateCoroutine);
            isCoroutineRunning = true;
        }



        //Let's see: are we close enough to the target node?
        //Yes: then the "to" node of the next edge is our target.
        //No: let's go to the current target.
        if ((transform.position - toVector).magnitude <= FlockAStar.NodeReachedThreshold)
        {
            i += 1;
            target = path[i].to;
            StopCoroutine(recalculateCoroutine);
            isCoroutineRunning = false;
        }

        seek = (toVector - transform.position).normalized;

        //debug
        if (FBoidShared.debug)
        {
            if (name == "FBoid " + FBoidShared.debugNumberBoid)
            {
                Debug.DrawLine(transform.position, toVector, Color.red);
            }
        }

        return seek * FlockAStar.SeekComponent;
    }

    private IEnumerator myCoroutine(float space)
    {
        //we know the distance between the current position and the target and the velocity. We can calculate
        //the time we expect to pass before we get there.
        float timeExpected = space / FBoidShared.BoidSpeed;

        //we wait for the timeExpected multiplied by our tolerance time factor
        yield return new WaitForSeconds(timeExpected * timeFactor);

        //if the coroutine was not interrupted in the meanwhile, it means that we still haven't reached
        //the target- or at least, that's what the boid thinks, since he might as well have passed it
        //thanks to the alignment component (or he might be stuck in some place because other boids pushed him
        //too much. In those cases, to avoid backtracking, that the boid might
        //want to do, we recalculate the path with A* in this way:
        //-we take the path from the successor of the target to the end
        //-we prepend, to this path, a path that leads this boid from its current position to the successor
        //of the target
        if (i < path.Length - 2)
        {
            GNode myPosition = new GNode(transform.position.z, transform.position.x);
            GNode nextTarget = path[i + 1].to;
            c.graph.addNodeAndFindEdges(myPosition);
            GEdge[] prePath = AStarSolver.Solve(c.graph, myPosition, nextTarget, c.myHeuristics[(int)c.heuristicToUse]);
            GEdge[] newPath = new GEdge[prePath.Length + path.Length - (i + 2)];
            Array.Copy(prePath, 0, newPath, 0, prePath.Length);
            Array.Copy(path, i + 2, newPath, prePath.Length, path.Length - (i + 2));
            path = newPath;
            i = 0;
            target = path[i].to;
        }else if(i < path.Length - 1)
        {
            GNode myPosition = new GNode(transform.position.z, transform.position.x);
            GNode nextTarget = path[i + 1].to;
            c.graph.addNodeAndFindEdges(myPosition);
            GEdge[] prePath = AStarSolver.Solve(c.graph, myPosition, nextTarget, c.myHeuristics[(int)c.heuristicToUse]);
            path = prePath;
            i = 0;
            target = path[i].to;
        }
        isCoroutineRunning = false;   
    }

}
