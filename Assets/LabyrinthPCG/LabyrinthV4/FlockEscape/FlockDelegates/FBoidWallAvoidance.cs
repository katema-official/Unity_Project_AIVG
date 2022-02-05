using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FBoidWallAvoidance : FBoidComponent
{

	//boolean used to know if we are extremely close to a wall
	public bool tooCloseToWall = false;

	public override Vector3 GetDirection(Collider[] neighbors, int size)
	{
		tooCloseToWall = false;
		Vector3 avoid = Vector3.zero;
		Vector3 tmp;
		for (int i = 0; i < size; i += 1)
		{
			if (neighbors[i].gameObject.layer != gameObject.layer)      //if it's a wall
			{
				tmp = transform.position - neighbors[i].ClosestPointOnBounds(transform.position);
				avoid += tmp.normalized / (tmp.magnitude + 0.0001f);

				if (tmp.magnitude < FBoidShared.wallThreshold)
				{
					tooCloseToWall = true;
				}
			}
		}

		return avoid.normalized * FBoidShared.AvoidComponent;
	}
}
