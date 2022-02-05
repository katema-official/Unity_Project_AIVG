using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockAStar : MonoBehaviour
{
    




    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }





    //The exit is at a known position, and I have decided that it will always be the room on the upper right room.
    //To make the path long, but always a bit random, we'll choose, as starting room, one on the left side of the dungeon.
    private GNode end;
    private GNode start;
    public static Graph graph;

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
            if(n.is_room && n.z < c.width/2){startCandidates.Add(n);}
        }
        end = candidate;

        //if there was no room on the left side, the dungeon is probably extremely small on the width.
        //if that happens, the starting room is chosen in a completely random way.
        if(startCandidates.Count == 0){start = graph.getNodes()[Random.Range(0, graph.getNodes().Length)];}

        //If instead the dungeon was big enough, select starting room randomly from the ones on the left
        start = startCandidates[Random.Range(0, startCandidates.Count)];

        //Debug.Log("Exit: z = " + end.z + ", x = " + end.x);
        //Debug.Log("Start: z = " + start.z + ", x = " + start.x);


        //we can now run A* to find the shortest path from start to end
        GEdge[] path = AStarSolver.Solve(graph, start, end, myHeuristics[(int)heuristicToUse]);

        foreach(GEdge e in path)
        {
            Debug.Log("z = " + e.to.z + ", x = " + e.to.x);
        }

    }




    private float distanceFromUpperRight(int z, int x)
    {
        return (Mathf.Sqrt(Mathf.Pow(c.width - z, 2) + Mathf.Pow(0 - x, 2)));
    }











    //CREDITS: Dario Maggiorini & Davide Gadia, University of Milan (Italy), Artificial Intelligence for Videogames
    public bool stopAtFirstHit = false;
    public Material visitedMaterial = null;

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
