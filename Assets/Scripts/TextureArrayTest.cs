using UnityEngine;

public class TextureArrayTest : MonoBehaviour
{
    [Header("Texture Settings")]
    public Material blockMaterial;
    
    [Range(0, 3)]
    public int textureIndex = 0;
    
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    
    void Start()
    {
        // Ensure textures are initialized
        if (!BlockTextureManager.isInitialized)
        {
            BlockTextureManager.Initialize();
        }
        
        // Get or add required components
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        
        // Create a simple cube mesh
        meshFilter.mesh = CreateCubeMeshWithTextureIndex(textureIndex);
        
        // Use the provided block material
        if (blockMaterial != null)
        {
            meshRenderer.material = blockMaterial;
            Debug.Log($"Applied BlockMaterial to test cube with texture index {textureIndex}");
        }
        else
        {
            Debug.LogError("Block material not assigned!");
        }
    }
    
    void Update()
    {
        // Update the texture index if it changed
        if (meshFilter != null && meshFilter.mesh != null && textureIndex >= 0)
        {
            Vector2[] uvs = new Vector2[meshFilter.mesh.vertexCount];
            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i] = new Vector2(textureIndex, 0);
            }
            meshFilter.mesh.uv2 = uvs; // Unity uses uv2 for UV1 channel
        }
    }
    
    Mesh CreateCubeMeshWithTextureIndex(int index)
    {
        Mesh mesh = new Mesh();
        
        // Cube vertices (8 corners)
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, -0.5f), // 0: back bottom left
            new Vector3(0.5f, -0.5f, -0.5f),  // 1: back bottom right  
            new Vector3(-0.5f, 0.5f, -0.5f),  // 2: back top left
            new Vector3(0.5f, 0.5f, -0.5f),   // 3: back top right
            new Vector3(-0.5f, -0.5f, 0.5f),  // 4: front bottom left
            new Vector3(0.5f, -0.5f, 0.5f),   // 5: front bottom right
            new Vector3(-0.5f, 0.5f, 0.5f),   // 6: front top left
            new Vector3(0.5f, 0.5f, 0.5f)     // 7: front top right
        };
        
        // Simple UVs 
        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), // Front
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), // Back
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), // Top
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), // Bottom
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), // Left
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1)  // Right
        };
        
        // Triangles for each face
        int[] triangles = new int[]
        {
            // Front face (Z+)
            4, 5, 7, 4, 7, 6,
            // Back face (Z-)
            1, 0, 2, 1, 2, 3,
            // Top face (Y+)
            2, 3, 7, 2, 7, 6,
            // Bottom face (Y-)
            0, 1, 5, 0, 5, 4,
            // Left face (X-)
            0, 4, 6, 0, 6, 2,
            // Right face (X+)
            5, 1, 3, 5, 3, 7
        };
        
        // Expanded vertices (24 vertices, 4 per face)
        Vector3[] expandedVerts = new Vector3[24];
        Vector2[] expandedUVs = new Vector2[24];
        
        // Front face
        expandedVerts[0] = vertices[4]; expandedUVs[0] = new Vector2(0, 0); 
        expandedVerts[1] = vertices[5]; expandedUVs[1] = new Vector2(1, 0);
        expandedVerts[2] = vertices[6]; expandedUVs[2] = new Vector2(0, 1);
        expandedVerts[3] = vertices[7]; expandedUVs[3] = new Vector2(1, 1);
        
        // Back face
        expandedVerts[4] = vertices[1]; expandedUVs[4] = new Vector2(0, 0);
        expandedVerts[5] = vertices[0]; expandedUVs[5] = new Vector2(1, 0);
        expandedVerts[6] = vertices[3]; expandedUVs[6] = new Vector2(0, 1);
        expandedVerts[7] = vertices[2]; expandedUVs[7] = new Vector2(1, 1);
        
        // Top face
        expandedVerts[8] = vertices[2]; expandedUVs[8] = new Vector2(0, 0);
        expandedVerts[9] = vertices[3]; expandedUVs[9] = new Vector2(1, 0);
        expandedVerts[10] = vertices[6]; expandedUVs[10] = new Vector2(0, 1);
        expandedVerts[11] = vertices[7]; expandedUVs[11] = new Vector2(1, 1);
        
        // Bottom face
        expandedVerts[12] = vertices[0]; expandedUVs[12] = new Vector2(0, 0);
        expandedVerts[13] = vertices[1]; expandedUVs[13] = new Vector2(1, 0);
        expandedVerts[14] = vertices[4]; expandedUVs[14] = new Vector2(0, 1);
        expandedVerts[15] = vertices[5]; expandedUVs[15] = new Vector2(1, 1);
        
        // Left face
        expandedVerts[16] = vertices[0]; expandedUVs[16] = new Vector2(0, 0);
        expandedVerts[17] = vertices[4]; expandedUVs[17] = new Vector2(1, 0);
        expandedVerts[18] = vertices[2]; expandedUVs[18] = new Vector2(0, 1);
        expandedVerts[19] = vertices[6]; expandedUVs[19] = new Vector2(1, 1);
        
        // Right face
        expandedVerts[20] = vertices[5]; expandedUVs[20] = new Vector2(0, 0);
        expandedVerts[21] = vertices[1]; expandedUVs[21] = new Vector2(1, 0);
        expandedVerts[22] = vertices[7]; expandedUVs[22] = new Vector2(0, 1);
        expandedVerts[23] = vertices[3]; expandedUVs[23] = new Vector2(1, 1);
        
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
        
        // Create UV1 for texture array index
        Vector2[] texIndices = new Vector2[24];
        for (int i = 0; i < 24; i++)
        {
            texIndices[i] = new Vector2(index, 0);
        }
        
        // Apply to mesh
        mesh.vertices = expandedVerts;
        mesh.uv = expandedUVs;
        mesh.triangles = expandedTriangles;
        mesh.uv2 = texIndices; // UV1 channel (texture indices)
        
        mesh.RecalculateNormals();
        
        return mesh;
    }
} 