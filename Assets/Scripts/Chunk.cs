using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour {
    public static int chunkSize = 16;
    private Block[,,] blocks = new Block[chunkSize, chunkSize, chunkSize];

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    
    [SerializeField] private Material blockMaterial;
    
    // Chunk position in the grid
    public Vector2Int ChunkPosition { get; private set; }
    
    // Reference to the world manager
    private WorldManager worldManager;

    // Initialize the chunk with position
    public void Initialize(Vector2Int chunkPosition)
    {
        ChunkPosition = chunkPosition;
        
        // Find the world manager if not set
        if (worldManager == null)
        {
            worldManager = FindObjectOfType<WorldManager>();
        }
        
        GenerateBlocks();
    }

    void Awake() {
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
        
        // Find the world manager
        worldManager = FindObjectOfType<WorldManager>();
    }

    void Start() {
        // If chunk position not set, default to position based on Transform
        if (ChunkPosition == default)
        {
            Vector3 pos = transform.position;
            ChunkPosition = new Vector2Int(
                Mathf.FloorToInt(pos.x / chunkSize),
                Mathf.FloorToInt(pos.z / chunkSize)
            );
            GenerateBlocks();
        }
        
        GenerateMesh();
    }

    void GenerateBlocks() {
        try
        {
            // If no world manager, use simple terrain generation
            if (worldManager == null)
            {
                Debug.LogWarning($"No WorldManager found for chunk at {ChunkPosition}. Using simple terrain generation.");
                GenerateSimpleTerrain();
                return;
            }
            
            // Get world position for this chunk
            Vector3 worldPosition = new Vector3(
                ChunkPosition.x * chunkSize,
                0,
                ChunkPosition.y * chunkSize
            );
            
            Debug.Log($"Generating chunk at {ChunkPosition} (world pos: {worldPosition})");
            
            for (int x = 0; x < chunkSize; x++) {
                for (int z = 0; z < chunkSize; z++) {
                    // World position of this block
                    float worldX = worldPosition.x + x;
                    float worldZ = worldPosition.z + z;
                    
                    // Get height from terrain generator
                    int terrainHeight = worldManager.GetTerrainHeight(worldX, worldZ);
                    
                    // Validate height (shouldn't be negative)
                    if (terrainHeight < 0)
                    {
                        Debug.LogError($"Terrain generator returned negative height ({terrainHeight}) at {worldX},{worldZ}");
                        terrainHeight = 0;
                    }
                    
                    // Debug occasional heights
                    if (x == 0 && z == 0)
                    {
                        Debug.Log($"Terrain height at chunk {ChunkPosition} origin: {terrainHeight}");
                    }
                    
                    for (int y = 0; y < chunkSize; y++) {
                        // World Y position
                        float worldY = worldPosition.y + y;
                        
                        // Default to air
                        BlockType blockType = BlockType.Air;
                        
                        // If below terrain height, it's solid ground
                        if (y < terrainHeight)
                        {
                            // Check if this should be a cave
                            if (worldManager.IsCave(worldX, worldY, worldZ))
                            {
                                blockType = BlockType.Air;
                            }
                            else if (y == terrainHeight - 1)
                            {
                                // Top block is grass
                                blockType = BlockType.Grass;
                            }
                            else if (y > terrainHeight - 4)
                            {
                                // A few blocks below the surface are dirt
                                blockType = BlockType.Dirt;
                            }
                            else
                            {
                                // Everything else is stone
                                blockType = BlockType.Stone;
                            }
                        }
                        
                        // Create the block of appropriate type
                        blocks[x, y, z] = new Block(blockType);
                    }
                }
            }
            
            Debug.Log($"Finished generating chunk at {ChunkPosition}");
        }
        catch (System.Exception e)
        {
            // Log the error and generate simple terrain as fallback
            Debug.LogError($"Error generating chunk at {ChunkPosition}: {e.Message}\n{e.StackTrace}");
            GenerateSimpleTerrain();
        }
    }
    
    // Simple terrain generation if no world manager is available
    private void GenerateSimpleTerrain()
    {
        // Get world position for this chunk
        Vector3 worldPosition = new Vector3(
            ChunkPosition.x * chunkSize,
            0,
            ChunkPosition.y * chunkSize
        );
        
        for (int x = 0; x < chunkSize; x++) {
            for (int z = 0; z < chunkSize; z++) {
                // World position of this block
                float worldX = worldPosition.x + x;
                float worldZ = worldPosition.z + z;
                
                // Simple terrain height formula
                float height = Mathf.PerlinNoise(worldX * 0.1f, worldZ * 0.1f) * 8 + 4;
                int terrainHeight = Mathf.FloorToInt(height);
                
                for (int y = 0; y < chunkSize; y++) {
                    // Terrain generation rules
                    if (y < terrainHeight) {
                        if (y == terrainHeight - 1) {
                            // Top block is grass
                            blocks[x, y, z] = new Block(BlockType.Grass);
                        } else if (y > terrainHeight - 4) {
                            // A few blocks below the surface are dirt
                            blocks[x, y, z] = new Block(BlockType.Dirt);
                        } else {
                            // Everything else is stone
                            blocks[x, y, z] = new Block(BlockType.Stone);
                        }
                    } else {
                        // Above terrain is air
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

                        // If neighbor is in bounds, check if it's solid
                        if (InBounds(neighborPos)) {
                            if (!blocks[neighborPos.x, neighborPos.y, neighborPos.z].IsSolid()) {
                                builder.AddFace(pos, i, block.GetTextureID(i));
                            }
                        }
                        // If neighbor is out of bounds, check adjacent chunk
                        else {
                            // Get world position of this chunk
                            Vector3 worldPosition = transform.position;
                            
                            // Calculate world position of the neighbor block
                            Vector3 neighborWorldPos = worldPosition + new Vector3(
                                pos.x + dir.x,
                                pos.y + dir.y,
                                pos.z + dir.z
                            );
                            
                            // Check if the neighbor block is solid
                            BlockType neighborBlockType = worldManager != null ? 
                                worldManager.GetBlockAt(neighborWorldPos) : 
                                BlockType.Air;
                                
                            if (neighborBlockType == BlockType.Air) {
                                builder.AddFace(pos, i, block.GetTextureID(i));
                            }
                        }
                    }
                }
            }
        }

        Mesh mesh = builder.Build();
        meshFilter.mesh = mesh;
        
        // Apply the same mesh to the collider
        meshCollider.sharedMesh = mesh;
    }

    bool InBounds(Vector3Int pos) {
        return pos.x >= 0 && pos.x < chunkSize &&
               pos.y >= 0 && pos.y < chunkSize &&
               pos.z >= 0 && pos.z < chunkSize;
    }

    /// <summary>
    /// Removes a block at the specified position within the chunk
    /// </summary>
    public void RemoveBlock(int x, int y, int z)
    {
        // Validate position
        if (!InBounds(new Vector3Int(x, y, z)))
        {
            Debug.LogWarning($"Tried to remove block at invalid position: ({x}, {y}, {z})");
            return;
        }
        
        // Set the block to air
        Debug.Log($"Removing block at local position ({x}, {y}, {z})");
        blocks[x, y, z] = new Block(BlockType.Air);
        
        // Regenerate the mesh to reflect the changes
        GenerateMesh();
    }

    /// <summary>
    /// Adds a block of the specified type at the position within the chunk
    /// </summary>
    public void AddBlock(int x, int y, int z, BlockType blockType)
    {
        // Validate position
        if (!InBounds(new Vector3Int(x, y, z)))
        {
            Debug.LogWarning($"Tried to add block at invalid position: ({x}, {y}, {z})");
            return;
        }
        
        // Place the block
        Debug.Log($"Adding block of type {blockType} at local position ({x}, {y}, {z})");
        blocks[x, y, z] = new Block(blockType);
        
        // Regenerate the mesh to reflect the changes
        GenerateMesh();
    }

    /// <summary>
    /// Gets the block type at the specified position within the chunk
    /// </summary>
    public BlockType GetBlockType(int x, int y, int z)
    {
        if (!InBounds(new Vector3Int(x, y, z)))
        {
            return BlockType.Air; // Return air for out of bounds
        }
        
        return blocks[x, y, z].GetBlockType();
    }

    /// <summary>
    /// Converts a world position to local chunk coordinates
    /// </summary>
    public Vector3Int WorldToLocalPosition(Vector3 worldPosition)
    {
        return new Vector3Int(
            Mathf.FloorToInt(worldPosition.x) - Mathf.FloorToInt(transform.position.x),
            Mathf.FloorToInt(worldPosition.y) - Mathf.FloorToInt(transform.position.y),
            Mathf.FloorToInt(worldPosition.z) - Mathf.FloorToInt(transform.position.z)
        );
    }
}