using UnityEngine;

public class ShapeGenerator : MonoBehaviour
{
    [Header("Cube")]
    [SerializeField] private bool showCube;
    [SerializeField] private Material cubeMaterial;
    [SerializeField] private float cubeSize;
    [SerializeField] private Vector3 cubePos;
    [SerializeField] private Vector3 cubeRot;

    [Header("Pyramid")]
    [SerializeField] private bool showPyramid;
    [SerializeField] private Material pyramidMaterial;
    [SerializeField] private float pyramidSize;
    [SerializeField] private Vector3 pyramidPos;
    [SerializeField] private Vector3 pyramidRot;

    [Header("Cylinder")]
    [SerializeField] private bool showCylinder;
    [SerializeField] private Material cylinderMaterial;
    [SerializeField] private float cylinderRadius;
    [SerializeField] private float cylinderHeight;
    [SerializeField, Range(6, 64)] private int cylinderSegments;
    [SerializeField] private Vector3 cylinderPos;
    [SerializeField] private Vector3 cylinderRot;

    [Header("Rectangular Column")]
    [SerializeField] private bool showRectangularColumn;
    [SerializeField] private Material columnMaterial;
    [SerializeField] private float columnSize;
    [SerializeField] private float columnHeight;
    [SerializeField] private Vector3 columnPos;
    [SerializeField] private Vector3 columnRot;

    [Header("Sphere")]
    [SerializeField] private bool showSphere;
    [SerializeField] private Material sphereMaterial;
    [SerializeField] private float sphereRadius;
    [SerializeField, Range(6, 64)] private int sphereLatitudeSegments;
    [SerializeField, Range(6, 64)] private int sphereLongitudeSegments;
    [SerializeField] private Vector3 spherePos;
    [SerializeField] private Vector3 sphereRot;

    [Header("Capsule")]
    [SerializeField] private bool showCapsule;
    [SerializeField] private Material capsuleMaterial;
    [SerializeField] private float capsuleRadius;
    [SerializeField] private float capsuleHeight;
    [SerializeField, Range(6, 64)] private int capsuleLatitudeSegments;
    [SerializeField, Range(6, 64)] private int capsuleLongitudeSegments;
    [SerializeField] private Vector3 capsulePos;
    [SerializeField] private Vector3 capsuleRot;

    private void OnRenderObject()
    {
        GL.PushMatrix();

        if (showCube) DrawCube();
        if (showPyramid) DrawPyramid();
        if (showCylinder) DrawCylinder();
        if (showRectangularColumn) DrawRectangularColumn();
        if (showSphere) DrawSphere();
        if (showCapsule) DrawCapsule();

        GL.PopMatrix();
    }

    private Vector3 ApplyRotation(Vector3 point, Vector3 pivot, Vector3 rotation)
    {
        Quaternion q = Quaternion.Euler(rotation);
        return q * (point - pivot) + pivot;
    }

    private void DrawCube()
    {
        if (!showCube || cubeMaterial == null) return;

        cubeMaterial.SetPass(0);
        GL.Begin(GL.LINES);

        Vector3[] v =
        {
            new(-1,  1, -1), new( 1,  1, -1), new( 1, -1, -1), new(-1, -1, -1),
            new(-1,  1,  1), new( 1,  1,  1), new( 1, -1,  1), new(-1, -1,  1)
        };

        for (int i = 0; i < v.Length; i++)
            v[i] = ApplyRotation(v[i] * (cubeSize / 2f), Vector3.zero, cubeRot) + cubePos;

        for (int i = 0; i < 4; i++)
            DrawLine(ProjectPoint(v[i]), ProjectPoint(v[(i + 1) % 4]));

        for (int i = 4; i < 8; i++)
            DrawLine(ProjectPoint(v[i]), ProjectPoint(v[4 + ((i + 1) % 4)]));

        for (int i = 0; i < 4; i++)
            DrawLine(ProjectPoint(v[i]), ProjectPoint(v[i + 4]));

        GL.End();
    }

    private void DrawPyramid()
    {
        if (!showPyramid || pyramidMaterial == null) return;

        pyramidMaterial.SetPass(0);
        GL.Begin(GL.LINES);

        Vector3[] baseVerts =
        {
            new(-1, 0, -1), new(1, 0, -1), new(1, 0, 1), new(-1, 0, 1)
        };

        Vector3 tip = new(0, 1.5f, 0);

        for (int i = 0; i < 4; i++)
            baseVerts[i] = ApplyRotation(baseVerts[i] * pyramidSize, Vector3.zero, pyramidRot) + pyramidPos;

        tip = ApplyRotation(tip * pyramidSize, Vector3.zero, pyramidRot) + pyramidPos;

        for (int i = 0; i < 4; i++)
            DrawLine(ProjectPoint(baseVerts[i]), ProjectPoint(baseVerts[(i + 1) % 4]));

        for (int i = 0; i < 4; i++)
            DrawLine(ProjectPoint(baseVerts[i]), ProjectPoint(tip));

        GL.End();
    }

