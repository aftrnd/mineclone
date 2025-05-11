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
    private Dictionary<Vector2Int, Chunk> loadedChunks = new Dictionary<Vector2Int, Chunk>();
    private Vector2Int lastPlayerChunkPosition;

    // Chunk queue for loading/unloading operations
    private Queue<Vector2Int> chunkLoadQueue = new Queue<Vector2Int>();
    private Queue<Vector2Int> chunkUnloadQueue = new Queue<Vector2Int>();

    // Terrain generator
    private TerrainGenerator terrainGenerator;

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
        
        // Initial chunk loading around player
        lastPlayerChunkPosition = GetChunkCoordFromPosition(player.position);
        Debug.Log($"Initial player chunk position: {lastPlayerChunkPosition}");
        
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
                Vector2Int coord = chunkLoadQueue.Dequeue();
                CreateChunkAt(coord);
            }
        }

        // Process chunk unload queue (1 per frame)
        if (chunkUnloadQueue.Count > 0)
        {
            Vector2Int coord = chunkUnloadQueue.Dequeue();
            UnloadChunk(coord);
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
        HashSet<Vector2Int> chunksToKeep = new HashSet<Vector2Int>();

        // Determine which chunks should be loaded
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int coord = new Vector2Int(playerChunkCoord.x + x, playerChunkCoord.y + z);
                chunksToKeep.Add(coord);

                // If chunk isn't already loaded, add it to the load queue
                if (!loadedChunks.ContainsKey(coord) && !chunkLoadQueue.Contains(coord))
                {
                    chunkLoadQueue.Enqueue(coord);
                    
                    if (debugMode)
                    {
                        Debug.Log($"Queued chunk for loading: {coord}");
                    }
                }
            }
        }

        // Find chunks to unload (any chunk not in chunksToKeep)
        List<Vector2Int> chunksToUnload = new List<Vector2Int>();
        foreach (Vector2Int coord in loadedChunks.Keys)
        {
            if (!chunksToKeep.Contains(coord) && !chunkUnloadQueue.Contains(coord))
            {
                chunksToUnload.Add(coord);
            }
        }

        // Queue chunks for unloading
        foreach (Vector2Int coord in chunksToUnload)
        {
            chunkUnloadQueue.Enqueue(coord);
            
            if (debugMode)
            {
                Debug.Log($"Queued chunk for unloading: {coord}");
            }
        }
    }

    private void CreateChunkAt(Vector2Int coord)
    {
        try
        {
            // If chunk already exists, don't create it again
            if (loadedChunks.ContainsKey(coord))
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
                chunkObject = new GameObject($"Chunk_{coord.x}_{coord.y}");
                chunkObject.transform.parent = transform;
                chunkObject.AddComponent<MeshFilter>();
                chunkObject.AddComponent<MeshRenderer>();
                chunkObject.AddComponent<MeshCollider>();
                chunkObject.AddComponent<Chunk>();
            }
            
            // Position the chunk (chunks are Chunk.chunkSize units wide)
            chunkObject.transform.position = new Vector3(coord.x * Chunk.chunkSize, 0, coord.y * Chunk.chunkSize);
            
            // Get the Chunk component
            Chunk chunk = chunkObject.GetComponent<Chunk>();
            if (chunk == null)
            {
                Debug.LogError($"Failed to get Chunk component for chunk at {coord}");
                Destroy(chunkObject);
                return;
            }
            
            // Initialize the chunk with its position
            chunk.Initialize(coord);
            
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
            loadedChunks.Add(coord, chunk);
            
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

    private void UnloadChunk(Vector2Int coord)
    {
        if (loadedChunks.TryGetValue(coord, out Chunk chunk))
        {
            if (chunk != null && chunk.gameObject != null)
            {
                Destroy(chunk.gameObject);
            }
            loadedChunks.Remove(coord);
            
            if (debugMode)
            {
                Debug.Log($"Unloaded chunk at {coord}");
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
        
        // Check if the chunk is loaded
        if (!loadedChunks.TryGetValue(chunkCoord, out Chunk chunk))
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
        
        // Check if the chunk is loaded
        if (!loadedChunks.TryGetValue(chunkCoord, out Chunk chunk))
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
} 