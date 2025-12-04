using System.Collections.Generic;
using UnityEngine;

public class BoxCollider3D
{
    public int ID;
    public Vector3 Center;
    public Vector3 Size;
    public Vector3 Min;
    public Vector3 Max;
    public Matrix4x4 Matrix;

    public BoxCollider3D(int id, Vector3 center, Vector3 size)
    {
        ID = id;
        UpdatePosition(center, size);
    }

    public void UpdatePosition(Vector3 center, Vector3 size)
    {
        Center = center;
        Size = size;
        Vector3 extents = size * 0.5f;
        Min = center - extents;
        Max = center + extents;
        Matrix = Matrix4x4.TRS(center, Quaternion.identity, size);
    }

    public bool IsColliding(BoxCollider3D other)
    {
        if (Max.x < other.Min.x || Min.x > other.Max.x) return false;
        if (Max.y < other.Min.y || Min.y > other.Max.y) return false;
        if (Max.z < other.Min.z || Min.z > other.Max.z) return false;
        return true;
    }
}

public class CollisionManager : MonoBehaviour
{
    public static CollisionManager Instance;

    private Dictionary<int, BoxCollider3D> colliders = new Dictionary<int, BoxCollider3D>();
    private int idCounter = 0;

    private void Awake()
    {
        Instance = this;
    }

    public int AddBox(Vector3 pos, Vector3 size)
    {
        int id = idCounter++;
        colliders.Add(id, new BoxCollider3D(id, pos, size));
        return id;
    }

    public void UpdateBox(int id, Vector3 pos, Vector3 size)
    {
        if (colliders.ContainsKey(id))
            colliders[id].UpdatePosition(pos, size);
    }

    public void RemoveBox(int id)
    {
        if (colliders.ContainsKey(id)) colliders.Remove(id);
    }

    public bool HasCollider(int id)
    {
        return colliders.ContainsKey(id);
    }

    public Matrix4x4 GetMatrix(int id)
    {
        if (colliders.ContainsKey(id)) return colliders[id].Matrix;
        return Matrix4x4.identity;
    }

    public BoxCollider3D GetCollider(int id)
    {
        if (colliders.ContainsKey(id)) return colliders[id];
        return null;
    }

    public Vector3 GetSize(int id)
    {
        if (colliders.ContainsKey(id)) return colliders[id].Size;
        return Vector3.one;
    }

    public void ForceSetMatrix(int id, Matrix4x4 matrix)
    {
        if (!colliders.ContainsKey(id)) return;
        var c = colliders[id];
        c.Matrix = matrix;
        c.UpdatePosition(matrix.GetPosition(), c.Size);
    }

    public List<int> CheckCollisions(int id, Vector3 pendingPos)
    {
        List<int> hits = new List<int>();
        if (!colliders.ContainsKey(id)) return hits;

        BoxCollider3D baseBox = colliders[id];
        BoxCollider3D temp = new BoxCollider3D(-1, pendingPos, baseBox.Size);

        foreach (var kvp in colliders)
        {
            if (kvp.Key == id) continue;
            if (temp.IsColliding(kvp.Value))
            {
                hits.Add(kvp.Key);
            }
        }
        return hits;
    }

    public bool IsSpaceOccupied(Vector3 center, Vector3 size)
    {
        BoxCollider3D temp = new BoxCollider3D(-999, center, size);

        foreach (var kvp in colliders)
        {
            if (temp.IsColliding(kvp.Value))
            {
                return true;
            }
        }
        return false;
    }
}