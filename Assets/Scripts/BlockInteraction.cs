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
    
    private bool isBreakingBlock = false;
    private bool isPlacingBlock = false;
    private Vector3? lastHitPosition = null;
    
    private void Start()
    {
        Debug.Log("BlockInteraction Start method called");
        
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
    }
    
    public void OnAttack(InputAction.CallbackContext context)
    {
        isBreakingBlock = context.ReadValueAsButton();
    }
    
    public void OnInteract(InputAction.CallbackContext context)
    {
        isPlacingBlock = context.ReadValueAsButton();
    }
    
    public void OnNextBlock(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            selectedBlockType++;
            // You would need to implement max block types based on your game
            Debug.Log($"Selected Block Type: {selectedBlockType}");
        }
    }
    
    public void OnPreviousBlock(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            selectedBlockType = Mathf.Max(1, selectedBlockType - 1);
            Debug.Log($"Selected Block Type: {selectedBlockType}");
        }
    }
    
    private void Update()
    {
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
            HandleBlockInteraction();
        }
        else
        {
            Debug.LogError("Failed to create or find BlockHighlighter. Block interaction is disabled.");
        }
    }
    
    private void HandleBlockInteraction()
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
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionDistance, blockLayer))
        {
            // Get the targeted block position
            Vector3 blockPosition = new Vector3(
                Mathf.Floor(hit.point.x - (hit.normal.x * 0.001f)) + 0.5f,
                Mathf.Floor(hit.point.y - (hit.normal.y * 0.001f)) + 0.5f,
                Mathf.Floor(hit.point.z - (hit.normal.z * 0.001f)) + 0.5f
            );
            
            // Update the block highlight - WITH NULL CHECK
            if (blockHighlighter != null)
            {
                if (lastHitPosition == null || blockPosition != lastHitPosition.Value)
                {
                    blockHighlighter.ShowHighlight(blockPosition);
                    lastHitPosition = blockPosition;
                }
            }
            
            // Break block
            if (isBreakingBlock && hit.collider.CompareTag("Block"))
            {
                // Find which chunk this block belongs to
                Chunk chunk = hit.transform.GetComponentInParent<Chunk>();
                if (chunk != null)
                {
                    // Convert world position to chunk-local coordinates
                    Vector3Int localPos = new Vector3Int(
                        Mathf.FloorToInt(hit.point.x - (hit.normal.x * 0.001f)) - Mathf.FloorToInt(chunk.transform.position.x),
                        Mathf.FloorToInt(hit.point.y - (hit.normal.y * 0.001f)) - Mathf.FloorToInt(chunk.transform.position.y),
                        Mathf.FloorToInt(hit.point.z - (hit.normal.z * 0.001f)) - Mathf.FloorToInt(chunk.transform.position.z)
                    );
                    
                    // Call the chunk's method to remove the block
                    // This would depend on your specific implementation
                    // chunk.RemoveBlock(localPos.x, localPos.y, localPos.z);
                    Debug.Log($"Breaking block at {localPos} in chunk at {chunk.transform.position}");
                }
            }
            
            // Place block
            if (isPlacingBlock)
            {
                // Calculate position for new block (adjacent to the face that was hit)
                Vector3 placePosition = hit.point + hit.normal * 0.001f;
                Vector3Int blockPos = new Vector3Int(
                    Mathf.FloorToInt(placePosition.x),
                    Mathf.FloorToInt(placePosition.y),
                    Mathf.FloorToInt(placePosition.z)
                );
                
                // Find the chunk for this position
                // This would depend on your world implementation
                // Chunk chunk = World.GetChunkFromWorldPos(blockPos);
                // if (chunk != null)
                // {
                //     // Add the block
                //     chunk.AddBlock(blockPos.x, blockPos.y, blockPos.z, selectedBlockType);
                // }
                Debug.Log($"Placing block of type {selectedBlockType} at {blockPos}");
            }
        }
        else if (blockHighlighter != null)
        {
            // No block in sight, hide the highlight
            blockHighlighter.HideHighlight();
            lastHitPosition = null;
        }
    }
} 