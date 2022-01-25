using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PartitioningTree2;

public class LabyrinthGenerator2 : MonoBehaviour
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
        if(minimumRoomZ >= smallestPartitionZ - 1 || minimumRoomX >= smallestPartitionX - 1){
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
                wallsArray.set(j, i, newWall);
            }
        }

        //now we have to remove some of those walls in order to generate our rooms. To do that,
        //we traverse the tree we have built, and when we'll find a leaf, we'll know that
        //all the walls inside that node need to be removed
        generateRooms(root);



        //----------CORRIDORS INITIALIZATION----------
        generateCorridors(root);


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
            possibleCutDimensions.Add(PTConstants.horizontalCutID);   //means "you can cut horizontally"
            //I use "heightOfNode/2" because I want to know if, by dividing the room along this
            //direction by exactly half, I would be able to get two new nodes that can contain
            //a room that respects the minimum X constrain. The same applies for the width,
            //see two lines below
        }

        if (widthOfNode / 2 >= smallestPartitionZ)
        {
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

        //if the list is not empty, we can choose a random cut axis from it
        int i = Random.Range(0, possibleCutDimensions.Count);
        int cut = possibleCutDimensions[i];

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

                //Let's also store the coordinate, on the x axis, of this horizontal cut.
                currentNode.cutWhere = p1.x + xCoordinateCut;

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

                currentNode.cutWhere = p1.z + zCoordinateCut;

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
            //debug
            roomsConnected.Add(new Point[] { new Point(room_z0, room_x0), new Point(room_z0 + z_length, room_x0 + x_length)});

            //we don't need to set the cut orientation: we did that during the tree generation

            //Debug.LogFormat("In space ({0},{1}) -> ({2},{3}), the chosen point is ({4},{5}) with length <{6},{7}>", current.p1.z, current.p1.x, current.p2.z, current.p2.x,
            //    room_z0, room_x0, z_length, x_length);


            //finally, we can remove the objects covering our room space
            for (int j = room_x0; j < room_x0 + x_length; j++)
            {
                for (int i = room_z0; i < room_z0 + z_length; i++)
                {
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
    private Point[] generateCorridors(Node current)
    {
        //base case: the node doesn't have any child (it is a "full" room, meaning a room where all the points
        //between the upper-left corner and the bottom-right one are part of the room), so he can return those
        //two points. Nothing more. It's great when things are this simple, aren't they?
        if (current.left_child == null && current.right_child == null)
        {
            return new Point[] {current.room_p1, current.room_p2};
        }

        //if the node is not a leaf, then, first of all, he must ask to both of his children the upper-left
        //point and lower-left point of their respective rooms, to then merge them together.
        Point[] leftChildRoomPoints = generateCorridors(current.left_child);
        Point[] rightChildRoomPoints = generateCorridors(current.right_child);

        //now we set the two points for this merged room.
        int minZ = Mathf.Min(leftChildRoomPoints[0].z, leftChildRoomPoints[1].z,
                    rightChildRoomPoints[0].z, rightChildRoomPoints[1].z);
        int minX = Mathf.Min(leftChildRoomPoints[0].x, leftChildRoomPoints[1].x,
                    rightChildRoomPoints[0].x, rightChildRoomPoints[1].x);
        int maxZ = Mathf.Max(leftChildRoomPoints[0].z, leftChildRoomPoints[1].z,
                    rightChildRoomPoints[0].z, rightChildRoomPoints[1].z);
        int maxX = Mathf.Max(leftChildRoomPoints[0].x, leftChildRoomPoints[1].x,
                    rightChildRoomPoints[0].x, rightChildRoomPoints[1].x);

        Point top_left = new Point(minZ, minX);
        Point bottom_right = new Point(maxZ, maxX);
        current.setRoomPoints(top_left, bottom_right);
        roomsConnected.Add(new Point[] { top_left, bottom_right });



        //now, this is where things get complicated. We have to distingush two scenarios:
        //1) we can generate a direct corridor between these two rooms
        //2) we have to generate a L shaped corridor (help)
        //but, simple things first: what will be the width of this corridor?
        int corridorWidth = Random.Range(minimumCorridorWidth, maximumCorridorWitdh + 1);

        //ok, that's the width. Now let's see: if we were to create a direct corridor between these two rooms (the
        //case we are all hoping for), what's the space we have at hand to do that, relatively also to the cut orientation?
        int minSearch = 0;
        int maxSearch = 0;
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
                for(int i = minSearch; i < maxSearch; i++)
                {
                    if(leftChildRoomPoints[0].z <= i && i < leftChildRoomPoints[1].z && rightChildRoomPoints[0].z <= i && i < rightChildRoomPoints[1].z)
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

                    LVectors.Add(new Point[] { new Point(boundaryCoordinates[0], current.cutWhere), new Point(boundaryCoordinates[1], current.cutWhere) });

                    //so, now we have the coordinates representing the columns to be emptied in order to join the two rooms.
                    //Even so, actually connecting the two rooms requries a bit of attenction, since we want to remove the space
                    //that is between the two rooms, not the columns as a whole.
                    //To do the cut, we procees as following:
                    //1) Given that we know the coordinate of the horizontal cut (a value on the x axis) that is stored on the current node,
                    //we first place ourselves, for each z coordinate that we have (the columns), in that point. Then, we "dig" upwards (removing
                    //all the units we find) untill we find a room. Same goes for digging downwards.
                    generateVerticalCorridorFromCut(current.cutWhere, boundaryCoordinates);

                }
                else
                {
                    if (allowSimplerCorridors && available_Z_coordinates.Count >= minimumCorridorWidth) 
                    {
                        //or, if that space is NOT enough to contain the randomly generated corridor width:
                        //is this space enough to contain another corridor, if the user allowed simpler corridors?
                        int[] boundaryCoordinates = new int[] { available_Z_coordinates[0], available_Z_coordinates[available_Z_coordinates.Count - 1] };
                        generateVerticalCorridorFromCut(current.cutWhere, boundaryCoordinates);
                    }
                    else
                    {
                        //this is where things get reaaaaally messy, because it is explicitly required to create a L-shaped corridor 
                        //It is in general not easy, because, to do this, we have to make some calculations based on the position of the two rooms.
                        //We'll also have to distinguish two kind of corridors: the ones that have the a constant width, and those that can have
                        //two different widths. It depends on the variable angleCorridorsHaveSameWidth.
                        //So... let's start by getting an idea of where those rooms are placed.
                        //In general, those two rooms can be:
                        //1) the one on the left is in the upper part of the cut, the right one is in the lowe part
                        //2) viceversa
                        //According to this displacement, we'll have to dig first down or up, and then, in both cases, to the right.

                        Point[] roomOnTheLeft;
                        Point[] roomOnTheRight;

                        if(leftChildRoomPoints[0].z <= rightChildRoomPoints[0].z)
                        {
                            roomOnTheLeft = leftChildRoomPoints;
                            roomOnTheRight = rightChildRoomPoints;
                        }
                        else
                        {
                            roomOnTheLeft = rightChildRoomPoints;
                            roomOnTheRight = leftChildRoomPoints;
                        }

                        //DigLShapedCorridorForHorizontalCut(roomOnTheLeft, roomOnTheRight, corridorWidth);
                    }
                }

                break;
            case PTConstants.verticalCutID:
                minSearch = Mathf.Min(leftChildRoomPoints[0].x, leftChildRoomPoints[1].x,
                    rightChildRoomPoints[0].x, rightChildRoomPoints[1].x);
                maxSearch = Mathf.Max(leftChildRoomPoints[0].x, leftChildRoomPoints[1].x,
                    rightChildRoomPoints[0].x, rightChildRoomPoints[1].x);

                List<int> available_X_coordinates = new List<int>();
                for (int i = minSearch; i < maxSearch; i++)
                {
                    if (leftChildRoomPoints[0].x <= i && i < leftChildRoomPoints[1].x && rightChildRoomPoints[0].x <= i && i < rightChildRoomPoints[1].x)
                    {
                        available_X_coordinates.Add(i);
                    }
                }

                if (available_X_coordinates.Count >= corridorWidth)
                {
                    int[] boundaryCoordinates = generateDirectCorridorBoundCoordinates(available_X_coordinates, corridorWidth);
                    LVectors.Add(new Point[] {new Point(current.cutWhere, boundaryCoordinates[0]), new Point(current.cutWhere, boundaryCoordinates[1])});
                    generateHorizontalCorridorFromCut(current.cutWhere, boundaryCoordinates);
                }
                else
                {
                    if (allowSimplerCorridors && available_X_coordinates.Count >= minimumCorridorWidth)
                    {
                        int[] boundaryCoordinates = new int[] { available_X_coordinates[0], available_X_coordinates[available_X_coordinates.Count - 1] };
                        generateHorizontalCorridorFromCut(current.cutWhere, boundaryCoordinates);
                    }
                    else
                    {
                        Point[] roomOnTheLeft;
                        Point[] roomOnTheRight;

                        if (leftChildRoomPoints[0].z <= rightChildRoomPoints[0].z)
                        {
                            roomOnTheLeft = leftChildRoomPoints;
                            roomOnTheRight = rightChildRoomPoints;
                        }
                        else
                        {
                            roomOnTheLeft = rightChildRoomPoints;
                            roomOnTheRight = leftChildRoomPoints;
                        }

                        //DigLShapedCorridorForVerticalCut(roomOnTheLeft, roomOnTheRight, corridorWidth);

                    }
                }
                break;
        }
        




        //in the end, after having merged the two rooms, we can return to our parent the coordinates (in points)
        //of our new room.
        return new Point[] { current.room_p1, current.room_p2 };
    }

    private int[] generateDirectCorridorBoundCoordinates(List<int> listOfAvailableCoordinates, int requiredWidth)
    {
        int leftWall = Random.Range(listOfAvailableCoordinates[0], listOfAvailableCoordinates[listOfAvailableCoordinates.Count - 1] - requiredWidth + 1);
        int rightWall = leftWall + requiredWidth;
        //Debug.Log("leftWall = " + leftWall + ", rightWall = " + rightWall);
        return new int[] { leftWall, rightWall };
    }


    private void generateHorizontalCorridorFromCut(int zMiddle, int[] boundaryCoordinates)
    {
        //first: dig left
        int z = zMiddle - 1;
        bool reached = false;
        while (!reached) {
            for (int rowIndex = boundaryCoordinates[0]; rowIndex < boundaryCoordinates[1]; rowIndex++)
            {
                reached = Dig(z, rowIndex, 1, Directions.left) || reached;
            }
            z--;
        }

        //second: dig right
        z = zMiddle;
        reached = false;
        while (!reached)
        {
            for (int rowIndex = boundaryCoordinates[0]; rowIndex < boundaryCoordinates[1]; rowIndex++)
            {
                reached = Dig(z, rowIndex, 1, Directions.right) || reached;
            }
            z++;
        }


        /*
        //first: dig left
        int z = 0;
        for (int rowIndex = boundaryCoordinates[0]; rowIndex < boundaryCoordinates[1]; rowIndex++)
        {
            z = zMiddle;
            while (wallsArray.get(z, rowIndex) != null)
            {
                Destroy(wallsArray.get(z, rowIndex));
                wallsArray.set(z, rowIndex, null);
                z--;
            }
        }

        //second: dig right
        for (int rowIndex = boundaryCoordinates[0]; rowIndex < boundaryCoordinates[1]; rowIndex++)
        {
            z = zMiddle + 1;
            while (wallsArray.get(z, rowIndex) != null)
            {
                Destroy(wallsArray.get(z, rowIndex));
                wallsArray.set(z, rowIndex, null);
                z++;
            }
        }

        */



        
    }

    private void generateVerticalCorridorFromCut(int xMiddle, int[] boundaryCoordinates)
    {
        //first: dig up
        int x = xMiddle - 1;
        bool reached = false;
        while (!reached)
        {
            for (int columnIndex = boundaryCoordinates[0]; columnIndex < boundaryCoordinates[1]; columnIndex++)
            {
                reached = Dig(columnIndex, x, 1, Directions.up) || reached;
            }
            x--;
        }

        //second: dig down
        x = xMiddle;
        reached = false;
        while (!reached)
        {
            for (int columnIndex = boundaryCoordinates[0]; columnIndex < boundaryCoordinates[1]; columnIndex++)
            {
                reached = Dig(columnIndex, x, 1, Directions.down) || reached;
            }
            x++;
        }

        /*
        //first: dig upwards
        int x = 0;
        for (int columnIndex = boundaryCoordinates[0]; columnIndex < boundaryCoordinates[1]; columnIndex++)
        {
            x = xMiddle;
            while (wallsArray.get(columnIndex, x) != null)
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
                x++;        //I go downwards
            }
        }
        */
    }

    //function that, given a starting point (z,x), a distance to dig and a direction to dig, will start digging
    //in that direction until:
    //1) It has dug as much as required. In this case, it returns false.
    //2) It has found an empty space (a room). In this case, it returns true.

    //there is a small problem here, for how we have built all of this: we have to consider that,
    //sometimes, the starting coordinates are not in contact with an empty space (that is, the actual room).
    //The same can happen when it stops to dig, and we assumed we found a room.
    //When that happens, this dig function should make sure to connect the tunnel tho the room we have in mind.
    //for this reason, I add two booleans:
    //1) reachStartingRoom: true when we have just started to dig and we want to make sure the tunnel is actually connected
    //to our starting room.
    //2) reachEndingRoom: true when we assume that this tunnel will be the last one we have to dig in order to reach
    //the destination room. 
    private bool Dig(int startZ, int startX, int distanceToDig, Directions direction)
    {

        switch (direction)
        {
            case Directions.up:
                Debug.LogFormat("I have to dig up. Start from ({0},{1}). For {2} steps.", startZ, startX, distanceToDig);
                for(int i = 0; i > -distanceToDig; i--)
                {
                    if(wallsArray.get(startZ, startX + i) != null)
                    {
                        Destroy(wallsArray.get(startZ, startX + i));
                        wallsArray.set(startZ, startX + i, null);
                    }else
                    {
                        //Debug.Log("UP RETURNED TRUE");
                        return true;}

                }
                break;

            case Directions.right:
                for (int j = 0; j < distanceToDig; j++)
                {
                    if(wallsArray.get(startZ + j, startX) != null)
                    {
                        Destroy(wallsArray.get(startZ + j, startX));
                        wallsArray.set(startZ + j, startX, null);
                    }else{return true;}
                }
                break;

            case Directions.down:
                for (int i = 0; i < distanceToDig; i++)
                {
                    if (wallsArray.get(startZ, startX + i) != null)
                    {
                        Destroy(wallsArray.get(startZ, startX + i));
                        wallsArray.set(startZ, startX + i, null);
                    }else{return true;}
                }
                break;

            case Directions.left:
                for (int j = 0; j > -distanceToDig; j--)
                {      
                    if (wallsArray.get(startZ + j, startX) != null)
                    {
                        Destroy(wallsArray.get(startZ + j, startX));
                        wallsArray.set(startZ + j, startX, null);
                    }else{return true;}
                }
                break;
        }
        return false;
    }

    //function to dig an L-shaped corridor between two rooms separated by a horizontal cut
    private void DigLShapedCorridorForHorizontalCut(Point[] leftRoom, Point[] rightRoom, int corridorWidth)
    {

        Debug.Log("HI THERE! The rooms i'm connecting are " + leftRoom[0].AsString() + " and " + rightRoom[0].AsString());

        int necessaryDig1;
        int necessaryDig2;
        int randomDig;
        bool finished = false;

        //we have to dig down/up first, and then (maybe) to the right.
        //much depends on whether the user wants those corridors to have the same width or they can have two different widths.
        //Now, while we have allowed a certain level of generality and freedom up until now, to actually build this freakin corridor,
        //we now have to impose our own rules. If we are required to build a corridor large 5, but the room's length along the z axis
        //is 2, we simply can't have a corridor like that! So, to make our lives simpler, here we are allowed to break, if necessary,
        //the minimum corridor width constrains imposed by the client. After all it is him who gave us unreasonable values, come on.
        int firstCorridorWidth = Mathf.Min(corridorWidth, leftRoom[1].z - leftRoom[0].z);

        //the second corridor can be randomly generated, or not
        int secondCorridorWidth = angleCorridorsHaveSameWidth ? firstCorridorWidth : Random.Range(minimumCorridorWidth, maximumCorridorWitdh + 1);

        //same thing as before.
        secondCorridorWidth = Mathf.Min(secondCorridorWidth, rightRoom[1].x - rightRoom[0].x);

        //calculate part of the dig depth
        necessaryDig2 = secondCorridorWidth;
        randomDig = Random.Range(0, rightRoom[1].x - rightRoom[0].x - necessaryDig2 + 1);


        int p;          //1 = first dig down, then right. 0 = first dig up, then right.
        Directions d;
        int offset;
        if (leftRoom[0].x <= rightRoom[0].x)
        {
            //the left room is above the right one.
            //Dig down, as much as requried to have the corridors we want.
            necessaryDig1 = rightRoom[0].x - leftRoom[1].x;  //NB: it has to be >= 0, since we are dealing with two rooms separated by a horizontal cut.

            //service variable, they are useful just to not write two times a code that would differ only because of those values
            p = 1;
            d = Directions.down;
            offset = 0;
        }
        else
        {
            //the left room is below the right one
            necessaryDig1 = leftRoom[0].x - rightRoom[1].x;
            p = 0;
            d = Directions.up;
            offset = -1;
        }


        //decide where to build the tunnel
        int rightBound = Mathf.Min(rightRoom[0].z, leftRoom[1].z);
        int zToStart = Random.Range(leftRoom[0].z, (rightBound - firstCorridorWidth) + 1);     
        zToStart = zToStart < leftRoom[0].z ? leftRoom[0].z : zToStart;     //the previous line could return a value lower then leftRoom[0].z
        Debug.LogFormat("zToStart = {0}", zToStart, leftRoom[1]);

        Debug.LogFormat("Digs: {0}, {1}, {2}", necessaryDig1, necessaryDig2, randomDig);

        //now that we have our starting point, the width and the depth of the tunnel, we can dig the tunnels.
        for (int i = 0; i < firstCorridorWidth; i++)
        {
            finished = Dig(zToStart + i, leftRoom[p].x + offset, necessaryDig1 + necessaryDig2 + randomDig, d) || finished;
        }
        int xToStart = 0;
        if (!finished)
        {
            //if, by digging up/down in this way, we have not reached the lower room, then we have to dig another tunnel, that starts from the lower/upper room
            //and, by digging to the left, reaches our previously created corridor, in such a way to form a L-shaped corridor.
            if (leftRoom[0].x <= rightRoom[0].x)
            {
                xToStart = rightRoom[0].x + randomDig;
            }
            else
            {
                xToStart = rightRoom[1].x - randomDig - necessaryDig2;
            }

            Debug.LogFormat("xToStart = {0}", xToStart);

            for (int j = 0; j < secondCorridorWidth; j++)
            {
                Dig(rightRoom[0].z - 1, xToStart + j, rightRoom[0].z - (zToStart + firstCorridorWidth), Directions.left);
            }

        }

        //Doing this, sadly, isn't enough. Our rooms are not always "empty rectangle". They are like that only for leaf nodes.
        //for this reason, we have to stretch the corridors we have created untill we hit a wall.
        //first, we have to do this on the vertical corridor
        bool end = false;
        int x = leftRoom[p].x;
        int upOrDown = p == 1 ? -1 : 1;     //it will make us dig up or down depending on where we dug before
        d = d == Directions.up ? Directions.down : Directions.up;       //because we have to dig on the opposition direction than before
        while (!end)
        {
            x += upOrDown;
            for (int i = zToStart; i < zToStart + firstCorridorWidth; i++)
            {
                end = Dig(i, x, 1, d) || end;
            }
        }
        end = false;
        int z = rightRoom[0].z - 1;
        //then, on the horizontal one (if needed)
        if (!finished)
        {
            while (!end)
            {
                z++;
                for (int j = xToStart; j < xToStart + secondCorridorWidth; j++)
                {
                    end = Dig(z, j, 1, Directions.right) || end;
                }
            }
        }
    }

    //function to dig an L-shaped corridor between two rooms separated by a vertical cut
    private void DigLShapedCorridorForVerticalCut(Point[] leftRoom, Point[] rightRoom, int corridorWidth)
    {
        //the process is similar to before. The most notable difference is that, in order to have a behavior similar to the
        //one we had before (and also to make the dungeon look more random), this time we have to dig BEFORE the tunnel to
        //the right, and THEN move up/down.
        int necessaryDig1;
        int necessaryDig2;
        int randomDig;
        bool finished = false;

        int firstCorridorWidth = Mathf.Min(corridorWidth, leftRoom[1].x - leftRoom[0].x);
        int secondCorridorWidth = angleCorridorsHaveSameWidth ? firstCorridorWidth : Random.Range(minimumCorridorWidth, maximumCorridorWitdh + 1);
        secondCorridorWidth = Mathf.Min(secondCorridorWidth, rightRoom[1].z - rightRoom[0].z);

        necessaryDig1 = rightRoom[0].z - leftRoom[1].z;
        necessaryDig2 = secondCorridorWidth;
        randomDig = Random.Range(0, rightRoom[1].z - rightRoom[0].z - necessaryDig2 + 1);

        int p;          //1 = first dig right, then down. 0 = first dig right, then up.
        Directions d;
        int offset;
        int low_highBound;
        int xToStart;
        if (leftRoom[0].x <= rightRoom[0].x)
        {
            p = 0;
            d = Directions.up;
            offset = -1;
            low_highBound = Mathf.Min(leftRoom[1].x, rightRoom[0].x);
            xToStart = Random.Range(leftRoom[0].x, (low_highBound - firstCorridorWidth) + 1);
            xToStart = xToStart < leftRoom[0].x ? leftRoom[0].x : xToStart;     //to make sure we dig starting from the room
        }
        else
        {
            p = 1;      
            d = Directions.down;
            offset = 0;
            low_highBound = Mathf.Max(leftRoom[0].x, rightRoom[1].x);
            xToStart = Random.Range(low_highBound, (leftRoom[1].x - firstCorridorWidth) + 1);
            xToStart = xToStart > (leftRoom[1].x - firstCorridorWidth) ? (leftRoom[1].x - firstCorridorWidth) : xToStart;   //as above

        }

        for (int i = 0; i < firstCorridorWidth; i++)
        {
            finished = Dig(leftRoom[1].z, xToStart + i, necessaryDig1 + necessaryDig2 + randomDig, Directions.right) || finished;
        }
        Debug.Log("dig = " + (necessaryDig1 + necessaryDig2 + randomDig));

        int zToStart = 0;
        if (!finished)
        {
            zToStart = rightRoom[0].z + randomDig;

            for (int j = 0; j < secondCorridorWidth; j++)
            {
                if (leftRoom[0].x <= rightRoom[0].x) {
                    Dig(zToStart + j, rightRoom[p].x + offset, rightRoom[0].x - (xToStart + firstCorridorWidth), d);
                }
                else
                {
                    Dig(zToStart + j, rightRoom[p].x + offset, xToStart - rightRoom[1].x, d);
                }
            }
        }

        
        bool end = false;
        int z = leftRoom[1].z;
        while (!end)
        {
            z--;
            for (int j = xToStart; j < xToStart + firstCorridorWidth; j++)
            {
                end = Dig(z, j, 1, Directions.left) || end;
            }
        }

        int x = rightRoom[p].x;
        int upOrDown = p == 1 ? -1 : 1;
        x = p == 1 ? (x - 1) : x;
        d = d == Directions.up ? Directions.down : Directions.up;
        if (!finished) {
            while (!end)
            {
                for (int i = zToStart; i < zToStart + secondCorridorWidth; i++)
                {
                    end = Dig(i, x, 1, d) || end;
                }
                x += upOrDown;
            }
        }
        
        LVectors.Add(new Point[] { leftRoom[0], rightRoom[0] });

    }








    // Update is called once per frame
    void Update()
    {
        
    }

    //VISIBLE DEBUG
    private static List<Point[]> gizmosVectors = new List<Point[]>();
    private static List<Point[]> LVectors = new List<Point[]>();
    private static List<Point[]> roomsConnected = new List<Point[]>();

    public void OnDrawGizmos()
    {
        
        foreach (Point[] points in gizmosVectors)
        {
            if(points[0].z == points[1].z)
            {
                Gizmos.color = Color.red;
            }else if(points[0].x == points[1].x)
            {
                Gizmos.color = Color.blue;
            }
            Gizmos.DrawLine(
                new Vector3(points[0].x, 1, points[0].z),
                new Vector3(points[1].x, 1, points[1].z));
        }

        foreach(Point[] points in LVectors)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(points[0].x, 1, points[0].z), new Vector3(points[1].x, 1, points[1].z));
        }

        foreach(Point[] points in roomsConnected)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(points[0].x, 1, points[0].z), new Vector3(points[1].x, 1, points[1].z));
        }

    }

}
