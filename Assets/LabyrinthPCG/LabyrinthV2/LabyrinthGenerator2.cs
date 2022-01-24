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

                    //so, now we have the coordinates representing the columns to be emptied in order to join the two rooms.
                    //Even so, actually connecting the two rooms requries a bit of attenction, since we want to remove the space
                    //that is between the two rooms, not the columns as a whole.
                    //To do the cut, we procees as following:
                    //1) Given that we know the coordinate of the horizontal cut (a value on the x axis) that is stored on the current node,
                    //we first place ourselves, for each z coordinate that we have (the columns), in that point. Then, we "dig" upwards (removing
                    //all the units we find) untill we find a room. Same goes for digging downwards.
                    generateHorizontalCorridorFromCut(current.cutWhere, boundaryCoordinates);

                }
                else
                {
                    if (allowSimplerCorridors) 
                    {
                        //or, if that space is NOT enough to contain the randomly generated corridor width:
                        //is this space enough to contain another corridor, if the user allowed simpler corridors?
                        int[] boundaryCoordinates = new int[] { available_Z_coordinates[0], available_Z_coordinates[available_Z_coordinates.Count - 1] };
                        generateHorizontalCorridorFromCut(current.cutWhere, boundaryCoordinates);
                    }
                    else
                    {
                        //this is where things get reaaaaally messy, because it is explicitly required to create a L-shaped corridor (also, eventually, U-shaped).
                        //It is in general not easy, because, to do this, we have to make some calculations based on the position of the two rooms.
                        //We'll also have to distinguish two kind of corridors: the ones that have the a constant width, and those that can have
                        //two (or more) different widths. It depends on the variable angleCorridorsHaveSameWidth.
                        //So... let's start by getting an idea of where those rooms are placed.
                        //In general, those two rooms can be:
                        //1) the one on the left is in the upper part of the cut, the right one is in the lowe part
                        //2) viceversa
                        //To make the results more random, we'll start from the room on the left and randomly decide if we want to dig
                        //to the right (and then up/downm) or up/down (and then to the right).
                        //Note that, in some occasions, we might need to dig, say, to the right, then down, then to the left.
                        //example: upper-left room with coordinates (0,0) - (10,10), lower-right with coordinates (0,20) - (5-25), and
                        //the random algorithm decided to dig to the right as first thing.

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

                        //now all the calculation rely on the fact that the room on the left is above or below the one on the right
                        if(roomOnTheLeft[0].x <= roomOnTheRight[0].x)
                        {
                            //the room on the left is above the one on the right. Choose a random direction to dig between right and down.
                            Directions[] possibleDirections = new Directions[] { Directions.down, Directions.right };
                            Directions chosenDir = possibleDirections[Random.Range(0, possibleDirections.Length)];

                            //We have to distinguish two scenarios, now:
                            //1) we dig down. Then, we might have to dig to the right.
                            //2) we dig to the right. Then, we might have to dig down, and then, maybe, to the left.
                            int necessaryDig;
                            int randomDig;
                            bool finished = false;
                            if (chosenDir == Directions.down)
                            {
                                //now, dig in that direction. How much? Well, enough that you will have below you/to your right
                                //the other room, but then you can dig some more, if you want.
                                necessaryDig = roomOnTheLeft[1].x - roomOnTheRight[0].x;  //NB: it has to be >= 0, since we are dealing with two rooms separated by a horizontal cut.   
                                randomDig = Random.Range(0, roomOnTheRight[1].x - roomOnTheRight[0].x);

                                //we have to dig a tunnel large as corridorWidth first, so not all places are elegible to begin.
                                //And what if the tunnel to generate is larger then the wall? Well, in this case, there's nothing we can do:
                                //we'll dig a tunnel large as the available wall.
                                corridorWidth = leftChildRoomPoints[1].z - leftChildRoomPoints[0].z >= corridorWidth ? corridorWidth : leftChildRoomPoints[1].z - leftChildRoomPoints[0].z;

                                //if the corridor is as large as the walls distance, we have to start from the point on the left, and we can't generate a random value.
                                int zToStart = corridorWidth == leftChildRoomPoints[1].z - leftChildRoomPoints[0].z ?
                                    leftChildRoomPoints[0].z :
                                    Random.Range(leftChildRoomPoints[0].z, leftChildRoomPoints[1].z - corridorWidth);

                                //now that we have our starting point and the width, we can dig the tunnels.
                                for (int i = 0; i < corridorWidth; i++) {
                                    finished = finished || Dig(zToStart + i, leftChildRoomPoints[1].x, necessaryDig + randomDig, Directions.down, true, false);
                                }

                                //have we already reached the room? then, stop. Else, we have to dig to the right.
                                if (!finished)
                                {
                                    //did the user want those kind of corridors of the same width? Or of different width?
                                    int oldWidth = corridorWidth;
                                    corridorWidth = angleCorridorsHaveSameWidth ? corridorWidth : Random.Range(minimumCorridorWidth, maximumCorridorWitdh + 1);

                                    //now, dig to the right, in such a way to create a L-shaped curve.
                                    for(int j = -1; j <= -corridorWidth; j--)
                                    {
                                        //here we know we'll touch the other room, so the distanceToDig can be set to 0

                                        necessaryDig = roomOnTheRight[0].z - zToStart - oldWidth;  
                                        randomDig = Random.Range(0, roomOnTheRight[1].x - roomOnTheRight[0].x);
                                        Dig(zToStart + oldWidth, leftChildRoomPoints[1].x + necessaryDig + randomDig + j, 0, Directions.right, false, true);
                                    }
                                }



                            }
                            else if(chosenDir == Directions.right)
                            {
                                
                                necessaryDig = Mathf.Abs(roomOnTheRight[1].z - roomOnTheLeft[0].z);     
                                randomDig = Random.Range(0, roomOnTheRight[1].z - roomOnTheRight[0].z);
                            }
                            



                        }
                        else
                        {
                            //the room on the left is below the one on the right. Choose a random direction to dig between right and up.
                            Directions[] possibleDirections = new Directions[] { Directions.up, Directions.right };
                            Directions chosenDir = possibleDirections[Random.Range(0, possibleDirections.Length)];
                        }
                        





                    }


                }




                break;
            case PTConstants.verticalCutID:
                minSearch = Mathf.Min(leftChildRoomPoints[0].x, leftChildRoomPoints[1].x,
                    rightChildRoomPoints[0].x, rightChildRoomPoints[1].x);    //because idk how those two rooms are disposed in the space
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
                    generateVerticalCorridorFromCut(current.cutWhere, boundaryCoordinates);
                }
                else
                {
                    if (allowSimplerCorridors)
                    {
                        int[] boundaryCoordinates = new int[] { available_X_coordinates[0], available_X_coordinates[available_X_coordinates.Count - 1] };
                        generateVerticalCorridorFromCut(current.cutWhere, boundaryCoordinates);
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


    private void generateHorizontalCorridorFromCut(int xMiddle, int[] boundaryCoordinates)
    {
        //first: dig upwards (or to the left)

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
    }

    private void generateVerticalCorridorFromCut(int zMiddle, int[] boundaryCoordinates)
    {
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
    private bool Dig(int startZ, int startX, int distanceToDig, Directions direction, bool reachStartingRoom, bool reachEndingRoom)
    {
        switch (direction)
        {
            case Directions.up:
                //we want to dig up, but we want to make sure we are connected to a room. To do that, we first dig down 'till we find an empty space.
                if (reachStartingRoom)
                {
                    int i = 1;
                    while (wallsArray.get(startZ, startX + i) != null)
                    {
                        Destroy(wallsArray.get(startZ, startX + i));
                        wallsArray.set(startZ, startX + i, null);
                        i++;
                    }
                }

                //now we actually dig up
                for(int i = 0; i > -distanceToDig; i--)
                {
                    if(wallsArray.get(startZ, startX + i) != null)
                    {
                        Destroy(wallsArray.get(startZ, startX + i));
                        wallsArray.set(startZ, startX + i, null);
                    }else{return true;}

                }

                //the client wants us to make sure we've reached a room, while digging up.
                if (reachEndingRoom)
                {
                    int i = 0;
                    while (wallsArray.get(startZ, startX - distanceToDig - i) != null)
                    {
                        Destroy(wallsArray.get(startZ, startX - distanceToDig - i));
                        wallsArray.set(startZ, startX - distanceToDig - i, null);
                        i++;
                    }
                    return true;
                }

                break;

            case Directions.right:
                if (reachStartingRoom)
                {
                    int j = 1;
                    while (wallsArray.get(startZ - j, startX) != null)
                    {
                        Destroy(wallsArray.get(startZ - j, startX));
                        wallsArray.set(startZ - j, startX, null);
                        j++;
                    }
                }

                //MIGHT BE J = 1, AND THE ABOVE J = 0, OCHO (AND THE ONE BELOW J = 1 TOO)
                for (int j = 0; j < distanceToDig; j++)
                {
                    if(wallsArray.get(startZ + j, startX) != null)
                    {
                        Destroy(wallsArray.get(startZ + j, startX));
                        wallsArray.set(startZ + j, startX, null);
                    }else{return true;}
                }

                if (reachEndingRoom)
                {
                    int j = 0;
                    while (wallsArray.get(startZ + distanceToDig + j, startX) != null)
                    {
                        Destroy(wallsArray.get(startZ + distanceToDig + j, startX));
                        wallsArray.set(startZ + distanceToDig + j, startX, null);
                        j++;
                    }
                    return true;
                }

                break;

            case Directions.down:
                if (reachStartingRoom)
                {
                    int i = 1;
                    while (wallsArray.get(startZ, startX - i) != null)
                    {
                        Destroy(wallsArray.get(startZ, startX - i));
                        wallsArray.set(startZ, startX - i, null);
                        i++;
                    }
                }

                for (int i = 0; i < distanceToDig; i++)
                {
                    if (wallsArray.get(startZ, startX + i) != null)
                    {
                        Destroy(wallsArray.get(startZ, startX + i));
                        wallsArray.set(startZ, startX + i, null);
                    }else{return true;}
                }

                if (reachEndingRoom)
                {
                    int i = 0;
                    while (wallsArray.get(startZ, startX + distanceToDig + i) != null)
                    {
                        Destroy(wallsArray.get(startZ, startX + distanceToDig + i));
                        wallsArray.set(startZ, startX + distanceToDig + i, null);
                        i++;
                    }
                    return true;
                }

                break;

            case Directions.left:
                if (reachStartingRoom)
                {
                    int j = 1;
                    while (wallsArray.get(startZ + j, startX) != null)
                    {
                        Destroy(wallsArray.get(startZ + j, startX));
                        wallsArray.set(startZ + j, startX, null);
                        j++;
                    }
                }

                for (int j = 0; j > -distanceToDig; j--)
                {
                    if (wallsArray.get(startZ + j, startX) != null)
                    {
                        Destroy(wallsArray.get(startZ + j, startX));
                        wallsArray.set(startZ + j, startX, null);
                    }else{return true;}
                }

                if (reachEndingRoom)
                {
                    int j = 0;
                    while (wallsArray.get(startZ - distanceToDig - j, startX) != null)
                    {
                        Destroy(wallsArray.get(startZ - distanceToDig - j, startX));
                        wallsArray.set(startZ - distanceToDig - j, startX, null);
                        j++;
                    }
                    return true;
                }

                break;
        }
        return false;
    }










    // Update is called once per frame
    void Update()
    {
        
    }

    //VISIBLE DEBUG
    private static List<Point[]> gizmosVectors = new List<Point[]>();

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
    }


}
