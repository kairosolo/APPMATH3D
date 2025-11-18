using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeGenerator : MonoBehaviour
{
    public float cubeSideLength;

    public Vector3 cubeCenter;
    public Vector3 cubeRotation;
    public Material cubeMaterial;

    public float focalLength;

    public Vector2 RotateBy(float angle, float axis1, float axis2)
    {
        var firstAxis = axis1 * Mathf.Cos(angle) - axis2 * Mathf.Sin(angle);
        var secondAxis = axis2 * Mathf.Cos(angle) + axis1 * Mathf.Sin(angle);
        return new Vector2(firstAxis, secondAxis);
    }

    public Vector3[] GetFrontSquare()
    {
        var halfLength = cubeSideLength * .5f;

        return new[] {
            new Vector3(cubeCenter.x + halfLength, cubeCenter.y + halfLength, -halfLength),
            new Vector3(cubeCenter.x - halfLength, cubeCenter.y + halfLength, -halfLength),
            new Vector3(cubeCenter.x - halfLength, cubeCenter.y - halfLength, -halfLength),
            new Vector3(cubeCenter.x + halfLength, cubeCenter.y - halfLength, -halfLength),
        };
    }

    public Vector3[] GetBackSquare()
    {
        var halfLength = cubeSideLength * .5f;

        return new[] {
            new Vector3(cubeCenter.x + halfLength, cubeCenter.y + halfLength, halfLength),
            new Vector3(cubeCenter.x - halfLength, cubeCenter.y + halfLength, halfLength),
            new Vector3(cubeCenter.x - halfLength, cubeCenter.y - halfLength, halfLength),
            new Vector3(cubeCenter.x + halfLength, cubeCenter.y - halfLength, halfLength),
        };
    }

    private void OnRenderObject()
    {
        DrawLines();
    }

    private void OnDrawGizmos()
    {
        DrawLines();
    }

    public void DrawLines()
    {
        if (cubeMaterial == null)
        {
            return;
        }
        GL.PushMatrix();
        GL.Begin(GL.LINES);
        cubeMaterial.SetPass(0);
        var squareVectors = GetFrontSquare();
        var backsquareVectors = GetBackSquare();

        //z axis rotation front square
        var halfLength = cubeSideLength * .5f;
        for (int i = 0; i < squareVectors.Length; i++)
        {
            var deductedVector = cubeCenter - squareVectors[i];
            var rotatedVectors = RotateBy(cubeRotation.z, deductedVector.x, deductedVector.y);
            squareVectors[i] = new Vector3(rotatedVectors.x, rotatedVectors.y) + cubeCenter;
        }

        // z axis back square
        for (int i = 0; i < backsquareVectors.Length; i++)
        {
            var deductedVector = cubeCenter - backsquareVectors[i];
            var rotatedVectors = RotateBy(cubeRotation.z, deductedVector.x, deductedVector.y);
            backsquareVectors[i] = new Vector3(rotatedVectors.x, rotatedVectors.y) + cubeCenter;
        }

        var frontScale = focalLength / ((cubeCenter.z - cubeSideLength * .5f) + focalLength);
        for (int i = 0; i < squareVectors.Length; i++)
        {
            GL.Color(cubeMaterial.color);
            var point1 = squareVectors[i] * frontScale;

            GL.Vertex3(point1.x, point1.y, 0);
            Debug.Log(point1);
            var point2 = squareVectors[(i + 1) % squareVectors.Length] * frontScale;

            GL.Vertex3(point2.x, point2.y, 0);
        }

        var backScale = focalLength / ((cubeCenter.z + cubeSideLength * .5f) + focalLength);
        for (int i = 0; i < backsquareVectors.Length; i++)
        {
            GL.Color(cubeMaterial.color);
            var point1 = backsquareVectors[i] * backScale;
            GL.Vertex3(point1.x, point1.y, 0);
            var point2 = backsquareVectors[(i + 1) % squareVectors.Length] * backScale;
            GL.Vertex3(point2.x, point2.y, 0);
        }

        for (int i = 0; i < backsquareVectors.Length; i++)
        {
            GL.Color(cubeMaterial.color);
            var point1 = squareVectors[i] * frontScale;
            GL.Vertex3(point1.x, point1.y, 0);
            var point2 = backsquareVectors[i] * backScale;
            GL.Vertex3(point2.x, point2.y, 0);
        }

        GL.End();
        GL.PopMatrix();
    }
}