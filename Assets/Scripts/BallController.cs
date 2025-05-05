using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor;
using UnityEngine;
using Geometry;
using UnityEngine.Assertions;

public class BallController : MonoBehaviour
{
    [SerializeField] private Vector3 force;
    [SerializeField] private GolfClub club;
    [SerializeField] private Vector3 hitVector;
    [SerializeField] private Vector3 screenPoint;
    [SerializeField] private Geometry.Plane ground;
    [SerializeField] private Vector3 ballPos;
    [SerializeField] private Vector3 hitPos;

    private Vector3 prevMouse = Vector3.negativeInfinity;
    // Start is called before the first frame update
    void Start()
    {
        ground = new Geometry.Plane(Vector3.up + Vector3.back, ballPos);
    }

    // Update is called once per frame
    void Update()
    {
        ProcessInput();
    }



    void ProcessInput()
    {
        if (Input.GetMouseButtonDown(0)) prevMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        else if (Input.GetMouseButton(0))
        {
            screenPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            hitPos = Quaternion.FromToRotation(Vector3.back, ground.normal) * (screenPoint - prevMouse) + ballPos;
            if((hitPos - ballPos).magnitude > 1) hitPos = (hitPos - ballPos).normalized + ballPos;
        }
        else if(Input.GetMouseButtonUp(0))
        {
            hitPos = ballPos;
        }
    }



    void Hit()
    {
        force = club.Force * hitVector;
        PhysicsManager.Instance.ApplyForce(this.gameObject, force);
    }

    [Serializable]
    private struct GolfClub
    {
        /*
        - Driver
        - Fairway Woods
        - Hybrid
        - Irons
        - Wedges
        - Putter
         */

        float force;
        public float Force { get { return force; } }
    }
}

namespace Geometry
{
    public static class Constants
    {
        public const float intersectTolerance = 0.01f;
        public const float intersectMin = 0.3f;
        public const float intersectMax = 1000f;
        public const int intersectMaxIterations = 1000;
    }
    public struct Ray
    {
        public Vector3 position;
        public Vector3 direction;

        public Ray(Vector3 pos, Vector3 dir) { position = pos; direction = dir; }
        public Vector3 At(float distance) => position + direction * distance;
    }

    public struct Plane
    {
        public Vector3 normal;
        public float constant;

        public Plane(Vector3 normal, Vector3 point)
        {
            this.normal = normal;
            constant = -Vector3.Dot(normal, point);
        }
        public float Distance(Vector3 point) => Mathf.Abs(Vector3.Dot(normal, point) + constant) / normal.magnitude;
        public int Side(Vector3 point) => (int)Mathf.Sign(Vector3.Dot(normal, point) + constant);
        public bool Facing(Vector3 eye) => (int)Mathf.Sign(-Vector3.Dot(normal, eye)) == 1;

        public Vector3 FindIntersect(Ray ray)
        {
            if (Vector3.Dot(normal, ray.direction) == 0) return Vector3.positiveInfinity; //Can't find intersect

            float min = Constants.intersectMin;
            float max = Constants.intersectMax;
            float dist = (min + max) / 2;
            int count = 0;

            //We binary search the intersection
            //It should not take more than log2(max - min) - log2(tolerance) iterations to find the intersect
            while (this.Side(ray.At(dist)) * this.Side(ray.At(dist + Constants.intersectTolerance)) > 0)
            {
                count++;
                if(count > Constants.intersectMaxIterations) return Vector3.positiveInfinity; //Can't find intersect
                if (Side(ray.At(min)) == Side(ray.At(dist))) min = dist;
                else if (Side(ray.At(max)) == Side(ray.At(dist))) max = dist;
                dist = (min + max) / 2;
            }
            return ray.At(dist);
        }
    }
}