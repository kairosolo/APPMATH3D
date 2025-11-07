using UnityEngine;

public class ShapeGenerator : MonoBehaviour
{
    [Header("Cube")]
    [SerializeField] private bool showCube;
    [SerializeField] private Material cubeMaterial;
    [SerializeField] private float cubeSize;
    [SerializeField] private Vector3 cubePos;

    [Header("Pyramid")]
    [SerializeField] private bool showPyramid;
    [SerializeField] private Material pyramidMaterial;
    [SerializeField] private float pyramidSize;
    [SerializeField] private Vector3 pyramidPos;

    [Header("Cylinder")]
    [SerializeField] private bool showCylinder;
    [SerializeField] private Material cylinderMaterial;
    [SerializeField] private float cylinderRadius;
    [SerializeField] private float cylinderHeight;
    [SerializeField, Range(6, 64)] private int cylinderSegments;
    [SerializeField] private Vector3 cylinderPos;

    [Header("Rectangular Column")]
    [SerializeField] private bool showRectangularColumn;
    [SerializeField] private Material columnMaterial;
    [SerializeField] private float columnSize;
    [SerializeField] private float columnHeight;
    [SerializeField] private Vector3 columnPos;

    [Header("Sphere")]
    [SerializeField] private bool showSphere;
    [SerializeField] private Material sphereMaterial;
    [SerializeField] private float sphereRadius;
    [SerializeField, Range(6, 64)] private int sphereLatitudeSegments;
    [SerializeField, Range(6, 64)] private int sphereLongitudeSegments;
    [SerializeField] private Vector3 spherePos;

    [Header("Capsule")]
    [SerializeField] private bool showCapsule;
    [SerializeField] private Material capsuleMaterial;
    [SerializeField] private float capsuleRadius;
    [SerializeField] private float capsuleHeight;
    [SerializeField, Range(6, 64)] private int capsuleLatitudeSegments;
    [SerializeField, Range(6, 64)] private int capsuleLongitudeSegments;
    [SerializeField] private Vector3 capsulePos;

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

    private void DrawCube()
    {
        if (!showCube || cubeMaterial == null) return;

        cubeMaterial.SetPass(0);
        GL.Begin(GL.LINES);

        float half = cubeSize / 2f;

        Vector2[] backSquare = CreateSquare(new Vector2(cubePos.x, cubePos.y), cubeSize);
        Vector2[] frontSquare = CreateSquare(new Vector2(cubePos.x, cubePos.y), cubeSize);

        float backPerspective = PerspectiveCamera.Instance.GetPerspective(cubePos.z - half);
        float frontPerspective = PerspectiveCamera.Instance.GetPerspective(cubePos.z + half);

        Vector2[] backRendered = RenderSquare(backSquare, backPerspective);
        Vector2[] frontRendered = RenderSquare(frontSquare, frontPerspective);

        for (int i = 0; i < 4; i++)
        {
            GL.Vertex3(backRendered[i].x, backRendered[i].y, 0);
            GL.Vertex3(frontRendered[i].x, frontRendered[i].y, 0);
        }

        GL.End();
    }

    private void DrawPyramid()
    {
        if (!showPyramid || pyramidMaterial == null) return;

        pyramidMaterial.SetPass(0);
        GL.Begin(GL.LINES);

        Vector3[] baseVerts =
        {
            new(-1, 0, -1), new(1, 0, -1),
            new(1, 0, 1),  new(-1, 0, 1)
        };

        Vector3 tip = new(0, 1.5f, 0);
        for (int i = 0; i < baseVerts.Length; i++) baseVerts[i] = baseVerts[i] * pyramidSize + pyramidPos;
        tip = tip * pyramidSize + pyramidPos;

        for (int i = 0; i < 4; i++)
            DrawLine(ProjectPoint(baseVerts[i]), ProjectPoint(baseVerts[(i + 1) % 4]));

        foreach (var basePoint in baseVerts)
            DrawLine(ProjectPoint(basePoint), ProjectPoint(tip));

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

            bottom[i] = new Vector3(x, 0, z) + cylinderPos;
            top[i] = new Vector3(x, cylinderHeight, z) + cylinderPos;
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
            new(-1, 0, -1), new(1, 0, -1),
            new(1, 0, 1),  new(-1, 0, 1)
        };

        Vector3[] topVerts = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            baseVerts[i] = baseVerts[i] * columnSize + columnPos;
            topVerts[i] = baseVerts[i] + Vector3.up * columnHeight * columnSize;
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
                ring[lon] = new Vector3(r * Mathf.Cos(phi), y, r * Mathf.Sin(phi)) + spherePos;
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

        int latSeg = Mathf.Max(6, capsuleLatitudeSegments);
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

            DrawLine(ProjectPoint(bottom1 + capsulePos), ProjectPoint(top1 + capsulePos));
            DrawLine(ProjectPoint(bottom1 + capsulePos), ProjectPoint(bottom2 + capsulePos));
            DrawLine(ProjectPoint(top1 + capsulePos), ProjectPoint(top2 + capsulePos));
        }

        GL.End();
    }

    private Vector2[] CreateSquare(Vector2 position, float size)
    {
        return new Vector2[]
        {
            position + new Vector2( 1,  1) * size,
            position + new Vector2(-1,  1) * size,
            position + new Vector2(-1, -1) * size,
            position + new Vector2( 1, -1) * size
        };
    }

    private Vector2[] RenderSquare(Vector2[] square, float perspective)
    {
        Vector2[] output = new Vector2[square.Length];
        for (int i = 0; i < square.Length; i++)
        {
            output[i] = square[i] * perspective;
            GL.Vertex(square[i] * perspective);
            GL.Vertex(square[(i + 1) % square.Length] * perspective);
        }
        return output;
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