
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph
{

	// holds all edgeds going out from a node
	private Dictionary<GNode, List<GEdge>> data;

	public Graph()
	{
		data = new Dictionary<GNode, List<GEdge>>();
	}

	public void AddEdge(GEdge e)
	{
		AddNode(e.from);
		AddNode(e.to);
		if (!data[e.from].Contains(e))
			data[e.from].Add(e);
	}

	// used only by AddEdge 
	public void AddNode(GNode n)
	{
		if (!data.ContainsKey(n))
			data.Add(n, new List<GEdge>());
	}

	// returns the list of edged exiting from a node
	public GEdge[] getConnections(GNode n)
	{
		if (!data.ContainsKey(n)) return new GEdge[0];
		return data[n].ToArray();
	}

	public GNode[] getNodes()
	{
		return data.Keys.ToArray();
	}

	//function to check if at given coordinates there is a node
	public bool isNodeAtCoordinates(float z, float x)
    {
		foreach(GNode n in getNodes())
        {
			if(n.z == z && n.x == x) {return true;}
        }
		return false;
    }

	//function used to get a node at some given coordinates
	public GNode getNodeAtCoordinates(float z, float x)
    {
		foreach (GNode n in getNodes())
		{
			if (n.z == z && n.x == x) {return n;}
		}
		return null;
	}

	//function used to check if there is and edge between two nodes (useful for animation)
	public bool areNodesConnected(GNode f, GNode t)
    {
		foreach(GEdge e in getConnections(f))
        {
			if(e.to == t) { return true; }
        }
		return false;
    }

	//used by the function below
	private float distance(float z1, float x1, float z2, float x2)
	{
		return (Mathf.Sqrt(Mathf.Pow(Mathf.Abs(z1 - z2),2) + Mathf.Pow(Mathf.Abs(x1 - x2), 2)));
	}

	//to add a node and find out his edges (used by FSeek)
	public void addNodeAndFindEdges(GNode n)
    {
		AddNode(n);
		foreach(GNode other in getNodes())
        {
			if (n != other)
			{
				bool isThereWall = Physics.Raycast(new Vector3(n.x, 0, n.z),
							(new Vector3(other.x, 0, other.z) - new Vector3(n.x, 0, n.z)).normalized,
							(new Vector3(other.x, 0, other.z) - new Vector3(n.x, 0, n.z)).magnitude,
							(1 << 6));
				if (!isThereWall)
				{
					AddEdge(new GEdge(n, other, distance(n.z, n.x, other.z, other.x)));
				}
			}
		}
    }

}