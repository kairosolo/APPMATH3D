using UnityEngine;

public class Platform : MonoBehaviour
{
    [SerializeField] private Material groundMaterial;
    [SerializeField] private Vector2 platformPos = new Vector2(0, -5);
    [SerializeField] private float platformWidth = 20f;
    [SerializeField] private float platformHeight = 1f;

    public Player.BoxDimensions GetDimensions()
    {
        return new Player.BoxDimensions
        {
            minX = platformPos.x - platformWidth / 2,
            maxX = platformPos.x + platformWidth / 2,
            minY = platformPos.y - platformHeight / 2,
            maxY = platformPos.y + platformHeight / 2,
        };
    }

    private void OnRenderObject()
    {
        if (groundMaterial == null) return;

        GL.PushMatrix();
        groundMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(groundMaterial.color);
        DrawWireframe(GetFace(platformPos, new Vector2(platformWidth, platformHeight)));
        GL.End();
        GL.PopMatrix();
    }

    private Vector2[] GetFace(Vector2 pos, Vector2 size)
    {
        var faceArray = new Vector2[]
        {
            new Vector2(size.x / 2, size.y / 2), new Vector2(-size.x / 2, size.y / 2),
            new Vector2(-size.x / 2, -size.y / 2), new Vector2(size.x / 2, -size.y / 2),
        };
        for (var i = 0; i < faceArray.Length; i++) faceArray[i] += pos;
        return faceArray;
    }

    private void DrawWireframe(Vector2[] squareElements)
    {
        for (var i = 0; i < squareElements.Length; i++)
        {
            GL.Vertex(squareElements[i]);
            GL.Vertex(squareElements[(i + 1) % squareElements.Length]);
        }
    }
}