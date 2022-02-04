using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PartitioningTree4;

public class LabyrinthGenerator4Animated : MonoBehaviour
{
    //object that is responsible for the creation of the graph representing the dungeon
    private GameObject graphGenerator;

    //the floor of the dungeon
    public GameObject floor;

    //----------VARIABLES FOR GENERATING THE ROOMS----------

    //the gameobject to use in order to create the labyrinth
    public GameObject unit;
    public Material wallsMaterial;

    //heights of the walls of the labyrinth
    public int heightOfWalls = 5;
    //scale (x and z) of the unit of the labyrinth
    public int unitScale = 1;

    //upper left coordinates of the labyrinth
    public int z0 = 0;
    public int x0 = 0;

    //measures of the labyrinth
    public int width = 100;
    public int height = 100;

    //the smallest dimensions a PARTITION can have
    public int smallestPartitionZ = 4;
    public int smallestPartitionX = 4;

    //boolean to specify if the rooms generated have to be separated (so
    //that only corridors can allow rooms to communicate) or not (two randomly
    //generated rooms can be directly connected thanks to the absence of a wall)
    public bool roomsMustBeSeparated = true;

    //the user can specify a minimum width and height that the generated rooms
    //must have. Note that the generated rooms won't necessarily have this dimension
    //but the algorithm will try to generate rooms larger then those value if possible.
    public int minimumRoomZ = 1;
    public int minimumRoomX = 1;

    //for repeatability
    public int RandomSeed = 0;

    //a matrix to store the walls of the labyrinth as a bitmap
    private int[,] wallsArrayBitmap;

    //the actual labyrinth
    private GameObject[,] wallsArray;

    //the root node of the tree that represents the dungeon
    private static Node root;

    //----------VARIABLES TO GENERATE THE CORRIDORS----------

    public int minimumHorizontalCorridorWidth = 1;
    public int maximumHorizontalCorridorWitdh = 1;
    public int minimumVerticalCorridorWidth = 1;
    public int maximumVerticalCorridorWitdh = 1;

    //since sometimes the start function will return without an initialization, because some cosntrains
    //were not respected, we need a variable to avoid calling the update when not needed.
    private bool initialized;

    //----------LOGIC FOR SEEING THE EVOLUTION OF THE DUNGEON-----------
    public float delayInGeneration;
    public bool animated = true;
    private int[,] finalWallsArrayBitmap;

    private Queue dungeonQueue;

