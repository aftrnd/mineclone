using UnityEngine;
using System.Collections.Generic;

public enum BiomeType
{
    Plains,
    Forest,
    Desert,
    Mountains,
    Ocean
}

public class TerrainGenerator
{
    // Minecraft-style world height settings
    public const int MIN_HEIGHT = 0;      // Bottom of the world
    public const int SEA_LEVEL = 64;      // Sea level / baseline
    public const int MAX_HEIGHT = 128;    // Maximum world height

    private int seed;
    private System.Random random;
    
    // Biome noise settings
    private float biomeScale = 0.005f;    // Larger scale means bigger biomes
    private float temperatureScale = 0.01f;
    private float humidityScale = 0.01f;
    
    // Base terrain noise settings
    private float terrainScale = 0.01f;   // Reduced for gentler slopes
    private float heightScale = 30f;      // Height variation
    private float baseHeight = SEA_LEVEL - 4; // Slightly below sea level
    
    // Advanced terrain settings
    private float mountainScale = 0.02f;
    private float mountainHeight = 45f;   // Taller mountains
    private float valleyScale = 0.03f;
    private float valleyDepth = 15f;
    
    // Cave settings
    private float caveScale = 0.04f;
    private float caveThreshold = 0.6f;
    private Vector3 caveNoiseOffset;
    private bool debugMode = false;
    
    // Biome thresholds
    private Dictionary<BiomeType, BiomeSettings> biomeSettings = new Dictionary<BiomeType, BiomeSettings>();

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
        
        // Initialize biome settings
        InitializeBiomeSettings();
        
