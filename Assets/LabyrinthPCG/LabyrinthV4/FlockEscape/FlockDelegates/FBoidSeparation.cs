using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FBoidSeparation : FBoidComponent
{

	//boolean used to know if we are too close to another boid
	public bool tooCloseToOtherBoid = false;

	override public Vector3 GetDirection(Collider[] neighbors, int size)
	{
		tooCloseToOtherBoid = false;
		Vector3 separation = Vector3.zero;
		Vector3 tmp;
		for (int i = 0; i < size; i += 1)
		{
			if (neighbors[i].gameObject.layer == gameObject.layer && gameObject != neighbors[i].gameObject)     //AVOID ALIASING, EXTREMELY IMPORTANT
			{
				tmp = (transform.position - neighbors[i].ClosestPointOnBounds(transform.position));
				separation += tmp.normalized / (tmp.magnitude + 0.0001f);
				if (tmp.magnitude < FBoidShared.separationDistance)
				{
					tooCloseToOtherBoid = true;
				}
			}
		}
		return separation.normalized * FBoidShared.SeparationComponent;
	}
}