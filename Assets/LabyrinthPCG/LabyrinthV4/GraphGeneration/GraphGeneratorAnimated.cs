using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphGeneratorAnimated : MonoBehaviour
{
    //the bitmap of the dungeon
    private int[,] dungeonBitmap;

    //a bitmap that is just like the one of the dungeon except that it doesn't consider rooms
    private int[,] corridorBitmap;

    //the graph we will build
    private Graph graph;

    //just some materials for the animation
    public Material roomMaterial;
    public Material corridorEntranceMaterial;
    public Material roomCorridorMaterial;
    public Material intersectionMaterial;
    public Material corridorMaterial;
    

    //for animation
    public float delayInGeneration;
    public bool _animated;

    private List<GNode> nodesList;
    private List<GameObject> cubesList;
    private bool canDrawNodes;
    public GameObject unitNodes;

    private List<GEdge> edgesList;
    private List<GameObject> linesList;
    private bool canDrawEdges;
    public GameObject edgeObject;

    //to have access to the LabyrinthGenarator
    private LabyrinthGenerator4Animated c;

    //the gameobject that, after we have created the graph, will use A* and spawn a flock
    private GameObject flockAStar;

    // Start is called before the first frame update
    void Start()
    {
        c = GameObject.Find("LabyrinthGenerator4").GetComponent<LabyrinthGenerator4Animated>();
        StartCoroutine(drawNodes());
        StartCoroutine(drawEdges());
        flockAStar = GameObject.Find("FlockEscape");
    }

    private void Awake()
    {
        nodesList = new List<GNode>();
        graph = new Graph();
        cubesList = new List<GameObject>();
        canDrawNodes = false;
        canDrawEdges = false;
        edgesList = new List<GEdge>();
        linesList = new List<GameObject>();
    }


    //functions that will be used by other classes to modify the graph we are building
    public void addNode(int z, int x, int type)
    {
        float zz = z * c.unitScale + (c.unitScale / 2);
        float xx = x * c.unitScale + (c.unitScale / 2);
        GNode n;
        switch (type)
        {
            //it's a room, add the node
            case 0:
                n = new GNode(zz, xx);
                n.is_room = true;
                graph.AddNode(n);
                nodesList.Add(n);
                break;
            case 1:
                //it's a corridor entrance node. We must, first of all, see if there is already a node in that position in
                //the graph (if a room is small enough, its center might correspond to a corridor entrance). If there is,
                //we just enrich the description of that node. If there isn't, we create a new node.
                GNode[] nodes = graph.getNodes();
                bool added = false;
                foreach(GNode nn in nodes)
                {
                    if(nn.z == zz && nn.x == xx)
                    {
                        nn.is_corridor_entrance = true;
                        added = true;
                    }
                }
                if (!added)
                {
                    n = new GNode(zz, xx);
                    n.is_corridor_entrance = true;
                    graph.AddNode(n);
                    nodesList.Add(n);
                }
                break;
        }

    }

    public void setBitmaps(int[,] normalBitmap, int[,] corridorsBitmap)
    {
        dungeonBitmap = normalBitmap;
        corridorBitmap = corridorsBitmap;

        //now, we have the room nodes and the corridor entrance nodes. What we still lack of are the intersection nodes.
        //To find them, we have to scan all the point of the corridors and see if those can see, in the 4 cardinal directions,
        //at least 3 other points, that can be of any kind: corridor points, other intersection points or room points (even
        //thought those shouldn't be visible since we have hidden them in the corridors bitmap).
        findIntersections();
        canDrawNodes = true;

        
    }







    public IEnumerator drawNodes()
    {
        while (!canDrawNodes)
        {
            yield return null;
        }

        if (_animated)
        {
            foreach (GNode n in nodesList)
            {
                GameObject g = Instantiate(unitNodes);
                g.transform.position = new Vector3(c.x0 + n.x, 0, c.z0 + n.z);
                g.transform.localScale = new Vector3(g.transform.localScale.x * c.unitScale, g.transform.localScale.y * c.heightOfWalls, g.transform.localScale.z * c.unitScale);
                if (n.is_room && n.is_corridor_entrance)
                {
                    g.GetComponent<MeshRenderer>().material = roomCorridorMaterial;
                }
                else if (n.is_room)
                {
                    g.GetComponent<MeshRenderer>().material = roomMaterial;
                }
                else if (n.is_corridor_entrance)
                {
                    g.GetComponent<MeshRenderer>().material = corridorEntranceMaterial;
                }
                else if (n.is_intersection)
                {
                    g.GetComponent<MeshRenderer>().material = intersectionMaterial;
                }
                else
                {
                    //TO SEE CORRIDORS
                    //g.GetComponent<MeshRenderer>().material = corridorMaterial;
                }
                cubesList.Add(g);
                yield return new WaitForSeconds(delayInGeneration);
            }
        }

        //now we have to build the edges from the nodes.
        buildEdges();
        //after drawing the nodes, draw the edges too
        canDrawEdges = true;
    }

    private void findIntersections()
    {
        float zz;
        float xx;
        //we search among all the points with value 0 in the bitmap
        for(int j = 0; j< c.height; j++)
        {
            for(int i = 0; i < c.width; i++)
            {
                //if the bitmap value is 0, it is a corridor and must be checked.
                //we must also check that, in that position, there isn't a corridor entrance.
                zz = i * c.unitScale + (c.unitScale / 2);
                xx = j * c.unitScale + (c.unitScale / 2);
                if (corridorBitmap[i,j] == 0 && graph.isNodeAtCoordinates(zz,xx) == false)
                {
                    int cubesVisible = lookAround(i, j, zz, xx);
                    GNode n = new GNode(zz, xx);
                    if (cubesVisible >= 3)
                    {
                        n.is_intersection = true;
                        graph.AddNode(n);
                        nodesList.Add(n);
                    }
                    else
                    {
                        //TO SEE CORRIDORS
                        //nodesList.Add(n);
                    }
                }
            }
        }
    }

    //function used by FindIntersections to see how many cubes (or rather, how many roomNodes/CorridorEntranceNodes/IntersectionNodes)
    //are around him. This function will look in all the four cardinal directions in respect to the given coordinates, see if one of those cubes is
    //visible (one is enough, if i.e. there are two on the north we are not interesed in the second) and add 1 to the result.
    private int lookAround(int i, int j, float z, float x)
    {
        int res = 0;
        res += lookUp(i, j, z, x);
        res += lookDown(i, j, z, x);
        res += lookRight(i, j, z, x);
        res += lookLeft(i, j, z, x);
        return res;
    }

    //functions used to look in the four cardinal directions by LookAround
    private int lookUp(int i, int j, float z, float x)
    {
        while (true)
        {
            x = x - c.unitScale;
            j = j - 1;
            //first of all, check if we went outside of the dungeon
            if (j < 0) return 0;
            //then, we want to check if there is a node at this coordinates.
            //if there is,then yes, a node is visible in this direction.
            if (graph.isNodeAtCoordinates(z, x)) return 1;
            //if not, check: is this a wall, for our bitmap?
            if(dungeonBitmap[i,j] == 1) return 0;
        }
    }

    private int lookDown(int i, int j, float z, float x)
    {
        while (true)
        {
            x = x + c.unitScale;
            j = j + 1;
            if (j >= c.height) return 0;
            if (graph.isNodeAtCoordinates(z, x)) return 1;
            if (dungeonBitmap[i,j] == 1) return 0;
        }
    }

    private int lookLeft(int i, int j, float z, float x)
    {
        while (true)
        {
            z = z - c.unitScale;
            i = i - 1;
            if (i < 0) return 0;
            if (graph.isNodeAtCoordinates(z, x)) return 1;
            if (dungeonBitmap[i,j] == 1) return 0;
        }
    }

    private int lookRight(int i, int j, float z, float x)
    {
        while (true)
        {
            z = z + c.unitScale;
            i = i + 1;
            if (i >= c.width) return 0;
            if (graph.isNodeAtCoordinates(z, x)) return 1;
            if (dungeonBitmap[i,j] == 1) return 0;
        }
    }





    private IEnumerator drawEdges()
    {
        while (!canDrawEdges)
        {
            yield return null;
        }

        if (_animated)
        {
            foreach(GEdge e in edgesList)
            {
                GameObject edge = Instantiate(edgeObject);
                LineRenderer lr = edge.GetComponent<LineRenderer>();
                lr.SetPosition(0, new Vector3(e.from.x, 0, e.from.z));
                lr.SetPosition(1, new Vector3(e.to.x, 0, e.to.z));
                linesList.Add(edge);
                yield return new WaitForSeconds(delayInGeneration);
            }

            //in the end, visualize the dungeon for a bit, and then start the flock exploration
            yield return new WaitForSeconds(3f);
        }

        //destroy, if necessary, all the cubes and edges created
        
        foreach(GameObject g in cubesList)
        {
            Destroy(g);
        }
        foreach (GameObject g in linesList)
        {
            Destroy(g);
        }

        //the last thing to do is to pass the control to the flockAStar component
        flockAStar.GetComponent<FlockAStar>().myInitialize(graph);

    }

    private void buildEdges()
    {
        //for all the nodes in this graph, we shoot a raycast toward each other node and see:
        //-it is visible? Then add an edge between the two nodes.
        //-it is not? Don't do anything.
        //if an edge is added, its weight is the distance between the two nodes in the space.
        foreach(GNode current in graph.getNodes())
        {
            foreach(GNode other in graph.getNodes())
            {
                if (current != other)
                {
                    RaycastHit hit;
                    bool isThereWall = Physics.Raycast(new Vector3(current.x, 0, current.z),
                        (new Vector3(other.x, 0, other.z) - new Vector3(current.x, 0, current.z)).normalized,
                        out hit,
                        (new Vector3(other.x, 0, other.z) - new Vector3(current.x, 0, current.z)).magnitude,
                        (1 << 6));
                    if (!isThereWall)
                    {
                        GEdge e = new GEdge(current, other,
                            (new Vector3(other.x, 0, other.z) - new Vector3(current.x, 0, current.z)).magnitude);
                        graph.AddEdge(e);

                        //to avoid bloating the animation with duplicated edges
                        if (graph.areNodesConnected(other, current)) {
                            edgesList.Add(e);
                        }
                    }
                }
            }
        }
    }

}
