using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Utility script to help debug world generation issues.
/// Add this to any GameObject in your scene.
/// </summary>
public class WorldDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldManager worldManager;
    [SerializeField] private Transform player;
    
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool logTerrainDetails = false;
    [SerializeField] private KeyCode toggleDebugKey = KeyCode.F3;
    [SerializeField] private KeyCode regenerateKey = KeyCode.F4;
    
    [Header("UI (Optional)")]
    [SerializeField] private TextMeshProUGUI debugText;
    
    private Camera mainCamera;
    private float updateTimer = 0f;
    private float updateInterval = 0.5f;
    
    private void Start()
    {
        if (worldManager == null)
        {
            worldManager = FindObjectOfType<WorldManager>();
            if (worldManager == null)
            {
                Debug.LogError("WorldDebugger: No WorldManager found!");
                enabled = false;
                return;
            }
        }
        
        if (player == null)
        {
            player = Camera.main?.transform.parent;
            if (player == null)
            {
                player = Camera.main?.transform;
            }
        }
        
        mainCamera = Camera.main;
    }
    
    private void Update()
    {
        // Toggle debug display
        if (Input.GetKeyDown(toggleDebugKey))
        {
            showDebugInfo = !showDebugInfo;
            if (debugText != null) debugText.gameObject.SetActive(showDebugInfo);
        }
        
        // Force regenerate chunks with F4
        if (Input.GetKeyDown(regenerateKey) && worldManager != null)
        {
            Debug.Log("Regenerating all visible chunks...");
            StartCoroutine(RegenerateChunks());
        }
        
        // Update debug info at intervals
        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0f)
        {
            updateTimer = updateInterval;
            if (showDebugInfo)
            {
                UpdateDebugInfo();
            }
        }
    }
    
    private System.Collections.IEnumerator RegenerateChunks()
    {
        // Find all chunks in the scene
        Chunk[] chunks = FindObjectsOfType<Chunk>();
        Debug.Log($"Found {chunks.Length} chunks to regenerate");
        
        // Regenerate each chunk
        foreach (Chunk chunk in chunks)
        {
            // Destroy the chunk GameObject
            Destroy(chunk.gameObject);
            yield return null; // Wait a frame between each chunk
        }
        
        // Let the WorldManager regenerate chunks
        yield return new WaitForSeconds(0.5f);
        Debug.Log("Chunks regenerated");
    }
    
    private void UpdateDebugInfo()
    {
        if (player == null || worldManager == null) return;
        
        // Get player's current position and chunk
        Vector3 playerPos = player.position;
        Vector2Int playerChunk = worldManager.GetChunkCoordFromPosition(playerPos);
        
        // Get terrain height at player position
        int terrainHeight = worldManager.GetTerrainHeight(playerPos.x, playerPos.z);
        
        // Create debug string
        string debugInfo = $"<b>Position:</b> ({playerPos.x:F1}, {playerPos.y:F1}, {playerPos.z:F1})\n" +
                          $"<b>Chunk:</b> {playerChunk.x}, {playerChunk.y}\n" +
                          $"<b>Terrain Height:</b> {terrainHeight}\n" +
                          $"<b>Block:</b> {GetBlockTypeAtPosition(playerPos)}";
        
        // Display in UI if available
        if (debugText != null)
        {
            debugText.text = debugInfo;
        }
        
        // Log terrain details if enabled
        if (logTerrainDetails)
        {
            // Check terrain and caves in a grid around player
            Debug.Log($"Terrain details at player position {playerPos}:");
            for (int z = -2; z <= 2; z++)
            {
                for (int x = -2; x <= 2; x++)
                {
                    float worldX = playerPos.x + x;
                    float worldZ = playerPos.z + z;
                    int height = worldManager.GetTerrainHeight(worldX, worldZ);
                    bool isCave = worldManager.IsCave(worldX, Mathf.FloorToInt(playerPos.y), worldZ);
                    Debug.Log($"Position ({worldX}, {worldZ}): Height={height}, Cave={isCave}");
                }
            }
        }
    }
    
    private BlockType GetBlockTypeAtPosition(Vector3 position)
    {
        if (worldManager == null) return BlockType.Air;
        return worldManager.GetBlockAt(position);
    }
    
    private void OnGUI()
    {
        if (!showDebugInfo || debugText != null) return;
        
        // Fall back to IMGUI if no TextMeshPro UI is set up
        GUIStyle style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft };
        style.normal.textColor = Color.white;
        
        if (player != null && worldManager != null)
        {
            Vector3 playerPos = player.position;
            Vector2Int playerChunk = worldManager.GetChunkCoordFromPosition(playerPos);
            int terrainHeight = worldManager.GetTerrainHeight(playerPos.x, playerPos.z);
            
            string debugInfo = $"Position: ({playerPos.x:F1}, {playerPos.y:F1}, {playerPos.z:F1})\n" +
                              $"Chunk: {playerChunk.x}, {playerChunk.y}\n" +
                              $"Terrain Height: {terrainHeight}\n" +
                              $"Block: {GetBlockTypeAtPosition(playerPos)}";
            
            GUI.Box(new Rect(10, 10, 300, 100), debugInfo, style);
        }
    }
} 