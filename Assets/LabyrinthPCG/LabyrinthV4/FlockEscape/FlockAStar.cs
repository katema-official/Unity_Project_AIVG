using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockAStar : MonoBehaviour
{
    

    //The flocks we will be using will find out what's the next position (node) they have to reach in the
    //following way:
    //-consider the current node towards which we are heading to. Is it closer then this threshold?
    //-Yes: then take the next node as the target to seek
    //-No: keep moving toward the current node.
    //When the final node will be reached, we can choose what to do (move randomly, destroy gameobjects...)
    public static float NodeReachedThreshold = 1f;
    public float _NodeReachedThreshold = 1f;

    [Range(0f, 1f)] public float _SeekComponent = 1f;
    public static float SeekComponent = 1f;

    //actual flocking properties
    public float radius = 1f;
    public int count = 100;
    public GameObject boid = null;


    private bool canSpawnBoids = false;

    // Start is called before the first frame update
    void Start()
    {
        stopAtFirstHit = _stopAtFirstHit;
        SeekComponent = _SeekComponent;
        NodeReachedThreshold = _NodeReachedThreshold;

        StartCoroutine(spawnBoids());
    }
   

    //The exit is at a known position, and I have decided that it will always be the room on the upper right room.
    //To make the path long, but always a bit random, we'll choose, as starting room, one on the left side of the dungeon.
    private GNode end;
    private GNode start;
    public Graph graph;
    public static GEdge[] path;

    //this still has some informations we need
    private LabyrinthGenerator4Animated c;

    public void myInitialize(Graph g)
    {

        c = GameObject.Find("LabyrinthGenerator4").GetComponent<LabyrinthGenerator4Animated>();
        graph = g;

        //first, find the exit
        GNode candidate = null;
        float minDist = Mathf.Infinity;

        //I take advantage of the fact that i'm doing a for to find all the rooms on the left side
        List<GNode> startCandidates = new List<GNode>();
        foreach(GNode n in graph.getNodes())
        {
            //upper right room
            if((n.is_room && candidate == null) || (n.is_room && (distanceFromUpperRight(n.z, n.x) < minDist))){candidate = n; minDist = distanceFromUpperRight(n.z, n.x); }

            //candidates room
            if(n.is_room && n.z < (c.width * c.unitScale)/2){startCandidates.Add(n);}
        }
        end = candidate;


        //if there was no room on the left side, the dungeon is probably extremely small on the width.
        //when that happens, the starting room is chosen in a completely random way.
        if (startCandidates.Count == 0)
        {
            start = graph.getNodes()[Random.Range(0, graph.getNodes().Length)];
        }
        else
        {
            //If instead the dungeon was big enough, select starting room randomly from the ones on the left
            start = startCandidates[Random.Range(0, startCandidates.Count)];
        }  
        //Debug.Log("Exit: z = " + end.z + ", x = " + end.x);
        //Debug.Log("Start: z = " + start.z + ", x = " + start.x);


        //we can now run A* to find the shortest path from start to end
        path = AStarSolver.Solve(graph, start, end, myHeuristics[(int)heuristicToUse]);

        //foreach(GEdge e in path)
        //{
        //    Debug.Log("z = " + e.to.z + ", x = " + e.to.x);
        //}


        //mo devo passare il percorso al seek component di ogni flock (posso renderlo statico?), e probabilmente generare qui (? sì dai) i flock stessi

        //we have the path to traverse, now we can create the flocks in the starting room.
        //Now: to make sure that the boids won't be created inside of a wall, a small radius should be used (1, for example).
        //The problem is that if a lot of boids are created at the same time in a small space, we have some lag.
        //To avoid this, we will spawn one boid at a time (we can decide how much time). Keep in mind that it is responsibility
        //of the user to make sure the boids are spawned in the room and not inside walls, so I suggest not to modify the radius value:
        //you can do it, but at your own risk.
        //TL;DR: KEEP THE RADIUS AT 1, OR YOU WON'T BE SURE THAT THE BOIDS ARE SPAWNED INSIDE THE ROOM
        canSpawnBoids = true;
        
    }


    private IEnumerator spawnBoids()
    {
        while (!canSpawnBoids)
        {
            yield return null;
        }


        if (boid != null)
        {
            for (int i = 0; i < count; i += 1)
            {
                GameObject go = Instantiate(boid);
                go.transform.position = new Vector3(
                    (path[0].from.x + c.unitScale/2) + Random.Range(-radius, radius), 
                    Random.Range(-c.heightOfWalls/2, c.heightOfWalls/2),
                    (path[0].from.z + c.unitScale / 2) + Random.Range(-radius, radius));
                //go.transform.LookAt(transform.position + Random.insideUnitSphere * radius);
                go.name = boid.name + " " + i;
            }
        }

    }



    private float distanceFromUpperRight(float z, float x)
    {
        return (Mathf.Sqrt(Mathf.Pow(c.width * c.unitScale - z, 2) + Mathf.Pow(0 - x, 2)));
    }











    //CREDITS: Dario Maggiorini & Davide Gadia, University of Milan (Italy), Artificial Intelligence for Videogames

    //used by AStarSolver
    public static bool stopAtFirstHit = false;
    public bool _stopAtFirstHit = false;

    public enum Heuristics { Euclidean, Manhattan, Bisector, FullBisector, Zero };
    public HeuristicFunction[] myHeuristics = { EuclideanEstimator, ManhattanEstimator, BisectorEstimator,
                                                 FullBisectorEstimator, ZeroEstimator };
    public Heuristics heuristicToUse = Heuristics.Euclidean;

    protected static float EuclideanEstimator(GNode from, GNode to)
    {
        return (new Vector3(from.x, 0, from.z) - new Vector3(to.x, 0, to.z)).magnitude;
    }

    protected static float ManhattanEstimator(GNode from, GNode to)
    {
        return (
                Mathf.Abs(from.x - to.x) +
                Mathf.Abs(from.z - to.z)
            );
    }

    protected static float BisectorEstimator(GNode from, GNode to)
    {
        Ray r = new Ray(Vector3.zero, new Vector3(to.x, 0, to.z));
        return Vector3.Cross(r.direction, new Vector3(from.x, 0, from.z) - r.origin).magnitude;
    }

    protected static float FullBisectorEstimator(GNode from, GNode to)
    {
        Ray r = new Ray(Vector3.zero, new Vector3(to.x, 0, to.z));
        Vector3 toBisector = Vector3.Cross(r.direction, new Vector3(from.x, 0, from.z) - r.origin);
        return toBisector.magnitude + (new Vector3(to.x, 0, to.z) - (new Vector3(from.x, 0, from.z) + toBisector)).magnitude;
    }

    protected static float ZeroEstimator(GNode from, GNode to) { return 0f; }


}
