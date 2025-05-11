using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour {
    public static int chunkSize = 16;
    private Block[,,] blocks = new Block[chunkSize, chunkSize, chunkSize];

    private MeshFilter meshFilter;

    void Start() {
        meshFilter = GetComponent<MeshFilter>();
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

                    Vector3 position = new Vector3(x, y, z);
                    builder.AddCube(position); // We'll define this shortly
                }
            }
        }

        meshFilter.mesh = builder.Build();
    }
}