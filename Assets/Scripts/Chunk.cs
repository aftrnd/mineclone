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

    // Add YLevel property
    public int YLevel { get; private set; }

    // Initialize the chunk with position
    public void Initialize(Vector2Int chunkPosition, int yLevel = 0)
    {
        ChunkPosition = chunkPosition;
        YLevel = yLevel;
        
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
                Debug.LogWarning($"No WorldManager found for chunk at {ChunkPosition}, Y={YLevel}. Using simple terrain generation.");
                GenerateSimpleTerrain();
                return;
            }
            
            // Get world position for this chunk
            Vector3 worldPosition = new Vector3(
                ChunkPosition.x * chunkSize,
                YLevel * chunkSize,
                ChunkPosition.y * chunkSize
            );
            
            Debug.Log($"Generating chunk at {ChunkPosition}, Y={YLevel} (world pos: {worldPosition})");
            
            for (int x = 0; x < chunkSize; x++) {
                for (int z = 0; z < chunkSize; z++) {
                    // World position of this block
                    float worldX = worldPosition.x + x;
                    float worldZ = worldPosition.z + z;
                    
                    // Get height from terrain generator
                    int terrainHeight = worldManager.GetTerrainHeight(worldX, worldZ);
                    
                    // Validate height (shouldn't be negative)
                    if (terrainHeight < TerrainGenerator.MIN_HEIGHT)
                    {
                        Debug.LogError($"Terrain generator returned invalid height ({terrainHeight}) at {worldX},{worldZ}");
                        terrainHeight = TerrainGenerator.MIN_HEIGHT;
                    }
                    
                    if (terrainHeight >= TerrainGenerator.MAX_HEIGHT)
                    {
                        terrainHeight = TerrainGenerator.MAX_HEIGHT - 1;
                    }
                    
                    // Debug occasional heights
                    if (x == 0 && z == 0)
                    {
                        Debug.Log($"Terrain height at chunk {ChunkPosition}, Y={YLevel} origin: {terrainHeight}");
                    }
                    
                    // Get biome weights for blending
                    float[,] biomeWeights = worldManager.GetBiomeWeightsAt(worldX, worldZ);
                    BiomeType dominantBiome = worldManager.GetBiomeAt(worldX, worldZ);
                    
                    for (int y = 0; y < chunkSize; y++) {
                        // World Y position
                        int worldY = YLevel * chunkSize + y;
                        
                        // Skip if outside world range
                        if (worldY < TerrainGenerator.MIN_HEIGHT || worldY >= TerrainGenerator.MAX_HEIGHT)
                        {
                            blocks[x, y, z] = new Block(BlockType.Air);
                            continue;
                        }
                        
                        // Default to air
                        BlockType blockType = BlockType.Air;
                        
                        // If below terrain height, it's solid ground
                        if (worldY <= terrainHeight)
                        {
                            // Check if this should be a cave
                            bool isCave = worldManager.IsCave(worldX, worldY, worldZ);
                            if (isCave)
                            {
                                blockType = BlockType.Air;
                            }
                            else
                            {
                                // Determine block type with biome blending
                                blockType = DetermineBlendedBlockType(worldX, worldZ, worldY, terrainHeight, biomeWeights);
                            }
                        }
                        
                        // Handle water (sea level)
                        if (blockType == BlockType.Air && worldY < TerrainGenerator.SEA_LEVEL)
                        {
                            // In the future, this would be water - for now we'll still use air
                            // blockType = BlockType.Water; 
                        }
                        
                        // Create the block of appropriate type
                        blocks[x, y, z] = new Block(blockType);
                    }
                }
            }
            
            Debug.Log($"Finished generating chunk at {ChunkPosition}, Y={YLevel}");
        }
        catch (System.Exception e)
        {
            // Log the error and generate simple terrain as fallback
            Debug.LogError($"Error generating chunk at {ChunkPosition}, Y={YLevel}: {e.Message}\n{e.StackTrace}");
            GenerateSimpleTerrain();
        }
    }
    
    // Update the DetermineBlockType method to handle biome blending
    private BlockType DetermineBlockType(BiomeType biome, int worldY, int terrainHeight)
    {
        // Bedrock layer (bottom 1-2 blocks of the world)
        if (worldY <= TerrainGenerator.MIN_HEIGHT + 1) 
        {
            return BlockType.Bedrock;
        }
        
        // Calculate depth from surface
        int depthFromSurface = terrainHeight - worldY;
        
        // Surface blocks (top layer)
        if (depthFromSurface == 0) 
        {
            switch (biome)
            {
                case BiomeType.Desert:
                    return BlockType.Sand;
                case BiomeType.Mountains:
                    // Snow on peaks above certain height
                    if (worldY > TerrainGenerator.SEA_LEVEL + 30)
                    {
                        return BlockType.SnowGrass;
                    }
                    // Regular grass or stone at lower elevations
                    return worldY > TerrainGenerator.SEA_LEVEL + 15 ? BlockType.Stone : BlockType.Grass;
                case BiomeType.Ocean:
                    return BlockType.Sand; // Sand at ocean floor
                default: // Plains, Forest
                    return BlockType.Grass;
            }
        }
        
        // Mid layer (few blocks below surface)
        if (depthFromSurface < 4) 
        {
            switch (biome)
            {
                case BiomeType.Desert:
                    return BlockType.Sand;
                case BiomeType.Ocean:
                    return worldY > TerrainGenerator.SEA_LEVEL - 10 ? BlockType.Sand : BlockType.Clay;
                default: // Mountains, Plains, Forest
                    return BlockType.Dirt;
            }
        }
        
        // Deep layer (everything else)
        return BlockType.Stone;
    }

    // New method to blend blocks between biomes
    private BlockType DetermineBlendedBlockType(float worldX, float worldZ, int worldY, int terrainHeight, float[,] biomeWeights)
    {
        // Depth from surface is key to determining block type
        int depthFromSurface = terrainHeight - worldY;
        
        // Bedrock is always bedrock regardless of biome
        if (worldY <= TerrainGenerator.MIN_HEIGHT + 1)
        {
            return BlockType.Bedrock;
        }
        
        // Deep underground is always stone
        if (depthFromSurface > 6)
        {
            return BlockType.Stone;
        }
        
        // Special handling for surface blocks (blend between biomes)
        if (depthFromSurface == 0)
        {
            // Check for special elevation-based overrides that apply regardless of biome
            if (worldY > TerrainGenerator.SEA_LEVEL + 35)
            {
                // High mountain peaks are always snowy
                return BlockType.SnowGrass;
            }
            
            // For areas near sea level, prioritize sand for smoother transitions to water
            if (worldY < TerrainGenerator.SEA_LEVEL + 2)
            {
                // Near sea level is more likely to be sand
                return BlockType.Sand;
            }
            
            // Surface blocks use biome blending - find strongest influences
            float desertWeight = biomeWeights[(int)BiomeType.Desert, 1];
            float mountainWeight = biomeWeights[(int)BiomeType.Mountains, 1];
            float oceanWeight = biomeWeights[(int)BiomeType.Ocean, 1];
            
            // Clear biome dominance
            if (desertWeight > 0.6f)
            {
                return BlockType.Sand;
            }
            if (oceanWeight > 0.6f)
            {
                return BlockType.Sand; // Ocean floor is sand
            }
            if (mountainWeight > 0.6f && worldY > TerrainGenerator.SEA_LEVEL + 20)
            {
                return BlockType.Stone; // High mountains are stone
            }
            
            // Blended areas - use a random factor seeded by position to create a natural pattern
            float randomFactor = Mathf.PerlinNoise(worldX * 0.8f, worldZ * 0.8f);
            
            // Desert/grass transition
            if (desertWeight > 0.2f && randomFactor < desertWeight)
            {
                return BlockType.Sand;
            }
            
            // Mountain/grass transition
            if (mountainWeight > 0.3f && worldY > TerrainGenerator.SEA_LEVEL + 15 && randomFactor < mountainWeight)
            {
                return BlockType.Stone;
            }
            
            // Default surface block is grass
            return BlockType.Grass;
        }
        
        // Mid layers - blend between dirt and sand primarily
        if (depthFromSurface <= 3)
        {
            float desertWeight = biomeWeights[(int)BiomeType.Desert, 1];
            float oceanWeight = biomeWeights[(int)BiomeType.Ocean, 1];
            
            // Stronger desert influence = sand
            if (desertWeight > 0.5f || (oceanWeight > 0.3f && worldY > TerrainGenerator.SEA_LEVEL - 8))
            {
                return BlockType.Sand;
            }
            
            // Default mid layer is dirt
            return BlockType.Dirt;
        }
        
        // Deeper layers blend to stone
        float stoneBlend = Mathf.Clamp01((depthFromSurface - 3) / 3f);
        float transitionNoise = Mathf.PerlinNoise(worldX * 0.5f, worldZ * 0.5f);
        
        if (transitionNoise < stoneBlend)
        {
            return BlockType.Stone;
        }
        return BlockType.Dirt;
    }

    // Update GenerateSimpleTerrain to handle Y level
    private void GenerateSimpleTerrain()
    {
        // Get world position for this chunk
        Vector3 worldPosition = new Vector3(
            ChunkPosition.x * chunkSize,
            YLevel * chunkSize,
            ChunkPosition.y * chunkSize
        );
        
        for (int x = 0; x < chunkSize; x++) {
            for (int z = 0; z < chunkSize; z++) {
                // World position of this block
                float worldX = worldPosition.x + x;
                float worldZ = worldPosition.z + z;
                
                // Simple terrain height formula
                float height = Mathf.PerlinNoise(worldX * 0.1f, worldZ * 0.1f) * 8 + TerrainGenerator.SEA_LEVEL - 10;
                int terrainHeight = Mathf.FloorToInt(height);
                
                for (int y = 0; y < chunkSize; y++) {
                    // World Y position
                    int worldY = YLevel * chunkSize + y;
                    
                    BlockType blockType = BlockType.Air;
                    
                    // Terrain generation rules
                    if (worldY < terrainHeight) {
                        if (worldY == terrainHeight - 1) {
                            // Top block is grass
                            blockType = BlockType.Grass;
                        } else if (worldY > terrainHeight - 4) {
                            // A few blocks below the surface are dirt
                            blockType = BlockType.Dirt;
                        } else {
                            // Everything else is stone
                            blockType = BlockType.Stone;
                        }
                    }
                    
                    // Generate bedrock at the bottom of the world
                    if (worldY <= TerrainGenerator.MIN_HEIGHT + 1) {
                        blockType = BlockType.Bedrock;
                    }
                    
                    blocks[x, y, z] = new Block(blockType);
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

    // Add a public method to regenerate the mesh without changing blocks
    public void RegenerateMesh()
    {
        GenerateMesh();
    }
}