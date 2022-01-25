using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PartitioningTree3
{
    public class PTConstants
    {
        //just some variables to associate a name to integers, will be useful
        //when generating the nodes of the tree.
        public const int horizontalCutID = 0;
        public const int verticalCutID = 1;
    }

    //class to represent a square of the dungeon
    public class Square
    {
        public int z;
        public int x;
        public Square(int z, int x)
        {
            this.z = z;
            this.x = x;
        }

        public string AsString()
        {
            return "(" + z + "," + x + ")";
        }
    }

    public class Node
    {
        //those represent the upper-left coordinates of the node and the lower-right coordinates
        //of the node (first INCLUSIVE, second EXCLUSIVE)
        public Square p1;
        public Square p2;

        //those, instead, represent the upper-left coordinates and lower-right coordinates of the ROOM
        //relative to this Node (first INCLUSIVE, second EXCLUSIVE), that is, a subset of the rectangle created by p1 and p2. In particular,
        //those two points represent the area, inside this node, that contains the room created (or joined rooms,
        //if this Node is not a leaf of the tree) plus some walls occasionally.
        public Square room_p1;
        public Square room_p2;

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
        public Node(Square p1, Square p2, Node parent)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.parent = parent;
        }

        public void setRoomSquares(Square room_p1, Square room_p2)
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

    public class MyUtility {
        public static bool boolContains(bool[] b, bool value)
        {
            for (int i = 0; i < b.Length; i++)
            {
                if (b[i] == value)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
