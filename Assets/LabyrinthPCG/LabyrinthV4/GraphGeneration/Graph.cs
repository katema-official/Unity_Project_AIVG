
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

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

}