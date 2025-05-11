using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour {
    public static int chunkSize = 16;
    private Block[,,] blocks = new Block[chunkSize, chunkSize, chunkSize];

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    
    [SerializeField] private Material blockMaterial;

    void Start() {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        
        // Set the appropriate tag and layer
        gameObject.tag = "Block";
        
        // Set the layer if MinecraftLayers exists
        if (System.Type.GetType("MinecraftLayers") != null)
        {
            gameObject.layer = MinecraftLayers.BlockLayer;
        }
        
        // Initialize texture manager if not initialized yet
        if (!BlockTextureManager.isInitialized) {
            BlockTextureManager.Initialize();
        }
        
        // Ensure we're using the BlockMaterial
        if (blockMaterial != null) {
            meshRenderer.material = blockMaterial;
            // Make sure the texture array is assigned to the material
            blockMaterial.SetTexture("_BlockTextureArray", BlockTextureManager.textureArray);
            blockMaterial.SetTexture("_TextureArray", BlockTextureManager.textureArray);
        } else {
            Debug.LogWarning("Block material not assigned to Chunk! Using default material.");
            // Try to find the material by name
            blockMaterial = Resources.Load<Material>("BlockMaterial");
            if (blockMaterial != null) {
                meshRenderer.material = blockMaterial;
                // Make sure the texture array is assigned to the material
                blockMaterial.SetTexture("_BlockTextureArray", BlockTextureManager.textureArray);
                blockMaterial.SetTexture("_TextureArray", BlockTextureManager.textureArray);
            }
        }
        
        GenerateBlocks();
        GenerateMesh();
    }

    void GenerateBlocks() {
        for (int x = 0; x < chunkSize; x++) {
            for (int z = 0; z < chunkSize; z++) {
                // Find the top layer for each x,z column (half the chunk height)
                int topY = chunkSize / 2 - 1;
                
                for (int y = 0; y < chunkSize; y++) {
                    if (y < chunkSize / 2) {
                        // Set the top layer to grass, everything below to dirt
                        if (y == topY) {
                            blocks[x, y, z] = new Block(BlockType.Grass);
                        } else {
                            blocks[x, y, z] = new Block(BlockType.Dirt);
                        }
                    } else {
                        blocks[x, y, z] = new Block(BlockType.Air);
                    }
                }
            }
        }
    }

    void GenerateMesh() {
        MeshBuilder builder = new MeshBuilder();

        for (int x = 0; x < chunkSize; x++) {
            for (int y = 0; y < chunkSize; y++) {
                for (int z = 0; z < chunkSize; z++) {
                    Block block = blocks[x, y, z];
                    if (!block.IsSolid()) continue;

                    Vector3Int pos = new Vector3Int(x, y, z);

                    // Check each face direction
                    for (int i = 0; i < 6; i++) {
                        Vector3Int dir = Vector3Int.RoundToInt(MeshBuilder.FaceDirection(i));
                        Vector3Int neighborPos = pos + dir;

                        if (!InBounds(neighborPos) || !blocks[neighborPos.x, neighborPos.y, neighborPos.z].IsSolid()) {
                            builder.AddFace(pos, i, block.GetTextureID(i)); // Only add face if neighbor is air or out of bounds
                        }
                    }
                }
            }
        }

        Mesh mesh = builder.Build();
        meshFilter.mesh = mesh;
        
        // Apply the same mesh to the collider
        meshCollider.sharedMesh = mesh;
        Debug.Log("Applied mesh to collider for chunk collision");
    }

    bool InBounds(Vector3Int pos) {
        return pos.x >= 0 && pos.x < chunkSize &&
               pos.y >= 0 && pos.y < chunkSize &&
               pos.z >= 0 && pos.z < chunkSize;
    }
}