using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GNode
{

	//for each node, i want to know if:
	//-it is a room node
	//-it is a corridor entrance
	//-it is an intersection
	//One could think that this could be done with an enum, but I actually want to be able
	//to also say that a node is both a corridor entrance and a room node (can happen when the room is particularly small)
	public bool is_room;
	public bool is_corridor_entrance;
	public bool is_intersection;

	//the coordinates (in the bitmap) of this node
	public int z;
	public int x;

	public GNode(int z, int x)
	{
		this.z = z;
		this.x = x;
	}
}
