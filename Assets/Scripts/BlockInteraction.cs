using UnityEngine;
using UnityEngine.InputSystem;

public class BlockInteraction : MonoBehaviour
{
    [Header("Block Interaction")]
    [SerializeField] private float interactionDistance = 5.0f; // Maximum reach in Minecraft is ~5 blocks
    [SerializeField] private LayerMask blockLayer;
    [SerializeField] private GameObject blockHighlighterObject; // Changed to GameObject
    [SerializeField] private Camera playerCamera; // Added direct reference to camera
    private BlockHighlighter blockHighlighter; // Private reference for internal use
    
    [Header("Block Selection")]
    [SerializeField] private int selectedBlockType = 1; // Default block type to place
    [SerializeField] private bool showSelectedBlockTypeInUI = true;
    private string[] blockTypeNames = { "Air", "Dirt", "Stone", "Grass" }; // Matches the BlockType enum
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private bool isBreakingBlock = false;
    private bool isPlacingBlock = false;
    private bool wasPlacingLastFrame = false;
    private Vector3? lastHitPosition = null;
    
    private float debugLogTimer = 0f;
    private float debugLogInterval = 1f; // Log every 1 second
    
    private bool wasBreakingLastFrame = false;
    
    private WorldManager worldManager;
    
    private void Start()
    {
        Debug.Log("BlockInteraction Start method called");
        
        // Check if we have a block layer set
        if (blockLayer.value == 0)
        {
            Debug.LogWarning("Block layer mask is 0 (Default) - this might cause issues with block detection.");
            // Set it to everything as a fallback
            blockLayer = Physics.AllLayers;
        }
        
        Debug.Log($"Block layer mask: {blockLayer.value}");
        
        // Check camera
        if (playerCamera == null)
        {
            // Try to find camera in our children first
            playerCamera = GetComponentInChildren<Camera>();
            
            // Fall back to Camera.main as last resort
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null)
                {
                    Debug.LogError("No camera found! Please assign a camera reference manually.");
                }
                else
                {
                    Debug.Log("Using Camera.main: " + playerCamera.name);
                }
            }
            else
            {
                Debug.Log("Found camera in children: " + playerCamera.name);
            }
        }
        else
        {
            Debug.Log("Using assigned camera: " + playerCamera.name);
        }
        
        // Detailed logging for BlockHighlighter initialization
        Debug.Log("Attempting to find BlockHighlighter...");
        
        // Convert GameObject reference to BlockHighlighter component
        if (blockHighlighterObject != null)
        {
            Debug.Log("blockHighlighterObject is assigned: " + blockHighlighterObject.name);
            blockHighlighter = blockHighlighterObject.GetComponent<BlockHighlighter>();
            if (blockHighlighter != null)
            {
                Debug.Log("Successfully found BlockHighlighter component from GameObject reference");
            }
            else
            {
                Debug.LogWarning("blockHighlighterObject doesn't have a BlockHighlighter component");
            }
        }
        else
        {
            Debug.Log("blockHighlighterObject is not assigned, trying alternative methods");
        }
        
        // Auto-find the block highlighter if it's not assigned
        if (blockHighlighter == null)
        {
            Debug.Log("Attempting to find BlockHighlighter as child component...");
            // First try to find it as a child component
            blockHighlighter = GetComponentInChildren<BlockHighlighter>();
            if (blockHighlighter != null)
            {
                Debug.Log("Found BlockHighlighter as child component: " + blockHighlighter.name);
            }
            
            // If not found, try to find by name
            if (blockHighlighter == null)
            {
                Debug.Log("Searching for child named 'BlockHighlighter'...");
                Transform highlighterTransform = transform.Find("BlockHighlighter");
                if (highlighterTransform != null)
                {
                    Debug.Log("Found child named 'BlockHighlighter'");
                    blockHighlighter = highlighterTransform.GetComponent<BlockHighlighter>();
                    if (blockHighlighter != null)
                    {
                        Debug.Log("Found BlockHighlighter component by name");
                    }
                    else
                    {
                        Debug.LogWarning("Child 'BlockHighlighter' doesn't have BlockHighlighter component");
                    }
                }
                else
                {
                    Debug.Log("No child named 'BlockHighlighter' found");
                }
            }
            
            // If still not found, create one
            if (blockHighlighter == null)
            {
                Debug.Log("Creating new BlockHighlighter GameObject and component");
                GameObject highlighterObject = new GameObject("BlockHighlighter");
                highlighterObject.transform.SetParent(transform);
                blockHighlighter = highlighterObject.AddComponent<BlockHighlighter>();
                if (blockHighlighter != null)
                {
                    Debug.Log("Successfully created new BlockHighlighter");
                }
                else
                {
                    Debug.LogError("Failed to create BlockHighlighter component");
                }
            }
        }
        
        // Final check
        if (blockHighlighter != null)
        {
            Debug.Log("BlockHighlighter is ready: " + blockHighlighter.name);
        }
        else
        {
            Debug.LogError("BlockHighlighter is STILL null after all attempts to find/create it");
        }
        
        // Make sure blocks have the correct tag
        Debug.Log("Checking block layer mask: " + blockLayer.value);
        
        // Find the world manager
        worldManager = FindObjectOfType<WorldManager>();
        if (worldManager == null)
        {
            Debug.LogWarning("WorldManager not found! Cross-chunk block interaction won't work.");
        }
    }
    
    public void OnAttack(InputAction.CallbackContext context)
    {
        isBreakingBlock = context.ReadValueAsButton();
        if (context.performed) // Button was just pressed
        {
            Debug.Log("Attack action triggered (breaking blocks)");
        }
    }
    
    public void OnInteract(InputAction.CallbackContext context)
    {
        isPlacingBlock = context.ReadValueAsButton();
        if (context.performed) // Button was just pressed
        {
            Debug.Log("Interact action triggered (placing blocks)");
        }
    }
    
    public void OnNextBlock(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            selectedBlockType = (selectedBlockType + 1) % (blockTypeNames.Length - 1);
            if (selectedBlockType == 0) selectedBlockType = 1; // Skip Air (0)
            Debug.Log($"Selected Block Type: {selectedBlockType} - {blockTypeNames[selectedBlockType]}");
        }
    }
    
    public void OnPreviousBlock(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            selectedBlockType--;
            if (selectedBlockType < 1) selectedBlockType = blockTypeNames.Length - 1; // Wrap around, skip Air (0)
            Debug.Log($"Selected Block Type: {selectedBlockType} - {blockTypeNames[selectedBlockType]}");
        }
    }
    
    private void Update()
    {
        // Handle direct number key block selection (1-9)
        HandleDirectBlockSelection();
        
        // Direct debugging of mouse buttons - remove once inputs are working
        if (Input.GetMouseButtonDown(0)) 
        {
            Debug.Log("Left mouse button pressed directly");
        }
        if (Input.GetMouseButtonDown(1)) 
        {
            Debug.Log("Right mouse button pressed directly");
            // Try to place a block directly
            TryPlaceBlockDirect();
        }
        
        // Make sure we have the highlighter before trying to use it
        if (blockHighlighter == null)
        {
            Debug.LogWarning("BlockHighlighter is null in Update. Trying to find it again...");
            // Try to find it first
            blockHighlighter = GetComponentInChildren<BlockHighlighter>();
            
            // If still null, create one
            if (blockHighlighter == null)
            {
                GameObject highlighterObject = new GameObject("BlockHighlighter");
                highlighterObject.transform.SetParent(transform);
                blockHighlighter = highlighterObject.AddComponent<BlockHighlighter>();
                Debug.Log("Created new BlockHighlighter in Update");
            }
        }
        
        // Only proceed if we have a valid highlighter
        if (blockHighlighter != null)
        {
            // Throttle the debug logs to avoid spamming the console
            if (debugLogTimer <= 0)
            {
                HandleBlockInteractionWithDebug();
                debugLogTimer = debugLogInterval;
            }
            else
            {
                // Regular update without logging
                HandleBlockInteraction();
                debugLogTimer -= Time.deltaTime;
            }
        }
        else
        {
            Debug.LogError("Failed to create or find BlockHighlighter. Block interaction is disabled.");
        }
    }
    
    // Direct block selection with number keys
    private void HandleDirectBlockSelection()
    {
        // Skip 0 (Air) - Start from 1
        if (Input.GetKeyDown(KeyCode.Alpha1) && 1 < blockTypeNames.Length)
        {
            selectedBlockType = 1; // Dirt
            Debug.Log($"Selected Block Type: {selectedBlockType} - {blockTypeNames[selectedBlockType]}");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && 2 < blockTypeNames.Length)
        {
            selectedBlockType = 2; // Stone
            Debug.Log($"Selected Block Type: {selectedBlockType} - {blockTypeNames[selectedBlockType]}");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && 3 < blockTypeNames.Length)
        {
            selectedBlockType = 3; // Grass
            Debug.Log($"Selected Block Type: {selectedBlockType} - {blockTypeNames[selectedBlockType]}");
        }
    }
    
    private void OnGUI()
    {
        // Only show if enabled
        if (showSelectedBlockTypeInUI)
        {
            // Define a style for the label
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 16;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            style.padding = new RectOffset(10, 10, 5, 5);
            
            // Create a background box for better visibility
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Get the block name
            string blockName = selectedBlockType < blockTypeNames.Length ? 
                blockTypeNames[selectedBlockType] : "Unknown";
            
            // Create a box in the upper right corner
            Rect boxRect = new Rect(Screen.width - 200, 10, 180, 40);
            GUI.Box(boxRect, "");
            
            // Add label text inside the box
            GUI.Label(boxRect, $"Selected Block: {blockName}", style);
            
            // Draw block color indicator
            Color blockColor = GetBlockColor(selectedBlockType);
            Rect colorRect = new Rect(Screen.width - 50, 15, 30, 30);
            GUI.backgroundColor = blockColor;
            GUI.Box(colorRect, "");
        }
    }
    
    // Helper method to get a color representing each block type
    private Color GetBlockColor(int blockTypeIndex)
    {
        switch (blockTypeIndex)
        {
            case 1: return new Color(0.6f, 0.4f, 0.2f); // Dirt - brown
            case 2: return new Color(0.5f, 0.5f, 0.5f); // Stone - gray
            case 3: return new Color(0.2f, 0.8f, 0.2f); // Grass - green
            default: return Color.white;
        }
    }
    
    // Replace the entire method with this clean version:
    private void HandleBlockInteractionWithDebug()
    {
        // Ensure we have a camera reference
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("No main camera found!");
                return;
            }
        }
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        // Draw the ray in the scene view (will only be visible in the editor)
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red, debugLogInterval);
        RaycastHit hit;
        
        Debug.Log($"BLOCK DETECTION: Casting ray from {ray.origin} in direction {ray.direction}");
        Debug.Log($"Using distance {interactionDistance} and layer mask {blockLayer.value}");
        
        // Try different layer masks to debug
        bool hitAnything = Physics.Raycast(ray, out hit, interactionDistance);
        if (hitAnything)
        {
            Debug.Log($"Hit SOMETHING (any layer) at distance {hit.distance}");
            Debug.Log($"Hit: {hit.collider.name}, Tag: {hit.collider.tag}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            
            // Draw a sphere at the hit point (visible in Scene view)
            Debug.DrawLine(ray.origin, hit.point, Color.green, debugLogInterval);
            DebugDrawSphere(hit.point, 0.1f, Color.green, debugLogInterval);
        }
        
        // Try the actual layer mask
        if (Physics.Raycast(ray, out hit, interactionDistance, blockLayer))
        {
            Debug.Log($"Hit BLOCK at distance {hit.distance}!");
            Debug.Log($"Block: {hit.collider.name}, Tag: {hit.collider.tag}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            
            // Get the targeted block position
            Vector3 blockPosition = new Vector3(
                Mathf.Floor(hit.point.x - (hit.normal.x * 0.001f)) + 0.5f,
                Mathf.Floor(hit.point.y - (hit.normal.y * 0.001f)) + 0.5f,
                Mathf.Floor(hit.point.z - (hit.normal.z * 0.001f)) + 0.5f
            );
            
            // Draw debug visuals for the block position
            DebugDrawCube(blockPosition, Vector3.one, Color.yellow, debugLogInterval);
            
            // Update the block highlight
            if (blockHighlighter != null)
            {
                if (lastHitPosition == null || blockPosition != lastHitPosition.Value)
                {
                    blockHighlighter.ShowHighlight(blockPosition);
                    lastHitPosition = blockPosition;
                    Debug.Log($"Updated highlight to position {blockPosition}");
                }
            }
            else
            {
                Debug.LogError("blockHighlighter is null when trying to show highlight!");
            }
            
            // Break block
            if (isBreakingBlock)
            {
                // Only break a block when attack input is first pressed, not while held
                // This prevents continuous breaking while holding the button
                if (!wasBreakingLastFrame)
                {
                    // Try to get chunk from hit collider
                    Chunk chunk = hit.collider.GetComponent<Chunk>();
                    
                    // If the hit collider is the chunk's mesh collider
                    if (chunk != null)
                    {
                        // Convert world position to chunk-local coordinates - using hit.point and normal
                        // Very important to offset slightly from the surface in the direction of the normal
                        // to ensure we're inside the correct block
                        Vector3 blockPos = hit.point - (hit.normal * 0.01f); // Move slightly inside the block
                        
                        Vector3Int localPos = new Vector3Int(
                            Mathf.FloorToInt(blockPos.x) - Mathf.FloorToInt(chunk.transform.position.x),
                            Mathf.FloorToInt(blockPos.y) - Mathf.FloorToInt(chunk.transform.position.y),
                            Mathf.FloorToInt(blockPos.z) - Mathf.FloorToInt(chunk.transform.position.z)
                        );
                        
                        // Call the chunk's method to remove the block
                        chunk.RemoveBlock(localPos.x, localPos.y, localPos.z);
                        Debug.Log($"Breaking block at {localPos} in chunk at {chunk.transform.position}");
                    }
                }
            }
            
            // Place block
            if (isPlacingBlock && !wasPlacingLastFrame)
            {
                // Calculate position for new block (adjacent to the face that was hit)
                Vector3 placePosition = hit.point + hit.normal * 0.001f;
                Vector3Int placeWorldPos = new Vector3Int(
                    Mathf.FloorToInt(placePosition.x),
                    Mathf.FloorToInt(placePosition.y),
                    Mathf.FloorToInt(placePosition.z)
                );
                
                // Try to determine which chunk this would belong to
                // Simple approach: check if position is within the bounds of the hit chunk
                Chunk chunk = hit.collider.GetComponent<Chunk>();
                if (chunk != null)
                {
                    Vector3Int localPos = chunk.WorldToLocalPosition(placePosition);
                    
                    // Check if the position is in bounds of this chunk
                    if (localPos.x >= 0 && localPos.x < Chunk.chunkSize &&
                        localPos.y >= 0 && localPos.y < Chunk.chunkSize &&
                        localPos.z >= 0 && localPos.z < Chunk.chunkSize)
                    {
                        // Determine block type from selectedBlockType index
                        BlockType blockTypeToPlace = BlockType.Dirt; // Default
                        
                        // Convert selectedBlockType (1-indexed) to a BlockType
                        switch (selectedBlockType)
                        {
                            case 1:
                                blockTypeToPlace = BlockType.Dirt;
                                break;
                            case 2:
                                blockTypeToPlace = BlockType.Stone;
                                break;
                            case 3:
                                blockTypeToPlace = BlockType.Grass;
                                break;
                            default:
                                blockTypeToPlace = BlockType.Dirt;
                                break;
                        }
                        
                        // Add the block
                        chunk.AddBlock(localPos.x, localPos.y, localPos.z, blockTypeToPlace);
                        Debug.Log($"Placed block of type {blockTypeToPlace} at local position {localPos} in chunk");
                    }
                    else
                    {
                        Debug.Log($"Position {placeWorldPos} is outside chunk bounds");
                        // Would need to find or create a new chunk for this position
                    }
                }
            }
            
            wasBreakingLastFrame = isBreakingBlock;
        }
        else if (hitAnything)
        {
            Debug.Log("Hit something but not on the block layer - check your layer mask!");
            if (blockHighlighter != null)
            {
                blockHighlighter.HideHighlight();
                lastHitPosition = null;
            }
            wasBreakingLastFrame = isBreakingBlock;
        }
        else
        {
            Debug.Log("No hit detected from raycast - nothing in front of player");
            
            if (blockHighlighter != null)
            {
                // No block in sight, hide the highlight
                blockHighlighter.HideHighlight();
                lastHitPosition = null;
            }
            wasBreakingLastFrame = isBreakingBlock;
        }
        
        // Handle these lines at the end of the method:
        wasPlacingLastFrame = isPlacingBlock;
    }
    
    // Update the HandleBlockInteraction method to use WorldManager
    private void HandleBlockInteraction()
    {
        // Ensure we have a camera reference
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                return;
            }
        }
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionDistance, blockLayer))
        {
            // Get the targeted block position
            Vector3 blockPosition = new Vector3(
                Mathf.Floor(hit.point.x - (hit.normal.x * 0.001f)) + 0.5f,
                Mathf.Floor(hit.point.y - (hit.normal.y * 0.001f)) + 0.5f,
                Mathf.Floor(hit.point.z - (hit.normal.z * 0.001f)) + 0.5f
            );
            
            // Update the block highlight
            if (blockHighlighter != null)
            {
                if (lastHitPosition == null || blockPosition != lastHitPosition.Value)
                {
                    blockHighlighter.ShowHighlight(blockPosition);
                    lastHitPosition = blockPosition;
                }
            }
            
            // Break block
            if (isBreakingBlock)
            {
                // Only break a block when attack input is first pressed, not while held
                if (!wasBreakingLastFrame)
                {
                    // Get the exact block position (center of the block)
                    Vector3 blockPos = hit.point - (hit.normal * 0.01f); // Move slightly inside the block
                    Vector3Int blockWorldPos = new Vector3Int(
                        Mathf.FloorToInt(blockPos.x),
                        Mathf.FloorToInt(blockPos.y),
                        Mathf.FloorToInt(blockPos.z)
                    );
                    
                    // Use WorldManager if available, otherwise fall back to direct chunk interaction
                    if (worldManager != null)
                    {
                        worldManager.SetBlockAt(blockWorldPos, BlockType.Air);
                        Debug.Log($"Breaking block at {blockWorldPos} using WorldManager");
                    }
                    else
                    {
                        // Legacy code path for single-chunk mode
                        Chunk chunk = hit.collider.GetComponent<Chunk>();
                        if (chunk != null)
                        {
                            Vector3Int localPos = chunk.WorldToLocalPosition(blockPos);
                            chunk.RemoveBlock(localPos.x, localPos.y, localPos.z);
                        }
                    }
                }
            }
            
            // Place block
            if (isPlacingBlock && !wasPlacingLastFrame)
            {
                // Calculate position for new block (adjacent to the face that was hit)
                Vector3 placePosition = hit.point + hit.normal * 0.001f;
                Vector3Int placeWorldPos = new Vector3Int(
                    Mathf.FloorToInt(placePosition.x),
                    Mathf.FloorToInt(placePosition.y),
                    Mathf.FloorToInt(placePosition.z)
                );
                
                // Get the selected block type
                BlockType blockTypeToPlace = GetSelectedBlockType();
                
                // Use WorldManager if available, otherwise fall back to direct chunk interaction
                if (worldManager != null)
                {
                    worldManager.SetBlockAt(placeWorldPos, blockTypeToPlace);
                    Debug.Log($"Placing {blockTypeToPlace} block at {placeWorldPos} using WorldManager");
                }
                else
                {
                    // Legacy code path for single-chunk mode
                    Chunk chunk = hit.collider.GetComponent<Chunk>();
                    if (chunk != null)
                    {
                        Vector3Int localPos = chunk.WorldToLocalPosition(placePosition);
                        
                        // Check if the position is in bounds of this chunk
                        if (localPos.x >= 0 && localPos.x < Chunk.chunkSize &&
                            localPos.y >= 0 && localPos.y < Chunk.chunkSize &&
                            localPos.z >= 0 && localPos.z < Chunk.chunkSize)
                        {
                            // Add the block
                            chunk.AddBlock(localPos.x, localPos.y, localPos.z, blockTypeToPlace);
                        }
                    }
                }
            }
        }
        else
        {
            if (blockHighlighter != null)
            {
                // No block in sight, hide the highlight
                blockHighlighter.HideHighlight();
                lastHitPosition = null;
            }
        }
        
        // Always update this at the end of the method
        wasBreakingLastFrame = isBreakingBlock;
        wasPlacingLastFrame = isPlacingBlock;
    }
    
    // Helper method to draw a debug sphere
    private void DebugDrawSphere(Vector3 center, float radius, Color color, float duration)
    {
        // Only works in editor
        #if UNITY_EDITOR
        const int segments = 8;
        float angle = 0f;
        
        // Draw three circles in different planes
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)i / segments * Mathf.PI * 2;
            float angle2 = (float)(i + 1) / segments * Mathf.PI * 2;
            
            // XY plane
            Debug.DrawLine(
                center + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0),
                center + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0),
                color, duration);
            
            // XZ plane
            Debug.DrawLine(
                center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius),
                center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius),
                color, duration);
            
            // YZ plane
            Debug.DrawLine(
                center + new Vector3(0, Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius),
                center + new Vector3(0, Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius),
                color, duration);
        }
        #endif
    }
    
    // Helper method to draw a debug cube
    private void DebugDrawCube(Vector3 center, Vector3 size, Color color, float duration)
    {
        // Only works in editor
        #if UNITY_EDITOR
        Vector3 halfSize = size / 2f;
        
        // Calculate the eight corners of the cube
        Vector3[] corners = new Vector3[8];
        corners[0] = center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
        corners[1] = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
        corners[2] = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
        corners[3] = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
        corners[4] = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
        corners[5] = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
        corners[6] = center + new Vector3(halfSize.x, halfSize.y, halfSize.z);
        corners[7] = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);
        
        // Draw the bottom edges
        Debug.DrawLine(corners[0], corners[1], color, duration);
        Debug.DrawLine(corners[1], corners[2], color, duration);
        Debug.DrawLine(corners[2], corners[3], color, duration);
        Debug.DrawLine(corners[3], corners[0], color, duration);
        
        // Draw the top edges
        Debug.DrawLine(corners[4], corners[5], color, duration);
        Debug.DrawLine(corners[5], corners[6], color, duration);
        Debug.DrawLine(corners[6], corners[7], color, duration);
        Debug.DrawLine(corners[7], corners[4], color, duration);
        
        // Draw the vertical edges
        Debug.DrawLine(corners[0], corners[4], color, duration);
        Debug.DrawLine(corners[1], corners[5], color, duration);
        Debug.DrawLine(corners[2], corners[6], color, duration);
        Debug.DrawLine(corners[3], corners[7], color, duration);
        #endif
    }
    
    private void TryPlaceBlockDirect()
    {
        // Force block placing when right-click is detected directly
        Debug.Log("Trying to place block directly from mouse input");
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionDistance, blockLayer))
        {
            // Calculate position for new block (adjacent to the face that was hit)
            Vector3 placePosition = hit.point + hit.normal * 0.001f;
            Vector3Int placeWorldPos = new Vector3Int(
                Mathf.FloorToInt(placePosition.x),
                Mathf.FloorToInt(placePosition.y),
                Mathf.FloorToInt(placePosition.z)
            );
            
            // Try to determine which chunk this would belong to
            Chunk chunk = hit.collider.GetComponent<Chunk>();
            if (chunk != null)
            {
                Vector3Int localPos = chunk.WorldToLocalPosition(placePosition);
                
                // Check if the position is in bounds of this chunk
                if (localPos.x >= 0 && localPos.x < Chunk.chunkSize &&
                    localPos.y >= 0 && localPos.y < Chunk.chunkSize &&
                    localPos.z >= 0 && localPos.z < Chunk.chunkSize)
                {
                    // Convert selectedBlockType (1-indexed) to a BlockType
                    BlockType blockTypeToPlace = GetSelectedBlockType();
                    
                    // Add the block
                    chunk.AddBlock(localPos.x, localPos.y, localPos.z, blockTypeToPlace);
                    Debug.Log($"Placed block directly: {blockTypeToPlace} at {localPos}");
                }
            }
        }
    }
    
    private BlockType GetSelectedBlockType()
    {
        switch (selectedBlockType)
        {
            case 1: return BlockType.Dirt;
            case 2: return BlockType.Stone;
            case 3: return BlockType.Grass;
            default: return BlockType.Dirt;
        }
    }
    
    public void SelectBlockType(int blockTypeIndex)
    {
        // Ensure the type is in range
        if (blockTypeIndex >= 1 && blockTypeIndex < blockTypeNames.Length)
        {
            selectedBlockType = blockTypeIndex;
            Debug.Log($"Selected Block Type: {selectedBlockType} - {blockTypeNames[selectedBlockType]}");
        }
    }
    
    public void BreakBlock(Vector3 position)
    {
        if (worldManager != null)
        {
            // Use world manager to break blocks (works across chunks)
            worldManager.SetBlockAt(position, BlockType.Air);
        }
        else
        {
            // Legacy code path for single-chunk mode
            RaycastHit hit;
            if (Physics.Raycast(position, Vector3.down, out hit, 0.1f, blockLayer))
            {
                Chunk chunk = hit.collider.GetComponentInParent<Chunk>();
                if (chunk != null)
                {
                    Vector3Int localPos = chunk.WorldToLocalPosition(position);
                    chunk.RemoveBlock(localPos.x, localPos.y, localPos.z);
                }
            }
        }
    }
    
    public void PlaceBlock(Vector3 position, BlockType blockType)
    {
        if (worldManager != null)
        {
            // Use world manager to place blocks (works across chunks)
            worldManager.SetBlockAt(position, blockType);
        }
        else
        {
            // Legacy code path for single-chunk mode
            RaycastHit hit;
            if (Physics.Raycast(position, Vector3.down, out hit, 0.1f, blockLayer))
            {
                Chunk chunk = hit.collider.GetComponentInParent<Chunk>();
                if (chunk != null)
                {
                    Vector3Int localPos = chunk.WorldToLocalPosition(position);
                    chunk.AddBlock(localPos.x, localPos.y, localPos.z, blockType);
                }
            }
        }
    }
} 