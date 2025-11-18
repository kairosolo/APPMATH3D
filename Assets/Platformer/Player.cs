using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material originalMaterial;
    [SerializeField] private Material collidedMaterial;

    [Header("References")]
    [SerializeField] private Platform platform;

    [Header("Cube")]
    [SerializeField] private float cubeSize = 1f;
    [SerializeField] private Vector2 startingPosition = new Vector2(0, -4);

    [Header("Physics")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundingTolerance = 0.1f;

    private Material currentMaterial;
    private Vector2 cubePos;
    private Vector2 cubeVelocity;
    private bool isGrounded;

    public struct BoxDimensions
    {
        public float minX, minY, maxX, maxY;

        public bool Collide(BoxDimensions other)
        {
            return (minX < other.maxX && maxX > other.minX && minY <= other.maxY && maxY >= other.minY);
        }
    }

    private void Start()
    {
        cubePos = startingPosition;
        currentMaterial = originalMaterial;
    }

    private void Update()
    {
        if (platform == null) return;
        HandleMovement();
        HandleCollision();
    }

    private void HandleMovement()
    {
        if (!isGrounded)
        {
            cubeVelocity.y += gravity * Time.deltaTime;
        }

        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            cubeVelocity.y = jumpForce;
        }

        cubePos += cubeVelocity * Time.deltaTime;
    }

    private void HandleCollision()
    {
        var cubeDimensions = new BoxDimensions
        {
            minX = cubePos.x - cubeSize / 2,
            maxX = cubePos.x + cubeSize / 2,
            minY = cubePos.y - cubeSize / 2,
            maxY = cubePos.y + cubeSize / 2,
        };
        var platformDimensions = platform.GetDimensions();

        bool isColliding = cubeDimensions.Collide(platformDimensions);
        float verticalDistance = cubeDimensions.minY - platformDimensions.maxY;
        bool isInToleranceZone = (verticalDistance > 0 && verticalDistance < groundingTolerance);
        isGrounded = isColliding || isInToleranceZone;

        if (isGrounded)
        {
            if (isColliding)
            {
                cubePos.y = platformDimensions.maxY + cubeSize / 2;
                cubeVelocity.y = 0;
            }
            currentMaterial = collidedMaterial;
        }
        else
        {
            currentMaterial = originalMaterial;
        }
    }

    private void OnRenderObject()
    {
        if (currentMaterial == null) return;

        GL.PushMatrix();
        currentMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(currentMaterial.color);
        DrawWireframe(GetFace(cubePos, cubeSize));
        GL.End();
        GL.PopMatrix();
    }

    private Vector2[] GetFace(Vector2 pos, float size)
    { return GetFace(pos, new Vector2(size, size)); }

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