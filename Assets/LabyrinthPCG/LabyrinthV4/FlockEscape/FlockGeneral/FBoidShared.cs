using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FBoidShared : MonoBehaviour
{

	[Range(0f, 10f)] public float _BoidFOV = 2f;
	public static float BoidFOW = 0f;

	[Range(1f, 20f)] public float _BoidSpeed = 10f;
	public static float BoidSpeed = 0f;

	[Range(0f, 1f)] public float _AlignComponent = 1f;
	public static float AlignComponent = 0f;

	[Range(0f, 1f)] public float _CohesionComponent = 1f;
	public static float CohesionComponent = 0f;



	[Range(0f, 1f)] public float _SeparationComponent = 1f;
	public static float SeparationComponent = 0f;

	//threshold that we use to check if two boids are too close
	public float _separationThreshold = 1f;
	public static float separationThreshold = 0f;

	//repulsion factor for when we want to heavily separate the boids
	public float _separationRepulsion = 1f;
	public static float separationRepulsion = 0f;


	[Range(0f, 1f)] public float _AvoidComponent = 1f;
	public static float AvoidComponent = 0f;

	//threshold that we use to check if a boid is too close to a wall
	public float _wallThreshold = 0.1f;
	public static float wallThreshold = 0.1f;

	//repulsion factor for when we want to heavily steer away from a wall
	public float _wallRepulsion = 10f;
	public static float wallRepulsion = 10f;


	public bool breath = false;
	[Range(0f, .2f)] public float amplitude = .1f;
	[Range(1f, 10f)] public float speed = 1f;

	private void Start()
	{
		OnValidate();
	}

	private void OnValidate()
	{
		BoidFOW = _BoidFOV;
		BoidSpeed = _BoidSpeed;
		AlignComponent = _AlignComponent;
		CohesionComponent = _CohesionComponent;
		SeparationComponent = _SeparationComponent;

		separationThreshold = _separationThreshold;
		separationRepulsion = _separationRepulsion;

		AvoidComponent = _AvoidComponent;
		wallThreshold = _wallThreshold;
		wallRepulsion = _wallRepulsion;
	}

	private void Update()
	{
		if (breath)
		{
			float c = 1f - ((Mathf.Cos(Time.realtimeSinceStartup * speed) + 1) * amplitude / 2f);
			float s = 1f - ((Mathf.Sin(Time.realtimeSinceStartup * speed) + 1) * amplitude / 2f);
			CohesionComponent = _CohesionComponent = c;
			SeparationComponent = _SeparationComponent = s;
		}
	}

}