        if (debugMode)
        {
            Debug.Log($"TerrainGenerator initialized with seed: {seed}");
            Debug.Log($"Cave noise offset: {caveNoiseOffset}");
            Debug.Log($"Terrain params: scale={terrainScale}, height={heightScale}, base={baseHeight}");
        }
    }
    
    private void InitializeBiomeSettings()
    {
        // Plains biome - gentle rolling hills
        biomeSettings[BiomeType.Plains] = new BiomeSettings
        {
            terrainScale = 0.02f,
            heightScale = 8f,
            baseHeight = SEA_LEVEL - 2,
            additionalTerrainNoise = 0.3f,
            topBlockType = BlockType.Grass,
            midBlockType = BlockType.Dirt,
            deepBlockType = BlockType.Stone,
            topLayerDepth = 1,
            midLayerDepth = 3
        };
        
        // Forest biome - more varied terrain with trees
        biomeSettings[BiomeType.Forest] = new BiomeSettings
        {
            terrainScale = 0.025f,
            heightScale = 15f,
            baseHeight = SEA_LEVEL,
            additionalTerrainNoise = 0.5f,
            topBlockType = BlockType.Grass,
            midBlockType = BlockType.Dirt,
            deepBlockType = BlockType.Stone,
            topLayerDepth = 1,
            midLayerDepth = 4
        };
        
        // Desert biome - dunes and flat areas
        biomeSettings[BiomeType.Desert] = new BiomeSettings
        {
            terrainScale = 0.03f,
            heightScale = 12f,
            baseHeight = SEA_LEVEL - 5,
            additionalTerrainNoise = 0.6f,
            topBlockType = BlockType.Sand,
            midBlockType = BlockType.Sand,
            deepBlockType = BlockType.Stone,
            topLayerDepth = 4,
            midLayerDepth = 4
        };
        
        // Mountains biome - high peaks and steep cliffs
        biomeSettings[BiomeType.Mountains] = new BiomeSettings
        {
            terrainScale = 0.04f,
            heightScale = 40f,
            baseHeight = SEA_LEVEL + 15,
            additionalTerrainNoise = 0.8f,
            topBlockType = BlockType.SnowGrass, // Snow at the top
            midBlockType = BlockType.Stone,
            deepBlockType = BlockType.Stone,
            topLayerDepth = 1,
            midLayerDepth = 2
        };
        
        // Ocean biome - below sea level
        biomeSettings[BiomeType.Ocean] = new BiomeSettings
        {
            terrainScale = 0.02f,
            heightScale = 8f,
            baseHeight = SEA_LEVEL - 15,
            additionalTerrainNoise = 0.2f,
            topBlockType = BlockType.Sand,  // Sand at the bottom
            midBlockType = BlockType.Clay,  // Clay under the sand
            deepBlockType = BlockType.Stone,
            topLayerDepth = 3,
            midLayerDepth = 2
        };
    }
    
    // Determine biome based on temperature and humidity
    public BiomeType GetBiomeAt(float worldX, float worldZ)
    {
        // Calculate the raw biome values
        BiomeData biomeData = CalculateBiomeData(worldX, worldZ);
        
        // Debug raw biome data occasionally
        if (debugMode && Mathf.FloorToInt(worldX) % 100 == 0 && Mathf.FloorToInt(worldZ) % 100 == 0)
        {
            Debug.Log($"Raw biome data at ({worldX}, {worldZ}): " +
                     $"temp={biomeData.temperature:F2}, " +
                     $"humidity={biomeData.humidity:F2}, " +
                     $"noise={biomeData.biomeNoise:F2}");
        }
        
        // Get biome weights - which biome is dominant in this area
        float[,] biomeWeights = CalculateBiomeWeights(biomeData);
        
        // Debug the biome weights sometimes
        if (debugMode && Mathf.FloorToInt(worldX) % 64 == 0 && Mathf.FloorToInt(worldZ) % 64 == 0)
        {
            string weightDebug = "Biome weights at (" + worldX + "," + worldZ + "): ";
            for (int i = 0; i < 5; i++)
            {
                weightDebug += $"{(BiomeType)i}={biomeWeights[i, 1]:F2} ";
            }
            Debug.Log(weightDebug);
        }
        
        // Find the biome with the highest weight
        BiomeType dominantBiome = BiomeType.Plains; // Default to plains
        float maxWeight = 0f;
        
        for (int i = 0; i < 5; i++) // Assumes 5 biome types
        {
            float weight = biomeWeights[i, 1];
            if (weight > maxWeight)
            {
                maxWeight = weight;
                dominantBiome = (BiomeType)i;
            }
        }
        
        return dominantBiome;
    }
    
    // Helper class to store temperature, humidity, and other biome data
    private struct BiomeData
    {
        public float temperature;
        public float humidity;
        public float biomeNoise;
    }
    
    // Calculate raw biome data from coordinates
    private BiomeData CalculateBiomeData(float worldX, float worldZ)
    {
        BiomeData data = new BiomeData();
        
        // Modify the noise scales to create better biome distribution
        float adjustedTemperatureScale = temperatureScale * 0.7f; // Larger temperature zones
        float adjustedHumidityScale = humidityScale * 0.8f; // Larger humidity zones
        float adjustedBiomeScale = biomeScale * 1.5f; // Smaller biome regions for more variety
        
        // Apply offsets to create different patterns
        float offsetTemp = seed % 1000 * 0.01f;
        float offsetHumid = seed % 500 * 0.01f;
        float offsetBiome = seed % 2000 * 0.01f;
        
        // Create temperature and humidity noise with specific scale settings
        data.temperature = Mathf.PerlinNoise(
            worldX * adjustedTemperatureScale + offsetTemp, 
            worldZ * adjustedTemperatureScale + offsetTemp + 500
        );
        
        data.humidity = Mathf.PerlinNoise(
            worldX * adjustedHumidityScale + offsetHumid + 1000, 
            worldZ * adjustedHumidityScale + offsetHumid + 1500
        );
        
        // Use larger scale noise for biome regions
        data.biomeNoise = Mathf.PerlinNoise(
            worldX * adjustedBiomeScale + offsetBiome + 3000, 
            worldZ * adjustedBiomeScale + offsetBiome + 2500
        );
        
        return data;
    }
    
    // Calculate biome weights for blending between biomes
    // Returns a 5x2 array with [biomeType, weight] for each biome
    private float[,] CalculateBiomeWeights(BiomeData data)
    {
        // Each row is a biome: [Plains, Forest, Desert, Mountains, Ocean]
        // [biomeType, weight]
        float[,] weights = new float[5, 2];
        
        // Initialize all biome weights
        for (int i = 0; i < 5; i++)
        {
            weights[i, 0] = i; // BiomeType as index
            weights[i, 1] = 0f; // Weight initialized to 0
        }
        
        // Ocean biome: dominant only when biomeNoise is VERY low
        // Reducing ocean threshold from 0.3f to 0.2f to make oceans less common
        float oceanThreshold = 0.2f;
        float oceanTransitionWidth = 0.1f;
        float oceanWeight = 0f;
        
        if (data.biomeNoise < oceanThreshold)
        {
            // Fully ocean when below threshold
            oceanWeight = 1.0f;
        }
        else if (data.biomeNoise < oceanThreshold + oceanTransitionWidth)
        {
            // Smooth transition from ocean to land
            oceanWeight = 1.0f - ((data.biomeNoise - oceanThreshold) / oceanTransitionWidth);
        }
        
        weights[(int)BiomeType.Ocean, 1] = oceanWeight;
        
        // Scale down other biome weights if in ocean transition zone
        float nonOceanScale = 1.0f - oceanWeight;
        
        // Desert weight: hot and dry
        float desertWeight = Mathf.Clamp01(data.temperature * 1.5f - 0.5f) * Mathf.Clamp01(1.0f - data.humidity * 1.5f);
        weights[(int)BiomeType.Desert, 1] = desertWeight * nonOceanScale;
        
        // Mountain weight: based on biomeNoise and low-to-mid temperature
        float mountainSuitability = data.biomeNoise * (1.0f - Mathf.Abs(data.temperature - 0.3f));
        float mountainWeight = Mathf.Clamp01(mountainSuitability * 2.0f - 0.7f);
        weights[(int)BiomeType.Mountains, 1] = mountainWeight * nonOceanScale;
        
        // Forest weight: mid-to-high temperature and high humidity
        float forestWeight = Mathf.Clamp01(data.temperature) * Mathf.Clamp01(data.humidity * 1.5f - 0.5f);
        weights[(int)BiomeType.Forest, 1] = forestWeight * nonOceanScale;
        
        // Plains: default biome, stronger in temperate areas with average humidity
        float plainsScore = (1.0f - Mathf.Abs(data.temperature - 0.5f)) * (1.0f - Mathf.Abs(data.humidity - 0.4f));
        float plainsWeight = Mathf.Clamp01(plainsScore * 1.5f);
        weights[(int)BiomeType.Plains, 1] = plainsWeight * nonOceanScale;
        
        // Ensure at least some weight is given to plains as a fallback if no biome has significant weight
        float totalNonOceanWeight = weights[(int)BiomeType.Plains, 1] + 
                                 weights[(int)BiomeType.Forest, 1] + 
                                 weights[(int)BiomeType.Desert, 1] + 
                                 weights[(int)BiomeType.Mountains, 1];
        
        if (totalNonOceanWeight < 0.1f && oceanWeight < 0.9f)
        {
            weights[(int)BiomeType.Plains, 1] += 0.2f * nonOceanScale;
            totalNonOceanWeight += 0.2f * nonOceanScale;
        }
        
        // Normalize the non-ocean weights
        if (totalNonOceanWeight > 0f)
        {
            for (int i = 0; i < 5; i++)
            {
                if (i != (int)BiomeType.Ocean)
                {
                    weights[i, 1] /= totalNonOceanWeight;
                    weights[i, 1] *= nonOceanScale; // Apply ocean scaling
                }
            }
        }
        
        // Debug
        if (debugMode && data.biomeNoise < 0.3f)
        {
            Debug.Log($"Biome data: temp={data.temperature:F2}, humid={data.humidity:F2}, noise={data.biomeNoise:F2}");
            Debug.Log($"Weights: Ocean={weights[(int)BiomeType.Ocean, 1]:F2}, " +
                     $"Plains={weights[(int)BiomeType.Plains, 1]:F2}, " +
                     $"Forest={weights[(int)BiomeType.Forest, 1]:F2}, " +
                     $"Desert={weights[(int)BiomeType.Desert, 1]:F2}, " +
                     $"Mountains={weights[(int)BiomeType.Mountains, 1]:F2}");
        }
        
        return weights;
    }
    
    // Get the height at a specific world position
    public int GetTerrainHeight(float worldX, float worldZ)
    {
        // Calculate biome data and weights
        BiomeData biomeData = CalculateBiomeData(worldX, worldZ);
        float[,] biomeWeights = CalculateBiomeWeights(biomeData);
        
        // Store the heights and weights for blending
        float[] heights = new float[5]; // One per biome
        float totalWeight = 0f;
        
        // Calculate the height for each biome
        for (int i = 0; i < 5; i++)
        {
            BiomeType biome = (BiomeType)i;
            float weight = biomeWeights[i, 1];
            
            if (weight > 0.01f) // Only calculate heights for biomes with significant weight
            {
                // Get biome-specific height
                heights[i] = GetBiomeSpecificHeight(worldX, worldZ, biome);
                totalWeight += weight;
            }
        }
        
        // Blend heights based on weights
        float blendedHeight = 0f;
        if (totalWeight > 0f)
        {
            for (int i = 0; i < 5; i++)
            {
                float normalizedWeight = biomeWeights[i, 1] / totalWeight;
                blendedHeight += heights[i] * normalizedWeight;
            }
        }
        else
        {
            // Fallback to plains if no weights
            blendedHeight = GetBiomeSpecificHeight(worldX, worldZ, BiomeType.Plains);
        }
        
        // Apply global limits
        int finalHeight = Mathf.Clamp(Mathf.FloorToInt(blendedHeight), MIN_HEIGHT, MAX_HEIGHT);
        
        // Ensure reasonable height (minimum of MIN_HEIGHT)
        finalHeight = Mathf.Max(MIN_HEIGHT, finalHeight);
        
        if (debugMode && worldX % 16 == 0 && worldZ % 16 == 0)
        {
            BiomeType dominantBiome = GetBiomeAt(worldX, worldZ);
            Debug.Log($"Terrain height at ({worldX},{worldZ}): {finalHeight}, Biome: {dominantBiome}");
        }
        
        // Return the final height as an integer
        return finalHeight;
    }
    
    private float GetBiomeSpecificHeight(float worldX, float worldZ, BiomeType biome)
    {
        BiomeSettings settings = biomeSettings[biome];
        
        // Use biome-specific settings for noise generation
        float baseNoise = Mathf.PerlinNoise(
            worldX * settings.terrainScale + seed % 123, 
            worldZ * settings.terrainScale + seed % 321
        );
        
        // Add some variation with multiple noise layers of different scales
        // This creates more natural looking terrain with both large and small features
        float largeFeatures = Mathf.PerlinNoise(
            worldX * settings.terrainScale * 0.5f + seed % 567, 
            worldZ * settings.terrainScale * 0.5f + seed % 765
        ) * 0.6f;
        
        float mediumFeatures = Mathf.PerlinNoise(
            worldX * settings.terrainScale * 2f + seed % 888, 
            worldZ * settings.terrainScale * 2f + seed % 999
        ) * 0.3f;
        
        float smallFeatures = Mathf.PerlinNoise(
            worldX * settings.terrainScale * 4f + seed % 1234, 
            worldZ * settings.terrainScale * 4f + seed % 5678
        ) * 0.1f * settings.additionalTerrainNoise;
        
        // Combine all noise layers for a more natural landscape
        float combinedNoise = (baseNoise * 0.6f) + (largeFeatures * 0.2f) + (mediumFeatures * 0.15f) + (smallFeatures * 0.05f);
        
        // Apply biome-specific height scaling
        float baseTerrainHeight = settings.baseHeight + combinedNoise * settings.heightScale;
        
        // Add special features for certain biomes
        
        // Add mountains for mountain biomes
        if (biome == BiomeType.Mountains)
        {
            // Use a special mountain noise with a different octave
            float mountainNoise = Mathf.PerlinNoise(
                worldX * mountainScale + seed % 246, 
                worldZ * mountainScale + seed % 642
            );
            
            // Create steeper mountains with exponential curve - use the class field mountainHeight
            float calculatedMountainHeight = this.mountainHeight * Mathf.Pow(mountainNoise, 3f) * 1.2f;
            
            // Only add mountain height where the noise is significant
            if (mountainNoise > 0.5f)
            {
                baseTerrainHeight += calculatedMountainHeight * ((mountainNoise - 0.5f) * 2f);
            }
        }
        
        // Add valleys for all biomes - but different intensities per biome
        float valleyMultiplier = 1.0f;
        if (biome == BiomeType.Mountains) valleyMultiplier = 1.5f;
        if (biome == BiomeType.Ocean) valleyMultiplier = 0.5f;
        
        float valleyNoise = Mathf.PerlinNoise(
            worldX * valleyScale + seed % 975, 
            worldZ * valleyScale + seed % 357
        );
        float valleyInfluence = valleyNoise * valleyDepth * valleyMultiplier;
        
        // For ocean biomes, make sure they generally stay below sea level
        if (biome == BiomeType.Ocean)
        {
            // Ensure oceans stay below sea level most of the time
            baseTerrainHeight = Mathf.Min(baseTerrainHeight, SEA_LEVEL - 5 + (valleyNoise * 4f));
        }
        
        // Combine the different terrain features
        float finalHeight = baseTerrainHeight - valleyInfluence;
        
        // Add special height modifiers for specific biomes
        if (biome == BiomeType.Mountains)
        {
            // Make mountain peaks more rounded/smooth as they approach MAX_HEIGHT
            if (finalHeight > MAX_HEIGHT - 20)
            {
                float excess = finalHeight - (MAX_HEIGHT - 20);
                float reduction = excess * 0.7f; // 70% reduction factor for peaks
                finalHeight -= reduction;
            }
        }
        
        // For deserts, add some dunes
        if (biome == BiomeType.Desert)
        {
            float duneNoise = Mathf.PerlinNoise(
                worldX * 0.05f + seed % 8888, 
                worldZ * 0.05f + seed % 9999
            );
            
            // Only add dunes in certain areas
            if (duneNoise > 0.6f)
            {
                float dunes = Mathf.PerlinNoise(
                    worldX * 0.15f, 
                    worldZ * 0.15f
                ) * 5f;
                
                finalHeight += dunes;
            }
        }
        
        return finalHeight;
    }
    
    // Get the block type at a specific position based on biome
    public BlockType GetBlockType(float worldX, int y, float worldZ, int surfaceHeight)
    {
        // Bedrock layer at the bottom of the world
        if (y <= MIN_HEIGHT + 1)
        {
            return BlockType.Bedrock;
        }
        
        BiomeType biome = GetBiomeAt(worldX, worldZ);
        BiomeSettings settings = biomeSettings[biome];
        
        // Above ground is air
        if (y > surfaceHeight)
            return BlockType.Air;
            
        // Surface layer 
        if (y == surfaceHeight)
            return settings.topBlockType;
            
        // A few layers below surface
        if (y > surfaceHeight - settings.midLayerDepth)
            return settings.midBlockType;
            
        // Everything else is deep layer (usually stone)
        return settings.deepBlockType;
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
        // Only generate caves between a certain height range (8 blocks above bedrock to 10 blocks below sea level)
        if (worldY < MIN_HEIGHT + 8 || worldY > SEA_LEVEL - 5)
        {
            return false;
        }
            
        // Calculate vertical bias - make caves more common deeper down
        float depthBias = Mathf.Clamp01((SEA_LEVEL - worldY) / 40f); 
        
        // Adjust threshold - smaller number = more caves
        // This is critical - make this value high enough to avoid too many caves
        float baseThreshold = 0.75f; // Start with a high threshold (fewer caves)
        float adjustedThreshold = baseThreshold - depthBias * 0.15f; // Lower threshold (more caves) deeper down
        
        BiomeType biome = GetBiomeAt(worldX, worldZ);
        
        // Mountain biomes have more caves
        if (biome == BiomeType.Mountains)
        {
            adjustedThreshold -= 0.05f;
        }
        
        // Use two noise layers to create natural cave shapes
        float primaryNoise = Perlin3D(worldX * caveScale, worldY * caveScale, worldZ * caveScale);
        
        // Use a second noise with different scale/offset for more natural shapes
        float secondaryNoise = Perlin3D(
            worldX * caveScale * 2.3f + 300,
            worldY * caveScale * 1.9f + 700, 
            worldZ * caveScale * 2.1f + 500
        );
        
        // Combine noise layers with multiplication - creates smaller, more natural caves
        // This is important to avoid giant ravines - multiplication creates sparser caves
        float combinedNoise = primaryNoise * secondaryNoise;
        
        // Cave shape control - need both noise values to be high for a cave to form
        bool isCave = combinedNoise > adjustedThreshold && primaryNoise > 0.5f;
        
        // Add additional restrictions to create smaller caves
        if (isCave)
        {
            // Small random factor for additional variation
            float random01 = (Mathf.Sin(worldX * 123.321f + worldY * 345.543f + worldZ * 567.765f) + 1) * 0.5f;
            
            // Make caves smaller with distance checks
            // Generate a "center point" for the cave system with large scale noise
            float caveCenterX = Mathf.PerlinNoise(worldX * 0.01f, worldZ * 0.01f) * 200;
            float caveCenterZ = Mathf.PerlinNoise(worldX * 0.01f + 500, worldZ * 0.01f + 500) * 200;
            
            // Distance from current point to nearest cave center
            float distToCaveCenter = Vector2.Distance(
                new Vector2(worldX, worldZ), 
                new Vector2(caveCenterX, caveCenterZ)
            ) * 0.1f;
            
            // More likely to be a cave near cave centers
            if (random01 < 0.3f && distToCaveCenter > 10f)
            {
                isCave = false;
            }
        }
        
        // Exclude caves very close to surface (within 5 blocks)
        int terrainHeight = GetTerrainHeight(worldX, worldZ);
        if (worldY > terrainHeight - 5)
        {
            isCave = false;
        }
        
        if (debugMode && (int)worldX % 16 == 0 && (int)worldZ % 16 == 0 && (int)worldY % 4 == 0)
        {
            Debug.Log($"Cave check at ({worldX},{worldY},{worldZ}): combined noise={combinedNoise}, threshold={adjustedThreshold}, result={isCave}");
        }
        
        return isCave;
    }
    
    // Biome settings class to hold per-biome configuration
    public class BiomeSettings
    {
        public float terrainScale;
        public float heightScale;
        public float baseHeight;
        public float additionalTerrainNoise;
        public BlockType topBlockType;
        public BlockType midBlockType;
        public BlockType deepBlockType;
        public int topLayerDepth;
        public int midLayerDepth;
    }

    // Also add a new method to get biome weights for block type blending
    public float[,] GetBiomeWeightsAt(float worldX, float worldZ)
    {
        BiomeData biomeData = CalculateBiomeData(worldX, worldZ);
        return CalculateBiomeWeights(biomeData);
    }
} 