using PartitioningTree;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LabyrinthGenerator1 : MonoBehaviour
{
    //if I wanted to make life simple, I would just create all my labyrinths
    //that have (0,0) as upper-left coordinate and (x,y) as lower-right coordinate.
    //But since I see the opportunity to make this code more general and I'm
    //a bit masochist, I'll make it so that the caller can choose
    //the upper left coordinates as well.
    //I could also ask the caller the center point of the labyrinth and its
    //width and height, but I see this as a more complicated approach,
    //since it is (at least from my perspective) easier to reason using
    //upper-left and lower-right coordinates of a rectangle shaped space
    //instead of its center and his dimensions.
    //And besides, given one representation, one can easily get the other.

    //the gameobject to use in order to create the labyrinth
    public GameObject unit;

    //heights of the walls of the labyrinth
    public int heightOfWalls = 5;
    //scale (x and z) of the unit of the labyrinth
    public int unitScale = 1;

    

    //upper left coordinates of the labyrinth
    public int z0 = 0;
    public int x0 = 0;

    //lower right coordinates of the labyrinth
    public int z1 = 40;
    public int x1 = 40;

    //the smallest dimensions a room can have
    public int smallestRoomZ = 2;
    public int smallestRoomX = 2;

    //for repeatability
    public int RandomSeed = 0;

    //value gained after some experiments, can be personalized to change
    //the behavior of the algorithm. Check the setMinimumXY method below
    private int minimumZ;
    private int minimumX;
    private int offsetFactorForRooms = 2;

    //just some variables to associate a name to integers, will be useful
    //in generateNode()
    private const int horizontalCutID = 0;
    private const int verticalCutID = 1;

    //a matrix to store the walls of the labyrinth
    private MyArray2OfGameObjects wallsArray;


    // Start is called before the first frame update
    void Start()
    {
        setMinimumZX();
        //for being stochastic or deterministic
        if (RandomSeed == 0) RandomSeed = (int)System.DateTime.Now.Ticks;
        Random.InitState(RandomSeed);

        Node root = generatePartitioningTree();
        //now we have the tree.
        //First of all, let's generate a labyrinth that is only made of walls
        unit.transform.localScale = new Vector3(unitScale, heightOfWalls, unitScale);
        wallsArray = new MyArray2OfGameObjects(z0, z1, x0, x1);
        for (int j = x0; j < x1; j++)
        {
            for (int i = z0; i < z1; i++)
            {
                GameObject newWall = Instantiate(unit);
                newWall.transform.position = new Vector3(i + unitScale / 2, 0, j + unitScale / 2);
                wallsArray.set(i, j, newWall);
            }
        }

        //now we have to remove some of those walls in order to generate our rooms. To do that,
        //we traverse the tree we have built, and when we'll find a leaf, we'll know that
        //all the walls inside that node need to be removed
        generateRooms(root);



    }

    private void setMinimumZX()
    {
        minimumZ = smallestRoomZ * offsetFactorForRooms;
        minimumX = smallestRoomX * offsetFactorForRooms;
    }

    //this will generate our Space Partitioning Tree
    private Node generatePartitioningTree()
    {
        Point p1 = new Point(z0, x0);
        Point p2 = new Point(z1, x1);
        return generateNode(p1, p2, null);  //the node without parent is the root.
    }

    //this recursive function will recursively generate a Node by:
    //1) creating a Node object and setting its two points
    //2) he will see if the node dimensions (width and height) are
    //large enough to allow the node to be partitioned (at least one of them)
    //3a) They are not? Then its children are null and the node is returned
    //3b) At least one of them is partitionable? Then partition randomly
    //this node, create two children, and, after creating the, return this
    //node
    private Node generateNode(Point p1, Point p2, Node parent)
    {
        //Debug.Log("Generated node: (" + p1.z + "," + p1.x + ") -> (" + p2.z + "," + p2.x + ")");
        Node currentNode = new Node(p1, p2, parent);

        int heightOfNode = Mathf.Abs(p1.x - p2.x);
        int widthOfNode = Mathf.Abs(p1.z - p2.z);
        List<int> possibleCutDimensions = new List<int>();

        if(heightOfNode/2 >= minimumX)
        {
            //Debug.Log("Horizontal cut is possible");
            possibleCutDimensions.Add(horizontalCutID);   //means "you can cut horizontally"
            //I use "heightOfNode/2" because I want to know if, by dividing the room along this
            //direction by exactly half, I would be able to get two new nodes that can contain
            //a room that respects the minimum Y constrain. The same applies for the width,
            //see two lines below
        }

        if(widthOfNode/2 >= minimumZ)
        {
            //Debug.Log("Vertical cut is possible");
            possibleCutDimensions.Add(verticalCutID);   //means "you can cut vertically"
        }

        //check: if the List is still empty, it means that no cut is possible, and we
        //have reached a leaf node. This node has no children and must be returned
        if(possibleCutDimensions.Count == 0)
        {
            //Debug.Log("The node: (" + p1.z + "," + p1.x + ") -> (" + p2.z + "," + p2.x + ") is a room");
            currentNode.left_child = null;
            currentNode.right_child = null;
            return currentNode;
        }

        //Debug.Log("Number of possible axis to perform the cut: " + possibleCutDimensions.Count);

        //if the list is not empty, we can choose a random cut axis from it
        int i = Random.Range(0, possibleCutDimensions.Count - 1);
        int cut = possibleCutDimensions[i];

        //Debug.Log("Axis chosen: " + cut);

        int interval = 0;
        Point l1;       //point coordinates for left child
        Point l2;
        Point r1;       //point coordinates for right child
        Point r2;
        switch(cut)
        {
            case horizontalCutID:
                //so, we have to cut horizontally, but the cut has a constrain:
                //the two new nodes generated must be large enough for our room.
                //So the cut can't happen in all places, only in those that can generate
                //two children big enough.
                //Same goes for the vertical cut
                interval = Mathf.Abs(p1.x - p2.x) - minimumX * 2;
                int xCoordinateCut = Random.Range(0, interval);
                xCoordinateCut += minimumX;

                //now that I have the coordinate for the cut, I can generate the two
                //children recursively
                l1 = new Point(p1.z, p1.x);
                l2 = new Point(p2.z, p1.x + xCoordinateCut);
                r1 = new Point(p1.z, p1.x + xCoordinateCut);
                r2 = new Point(p2.z, p2.x);
                currentNode.left_child = generateNode(l1, l2, currentNode);
                currentNode.right_child = generateNode(r1, r2, currentNode);

                break;

            case verticalCutID:
                interval = Mathf.Abs(p1.z - p2.z) - minimumZ * 2;
                int zCoordinateCut = Random.Range(0, interval);
                zCoordinateCut += minimumZ;

                l1 = new Point(p1.z, p1.x);
                l2 = new Point(p1.z + zCoordinateCut, p2.x);
                r1 = new Point(p1.z + zCoordinateCut, p1.x);
                r2 = new Point(p2.z, p2.x);
                currentNode.left_child = generateNode(l1, l2, currentNode);
                currentNode.right_child = generateNode(r1, r2, currentNode);
                break;

            default:
                Debug.Log("Something went wrong when deciding on which dimension to cut");
                break;
        }
        return currentNode;
        
    }

    public void generateRooms(Node root)
    {
        exploreNodeToGenerateRoom(root);
    }

    public void exploreNodeToGenerateRoom(Node current)
    {
        //Debug.Log("(" + current.p1.z + "," + current.p1.x + ") -> (" + current.p2.z + "," + current.p2.x + ")");
        if(current.left_child != null && current.right_child != null){
            //if it is not a leaf node, then explore its childred

            Debug.Log("(" + current.p1.z + "," + current.p1.x + ") -> (" + current.p2.z + "," + current.p2.x + ") recurses");
            exploreNodeToGenerateRoom(current.left_child);
            exploreNodeToGenerateRoom(current.right_child);
        }
        else
        {
            
            //it IS a leaf node:
            //being a leaf node, it means that, inside this node, there has to be a room.
            //How big? Well, to make the labyrinth more "natural" (random) it will have
            //a random dimension between the minimum z and x dimensions and the p2.z-p1.z and p2.x-p1.x.
            //To guarantee that all rooms are completely saperated one from each other, we add a +1
            //to the minimum dimensions and -1 to the maximum dimensions.

            int maxWidthOfThisRoom = Mathf.Abs(current.p1.z - current.p2.z) - 2;
            int maxHeightOfThisRoom = Mathf.Abs(current.p1.x - current.p2.x) - 2;
            int room_z0_min = current.p1.z + 1;
            int room_x0_min = current.p1.x + 1;

            //to ensure the room is larger then the minimum required, I use a max function.
            int width = Random.Range(smallestRoomZ, maxWidthOfThisRoom);
            int height = Random.Range(smallestRoomX, maxHeightOfThisRoom);

            //now we have to choose where to place the upper-left corner of the room (that means,
            //where exactly we want to place the room in the given space)
            //the upper-left corner can't be placed in all places: only in a specific subset
            //of the given space (from the current node points), that is somewhere in the upper-left part
            //of course. In particular, given the width and height, we want our point to:
            //1) be at least one position ahead of the upper-left point of the given space (to avoid collisions
            //with other rooms)
            //2) be at least one position behind of the bottom-right point of the given space (same reason)
            //3) be in a place such that there is enough space, on both axis, to build the desired room
            //without breaking the aforementioned constrains (enough space both on the right and on the bottom)
            int room_z0 = Random.Range(current.p1.z + 1, current.p2.z - 1 - width);
            int room_x0 = Random.Range(current.p1.x + 1, current.p2.x - 1 - height);

            Debug.Log(@"(" + current.p1.z + "," + current.p1.x + ") -> (" + current.p2.z + "," + current.p2.x + ") does not recurse\n" +
                "In particulare, the room it generates is (" + room_z0 + "," + room_x0 + ") -> w=" + width + ", h=" + height);

            //finally, we can remove the objects covering our room space
            for (int j = room_x0; j < room_x0 + height; j++)
            {
                for(int i = room_z0; i < room_z0 + width; i++)
                {
                    Debug.Log("Removing (" + i + "," + j + ")");
                    Destroy(wallsArray.get(i, j));
                    wallsArray.set(i, j, null);
                }
            }
        }

    }


    // Update is called once per frame
    void Update()
    {
        
    }
}




