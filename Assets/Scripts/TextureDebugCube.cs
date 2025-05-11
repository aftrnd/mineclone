using UnityEngine;
using System.Collections.Generic;

public class TextureDebugCube : MonoBehaviour
{
    public Material material;
    [Range(0, 3)]
    public int textureIndex = 0;
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    
    void Start()
    {
        // Create a simple cube mesh with explicit UVs and texture indices
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        if (meshFilter == null || meshRenderer == null)
        {
            Debug.LogError("Missing MeshFilter or MeshRenderer component");
            return;
        }
        
        // Assign the material if provided
        if (material != null)
        {
            meshRenderer.material = material;
        }
        
        // Create mesh with UV1 channel for texture index
        CreateCubeMesh();
    }
    
    void Update()
    {
        // Allow changing texture index at runtime for testing
        if (meshFilter.mesh != null && textureIndex >= 0)
        {
            List<Vector2> textureIndices = new List<Vector2>();
            for (int i = 0; i < meshFilter.mesh.vertexCount; i++)
            {
                textureIndices.Add(new Vector2(textureIndex, 0));
            }
            meshFilter.mesh.SetUVs(1, textureIndices);
        }
    }
    
    void CreateCubeMesh()
    {
        Mesh mesh = new Mesh();
        
        // Cube vertices (8 corners)
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0, 0), // 0: back bottom left
            new Vector3(1, 0, 0), // 1: back bottom right  
            new Vector3(0, 1, 0), // 2: back top left
            new Vector3(1, 1, 0), // 3: back top right
            new Vector3(0, 0, 1), // 4: front bottom left
            new Vector3(1, 0, 1), // 5: front bottom right
            new Vector3(0, 1, 1), // 6: front top left
            new Vector3(1, 1, 1)  // 7: front top right
        };
        
        // Regular UVs (0-1 range for each face)
        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), // Front
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), // Back
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), // Top
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), // Bottom
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), // Left
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1)  // Right
        };
        
        // Create triangles for each face
        int[] triangles = new int[]
        {
            // Front (Z+)
            4, 5, 7, 4, 7, 6,
            // Back (Z-)
            1, 0, 2, 1, 2, 3,
            // Top (Y+)
            2, 6, 7, 2, 7, 3,
            // Bottom (Y-)
            0, 1, 5, 0, 5, 4,
            // Left (X-)
            0, 4, 6, 0, 6, 2,
            // Right (X+)
            5, 1, 3, 5, 3, 7
        };
        
        // Create the expanded vertex array for the cube (24 vertices, 4 per face)
        Vector3[] expandedVertices = new Vector3[24];
        
        // Front face (Z+)
        expandedVertices[0] = vertices[4]; // Bottom left
        expandedVertices[1] = vertices[5]; // Bottom right
        expandedVertices[2] = vertices[6]; // Top left
        expandedVertices[3] = vertices[7]; // Top right
        
        // Back face (Z-)
        expandedVertices[4] = vertices[1]; // Bottom right
        expandedVertices[5] = vertices[0]; // Bottom left
        expandedVertices[6] = vertices[3]; // Top right
        expandedVertices[7] = vertices[2]; // Top left
        
        // Top face (Y+)
        expandedVertices[8] = vertices[2]; // Back left
        expandedVertices[9] = vertices[3]; // Back right
        expandedVertices[10] = vertices[6]; // Front left
        expandedVertices[11] = vertices[7]; // Front right
        
        // Bottom face (Y-)
        expandedVertices[12] = vertices[0]; // Back left
        expandedVertices[13] = vertices[1]; // Back right
        expandedVertices[14] = vertices[4]; // Front left
        expandedVertices[15] = vertices[5]; // Front right
        
        // Left face (X-)
        expandedVertices[16] = vertices[0]; // Back bottom
        expandedVertices[17] = vertices[4]; // Front bottom
        expandedVertices[18] = vertices[2]; // Back top
        expandedVertices[19] = vertices[6]; // Front top
        
        // Right face (X+)
        expandedVertices[20] = vertices[5]; // Front bottom
        expandedVertices[21] = vertices[1]; // Back bottom
        expandedVertices[22] = vertices[7]; // Front top
        expandedVertices[23] = vertices[3]; // Back top
        
        // Create the texture index UV set (UV1)
        List<Vector2> textureIndices = new List<Vector2>();
        for (int i = 0; i < 24; i++)
        {
            textureIndices.Add(new Vector2(textureIndex, 0));
        }
        
        // Expanded triangles
        int[] expandedTriangles = new int[]
        {
            0, 1, 3, 0, 3, 2,       // Front
            4, 5, 7, 4, 7, 6,       // Back
            8, 9, 11, 8, 11, 10,    // Top
            12, 13, 15, 12, 15, 14, // Bottom
            16, 17, 19, 16, 19, 18, // Left
            20, 21, 23, 20, 23, 22  // Right
        };
        
        // Apply to mesh
        mesh.vertices = expandedVertices;
        mesh.uv = uvs;
        mesh.triangles = expandedTriangles;
        mesh.SetUVs(1, textureIndices);
        
        mesh.RecalculateNormals();
        
        // Assign to the mesh filter
        meshFilter.mesh = mesh;
    }
} 