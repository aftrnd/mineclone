using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    [Header("World Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private int viewDistance = 5;
    [SerializeField] private Material blockMaterial;
    [SerializeField] private GameObject chunkPrefab;

    [Header("Generation Settings")]
    [SerializeField] private int seed = 0;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private bool generateCaves = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Dictionary to keep track of loaded chunks
    private Dictionary<string, Chunk> loadedChunks = new Dictionary<string, Chunk>();
    private Vector2Int lastPlayerChunkPosition;

    // Chunk queue for loading/unloading operations
    private Queue<ChunkCoord> chunkLoadQueue = new Queue<ChunkCoord>();
    private Queue<string> chunkUnloadQueue = new Queue<string>();

    // Terrain generator
    private TerrainGenerator terrainGenerator;

    public struct ChunkCoord
    {
        public int x;
        public int y;
        public int z;
        
        public override string ToString()
        {
            return $"{x}_{y}_{z}";
        }
        
        public override bool Equals(object obj)
        {
            if (!(obj is ChunkCoord)) return false;
            ChunkCoord other = (ChunkCoord)obj;
            return x == other.x && y == other.y && z == other.z;
        }
        
        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        }
    }

    private void Awake()
    {
        if (player == null)
        {
            player = Camera.main?.transform.parent;
            if (player == null)
            {
                Debug.LogError("No player reference found and couldn't auto-assign! World generation will not work correctly.");
                player = transform; // Fallback to self
            }
            else
            {
                Debug.Log("Player reference auto-assigned to main camera's parent: " + player.name);
            }
        }

        if (useRandomSeed)
        {
            seed = Random.Range(0, int.MaxValue);
        }
        
        // Initialize the terrain generator
        terrainGenerator = new TerrainGenerator(seed);
        Debug.Log($"World initialized with seed: {seed}");
        
        // Validate the block material
        if (blockMaterial == null)
        {
            Debug.LogWarning("No block material assigned to WorldManager. Chunks may not render correctly.");
        }
        
        // Validate chunk prefab
        if (chunkPrefab == null)
        {
            Debug.LogWarning("No chunk prefab assigned! Will create chunks dynamically but they may not have correct components.");
        }
    }

    private void Start()
    {
        Debug.Log($"Starting world generation at player position: {player.position}");
        
        // Position player at a safe height before generating chunks
        PositionPlayerAtSafeHeight();
        
        // Initial chunk loading around player
        lastPlayerChunkPosition = GetChunkCoordFromPosition(player.position);
        Debug.Log($"Initial player chunk position: {lastPlayerChunkPosition}");
        
        // Generate the player's chunk and immediate neighbors IMMEDIATELY
        // This prevents the player from falling through ungenerated chunks
        GenerateImmediateChunks();
        
        // Then queue up the rest of the visible chunks
        LoadChunksAroundPlayer();
    }

    private void Update()
    {
        // Check if player has moved to a new chunk
        Vector2Int currentPlayerChunkPos = GetChunkCoordFromPosition(player.position);

        if (currentPlayerChunkPos != lastPlayerChunkPosition)
        {
            if (debugMode)
            {
                Debug.Log($"Player moved to new chunk: {currentPlayerChunkPos} (from {lastPlayerChunkPosition})");
            }
            
            lastPlayerChunkPosition = currentPlayerChunkPos;
            LoadChunksAroundPlayer();
        }

        // Process chunk load queue (maximum of 2 chunks per frame to prevent stuttering)
        int chunksToLoadThisFrame = Mathf.Min(2, chunkLoadQueue.Count);
        for (int i = 0; i < chunksToLoadThisFrame; i++)
        {
            if (chunkLoadQueue.Count > 0)
            {
                ChunkCoord coord = chunkLoadQueue.Dequeue();
                CreateChunkAt(coord);
            }
        }

        // Process chunk unload queue (1 per frame)
        if (chunkUnloadQueue.Count > 0)
        {
            string key = chunkUnloadQueue.Dequeue();
            UnloadChunk(key);
        }
        
        // Debug display
        if (debugMode && Time.frameCount % 300 == 0)
        {
            Debug.Log($"Active chunks: {loadedChunks.Count}, Load queue: {chunkLoadQueue.Count}, Unload queue: {chunkUnloadQueue.Count}");
        }
    }

    private void LoadChunksAroundPlayer()
    {
        Vector2Int playerChunkCoord = lastPlayerChunkPosition;
        HashSet<string> chunksToKeep = new HashSet<string>();

        // Calculate vertical chunk range
        int minYLevel = Mathf.FloorToInt(TerrainGenerator.MIN_HEIGHT / (float)Chunk.chunkSize);
        int maxYLevel = Mathf.CeilToInt(TerrainGenerator.MAX_HEIGHT / (float)Chunk.chunkSize);
        
        // Calculate player's Y chunk
        int playerYChunk = Mathf.FloorToInt(player.position.y / Chunk.chunkSize);
        
        if (debugMode)
        {
            Debug.Log($"Vertical chunk range: {minYLevel} to {maxYLevel}, player at Y chunk: {playerYChunk}");
        }
        
        // Store chunks we've already queued to avoid duplicates
        HashSet<string> queuedChunks = new HashSet<string>();
        
        // Determine which chunks should be loaded (horizontal and vertical)
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int horizontalCoord = new Vector2Int(playerChunkCoord.x + x, playerChunkCoord.y + z);
                
                // Calculate priority - for prioritizing close chunks in the queue
                float distance = Mathf.Sqrt(x * x + z * z);
                
                // Focus on chunks near the player's Y position
                int yStart = Mathf.Max(minYLevel, playerYChunk - 2);
                int yEnd = Mathf.Min(maxYLevel, playerYChunk + 2);
                
                // First handle chunks close to the player vertically
                for (int y = yStart; y <= yEnd; y++)
                {
                    string chunkKey = $"{horizontalCoord.x}_{y}_{horizontalCoord.y}";
                    chunksToKeep.Add(chunkKey);
                    
                    // If chunk isn't already loaded or queued, add it to the load queue
                    if (!loadedChunks.ContainsKey(chunkKey) && !queuedChunks.Contains(chunkKey))
                    {
                        ChunkCoord coord = new ChunkCoord { x = horizontalCoord.x, y = y, z = horizontalCoord.y };
                        chunkLoadQueue.Enqueue(coord);
                        queuedChunks.Add(chunkKey);
                        
                        if (debugMode)
                        {
                            Debug.Log($"Queued chunk for loading: {chunkKey} (near player)");
                        }
                    }
                }
                
                // Then add additional vertical chunks if needed
                // But only for closer chunks to avoid excessive loading
                if (distance <= viewDistance * 0.7f)
                {
                    // Add remaining vertical chunks
                    for (int y = minYLevel; y <= maxYLevel; y++)
                    {
                        // Skip the ones we've already handled
                        if (y >= yStart && y <= yEnd) continue;
                        
                        string chunkKey = $"{horizontalCoord.x}_{y}_{horizontalCoord.y}";
                        chunksToKeep.Add(chunkKey);
                        
                        // If chunk isn't already loaded or queued, add it to the load queue
                        if (!loadedChunks.ContainsKey(chunkKey) && !queuedChunks.Contains(chunkKey))
                        {
                            ChunkCoord coord = new ChunkCoord { x = horizontalCoord.x, y = y, z = horizontalCoord.y };
                            chunkLoadQueue.Enqueue(coord);
                            queuedChunks.Add(chunkKey);
                            
                            if (debugMode)
                            {
                                Debug.Log($"Queued chunk for loading: {chunkKey} (vertical column)");
                            }
                        }
                    }
                }
            }
        }

        // Find chunks to unload (any chunk not in chunksToKeep)
        List<string> chunksToUnload = new List<string>();
        foreach (string key in loadedChunks.Keys)
        {
            if (!chunksToKeep.Contains(key) && !chunkUnloadQueue.Contains(key))
            {
                chunksToUnload.Add(key);
            }
        }

        // Queue chunks for unloading
        foreach (string key in chunksToUnload)
        {
            chunkUnloadQueue.Enqueue(key);
            
            if (debugMode)
            {
                Debug.Log($"Queued chunk for unloading: {key}");
            }
        }
    }

    private void CreateChunkAt(ChunkCoord coord)
    {
        try
        {
            string chunkKey = coord.ToString();
            
            // If chunk already exists, don't create it again
            if (loadedChunks.ContainsKey(chunkKey))
            {
                return;
            }

            // Create a new GameObject for the chunk
            GameObject chunkObject;
            
            if (chunkPrefab != null)
            {
                chunkObject = Instantiate(chunkPrefab, transform);
            }
            else
            {
                chunkObject = new GameObject($"Chunk_{coord.x}_{coord.y}_{coord.z}");
                chunkObject.transform.parent = transform;
                chunkObject.AddComponent<MeshFilter>();
                chunkObject.AddComponent<MeshRenderer>();
                chunkObject.AddComponent<MeshCollider>();
                chunkObject.AddComponent<Chunk>();
            }
            
            // Position the chunk (chunks are Chunk.chunkSize units wide)
            chunkObject.transform.position = new Vector3(
                coord.x * Chunk.chunkSize, 
                coord.y * Chunk.chunkSize, 
                coord.z * Chunk.chunkSize
            );
            
            // Get the Chunk component
            Chunk chunk = chunkObject.GetComponent<Chunk>();
            if (chunk == null)
            {
                Debug.LogError($"Failed to get Chunk component for chunk at {coord}");
                Destroy(chunkObject);
                return;
            }
            
            // Initialize the chunk with its position
            chunk.Initialize(new Vector2Int(coord.x, coord.z), coord.y);
            
            // Set the chunk's material
            if (blockMaterial != null)
            {
                MeshRenderer renderer = chunk.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material = blockMaterial;
                }
            }
            
            // Track the chunk
            loadedChunks.Add(chunkKey, chunk);
            
            if (debugMode)
            {
                Debug.Log($"Created chunk at {coord}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating chunk at {coord}: {e.Message}\n{e.StackTrace}");
        }
    }

    private void UnloadChunk(string chunkKey)
    {
        if (loadedChunks.TryGetValue(chunkKey, out Chunk chunk))
        {
            if (chunk != null && chunk.gameObject != null)
            {
                Destroy(chunk.gameObject);
            }
            loadedChunks.Remove(chunkKey);
            
            if (debugMode)
            {
                Debug.Log($"Unloaded chunk: {chunkKey}");
            }
        }
    }

    // Get chunk coordinates from a world position
    public Vector2Int GetChunkCoordFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / Chunk.chunkSize);
        int z = Mathf.FloorToInt(position.z / Chunk.chunkSize);
        return new Vector2Int(x, z);
    }

    // Get a block from world coordinates (returns null if chunk not loaded)
    public BlockType GetBlockAt(Vector3 worldPosition)
    {
        // Get the chunk coordinates
        Vector2Int chunkCoord = GetChunkCoordFromPosition(worldPosition);
        int yLevel = Mathf.FloorToInt(worldPosition.y / Chunk.chunkSize);
        
        // Create the chunk key
        string chunkKey = $"{chunkCoord.x}_{yLevel}_{chunkCoord.y}";
        
        // Check if the chunk is loaded
        if (!loadedChunks.TryGetValue(chunkKey, out Chunk chunk))
        {
            return BlockType.Air; // Default to air if chunk not loaded
        }
        
        // Convert world position to local chunk position
        Vector3Int localPos = chunk.WorldToLocalPosition(worldPosition);
        
        // Get the block type
        return chunk.GetBlockType(localPos.x, localPos.y, localPos.z);
    }

    // Set a block at world coordinates
    public void SetBlockAt(Vector3 worldPosition, BlockType blockType)
    {
        // Get the chunk coordinates
        Vector2Int chunkCoord = GetChunkCoordFromPosition(worldPosition);
        int yLevel = Mathf.FloorToInt(worldPosition.y / Chunk.chunkSize);
        
        // Create the chunk key
        string chunkKey = $"{chunkCoord.x}_{yLevel}_{chunkCoord.y}";
        
        // Check if the chunk is loaded
        if (!loadedChunks.TryGetValue(chunkKey, out Chunk chunk))
        {
            Debug.LogWarning($"Tried to set block in unloaded chunk at {worldPosition}");
            return;
        }
        
        // Convert world position to local chunk position
        Vector3Int localPos = chunk.WorldToLocalPosition(worldPosition);
        
        // Set the block
        if (blockType == BlockType.Air)
        {
            chunk.RemoveBlock(localPos.x, localPos.y, localPos.z);
        }
        else
        {
            chunk.AddBlock(localPos.x, localPos.y, localPos.z, blockType);
        }
        
        // If this block is at a chunk boundary, update adjacent chunks too
        UpdateAdjacentChunks(worldPosition, blockType);
    }

    // Add a method to update adjacent chunks when blocks at chunk boundaries are changed
    private void UpdateAdjacentChunks(Vector3 worldPosition, BlockType blockType)
    {
        // Check if this block is at a chunk boundary
        Vector3Int localPos = new Vector3Int(
            Mathf.FloorToInt(worldPosition.x) % Chunk.chunkSize,
            Mathf.FloorToInt(worldPosition.y) % Chunk.chunkSize,
            Mathf.FloorToInt(worldPosition.z) % Chunk.chunkSize
        );
        
        // Normalize to handle negative positions correctly
        if (localPos.x < 0) localPos.x += Chunk.chunkSize;
        if (localPos.y < 0) localPos.y += Chunk.chunkSize;
        if (localPos.z < 0) localPos.z += Chunk.chunkSize;
        
        // Check if block is at the edge of a chunk
        bool atXMin = localPos.x == 0;
        bool atXMax = localPos.x == Chunk.chunkSize - 1;
        bool atYMin = localPos.y == 0;
        bool atYMax = localPos.y == Chunk.chunkSize - 1;
        bool atZMin = localPos.z == 0;
        bool atZMax = localPos.z == Chunk.chunkSize - 1;
        
        // If the block isn't at a chunk boundary, no need to update adjacent chunks
        if (!atXMin && !atXMax && !atYMin && !atYMax && !atZMin && !atZMax)
        {
            return;
        }
        
        // Updated adjacent chunks to rebuild their meshes
        // Only do this for the boundaries where the block is positioned
        
        // X boundaries
        if (atXMin)
        {
            Vector3 adjacentPos = worldPosition + new Vector3(-0.1f, 0, 0);
            UpdateChunkMeshAt(adjacentPos);
        }
        if (atXMax)
        {
            Vector3 adjacentPos = worldPosition + new Vector3(0.1f, 0, 0);
            UpdateChunkMeshAt(adjacentPos);
        }
        
        // Y boundaries
        if (atYMin)
        {
            Vector3 adjacentPos = worldPosition + new Vector3(0, -0.1f, 0);
            UpdateChunkMeshAt(adjacentPos);
        }
        if (atYMax)
        {
            Vector3 adjacentPos = worldPosition + new Vector3(0, 0.1f, 0);
            UpdateChunkMeshAt(adjacentPos);
        }
        
        // Z boundaries
        if (atZMin)
        {
            Vector3 adjacentPos = worldPosition + new Vector3(0, 0, -0.1f);
            UpdateChunkMeshAt(adjacentPos);
        }
        if (atZMax)
        {
            Vector3 adjacentPos = worldPosition + new Vector3(0, 0, 0.1f);
            UpdateChunkMeshAt(adjacentPos);
        }
    }

    // Helper method to update a chunk's mesh at a specific position
    private void UpdateChunkMeshAt(Vector3 worldPosition)
    {
        // Get the chunk coordinates
        Vector2Int chunkCoord = GetChunkCoordFromPosition(worldPosition);
        int yLevel = Mathf.FloorToInt(worldPosition.y / Chunk.chunkSize);
        
        // Create the chunk key
        string chunkKey = $"{chunkCoord.x}_{yLevel}_{chunkCoord.y}";
        
        // Check if the chunk is loaded
        if (loadedChunks.TryGetValue(chunkKey, out Chunk chunk))
        {
            // Regenerate the mesh
            chunk.RegenerateMesh();
        }
    }
    
    // Get terrain height at a world position
    public int GetTerrainHeight(float worldX, float worldZ)
    {
        if (terrainGenerator == null)
        {
            Debug.LogError("TerrainGenerator is null when trying to get terrain height!");
            return 8; // Default fallback height
        }
        
        return terrainGenerator.GetTerrainHeight(worldX, worldZ);
    }
    
    // Check if a position should be a cave
    public bool IsCave(float worldX, float worldY, float worldZ)
    {
        if (!generateCaves)
            return false;
            
        if (terrainGenerator == null)
        {
            Debug.LogError("TerrainGenerator is null when checking for caves!");
            return false;
        }
        
        return terrainGenerator.IsCave(worldX, worldY, worldZ);
    }

    // Get block type at a specific world position, taking biome into account
    public BlockType GetBlockTypeAt(float worldX, int worldY, float worldZ, int surfaceHeight)
    {
        if (terrainGenerator == null)
        {
            Debug.LogError("TerrainGenerator is null when trying to get block type!");
            return BlockType.Stone; // Default fallback
        }
        
        return terrainGenerator.GetBlockType(worldX, worldY, worldZ, surfaceHeight);
    }

    // Get biome at a specific world position
    public BiomeType GetBiomeAt(float worldX, float worldZ)
    {
        if (terrainGenerator == null)
        {
            Debug.LogError("TerrainGenerator is null when trying to get biome!");
            return BiomeType.Plains; // Default fallback
        }
        
        return terrainGenerator.GetBiomeAt(worldX, worldZ);
    }

    // Add this method to position the player at a safe starting height
    public void PositionPlayerAtSafeHeight()
    {
        if (player == null)
        {
            Debug.LogError("Cannot position player: no player reference!");
            return;
        }
        
        // Get current player position
        Vector3 playerPos = player.position;
        
        // Get terrain height at player's x/z position
        int terrainHeight = GetTerrainHeight(playerPos.x, playerPos.z);
        
        // Set the player 5 blocks above terrain
        float safeY = terrainHeight + 5f;
        
        // Make sure player is above sea level
        safeY = Mathf.Max(safeY, TerrainGenerator.SEA_LEVEL + 2f);
        
        // Update player position
        player.position = new Vector3(playerPos.x, safeY, playerPos.z);
        
        Debug.Log($"Positioned player at safe height: {safeY} (terrain: {terrainHeight})");
    }

    // Add a new method to immediately generate chunks under the player
    private void GenerateImmediateChunks()
    {
        Vector2Int playerChunkCoord = lastPlayerChunkPosition;
        
        // Calculate the range of vertical chunks the player might interact with
        int minYLevel = Mathf.FloorToInt(TerrainGenerator.MIN_HEIGHT / (float)Chunk.chunkSize);
        int maxYLevel = Mathf.CeilToInt(TerrainGenerator.MAX_HEIGHT / (float)Chunk.chunkSize);
        int playerYChunk = Mathf.FloorToInt(player.position.y / Chunk.chunkSize);
        
        // Get the chunk directly under the player and immediate neighbors
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                // Create a 3x3 grid around player
                Vector2Int coord = new Vector2Int(playerChunkCoord.x + x, playerChunkCoord.y + z);
                
                // Generate a vertical column of chunks at this coordinate
                // Focus on chunks around player Y level first
                CreateChunkAt(new ChunkCoord { x = coord.x, y = playerYChunk, z = coord.y });
                
                // Add chunk below player
                if (playerYChunk > minYLevel)
                {
                    CreateChunkAt(new ChunkCoord { x = coord.x, y = playerYChunk - 1, z = coord.y });
                }
                
                // Add chunk above player 
                if (playerYChunk < maxYLevel - 1)
                {
                    CreateChunkAt(new ChunkCoord { x = coord.x, y = playerYChunk + 1, z = coord.y });
                }
            }
        }
        
        Debug.Log("Generated immediate chunks around player");
    }

    // Add the missing GetBiomeWeightsAt method
    public float[,] GetBiomeWeightsAt(float worldX, float worldZ)
    {
        if (terrainGenerator == null)
        {
            Debug.LogError("TerrainGenerator is null when trying to get biome weights!");
            
            // Return a default array with only Plains biome
            float[,] defaultWeights = new float[5, 2];
            for (int i = 0; i < 5; i++)
            {
                defaultWeights[i, 0] = i;
                defaultWeights[i, 1] = i == (int)BiomeType.Plains ? 1f : 0f;
            }
            
            return defaultWeights;
        }
        
        return terrainGenerator.GetBiomeWeightsAt(worldX, worldZ);
    }
} 