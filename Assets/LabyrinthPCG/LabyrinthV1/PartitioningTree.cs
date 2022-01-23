using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PartitioningTree
{
    public class PCGPartitioningTree
    {
        //the root node representing the entire space allocated for the labyrinth
        public Node root;

        public PCGPartitioningTree(Node root)
        {
            this.root = root;
        }

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
    }

    public class Node
    {
        //those represent the upper-left coordinates of the node and the lower-right coordinates
        //of the node
        public Point p1;
        public Point p2;

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

    }

    //class used for allowing negative index bidimensional arrays of GameObjects
    public class MyArray2OfGameObjects{
        
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

}





