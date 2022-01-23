using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PartitioningTree2
{

    
    public class PTConstants {
        //just some variables to associate a name to integers, will be useful
        //when generating the nodes of the tree.
        public const int horizontalCutID = 0;
        public const int verticalCutID = 1;
    }

    //class to represent a point in space
    public class Point
    {
        public int z;
        public int x;
        public Point(int z, int x)
        {
            this.z = z;
            this.x = x;
        }

        public override string ToString()
        {
            return "(" + z + "," + x + ")";
        }
    }

    public class Node
    {
        //those represent the upper-left coordinates of the node and the lower-right coordinates
        //of the node
        public Point p1;
        public Point p2;

        //those, instead, represent the upper-left coordinates and lower-right coordinates of the ROOM
        //relative to this Node, that is, a subset of the rectangle created by p1 and p2. In particular,
        //those two points represent the area, inside this node, that contains the room created (or joined rooms,
        //if this Node is not a leaf of the tree) plus some walls occasionally.
        public Point room_p1;
        public Point room_p2;

        //We also want to store, in a node with children, the nature of the cut (horizontal, vertical...)
        public int cutOrientation;

        //together with the nature of the cut, we also want to know at what coordinate the cut was done.
        //If it was a horizontal cut, then this value will represent a x coordinate.
        //If it was a vertical cut, this value will represent a z coordinate.
        public int cutWhere;

        //this represents the parent node of this node
        public Node parent;

        //those prepresent the two children of this node, that is, the two sub-areas produced
        //by partitioning the area of this node
        public Node left_child;
        public Node right_child;
        public Node(Point p1, Point p2, Node parent)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.parent = parent;
        }

        public void setRoomPoints(Point room_p1, Point room_p2)
        {
            this.room_p1 = room_p1;
            this.room_p2 = room_p2;
        }

    }

    //class used for allowing negative index bidimensional arrays of GameObjects
    public class MyArray2OfGameObjects
    {

        private GameObject[,] data;
        private int offsetZ;
        private int offsetX;

        public MyArray2OfGameObjects(int minimumZ, int maximumZ, int minimumX, int maximumX)
        {
            data = new GameObject[maximumZ - minimumZ, maximumX - minimumX];
            offsetZ = 0 - minimumZ;
            offsetX = 0 - minimumX;
        }

        public GameObject get(int z, int x)
        {
            return data[z + offsetZ, x + offsetX];
        }

        public void set(int z, int x, GameObject obj)
        {
            data[z + offsetZ, x + offsetX] = obj;
        }
    }

    enum Directions
    {
        up,
        down,
        left,
        right
    }

}
