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

    //GameObjects array for animating the creation of the graph
    private List<GNode> nodesList;

    //for animation
    public float delayInGeneration;
    public bool animated;
    private List<GameObject> cubesList;
    private bool canDraw;

    //to have access to the LabyrinthGenarator
    LabyrinthGenerator4Animated c;

    // Start is called before the first frame update
    void Start()
    {
        //dungeonBitmap = new int[GameObject.Find("LabyrinthGenerator4").GetComponent<LabyrinthGenerator4Animated>().width, 
        //    GameObject.Find("LabyrinthGenerator4").GetComponent<LabyrinthGenerator4Animated>().height];
        //corridorBitmap = new int[GameObject.Find("LabyrinthGenerator4").GetComponent<LabyrinthGenerator4Animated>().width,
        //    GameObject.Find("LabyrinthGenerator4").GetComponent<LabyrinthGenerator4Animated>().height];
        c = GameObject.Find("LabyrinthGenerator4").GetComponent<LabyrinthGenerator4Animated>();
        StartCoroutine(drawNodes());
        
    }

    private void Awake()
    {
        nodesList = new List<GNode>();
        graph = new Graph();
        cubesList = new List<GameObject>();
    }


    //functions that will be used by other classes to modify the graph we are building
    public void addNode(int z, int x, int type)
    {
        //if(graph == null) { graph = new Graph(); }  //if I put the creation of the graph in the Start it doesn't work
        //if (nodesList == null) { nodesList = new List<GNode>(); }
        GNode n = new GNode(z, x);
        switch (type)
        {
            //it's a room, add the node
            case 0:
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
                    if(nn.z == z && nn.x == x)
                    {
                        nn.is_corridor_entrance = true;
                        added = true;
                    }
                }
                if (!added)
                {
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
        findIntersections(corridorsBitmap);
        



        canDraw = true;


        //debug
        /*
        string s = "";
        for(int j = 0; j < GameObject.Find("LabyrinthGenerator4").GetComponent<LabyrinthGenerator4Animated>().height; j++)
        {
            for (int i = 0; i < GameObject.Find("LabyrinthGenerator4").GetComponent<LabyrinthGenerator4Animated>().width; i++)
            {
                s += " " + normalBitmap[i, j];
            }
            Debug.Log(s);
            s = "";
        }

        Debug.Log("------------");
        s = "";
        for (int j = 0; j < GameObject.Find("LabyrinthGenerator4").GetComponent<LabyrinthGenerator4Animated>().height; j++)
        {
            for (int i = 0; i < GameObject.Find("LabyrinthGenerator4").GetComponent<LabyrinthGenerator4Animated>().width; i++)
            {
                s += " " + corridorsBitmap[i, j];
            }
            Debug.Log(s);
            s = "";
        }
        */




    }







    public IEnumerator drawNodes()
    {
        while (!canDraw)
        {
            yield return null;
        }

        if (!animated)
        {
            delayInGeneration = 0f;
        }
        

        foreach (GNode n in nodesList)
        {
            GameObject g = Instantiate(c.unit);
            g.transform.position = new Vector3(c.x0 + c.unitScale * n.x + c.unitScale / 2, 0, c.z0 + c.unitScale * n.z + c.unitScale / 2);
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
            cubesList.Add(g);
            yield return new WaitForSeconds(delayInGeneration);
        }
    }

    private void findIntersections(int[,] corridorsBitmap)
    {
        //we search among all the points with value 0 in the bitmap
        for(int j = 0; j< c.height; j++)
        {
            for(int i = 0; i < c.width; i++)
            {
                //if the bitmap value is 0, it is a corridor and must be checked.
                //we must also check that, in that position, there isn't a corridor entrance.
                if(corridorBitmap[i,j] == 0 && graph.isNodeAtCoordinates(i,j) == false)
                {
                    int cubesVisible = lookAround(i, j, corridorBitmap);
                    if(cubesVisible >= 3)
                    {
                        GNode n = new GNode(i, j);
                        n.is_intersection = true;
                        graph.AddNode(n);
                        nodesList.Add(n);
                    }
                }
            }
        }
    }

    //function used by FindIntersections to see how many cubes (or rather, how many roomNodes/CorridorEntranceNodes/IntersectionNodes)
    //are around him. This function will look in all the four cardinal directions in respect to the given coordinates, see if one of those cubes is
    //visible (one is enough, if i.e. there are two on the north we are not interesed in the second) and add 1 to the result.
    private int lookAround(int z, int x, int[,] corridorBitmap)
    {
        int res = 0;
        res += lookUp(z, x, corridorBitmap);
        res += lookDown(z, x, corridorBitmap);
        res += lookRight(z, x, corridorBitmap);
        res += lookLeft(z, x, corridorBitmap);
        return res;
    }

    //functions used to look in the four cardinal directions by LookAround
    private int lookUp(int z, int x, int[,] corridorBitmap)
    {
        while (true)
        {
            x = x - 1;
            //first of all, check if we went outside of the dungeon
            if (x < 0) return 0;
            //then, we want to check if there is a node at this coordinates.
            //if there is,then yes, a node is visible in this direction.
            if (graph.isNodeAtCoordinates(z, x)) return 1;
            //if not, check: is this a wall, for our bitmap?
            if(corridorBitmap[z,x] == 1) return 0;
        }
    }

    private int lookDown(int z, int x, int[,] corridorBitmap)
    {
        while (true)
        {
            x = x + 1;
            if (x >= c.height) return 0;
            if (graph.isNodeAtCoordinates(z, x)) return 1;
            if (corridorBitmap[z, x] == 1)return 0;
        }
    }

    private int lookLeft(int z, int x, int[,] corridorBitmap)
    {
        while (true)
        {
            z = z - 1;
            if (z < 0) return 0;
            if (graph.isNodeAtCoordinates(z, x)) return 1;
            if (corridorBitmap[z, x] == 1) return 0;
        }
    }

    private int lookRight(int z, int x, int[,] corridorBitmap)
    {
        while (true)
        {
            z = z + 1;
            if (z >= c.width) return 0;
            if (graph.isNodeAtCoordinates(z, x)) return 1;
            if (corridorBitmap[z, x] == 1) return 0;
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
