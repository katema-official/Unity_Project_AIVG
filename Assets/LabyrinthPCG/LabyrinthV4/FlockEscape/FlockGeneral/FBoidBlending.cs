using UnityEngine;

public abstract class FBoidComponent : MonoBehaviour
{
	public abstract Vector3 GetDirection(Collider[] neighbors, int size);
}

public class FBoidBlending : MonoBehaviour
{

	private Collider[] neighbors = new Collider[1000];
	// this must be large enough

	private FBoidAlign align;
	private FBoidCohesion cohesion;
	private FBoidSeparation separation;
	private FBoidWallAvoidance avoid;
	private FSeek seek;

	//i will treat the walls separately
	private Collider[] neighborsWalls = new Collider[200];

	private void Awake()
	{
		//I manually take the components
		align = GetComponent<FBoidAlign>();
		cohesion = GetComponent<FBoidCohesion>();
		separation = GetComponent<FBoidSeparation>();
		avoid = GetComponent<FBoidWallAvoidance>();
		seek = GetComponent<FSeek>();
	}

	void FixedUpdate()
	{

		Vector3 globalDirection = Vector3.zero;

		int count = Physics.OverlapSphereNonAlloc(transform.position, FBoidShared.BoidFOW, neighbors);
		int countWall = Physics.OverlapSphereNonAlloc(transform.position, FBoidShared.wallThreshold, neighborsWalls, (1 << 6));

		//I maually take the accelerations
		Vector3 acc1 = align.GetDirection(neighbors, count);
		Vector3 acc2 = cohesion.GetDirection(neighbors, count);
		Vector3 acc3 = separation.GetDirection(neighbors, count);
		Vector3 acc4 = avoid.GetDirection(neighbors, countWall);
		Vector3 acc5 = seek.GetDirection(neighbors, count);

		//if I am too close to a wall, I want to get repulsed a lot from it (actually, just the value
		//that the user specified for me)
		acc4 = acc4 * FBoidShared.wallRepulsion;

		//if (acc4 != Vector3.zero)
		//{

			if (separation.tooCloseToOtherBoid)
			{
				//if I am too close to another boid, I want to get repulsed a lot from it (like before)
				acc3 = acc3 * FBoidShared.separationRepulsion;
			}

			globalDirection += acc1 + acc2 + acc3 + acc4 + acc5;
        //}
        //else
        //{
		//	globalDirection = acc4;
        //}

		if (globalDirection != Vector3.zero)
		{
			transform.rotation = Quaternion.LookRotation((globalDirection.normalized + transform.forward) / 2f);
		}

		transform.position += transform.forward * FBoidShared.BoidSpeed * Time.deltaTime;
	}
}