    private void Start()
    {
        graphGenerator = GameObject.Find("GraphGenerator");

        //create the floor
        GameObject f = Instantiate(floor);
        f.transform.position = new Vector3((x0 + height / 2)*unitScale, -0.01f, (z0 + width / 2)*unitScale);
        f.transform.localScale = new Vector3(f.transform.localScale.x * height, f.transform.localScale.y, f.transform.localScale.z * width);

        //create the roof
        GameObject r = Instantiate(floor);
        r.transform.position = new Vector3((x0 + height / 2) * unitScale, (unit.transform.localScale.y*heightOfWalls)/2 + 0.01f, (z0 + width / 2) * unitScale);
        r.transform.localScale = new Vector3(r.transform.localScale.x * height, r.transform.localScale.y, r.transform.localScale.z * width);
        r.transform.Rotate(180, 0, 0);


        dungeonQueue = new Queue();
        initialized = false;
        wallsArray = new GameObject[width, height];
        //no 0-dimension rooms allowed
        if (minimumRoomZ <= 0 || minimumRoomX <= 0)
        {
            UnityEditor.EditorUtility.DisplayDialog("Error", "The minimum room dimension must be > 0", "OK");
            return;
        }

        //If the minimum width of a corridor is more then the minimum width the room can have,
        //the dungeon is not generated, because it can't guarantee that it will be always possible
        //to generate that corridor.
        if (minimumHorizontalCorridorWidth > minimumRoomX || minimumVerticalCorridorWidth > minimumRoomZ)
        {
            UnityEditor.EditorUtility.DisplayDialog("Error", "The minimum width for a corridor can't be more than the minimum room width", "OK");
            return;
        }

        //Just to make my work simpler, I assume that the maximum width of a corridor can't be more then the partition
        if (maximumHorizontalCorridorWitdh > smallestPartitionX || maximumVerticalCorridorWitdh > smallestPartitionZ)
        {
            UnityEditor.EditorUtility.DisplayDialog("Error", "The maximum width for a corridor can't be more than the parition width", "OK");
            return;
        }

        //and of course...
        if (minimumHorizontalCorridorWidth > maximumHorizontalCorridorWitdh || minimumVerticalCorridorWidth > maximumVerticalCorridorWitdh)
        {
            UnityEditor.EditorUtility.DisplayDialog("Error", "The minimum width for a corridor can't be more than the maximum width for a corridor", "OK");
            return;
        }

        //for being stochastic or deterministic
        if (RandomSeed == 0) RandomSeed = (int)System.DateTime.Now.Ticks;
        Random.InitState(RandomSeed);

        root = generatePartitioningTree();
        //now we have the tree.
        //First of all, let's generate a labyrinth that is only made of walls
        wallsArrayBitmap = new int[width, height];
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                wallsArrayBitmap[i, j] = 1;
                wallsArray[i, j] = null;
            }
        }
        finalWallsArrayBitmap = new int[width, height];

        //now we have to remove some of those walls in order to generate our rooms. To do that,
        //we traverse the tree we have built, and when we'll find a leaf, we'll know that
        //all the walls inside that node need to be removed
        generateRooms(root);

        //----------CORRIDORS INITIALIZATION----------
        generateCorridors(root);

        //----------ANIMATION AND LABYRINTH DRAWING----------

        if (!animated)
        {
            delayInGeneration = 0;
        }
        initialized = true;
        StartCoroutine(drawLabyrinth());
    }



    //----------LOGIC FOR GENERATING THE ROOMS-----------

    //this will generate our Space Partitioning Tree
    private Node generatePartitioningTree()
    {
        Square p1 = new Square(0, 0);
        Square p2 = new Square(width, height);
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
    private Node generateNode(Square p1, Square p2, Node parent)
    {
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
            currentNode.left_child = null;
            currentNode.right_child = null;
            return currentNode;
        }

        //if the list is not empty, we can choose a random cut axis from it
        int i = Random.Range(0, possibleCutDimensions.Count);
        int cut = possibleCutDimensions[i];

        int interval = 0;
        Square l1;       //square coordinates for left child
        Square l2;
        Square r1;       //square coordinates for right child
        Square r2;
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
                l1 = new Square(p1.z, p1.x);
                l2 = new Square(p2.z, p1.x + xCoordinateCut);
                r1 = new Square(p1.z, p1.x + xCoordinateCut);
                r2 = new Square(p2.z, p2.x);
                currentNode.left_child = generateNode(l1, l2, currentNode);
                currentNode.right_child = generateNode(r1, r2, currentNode);

                //debug
                gizmosVectors.Add(new Square[] { new Square(p2.z, p1.x + xCoordinateCut), new Square(p1.z, p1.x + xCoordinateCut) });

                break;

            case PTConstants.verticalCutID:
                interval = Mathf.Abs(p1.z - p2.z) - smallestPartitionZ * 2;
                int zCoordinateCut = Random.Range(0, interval + 1);
                zCoordinateCut += smallestPartitionZ;

                currentNode.cutWhere = p1.z + zCoordinateCut;

                l1 = new Square(p1.z, p1.x);
                l2 = new Square(p1.z + zCoordinateCut, p2.x);
                r1 = new Square(p1.z + zCoordinateCut, p1.x);
                r2 = new Square(p2.z, p2.x);
                currentNode.left_child = generateNode(l1, l2, currentNode);
                currentNode.right_child = generateNode(r1, r2, currentNode);

                //debug
                gizmosVectors.Add(new Square[] { new Square(p1.z + zCoordinateCut, p2.x), new Square(p1.z + zCoordinateCut, p1.x) });

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
        if (current.left_child != null && current.right_child != null)
        {
            //if it is not a leaf node, then explore its childred

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

            //To generate a room, we pick two random points inside the given area.
            //So, let's consider those constrains while calculating the room points
            int actualPartitionZ = Mathf.Abs(current.p1.z - current.p2.z);
            int actualPartitionX = Mathf.Abs(current.p1.x - current.p2.x);

            //Actually there are other constrains. The room generated must be large enough to cover at least 1/4 of
            //the partition given in order to make sure it will be possible to connect this room to the other.
            //This is way our algorithm will always prefer rooms large as required by the user, but, if necessary,
            //will generate smaller rooms.
            int generationAreaZ;
            int generationAreaX;
            if (roomsMustBeSeparated)
            {
                generationAreaZ = actualPartitionZ - 2;
                generationAreaX = actualPartitionX - 2;
            }
            else
            {
                generationAreaZ = actualPartitionZ;
                generationAreaX = actualPartitionX;
            }

            int minimumRoomZCalculated = (int)Mathf.Floor(generationAreaZ / 2) + 1;
            int minimumRoomXCalculated = (int)Mathf.Floor(generationAreaX / 2) + 1;
            int minZ = Mathf.Max(minimumRoomZ, minimumRoomZCalculated);
            int minX = Mathf.Max(minimumRoomX, minimumRoomXCalculated);

            int room_z0 = obtainStartingCoordinateForRoom(current.p1.z, minZ, actualPartitionZ, roomsMustBeSeparated);
            int room_x0 = obtainStartingCoordinateForRoom(current.p1.x, minX, actualPartitionX, roomsMustBeSeparated);
            //given the point, I can randomly calculate the length of the room in that axis following the constrains
            int z_length = obtainLengthForRoom(current.p1.z, minZ, actualPartitionZ, roomsMustBeSeparated, room_z0);
            int x_length = obtainLengthForRoom(current.p1.x, minX, actualPartitionX, roomsMustBeSeparated, room_x0);

            //now, for this leaf node, i can set its room points
            current.setRoomSquares(new Square(room_z0, room_x0), new Square(room_z0 + z_length, room_x0 + x_length));

            //we don't need to set the cut orientation: we did that during the tree generation

            //finally, we can remove the objects covering our room space
            for (int j = room_x0; j < room_x0 + x_length; j++)
            {
                for (int i = room_z0; i < room_z0 + z_length; i++)
                {
                    wallsArrayBitmap[i, j] = 0;
                }
            }

            //for visualization
            int[,] d = (int[,])wallsArrayBitmap.Clone();
            dungeonQueue.Enqueue(d);

            //let's tell to the graph that the center of this room is a (room) node
            addNode(room_z0 + (int) Mathf.Floor((z_length - 1)/2), room_x0 + (int) Mathf.Floor((x_length - 1)/2), 0);
        }
    }

    private int obtainStartingCoordinateForRoom(int minCoordinate, int minLength, int partitionLength, bool roomsMustBeSeparated)
    {
        int minInclusive = minCoordinate;
        int maxExclusive = minCoordinate + partitionLength;
        if (roomsMustBeSeparated)
        {
            minInclusive += 1;
            maxExclusive -= 1;      //so that I won't touch the walls

        }
        maxExclusive -= minLength;  //this way I'm sure that the point I'll give will allow me to generate a room large enough as required
        //if the minLength requested is more then the actual length i can free for this room, i make this room the biggest possible
        maxExclusive = Mathf.Clamp(maxExclusive, minInclusive, minCoordinate + partitionLength);
        return Random.Range(minInclusive, maxExclusive + 1);
    }

    private int obtainLengthForRoom(int minCoordinate, int minLength, int partitionLength, bool roomsMustBeSeparated, int startingValue)
    {
        int maxValue = minCoordinate + partitionLength;
        if (roomsMustBeSeparated)
        {
            maxValue -= 1;
        }
        //the length of the room has a minimum, and the maximum possible value is the distance between the startingPoint
        int ret = Random.Range(minLength, Mathf.Abs(maxValue - startingValue) + 1);

        //we make sure the length is not too much
        int max = partitionLength - (roomsMustBeSeparated == true ? 2 : 0);
        ret = Mathf.Clamp(ret, 0, max);
        return ret;
    }




    //----------LOGIC FOR GENERATING THE CORRIDORS----------

    //now that we have our beautiful tree with his nodes and rooms, we have to generate the corridors.
    //to do that, we use the constrains imposed by the user.

    //this recursive function will return to the current node the room coordinates of its children combined
    private Square[] generateCorridors(Node current)
    {
        //base case: the node doesn't have any child (it is a "full" room, meaning a room where all the points
        //between the upper-left corner and the bottom-right one are part of the room), so he can return those
        //two points. Nothing more. It's great when things are this simple, aren't they?
        if (current.left_child == null && current.right_child == null)
        {
            return new Square[] { current.room_p1, current.room_p2 };
        }

        //if the node is not a leaf, then, first of all, he must ask to both of his children the upper-left
        //point and lower-left point of their respective rooms, to then merge them together.
        Square[] leftChildRoomPoints = generateCorridors(current.left_child);
        Square[] rightChildRoomPoints = generateCorridors(current.right_child);

        //now we set the two points for this merged room.
        int minZ = Mathf.Min(leftChildRoomPoints[0].z, leftChildRoomPoints[1].z,
                    rightChildRoomPoints[0].z, rightChildRoomPoints[1].z);
        int minX = Mathf.Min(leftChildRoomPoints[0].x, leftChildRoomPoints[1].x,
                    rightChildRoomPoints[0].x, rightChildRoomPoints[1].x);
        int maxZ = Mathf.Max(leftChildRoomPoints[0].z, leftChildRoomPoints[1].z,
                    rightChildRoomPoints[0].z, rightChildRoomPoints[1].z);
        int maxX = Mathf.Max(leftChildRoomPoints[0].x, leftChildRoomPoints[1].x,
                    rightChildRoomPoints[0].x, rightChildRoomPoints[1].x);

        Square top_left = new Square(minZ, minX);
        Square bottom_right = new Square(maxZ, maxX);
        current.setRoomSquares(top_left, bottom_right);
        roomsConnected.Add(new Square[] { top_left, bottom_right });

        //we can now actually think about how to generate the corridor
        int minSearch = 0;
        int maxSearch = 0;
        int corridorWidth;
        int[] boundaryCoordinates;
        switch (current.cutOrientation)
        {
            case PTConstants.horizontalCutID:
                //What will be the width of this corridor?
                corridorWidth = Random.Range(minimumVerticalCorridorWidth, maximumVerticalCorridorWitdh + 1);
                //actually, the corridor can't be larger then the room, so:
                corridorWidth = Mathf.Clamp(corridorWidth, 0, leftChildRoomPoints[1].z - leftChildRoomPoints[0].z);

                //if the cut was done horizontally, then we have to search all the possible "columns" eligible for building a direct corridor.
                minSearch = Mathf.Min(leftChildRoomPoints[0].z, leftChildRoomPoints[1].z,
                    rightChildRoomPoints[0].z, rightChildRoomPoints[1].z);    //because idk how those two rooms are disposed in the space
                maxSearch = Mathf.Max(leftChildRoomPoints[0].z, leftChildRoomPoints[1].z,
                    rightChildRoomPoints[0].z, rightChildRoomPoints[1].z);

                //now, for each possible z value between the minimum and the maximum, check: is that coordinate, that
                //represents a column, one on which both rooms lie for at least one unit?
                List<int> available_Z_coordinates = getZIntersections(minSearch, maxSearch, leftChildRoomPoints, rightChildRoomPoints);

                //based on that information, tell me the left and right walls (coordinates on z axis) for this corridor
                boundaryCoordinates = generateDirectCorridorBoundCoordinates(available_Z_coordinates, corridorWidth, leftChildRoomPoints[1].z);

                LVectors.Add(new Square[] { new Square(boundaryCoordinates[0], current.cutWhere), new Square(boundaryCoordinates[1], current.cutWhere) });

                //so, now we have the coordinates representing the columns to be emptied in order to join the two rooms.
                //Even so, actually connecting the two rooms requries a bit of attenction, since we want to remove the space
                //that is between the two rooms, not the columns as a whole.
                //To do the cut, we procees as following:
                //1) Given that we know the coordinate of the horizontal cut (a value on the x axis) that is stored on the current node,
                //we first place ourselves, for each z coordinate that we have (the columns), in that point. Then, we "dig" upwards (removing
                //all the units we find) until we find a room. Same goes for digging downwards.
                generateVerticalCorridorFromCut(current.cutWhere, boundaryCoordinates);
                break;

            case PTConstants.verticalCutID:
                corridorWidth = Random.Range(minimumVerticalCorridorWidth, maximumVerticalCorridorWitdh + 1);
                corridorWidth = Mathf.Clamp(corridorWidth, 0, leftChildRoomPoints[1].x - leftChildRoomPoints[0].x);

                minSearch = Mathf.Min(leftChildRoomPoints[0].x, leftChildRoomPoints[1].x,
                    rightChildRoomPoints[0].x, rightChildRoomPoints[1].x);
                maxSearch = Mathf.Max(leftChildRoomPoints[0].x, leftChildRoomPoints[1].x,
                    rightChildRoomPoints[0].x, rightChildRoomPoints[1].x);

                List<int> available_X_coordinates = getXIntersections(minSearch, maxSearch, leftChildRoomPoints, rightChildRoomPoints);

                //here we have unfortunately to distinguish two cases:
                //1) the left room is below the right one
                //2) the left room is above the right one
                Square[] boundChild;
                if (leftChildRoomPoints[0].x <= rightChildRoomPoints[0].x)
                {
                    boundChild = leftChildRoomPoints;
                }
                else
                {
                    boundChild = rightChildRoomPoints;
                }

                boundaryCoordinates = generateDirectCorridorBoundCoordinates(available_X_coordinates, corridorWidth, boundChild[1].x);

                LVectors.Add(new Square[] { new Square(current.cutWhere, boundaryCoordinates[0]), new Square(current.cutWhere, boundaryCoordinates[1]) });
                generateHorizontalCorridorFromCut(current.cutWhere, boundaryCoordinates);
                break;

        }

        int[,] d = (int[,])wallsArrayBitmap.Clone();
        dungeonQueue.Enqueue(d);
        finalWallsArrayBitmap = (int[,]) d.Clone();

        //in the end, after having merged the two rooms, we can return to our parent the coordinates (in points)
        //of our new room.
        return new Square[] { current.room_p1, current.room_p2 };
    }



    private List<int> getZIntersections(int minSearch, int maxSearch, Square[] leftChildRoomPoints, Square[] rightChildRoomPoints)
    {
        List<int> common = new List<int>();
        for (int i = minSearch; i < maxSearch; i++)
        {
            if (leftChildRoomPoints[0].z <= i && i < leftChildRoomPoints[1].z && rightChildRoomPoints[0].z <= i && i < rightChildRoomPoints[1].z)
            {
                common.Add(i);
            }
        }
        return common;
    }

    private List<int> getXIntersections(int minSearch, int maxSearch, Square[] leftChildRoomPoints, Square[] rightChildRoomPoints)
    {
        List<int> common = new List<int>();
        for (int i = minSearch; i < maxSearch; i++)
        {
            if (leftChildRoomPoints[0].x <= i && i < leftChildRoomPoints[1].x && rightChildRoomPoints[0].x <= i && i < rightChildRoomPoints[1].x)
            {
                common.Add(i);
            }
        }
        return common;
    }




    private int[] generateDirectCorridorBoundCoordinates(List<int> listOfAvailableCoordinates, int requiredWidth, int rightBound)
    {
        if (listOfAvailableCoordinates[0] > rightBound - requiredWidth)
        {
            return new int[] { rightBound - requiredWidth, rightBound };
        }

        int leftWall = Random.Range(listOfAvailableCoordinates[0], listOfAvailableCoordinates[listOfAvailableCoordinates.Count - 1] - requiredWidth + 2);
        int rightWall = leftWall + requiredWidth;
        return new int[] { leftWall, rightWall };
    }

    private void generateVerticalCorridorFromCut(int xMiddle, int[] boundaryCoordinates)
    {
        //first: dig up
        digVerticallyInSearchForRoom(xMiddle, boundaryCoordinates, Directions.up);
        //then: dig down
        digVerticallyInSearchForRoom(xMiddle, boundaryCoordinates, Directions.down);
    }

    private void digVerticallyInSearchForRoom(int xMiddle, int[] boundaryCoordinates, Directions dir)
    {
        int x = -1;
        int offset = -1;
        if (dir == Directions.up)
        {
            x = xMiddle - 1;
            offset = -1;
        }
        else if (dir == Directions.down)
        {
            x = xMiddle;
            offset = 1;
        }

        bool[] finished = new bool[boundaryCoordinates[1] - boundaryCoordinates[0]];
        //keep digging until:
        //1) you've found a room
        //2) you've digged along an entire edge of a room
        bool reached = false;
        bool edgeEncountered = false;
        int c = 0;
        while (!reached)
        {
            //let's check the following walls to see if we still need to dig (true = there is a full space, false = it is empty)
            c = 0;
            for (int columnIndex = boundaryCoordinates[0]; columnIndex < boundaryCoordinates[1]; columnIndex++)
            {
                finished[c] = wallsArrayBitmap[columnIndex, x] == 1 ? true : false;
                c++;
            }

            if (!MyUtility.boolContains(finished, true))
            {
                //I found the room
                reached = true;

                //GRAPH: let's tell to the graph that this is the entrance of a corridor
                addNode(boundaryCoordinates[0] + (int)Mathf.Floor((boundaryCoordinates[1] - boundaryCoordinates[0] - 1) / 2), 
                    x, 1);
            }
            else if (MyUtility.boolContains(finished, false) && MyUtility.boolContains(finished, true) && !edgeEncountered)
            {
                //I found the edge of the other room, because I could dig some columns, but others no.
                edgeEncountered = true;

                //GRAPH: let's tell to the graph that this is the entrance of a corridor
                addNode(boundaryCoordinates[0] + (int)Mathf.Floor((boundaryCoordinates[1] - boundaryCoordinates[0] - 1) / 2),
                    x, 1);
            }

            if (edgeEncountered && !MyUtility.boolContains(finished, false))
            {
                //I would go over the room if I proceeded, so stop digging
                reached = true;
            }

            if (!reached)
            {
                for (int columnIndex = boundaryCoordinates[0]; columnIndex < boundaryCoordinates[1]; columnIndex++)
                {
                    Dig(columnIndex, x);
                }
                x += offset;
            }

            //if we are trying to dig outside the dungeon, then we have finished
            if (x < 0 || x > width - 1)
            {
                reached = true;
            }

        }
    }



    //false = I didn't dig in there because it was already empty
    //true = I actually dug there
    private bool Dig(int z, int x)
    {
        //I can't dig also if I'm asked to dig outside of the dungeon
        if (z < 0 || z >= width || x < 0 || x >= height || wallsArrayBitmap[z, x] == 0)
        {
            return false;
        }
        else
        {
            wallsArrayBitmap[z, x] = 0;
            return true;
        }
    }


    private void generateHorizontalCorridorFromCut(int zMiddle, int[] boundaryCoordinates)
    {
        //first: dig left
        digHorizontallyInSearchForRoom(zMiddle, boundaryCoordinates, Directions.left);
        //then: dig right
        digHorizontallyInSearchForRoom(zMiddle, boundaryCoordinates, Directions.right);
    }

    private void digHorizontallyInSearchForRoom(int zMiddle, int[] boundaryCoordinates, Directions dir)
    {
        int z = -1;
        int offset = -1;
        if (dir == Directions.left)
        {
            z = zMiddle - 1;
            offset = -1;
        }
        else if (dir == Directions.right)
        {
            z = zMiddle;
            offset = 1;
        }

        bool[] finished = new bool[boundaryCoordinates[1] - boundaryCoordinates[0]];
        //keep digging until:
        //1) you've found a room
        //2) you've digged along an entire edge of a room
        bool reached = false;
        bool edgeEncountered = false;
        int c = 0;
        while (!reached)
        {
            //let's check the following walls to see if we still need to dig (true = there is a full space, false = it is empty)
            c = 0;
            for (int rowIndex = boundaryCoordinates[0]; rowIndex < boundaryCoordinates[1]; rowIndex++)
            {
                finished[c] = wallsArrayBitmap[z, rowIndex] == 1 ? true : false;
                c++;
            }

            if (!MyUtility.boolContains(finished, true))
            {
                //I found the room
                reached = true;

                //GRAPH: let's tell to the graph that this is the entrance of a corridor
                addNode(z, boundaryCoordinates[0] + (int)Mathf.Floor((boundaryCoordinates[1] - boundaryCoordinates[0] - 1) / 2), 1);
            }
            else if (MyUtility.boolContains(finished, false) && MyUtility.boolContains(finished, true) && !edgeEncountered)
            {
                //I found the edge of the other room, because I could dig some columns, but others no.
                edgeEncountered = true;

                //GRAPH: let's tell to the graph that this is the entrance of a corridor
                addNode(z, boundaryCoordinates[0] + (int)Mathf.Floor((boundaryCoordinates[1] - boundaryCoordinates[0] - 1) / 2), 1);
            }

            if (edgeEncountered && !MyUtility.boolContains(finished, false))
            {
                //I would go over the room if I proceeded, so stop digging
                reached = true;
            }

            bool b;
            if (!reached)
            {
                for (int rowIndex = boundaryCoordinates[0]; rowIndex < boundaryCoordinates[1]; rowIndex++)
                {
                    b = Dig(z, rowIndex);
                }
                z += offset;
            }

            if (z < 0 || z > height - 1)
            {
                reached = true;
            }
        }
    }



    public IEnumerator drawLabyrinth()
    {
        if (animated) 
        {
            while (dungeonQueue.Count > 0)
            {
                if (initialized)
                {
                    fromBitmapToDungeon((int[,])dungeonQueue.Dequeue());
                }
                yield return new WaitForSeconds(delayInGeneration);
            }
        }
        else
        {
            fromBitmapToDungeon(finalWallsArrayBitmap);
        }

        //----------GRAPH FINALIZATION-----------
        generateBitmapsForGraphGenerator(root);
    }

    private void fromBitmapToDungeon(int[,] myWallsArrayBitmap)
    {
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                //draw the actual labyrinth. If in [i,j] there is a 1 and there is a gameobject,
                //or in [i,j] there is a 0 and no gameObject, don't touch.
                //if instead the two values are not the same, change the wallsArray accordingly to the wallsArrayBitmap

                if (myWallsArrayBitmap[i, j] == 1 && wallsArray[i, j] == null)
                {
                    GameObject g = Instantiate(unit);
                    g.transform.position = new Vector3(x0 + j * unitScale + unitScale / 2, 0, z0 + i * unitScale + unitScale / 2);
                    g.transform.localScale = new Vector3(unitScale, heightOfWalls, unitScale);
                    g.GetComponent<MeshRenderer>().material = wallsMaterial;
                    wallsArray[i, j] = g;
                }
                else if (myWallsArrayBitmap[i, j] == 0 && wallsArray[i, j] != null)
                {
                    //destroy the unit
                    Destroy(wallsArray[i, j]);
                    wallsArray[i, j] = null;
                }
            }
        }
    }


    //---------------------VISIBLE DEBUG----------------------
    private static List<Square[]> gizmosVectors = new List<Square[]>();
    private static List<Square[]> LVectors = new List<Square[]>();
    private static List<Square[]> roomsConnected = new List<Square[]>();

    public void OnDrawGizmos()
    {

        foreach (Square[] points in gizmosVectors)
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
                new Vector3(points[0].x, 1, points[0].z),
                new Vector3(points[1].x, 1, points[1].z));
        }

        foreach (Square[] points in LVectors)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(points[0].x, 1, points[0].z), new Vector3(points[1].x, 1, points[1].z));
        }

        foreach (Square[] points in roomsConnected)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(points[0].x, 1, points[0].z), new Vector3(points[1].x, 1, points[1].z));
        }
    }



    //----------GENERATION OF THE NODES OF THE GRAPH------------
    //function used to add a room node to the graph, where (z,x) represent the center of the room
    //if type = 0, we want to add a room node. If type = 1, we want to add a corridor entrance node.
    private void addNode(int z, int x, int type)
    {
        graphGenerator.GetComponent<GraphGeneratorAnimated>().addNode(z, x, type);
    }

    //function used to set the two bitmaps of the graph
    private void generateBitmapsForGraphGenerator(Node root)
    {
        int[,] corridorsBitmap = (int[,]) finalWallsArrayBitmap.Clone();
        generateCorridorsBitmap(root, corridorsBitmap);
        graphGenerator.GetComponent<GraphGeneratorAnimated>().setBitmaps(finalWallsArrayBitmap, corridorsBitmap);
    }

    private void generateCorridorsBitmap(Node current, int[,] corridorBitmap)
    {
        //base case: a room has been reached, so set to 1 all the points inside the room (we don't want them in
        //this corridor bitmap, so we will consider them as full spaces)
        if(current.left_child == null && current.right_child == null)
        {
            for(int i = current.room_p1.z; i < current.room_p2.z; i++)
            {
                for (int j = current.room_p1.x; j < current.room_p2.x; j++)
                {
                    corridorBitmap[i, j] = 1;
                }
            }
        }
        else
        {
            generateCorridorsBitmap(current.left_child, corridorBitmap);
            generateCorridorsBitmap(current.right_child, corridorBitmap);
        }
    }


}
