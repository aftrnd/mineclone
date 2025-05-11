using UnityEngine;

public class TerrainGenerator
{
    private int seed;
    private System.Random random;
    
    // Noise settings
    private float terrainScale = 0.1f;
    private float heightScale = 8f;
    private float baseHeight = 4f;
    
    // Optional: More advanced terrain settings
    private float mountainScale = 0.03f;
    private float mountainHeight = 16f;
    private float valleyScale = 0.05f;
    private float valleyDepth = 4f;
    
    // Cave settings
    private float caveScale = 0.05f;  // Reduced scale for more natural caves
    private float caveThreshold = 0.6f;  // Increased threshold (higher = fewer caves)
    private Vector3 caveNoiseOffset;
    private bool debugMode = false;

    public TerrainGenerator(int seed)
    {
        this.seed = seed;
        random = new System.Random(seed);
        
        // Initialize the noise offsets to avoid repetitive patterns
        float offsetX = (float)random.NextDouble() * 10000f;
        float offsetZ = (float)random.NextDouble() * 10000f;
        
        // Set random offsets for cave noise to make them unique per seed
        caveNoiseOffset = new Vector3(
            (float)random.NextDouble() * 1000f,
            (float)random.NextDouble() * 1000f,
            (float)random.NextDouble() * 1000f
        );
        
        if (debugMode)
        {
            Debug.Log($"TerrainGenerator initialized with seed: {seed}");
            Debug.Log($"Cave noise offset: {caveNoiseOffset}");
            Debug.Log($"Terrain params: scale={terrainScale}, height={heightScale}, base={baseHeight}");
        }
    }
    
    // Get the height at a specific world position
    public int GetTerrainHeight(float worldX, float worldZ)
    {
        // Basic terrain with Perlin noise
        float baseNoise = Mathf.PerlinNoise(worldX * terrainScale, worldZ * terrainScale);
        float baseTerrainHeight = baseNoise * heightScale + baseHeight;
        
        // Add mountains with a different scale
        float mountainNoise = Mathf.PerlinNoise(worldX * mountainScale, worldZ * mountainScale);
        float mountainInfluence = Mathf.Pow(mountainNoise, 2f) * mountainHeight;
        
        // Add valleys with a different scale
        float valleyNoise = Mathf.PerlinNoise(worldX * valleyScale + 500, worldZ * valleyScale + 500);
        float valleyInfluence = valleyNoise * valleyDepth;
        
        // Combine the different terrain features
        float finalHeight = baseTerrainHeight + mountainInfluence - valleyInfluence;
        
        // Ensure reasonable height (minimum of 1 block)
        int heightValue = Mathf.Max(1, Mathf.FloorToInt(finalHeight));
        
        if (debugMode && worldX % 16 == 0 && worldZ % 16 == 0)
        {
            Debug.Log($"Terrain height at ({worldX},{worldZ}): {heightValue}");
        }
        
        // Return the final height as an integer
        return heightValue;
    }
    
    // Get the block type at a specific position
    public BlockType GetBlockType(int x, int y, int z, int surfaceHeight)
    {
        // Above ground is air
        if (y > surfaceHeight)
            return BlockType.Air;
            
        // Surface layer is grass
        if (y == surfaceHeight)
            return BlockType.Grass;
            
        // A few layers below surface is dirt
        if (y > surfaceHeight - 4)
            return BlockType.Dirt;
            
        // Everything else is stone
        return BlockType.Stone;
    }
    
    // Generate 3D noise for caves using multiple 2D Perlin noise samples
    private float Perlin3D(float x, float y, float z)
    {
        // Add offsets to coordinates based on seed
        x += caveNoiseOffset.x;
        y += caveNoiseOffset.y;
        z += caveNoiseOffset.z;
        
        // Sample noise in 3 perpendicular planes and combine
        float xy = Mathf.PerlinNoise(x, y);
        float xz = Mathf.PerlinNoise(x, z);
        float yz = Mathf.PerlinNoise(y, z);
        
        // Combine all samples (simpler combination for better terrain)
        return (xy + xz + yz) / 3f;
    }
    
    // Check if a position should be a cave
    public bool IsCave(float worldX, float worldY, float worldZ)
    {
        // Calculate vertical bias - make caves less common near the surface
        float depthBias = Mathf.Clamp01(worldY / 40f); // Less caves higher up
        float adjustedThreshold = caveThreshold + depthBias * 0.3f; // Increase threshold near surface
        
        // 3D Perlin noise for caves
        float caveNoise = Perlin3D(worldX * caveScale, worldY * caveScale, worldZ * caveScale);
        
        // Create caves - a cave is where noise exceeds threshold
        bool isCave = caveNoise > adjustedThreshold;
        
        // Exclude caves very close to surface (within 3 blocks)
        int terrainHeight = GetTerrainHeight(worldX, worldZ);
        if (worldY > terrainHeight - 3)
        {
            isCave = false;
        }
        
        if (debugMode && (int)worldX % 16 == 0 && (int)worldZ % 16 == 0 && (int)worldY % 4 == 0)
        {
            Debug.Log($"Cave check at ({worldX},{worldY},{worldZ}): noise={caveNoise}, threshold={adjustedThreshold}, result={isCave}");
        }
        
        return isCave;
    }
} 