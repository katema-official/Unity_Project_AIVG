
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

}