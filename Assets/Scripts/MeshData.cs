using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshData
{
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<int> triangles = new List<int>();
    private readonly List<Vector2> UVs = new List<Vector2>();
    
    private readonly List<Vector3> colliderVertices = new List<Vector3>();
    private readonly List<int> colliderTriangles = new List<int>();

    public MeshData() { }

    public Vector3[] GetVertices()
    {
        return vertices.ToArray();
    }

    public int[] GetTriangles()
    {
        return triangles.ToArray();
    }

    public Vector2[] GetUVs()
    {
        return UVs.ToArray();
    }
    
    public Vector3[] GetColliderVertices()
    {
        return colliderVertices.ToArray();
    }

    public int[] GetColliderTriangles()
    {
        return colliderTriangles.ToArray();
    }
    
    public void AddQuad(Vector3 lowerRight, Vector3 upperRight, Vector3 upperLeft, Vector3 lowerLeft, Vector2[] quadUVs, bool hasCollision)
    {
        vertices.Add(lowerRight);
        vertices.Add(upperRight);
        vertices.Add(upperLeft);
        vertices.Add(lowerLeft);
    
        if (hasCollision)
        {
            colliderVertices.Add(lowerRight);
            colliderVertices.Add(upperRight);
            colliderVertices.Add(upperLeft);
            colliderVertices.Add(lowerLeft);
        }
        
        triangles.Add(vertices.Count - 4);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 2);
        
        triangles.Add(vertices.Count - 4);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 1);
        
        if (hasCollision)
        {
            colliderTriangles.Add(colliderVertices.Count - 4);
            colliderTriangles.Add(colliderVertices.Count - 3);
            colliderTriangles.Add(colliderVertices.Count - 2);
            
            colliderTriangles.Add(colliderVertices.Count - 4);
            colliderTriangles.Add(colliderVertices.Count - 2);
            colliderTriangles.Add(colliderVertices.Count - 1);
        }
        
        UVs.AddRange(quadUVs);
    }
}
