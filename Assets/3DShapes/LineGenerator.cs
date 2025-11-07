using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineGenerator : MonoBehaviour
{
    public Material material;
    public Vector3 cubeCenter;
    public Vector3 cubeOtherCenter;
    public Vector3 cubeRotation;
    public float cubeSideLength;
    public float focalLength;

    private void OnPostRender()
    {
        DrawLine();
    }

    public void OnDrawGizmos()
    {
        DrawLine();
    }

    public void DrawLine()
    {

        if (material == null)
        {
            Debug.LogError("You need to add a material");
            return;
        }
        GL.PushMatrix();

        GL.Begin(GL.LINES);
        material.SetPass(0);


        var squareOne = GetFrontSquare(cubeCenter);
        var squareTwo = GetFrontSquare(cubeOtherCenter);

        var squareOneScale = focalLength / ((cubeCenter.z - cubeSideLength * .5f) + focalLength);
        var squareTwoScale = focalLength / ((cubeOtherCenter.z - cubeSideLength * .5f) + focalLength);
        /*
        for (int i = 0; i < squareVectors.Length; i++ )
        {
            
            var deductedVector = cubeCenter - squareVectors[i];
            var rotatedVectors =  RotateBy(cubeRotation.z, deductedVector.x, deductedVector.y);
            squareVectors[i] = rotatedVectors;  
        }
        */
        

        
        
        if(CheckCollision(squareTwo, squareOne))
        {
            Debug.Log("Collision Detected");
        }

        DrawSquare(squareOne, squareOneScale);
        DrawSquare(squareTwo, squareTwoScale);

        GL.PopMatrix();
        GL.End();

    }

    private void DrawSquare(Vector3[] squareVectors, float frontScale)
    {
        for (int i = 0; i < squareVectors.Length; i++)
        {

            GL.Color(material.color);
            var point1 = squareVectors[i] * frontScale;
            GL.Vertex3(point1.x, point1.y, 0);
            var point2 = squareVectors[(i + 1) % squareVectors.Length] * frontScale;
            GL.Vertex3(point2.x, point2.y, 0);

        }
    }

    public Vector2 RotateBy(float angle, float axis1, float axis2)
    {
        var firstAxis = axis1 * Mathf.Cos(angle) - axis2 * Mathf.Sin(angle);
        var secondAxis = axis2 * Mathf.Cos(angle) + axis1 * Mathf.Sin(angle);
        return new Vector2(firstAxis, secondAxis);
    }

    public Vector3[] GetFrontSquare(Vector2 boxCenter)
    {
        var halfLength = cubeSideLength * .5f;

        return new[] {
            new Vector3(boxCenter.x + halfLength, boxCenter.y + halfLength, -halfLength),
            new Vector3(boxCenter.x - halfLength, boxCenter.y + halfLength, -halfLength),
            new Vector3(boxCenter.x - halfLength, boxCenter.y - halfLength, -halfLength),
            new Vector3(boxCenter.x + halfLength, boxCenter.y - halfLength, -halfLength),
        };
    }

    public bool CheckCollision(Vector3[] box1, Vector3[] box2)
    {
        /*
        var xMin = box1[1].x > box2[0].x;
        var xMax = box1[0].x < box2[1].x;
        var yMin = box1[1].y > box2[2].y;
        var yMax = box1[2].y < box2[1].y;
        */

        var xMin = box1[0].x >= box2[1].x;
        var xMax = box1[1].x <= box2[0].x;
        var yMin = box1[2].y <= box2[1].y;
        var yMax = box1[1].y >= box2[2].y;

        Debug.Log($"{xMin} {xMax} {yMin} {yMax}");

        if (xMin && xMax && yMin && yMax)
        {
            return true;
        }

        return false;
    }
}
