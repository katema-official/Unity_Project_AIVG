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

    private bool canDraw;

    // Start is called before the first frame update
    void Start()
    {
        //dungeonBitmap = new int[GameObject.Find("LabyrinthGenerator4").GetComponent<LabyrinthGenerator4Animated>().width, 
        //    GameObject.Find("LabyrinthGenerator4").GetComponent<LabyrinthGenerator4Animated>().height];
        //corridorBitmap = new int[GameObject.Find("LabyrinthGenerator4").GetComponent<LabyrinthGenerator4Animated>().width,
        //    GameObject.Find("LabyrinthGenerator4").GetComponent<LabyrinthGenerator4Animated>().height];
        StartCoroutine(drawNodes());
        
    }

    private void Awake()
    {
        nodesList = new List<GNode>();
        graph = new Graph();
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


        //qui devo chiamare una funzione
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
        LabyrinthGenerator4Animated c = GameObject.Find("LabyrinthGenerator4").GetComponent<LabyrinthGenerator4Animated>();

        print(graph);
        print(nodesList);
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
            yield return new WaitForSeconds(delayInGeneration);
        }





    }




    // Update is called once per frame
    void Update()
    {
        
    }
}
