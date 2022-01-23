using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PartitioningTree2;

public class LabyrinthGenerator2CR : MonoBehaviour
{
    //----------VARIABLES FOR GENERATING THE ROOMS----------

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

    //the smallest dimensions a PARTITION can have
    public int smallestPartitionZ = 4;
    public int smallestPartitionX = 4;

    //boolean to specify if the rooms generated have to be separated (so
    //that only corridors can allow rooms to communicate) or not (two randomly
    //generated rooms can be directly connected thanks to the absence of a wall)
    public bool roomsMustBeSeparated = true;

    //the user can specify a minimum width and height that the generated rooms
    //must have. If 0 is supplied, then the rooms are truly random generated (this
    //might be a problem because somw leaf nodes of the tree could generate rooms
    //of 0 length in some axis. We can avoid this by always giving a value that is
    //greater than 0, so 1 or above will do it).
    //If a value greater then smallestPartitionZ/X is supplied, it is rounded
    //to smallestPartitionZ/X
    public int minimumRoomZ = 1;
    public int minimumRoomX = 1;

    //for repeatability
    public int RandomSeed = 0;

    //a matrix to store the walls of the labyrinth
    private MyArray2OfGameObjects wallsArray;

    //----------VARIABLES TO GENERATE THE CORRIDORS----------

    //what is the minimum width a corridor can have?
    public int minimumCorridorWidth = 1;
    //what is the maximum width a corridor can have?
    public int maximumCorridorWitdh = 1;

    //if there has to be a 90-degree corridor, do you want it to have its two corridors of
    //(possibly) different width or the same?
    public bool angleCorridorsHaveSameWidth = true;

    //in the generation of a corridor, this might happen:
    //even though there could be a corridor between minimumCorridorWidth and maximumCorridorWitdh
    //to directly connect the two rooms, the random process generated a width that, to be allowed, requires
    //a 90-degree corridor. Do you want this to happen, or do you want to "occam rasor" it and
    //just allow direct corridors to be generated, if possible?
    public bool allowSimplerCorridors = false;