    private void DrawCylinder()
    {
        if (!showCylinder || cylinderMaterial == null) return;

        cylinderMaterial.SetPass(0);
        GL.Begin(GL.LINES);

        cylinderSegments = Mathf.Max(6, cylinderSegments);

        Vector3[] bottom = new Vector3[cylinderSegments];
        Vector3[] top = new Vector3[cylinderSegments];

        for (int i = 0; i < cylinderSegments; i++)
        {
            float angle = (2 * Mathf.PI / cylinderSegments) * i;
            float x = Mathf.Cos(angle) * cylinderRadius;
            float z = Mathf.Sin(angle) * cylinderRadius;

            bottom[i] = ApplyRotation(new Vector3(x, 0, z), Vector3.zero, cylinderRot) + cylinderPos;
            top[i] = ApplyRotation(new Vector3(x, cylinderHeight, z), Vector3.zero, cylinderRot) + cylinderPos;
        }

        for (int i = 0; i < cylinderSegments; i++)
        {
            int next = (i + 1) % cylinderSegments;
            DrawLine(ProjectPoint(bottom[i]), ProjectPoint(bottom[next]));
            DrawLine(ProjectPoint(top[i]), ProjectPoint(top[next]));
            DrawLine(ProjectPoint(bottom[i]), ProjectPoint(top[i]));
        }

        GL.End();
    }

    private void DrawRectangularColumn()
    {
        if (!showRectangularColumn || columnMaterial == null) return;

        columnMaterial.SetPass(0);
        GL.Begin(GL.LINES);

        Vector3[] baseVerts =
        {
            new(-1, 0, -1), new(1, 0, -1), new(1, 0, 1), new(-1, 0, 1)
        };

        Vector3[] topVerts = new Vector3[4];

        for (int i = 0; i < 4; i++)
        {
            baseVerts[i] = ApplyRotation(baseVerts[i] * columnSize, Vector3.zero, columnRot) + columnPos;
            topVerts[i] = baseVerts[i] + ApplyRotation(Vector3.up * columnHeight * columnSize, Vector3.zero, columnRot);
        }

        for (int i = 0; i < 4; i++)
        {
            DrawLine(ProjectPoint(baseVerts[i]), ProjectPoint(baseVerts[(i + 1) % 4]));
            DrawLine(ProjectPoint(topVerts[i]), ProjectPoint(topVerts[(i + 1) % 4]));
            DrawLine(ProjectPoint(baseVerts[i]), ProjectPoint(topVerts[i]));
        }

        GL.End();
    }

    private void DrawSphere()
    {
        if (!showSphere || sphereMaterial == null) return;

        sphereMaterial.SetPass(0);
        GL.Begin(GL.LINES);

        int latSeg = Mathf.Max(6, sphereLatitudeSegments);
        int lonSeg = Mathf.Max(6, sphereLongitudeSegments);

        for (int lat = 0; lat <= latSeg; lat++)
        {
            float theta = Mathf.PI * lat / latSeg;
            float y = Mathf.Cos(theta) * sphereRadius;
            float r = Mathf.Sin(theta) * sphereRadius;

            Vector3[] ring = new Vector3[lonSeg + 1];

            for (int lon = 0; lon <= lonSeg; lon++)
            {
                float phi = (2 * Mathf.PI / lonSeg) * lon;
                ring[lon] = new Vector3(r * Mathf.Cos(phi), y, r * Mathf.Sin(phi));
                ring[lon] = ApplyRotation(ring[lon], Vector3.zero, sphereRot) + spherePos;
            }

            for (int i = 0; i < lonSeg; i++)
                DrawLine(ProjectPoint(ring[i]), ProjectPoint(ring[i + 1]));
        }

        GL.End();
    }

    private void DrawCapsule()
    {
        if (!showCapsule || capsuleMaterial == null) return;

        capsuleMaterial.SetPass(0);
        GL.Begin(GL.LINES);

        int lonSeg = Mathf.Max(6, capsuleLongitudeSegments);
        float halfBody = capsuleHeight * 0.5f;

        for (int lon = 0; lon < lonSeg; lon++)
        {
            float phi = (2 * Mathf.PI / lonSeg) * lon;
            float nextPhi = (2 * Mathf.PI / lonSeg) * (lon + 1);

            Vector3 bottom1 = new(Mathf.Cos(phi) * capsuleRadius, -halfBody, Mathf.Sin(phi) * capsuleRadius);
            Vector3 top1 = new(Mathf.Cos(phi) * capsuleRadius, halfBody, Mathf.Sin(phi) * capsuleRadius);

            Vector3 bottom2 = new(Mathf.Cos(nextPhi) * capsuleRadius, -halfBody, Mathf.Sin(nextPhi) * capsuleRadius);
            Vector3 top2 = new(Mathf.Cos(nextPhi) * capsuleRadius, halfBody, Mathf.Sin(nextPhi) * capsuleRadius);

            bottom1 = ApplyRotation(bottom1, Vector3.zero, capsuleRot) + capsulePos;
            top1 = ApplyRotation(top1, Vector3.zero, capsuleRot) + capsulePos;
            bottom2 = ApplyRotation(bottom2, Vector3.zero, capsuleRot) + capsulePos;
            top2 = ApplyRotation(top2, Vector3.zero, capsuleRot) + capsulePos;

            DrawLine(ProjectPoint(bottom1), ProjectPoint(top1));
            DrawLine(ProjectPoint(bottom1), ProjectPoint(bottom2));
            DrawLine(ProjectPoint(top1), ProjectPoint(top2));
        }

        GL.End();
    }

    private Vector2 ProjectPoint(Vector3 point)
    {
        float perspective = PerspectiveCamera.Instance.GetPerspective(point.z);
        return new Vector2(point.x * perspective, point.y * perspective);
    }

    private void DrawLine(Vector2 a, Vector2 b)
    {
        GL.Vertex3(a.x, a.y, 0);
        GL.Vertex3(b.x, b.y, 0);
    }
}