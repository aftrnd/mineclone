using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour {
    public static int chunkSize = 16;
    private Block[,,] blocks = new Block[chunkSize, chunkSize, chunkSize];

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    
    [SerializeField] private Material blockMaterial;

    void Start() {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        // Ensure we're using the BlockMaterial
        if (blockMaterial != null) {
            meshRenderer.material = blockMaterial;
        } else {
            Debug.LogWarning("Block material not assigned to Chunk! Using default material.");
            // Try to find the material by name
            blockMaterial = Resources.Load<Material>("BlockMaterial");
            if (blockMaterial != null) {
                meshRenderer.material = blockMaterial;
            }
        }
        
        GenerateBlocks();
        GenerateMesh();
    }

    void GenerateBlocks() {
        for (int x = 0; x < chunkSize; x++) {
            for (int y = 0; y < chunkSize; y++) {
                for (int z = 0; z < chunkSize; z++) {
                    if (y < chunkSize / 2)
                        blocks[x, y, z] = new Block(BlockType.Dirt);
                    else
                        blocks[x, y, z] = new Block(BlockType.Air);
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

        meshFilter.mesh = builder.Build();
    }

    bool InBounds(Vector3Int pos) {
        return pos.x >= 0 && pos.x < chunkSize &&
               pos.y >= 0 && pos.y < chunkSize &&
               pos.z >= 0 && pos.z < chunkSize;
    }
}