    // Start is called before the first frame update
    void Start()
    {
        //----------ROOMS INITIALIZATION----------

        //first things first: we have some variables that could create some conflicts:
        //let's imagine that our client asked us to:
        //1) have as smallest partition a value of 6 for both Z and X
        //2) roomsMustBeSeparated = true
        //3) have a required minimum room size for both Z and X a value greater or equal then 5
        //Those constrains can't be satisfied, so, when this happens, we simply set
        //roomsMustBeSeparated to false. It is responsibility of the client to give us
        //meaningful values
        if (minimumRoomZ >= smallestPartitionZ - 1 || minimumRoomX >= smallestPartitionX - 1)
        {
            roomsMustBeSeparated = false;
        }
        //oh and also, if the required minimum room size is greater then the smallest partition value...
        //guess what.
        minimumRoomZ = minimumRoomZ > smallestPartitionZ ? smallestPartitionZ : minimumRoomZ;
        minimumRoomX = minimumRoomX > smallestPartitionX ? smallestPartitionX : minimumRoomX;

        //for being stochastic or deterministic
        if (RandomSeed == 0) RandomSeed = (int)System.DateTime.Now.Ticks;
        Random.InitState(RandomSeed);

        Node root = generatePartitioningTree();
        //now we have the tree.
        //First of all, let's generate a labyrinth that is only made of walls
        unit.transform.localScale = new Vector3(unitScale, heightOfWalls, unitScale);
        wallsArray = new MyArray2OfGameObjects(z0, z1, x0, x1);
        for (int j = z0; j < z1; j++)
        {
            for (int i = x0; i < x1; i++)
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



        //----------CORRIDORS INITIALIZATION----------
        Stack stack = new Stack();
        generateCorridors(root, stack);


    }

    //----------LOGIC FOR GENERATING THE ROOMS-----------

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

        if (heightOfNode / 2 >= smallestPartitionX)
        {
            //Debug.Log("Horizontal cut is possible");
            possibleCutDimensions.Add(PTConstants.horizontalCutID);   //means "you can cut horizontally"
            //I use "heightOfNode/2" because I want to know if, by dividing the room along this
            //direction by exactly half, I would be able to get two new nodes that can contain
            //a room that respects the minimum X constrain. The same applies for the width,
            //see two lines below
        }

        if (widthOfNode / 2 >= smallestPartitionZ)
        {
            //Debug.Log("Vertical cut is possible");
            possibleCutDimensions.Add(PTConstants.verticalCutID);   //means "you can cut vertically"
        }

        //check: if the List is still empty, it means that no cut is possible, and we
        //have reached a leaf node. This node has no children and must be returned
        if (possibleCutDimensions.Count == 0)
        {
            //Debug.Log("The node: (" + p1.z + "," + p1.x + ") -> (" + p2.z + "," + p2.x + ") is a room");
            currentNode.left_child = null;
            currentNode.right_child = null;
            return currentNode;
        }

        //Debug.Log("Number of possible axis to perform the cut: " + possibleCutDimensions.Count);

        //if the list is not empty, we can choose a random cut axis from it
        int i = Random.Range(0, possibleCutDimensions.Count);
        int cut = possibleCutDimensions[i];

        Debug.Log(depth + "> Axis chosen: " + cut + ", i choose one of " + possibleCutDimensions.Count + " possible values");
        depth += 1;

        int interval = 0;
        Point l1;       //point coordinates for left child
        Point l2;
        Point r1;       //point coordinates for right child
        Point r2;
        currentNode.cutOrientation = cut;       //to store what kind of cut is the one done on this node
        switch (cut)
        {
            case PTConstants.horizontalCutID:
                //so, we have to cut horizontally, but the cut has a constrain:
                //the two new nodes generated must be large enough for our room.
                //So the cut can't happen in all places, only in those that can generate
                //two children big enough.
                //Same goes for the vertical cut
                interval = Mathf.Abs(p1.x - p2.x) - smallestPartitionX * 2;
                int xCoordinateCut = Random.Range(0, interval + 1);
                xCoordinateCut += smallestPartitionX;

                //now that I have the coordinate for the cut, I can generate the two
                //children recursively
                l1 = new Point(p1.z, p1.x);
                l2 = new Point(p2.z, p1.x + xCoordinateCut);
                r1 = new Point(p1.z, p1.x + xCoordinateCut);
                r2 = new Point(p2.z, p2.x);
                currentNode.left_child = generateNode(l1, l2, currentNode);
                currentNode.right_child = generateNode(r1, r2, currentNode);

                //debug
                gizmosVectors.Add(new Point[] { new Point(p2.z, p1.x + xCoordinateCut), new Point(p1.z, p1.x + xCoordinateCut) });

                break;

            case PTConstants.verticalCutID:
                interval = Mathf.Abs(p1.z - p2.z) - smallestPartitionZ * 2;
                int zCoordinateCut = Random.Range(0, interval + 1);
                zCoordinateCut += smallestPartitionZ;

                l1 = new Point(p1.z, p1.x);
                l2 = new Point(p1.z + zCoordinateCut, p2.x);
                r1 = new Point(p1.z + zCoordinateCut, p1.x);
                r2 = new Point(p2.z, p2.x);
                currentNode.left_child = generateNode(l1, l2, currentNode);
                currentNode.right_child = generateNode(r1, r2, currentNode);

                //debug
                gizmosVectors.Add(new Point[] { new Point(p1.z + zCoordinateCut, p2.x), new Point(p1.z + zCoordinateCut, p1.x) });

                break;

            default:
                Debug.Log("Something went wrong when deciding on which dimension to cut (should never happen)");
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
        if (current.left_child != null && current.right_child != null)
        {
            //if it is not a leaf node, then explore its childred

            //Debug.Log("(" + current.p1.z + "," + current.p1.x + ") -> (" + current.p2.z + "," + current.p2.x + ") recurses");
            exploreNodeToGenerateRoom(current.left_child);
            exploreNodeToGenerateRoom(current.right_child);
        }
        else
        {

            //it IS a leaf node:
            //being a leaf node, it means that, inside this node, there has to be a room.
            //To generate a room, we pick two random points inside the given area that
            //respects all the constrains we might have (roomsMustBeSeparated, minimumRoomZ
            //and minimumRoomX).
            //let's then write two functions (both will work for the Z and X coordinate):
            //1) one that tells us where is the upper-left coordinate of the point
            //2) one that tells us its length in a certain axis


            //To generate a room, we pick two random points inside the given area (the
            //two points though must be INSIDE the area, not on the edges, or else we might
            //have two rooms that are directly connected to each other, and that's not what
            //I want).
            //So, let's consider those constrains while calculating the room points
            int actualPartitionZ = Mathf.Abs(current.p1.z - current.p2.z);
            int actualPartitionX = Mathf.Abs(current.p1.x - current.p2.x);
            int room_z0 = obtainStartingCoordinateForRoom(current.p1.z, minimumRoomZ, actualPartitionZ, roomsMustBeSeparated);
            int room_x0 = obtainStartingCoordinateForRoom(current.p1.x, minimumRoomX, actualPartitionX, roomsMustBeSeparated);
            //given the point, I can randomly calculate the length of the room in that axis following the constrains
            int z_length = obtainLengthForRoom(current.p1.z, minimumRoomZ, actualPartitionZ, roomsMustBeSeparated, room_z0);
            int x_length = obtainLengthForRoom(current.p1.x, minimumRoomX, actualPartitionX, roomsMustBeSeparated, room_x0);

            //now, for this leaf node, i can set its room points
            current.setRoomPoints(new Point(room_z0, room_x0), new Point(room_z0 + z_length, room_x0 + x_length));
            //we don't need to set the cut orientation: we did that during the tree generation

            //Debug.LogFormat("In space ({0},{1}) -> ({2},{3}), the chosen point is ({4},{5}) with length <{6},{7}>", current.p1.z, current.p1.x, current.p2.z, current.p2.x,
            //    room_z0, room_x0, z_length, x_length);


            //finally, we can remove the objects covering our room space
            for (int j = room_x0; j < room_x0 + x_length; j++)
            {
                for (int i = room_z0; i < room_z0 + z_length; i++)
                {
                    //Debug.Log("Removing (" + i + "," + j + ")");
                    Destroy(wallsArray.get(i, j));
                    wallsArray.set(i, j, null);
                }
            }
        }

    }

    private int obtainStartingCoordinateForRoom(int minCoordinate, int minLength, int partitionLength, bool roomsMustBeSeparated)
    {
        int minInclusive = minCoordinate;
        int maxInclusive = minCoordinate + partitionLength;
        if (roomsMustBeSeparated)
        {
            minInclusive += 1;
            maxInclusive -= 1;      //so that I won't touch the walls

        }
        maxInclusive -= minLength;  //this way I'm sure that the point I'll give will allow me to generate a room large enough as required
        return Random.Range(minInclusive, maxInclusive + 1);
    }

    private int obtainLengthForRoom(int minCoordinate, int minLength, int partitionLength, bool roomsMustBeSeparated, int startingValue)
    {
        int maxValue = minCoordinate + partitionLength;
        if (roomsMustBeSeparated)
        {
            maxValue -= 1;
        }
        //the length of the room has a minimum, and the maximum possible value is the distance between the startingPoint
        return Random.Range(minLength, Mathf.Abs(maxValue - startingValue) + 1);

    }



    //----------LOGIC FOR GENERATING THE CORRIDORS----------

    //now that we have our beautiful tree with his nodes and rooms, we have to generate the corridors.
    //to do that, we use the constrains imposed by the user.

    //this recursive function will return to the current node the room coordinates of one of its children
    private IEnumerator generateCorridors(Node current, Stack stack)
    {
        //base case: the node doesn't have any child (it is a "full" room, meaning a room where all the points
        //between the upper-left corner and the bottom-right one are part of the room), so he can return those
        //two points. Nothing more. It's great when things are this simple, aren't they?
        if (current.left_child == null && current.right_child == null)
        {
            stack.Push(new Point[] { current.room_p1, current.room_p2 });
            yield return null;
        }

        //if the node is not a leaf, then, first of all, he must ask to both of his children the upper-left
        //point and lower-left point of their respective rooms, to then merge them together.
        generateCorridors(current.left_child, stack);
        generateCorridors(current.right_child, stack);
        Point[] leftChildRoomPoints = (Point[]) stack.Pop();
        Point[] rightChildRoomPoints = (Point[]) stack.Pop();

        yield return new WaitForSeconds(3000);

        //now we set the two points for this merged room:
        current.setRoomPoints(leftChildRoomPoints[0], rightChildRoomPoints[1]);
        Debug.Log("My room points are " + current.room_p1.ToString() + " and " + current.room_p2.ToString());



        //now, this is where things get complicated. We have to distingush two scenarios:
        //1) we can generate a direct corridor between these two rooms
        //2) we have to generate a L shaped corridor (help)
        //but, simple things first: what will be the width of this corridor?
        int corridorWidth = Random.Range(minimumCorridorWidth, maximumCorridorWitdh + 1);
        Debug.Log("corridorWidth = " + corridorWidth);

        //ok, that's the width. Now let's see: if we were to create a direct corridor between these two rooms (the
        //case we are all hoping for), what's the space we have at hand to do that, relatively also to the cut orientation?
        int minSearch = 0;
        int maxSearch = 0;
        int minBound = 0;
        int maxBound = 0;
        switch (current.cutOrientation)
        {
            case PTConstants.horizontalCutID:
                //if the cut was done horizontally, then we have to search all the possible "columns" eligible for building a direct corridor.
                minSearch = Mathf.Min(leftChildRoomPoints[0].z, leftChildRoomPoints[1].z,
                    rightChildRoomPoints[0].z, rightChildRoomPoints[1].z);    //because idk how those two rooms are disposed in the space
                maxSearch = Mathf.Max(leftChildRoomPoints[0].z, leftChildRoomPoints[1].z,
                    rightChildRoomPoints[0].z, rightChildRoomPoints[1].z);

                //now, for each possible z value between the minimum and the maximum, check: is that coordinate, that
                //represents a column, one on which both rooms lie for at least one unit?
                List<int> available_Z_coordinates = new List<int>();
                for (int i = minSearch; i < maxSearch; i++)
                {
                    //MIGHT (NOT) BE LESS EQUALS
                    if (leftChildRoomPoints[0].z <= i && i <= leftChildRoomPoints[1].z && rightChildRoomPoints[0].z <= i && i <= rightChildRoomPoints[1].z)
                    {
                        available_Z_coordinates.Add(i);
                    }
                }

                //now, the million dollar question: is that space enough to contain the randomly generated corridor width?
                if (available_Z_coordinates.Count >= corridorWidth)
                {
                    //yes! Then, in some of that space, you can generate a direct corridor. Now, just choose the actual boundaries
                    //of the corridor
                    int[] boundaryCoordinates = generateDirectCorridorBoundCoordinates(available_Z_coordinates, corridorWidth);

                    //so, now we have the coordinates representing the columns to be emptied in order to join the two rooms.
                    //Even so, actually connecting the two rooms requries a bit of attenction, since we want to remove the space
                    //that is between the two rooms, not the columns as a whole.
                    //To do the cut, we procees as following:
                    //1) Given that we know the coordinate of the horizontal cut (a value on the x axis) that is stored on the current node,
                    //we first place ourselves, for each z coordinate that we have (the columns), in that point. Then, we "dig" upwards (removing
                    //all the units we find) untill we find a room. Same goes for digging downwards.

                    //first: dig upwards
                    int xMiddle = current.cutWhere;
                    int x = 0;
                    for(int columnIndex = boundaryCoordinates[0]; columnIndex < boundaryCoordinates[1]; columnIndex++)
                    {
                        x = xMiddle;
                        while(wallsArray.get(columnIndex, x) != null)
                        {
                            Destroy(wallsArray.get(columnIndex, x));
                            wallsArray.set(columnIndex, x, null);
                            x--;        //I go upwards
                        }
                    }

                    //second, dig downwards
                    for (int columnIndex = boundaryCoordinates[0]; columnIndex < boundaryCoordinates[1]; columnIndex++)
                    {
                        x = xMiddle + 1;        //because we destroyed the units on xMiddle when we dug up
                        while (wallsArray.get(columnIndex, x) != null)
                        {
                            Destroy(wallsArray.get(columnIndex, x));
                            wallsArray.set(columnIndex, x, null);
                            x++;        //I go upwards
                        }
                    }


                    /*
                    minBound = Mathf.Min(leftChildRoomPoints[0].x, leftChildRoomPoints[1].x,
                        rightChildRoomPoints[0].x, rightChildRoomPoints[1].x);
                    maxBound = Mathf.Max(leftChildRoomPoints[0].x, leftChildRoomPoints[1].x,
                        rightChildRoomPoints[0].x, rightChildRoomPoints[1].x);

                    Debug.LogFormat("the current room has coordinates {0} and {1}, and min x = {2}, max x = {3}", current.room_p1.ToString(), current.room_p2.ToString(), minBound, maxBound);

                    //very simple strategy to actually dig this corridor: start from the top. Until you find an empty space, don't dig.
                    //Then, after finding it, start digging. You will eventually find a wall. From there, keep digging. As soon as you
                    //find another empty space, it means that you've found the other room. Great, now you can stop digging, at least
                    //for that column. Let's represent this behaviour using a int, it will suffice.

                    //0 = I still haven't encountered an empty space
                    //1 = I have found an empty space, so I started digging
                    //2 = I found a full space, finally I'm digging for real
                    //3 = I found an empty space, it means that i reached the other room, so I can stop finally.
                    //the state "3" isn't really ever reached, since, what that happens, the current for is broken (break;)
                    //and the state goes back to 0.
                    int state = 0;


                    for (int columnIndex = boundaryCoordinates[0]; columnIndex < boundaryCoordinates[1]; columnIndex++) //for all columns...
                    {
                        state = 0;
                        for (int i = minBound; i < maxBound; i++)       //for all the cells inside this column...
                        {

                            if (state == 0 && wallsArray.get(columnIndex, i) == null)
                            {
                                //i find the first room
                                state = 1;

                            }
                            else if (state == 1 && wallsArray.get(columnIndex, i) != null)
                            {
                                //i find the wall to actually dig
                                state = 2;
                                Destroy(wallsArray.get(columnIndex, i));
                                wallsArray.set(columnIndex, i, null);       //I destroy this tile

                            }
                            else if (state == 2 && wallsArray.get(columnIndex, i) != null)
                            {
                                //I'm digging baby
                                Destroy(wallsArray.get(columnIndex, i));
                                wallsArray.set(columnIndex, i, null);

                            }
                            else if (state == 2 && wallsArray.get(columnIndex, i) == null)
                            {
                                //whoops, I dug too much, I reached the other room- wait, that's great! I finished!
                                state = 0;

                            }

                        }
                    }

                */
                }
                //or, if that space is NOT enough to contain the randomly generated corridor width,
                //is this space enough to contain another corridor, if the user allowed simpler corridors?





                break;
            case PTConstants.verticalCutID:

                break;
        }





        //in the end, after having merged the two rooms, we can return to our parent the coordinates (in points)
        //of our new room.
        //return new Point[] { current.room_p1, current.room_p2 };
    }

    private int[] generateDirectCorridorBoundCoordinates(List<int> listOfAvailableCoordinates, int requiredWidth)
    {
        int leftWall = Random.Range(listOfAvailableCoordinates[0], listOfAvailableCoordinates[listOfAvailableCoordinates.Count - 1] - requiredWidth + 1);
        int rightWall = leftWall + requiredWidth;
        Debug.Log("leftWall = " + leftWall + ", rightWall = " + rightWall);
        return new int[] { leftWall, rightWall };
    }












    // Update is called once per frame
    void Update()
    {

    }

    //VISIBLE DEBUG
    private static List<Point[]> gizmosVectors = new List<Point[]>();
    private int depth = 0;

    public void OnDrawGizmos()
    {

        foreach (Point[] points in gizmosVectors)
        {
            if (points[0].z == points[1].z)
            {
                Gizmos.color = Color.red;
            }
            else if (points[0].x == points[1].x)
            {
                Gizmos.color = Color.blue;
            }
            Gizmos.DrawLine(
                new Vector3(points[0].x, 2, points[0].z),
                new Vector3(points[1].x, 2, points[1].z));
        }
    }
}


public class StackRecursivePointsArray
{
    private List<Point[]> stack;

    public StackRecursivePointsArray()
    {
        stack = new List<Point[]>();
    }

    public void push(Point[] arr)
    {
        stack.Add(arr);
    }

    public Point[] Pop()
    {
        Point[] res = stack[stack.Count - 1];
        stack.RemoveAt(stack.Count - 1);
        return res;
    }


}

