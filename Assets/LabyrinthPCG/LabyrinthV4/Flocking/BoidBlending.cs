using UnityEngine;

public abstract class BoidComponent : MonoBehaviour {
	public abstract Vector3 GetDirection (Collider [] neighbors, int size);
}

public class BoidBlending : MonoBehaviour {

	private Collider [] neighbors = new Collider [200];
	// this must be large enough

	BoidAlign align;
	BoidCohesion cohesion;
	BoidSeparation separation;
	BoidWallAvoidance avoid;

	private void Awake()
    {
		//I manually take the components
		align = GetComponent<BoidAlign>();
		cohesion = GetComponent<BoidCohesion>();
		separation = GetComponent<BoidSeparation>();
		avoid = GetComponent<BoidWallAvoidance>();
	}

    void FixedUpdate () {

		Vector3 globalDirection = Vector3.zero;

		int count = Physics.OverlapSphereNonAlloc (transform.position, BoidShared.BoidFOW, neighbors);

		//I maually take the accelerations
		Vector3 acc1 = align.GetDirection(neighbors, count);
		Vector3 acc2 = cohesion.GetDirection(neighbors, count);
		Vector3 acc3 = separation.GetDirection(neighbors, count);
		Vector3 acc4 = avoid.GetDirection(neighbors, count);

		//if I am too close to a wall, I want to get repulsed a lot from it (actually, just the value
		//that the user specified for me)
		if (avoid.tooCloseToWall)
		{
			acc4 = acc4 * BoidShared.wallRepulsion;
		}else if (separation.tooCloseToOtherBoid)
        {
			//if I am too close to another boid, I want to get repulsed a lot from it (like before)
			acc3 = acc3 * BoidShared.separationRepulsion;
        }

		
		
		
		globalDirection += acc1 + acc2 + acc3 + acc4;

		

		/*
		foreach (BoidComponent bc in GetComponents<BoidComponent> ()) {
			Vector3 tmp = bc.GetDirection(neighbors, count);
			//Debug.Log("bc component = " + bc + ", steering = " + tmp);
			globalDirection += tmp;
		}
		*/

		if (globalDirection != Vector3.zero) {
			transform.rotation = Quaternion.LookRotation ((globalDirection.normalized + transform.forward) / 2f);
		}

		transform.position += transform.forward * BoidShared.BoidSpeed * Time.deltaTime;
	}
}