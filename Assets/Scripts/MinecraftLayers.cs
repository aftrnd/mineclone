using UnityEngine;

/// <summary>
/// Defines layers and tags for the Minecraft clone.
/// Add this to your GameInitializer or another startup object.
/// </summary>
public class MinecraftLayers : MonoBehaviour
{
    public static string BlockTag = "Block";
    public static string PlayerTag = "Player";
    
    // You'll need to set these in the Unity Editor under Edit -> Project Settings -> Tags and Layers
    public static int BlockLayer = 6; // Layer 6
    public static int PlayerLayer = 7; // Layer 7
    
    void Awake()
    {
        // Make sure your layers are set up in the TagManager
        Debug.Log("MinecraftLayers initialized");
        
        // Tag the player if it's not already tagged
        if (!gameObject.CompareTag(PlayerTag) && GetComponent<MinecraftPlayerController>() != null)
        {
            gameObject.tag = PlayerTag;
            Debug.Log("Tagged player with Player tag");
        }
        
        // Set the player layer
        if (GetComponent<MinecraftPlayerController>() != null)
        {
            gameObject.layer = PlayerLayer;
            Debug.Log("Set player layer to " + PlayerLayer);
        }
    }
    
    /// <summary>
    /// Set up a GameObject as a block with the correct tag and layer
    /// </summary>
    public static void SetupBlockObject(GameObject blockObject)
    {
        blockObject.tag = BlockTag;
        blockObject.layer = BlockLayer;
    }
    
    /// <summary>
    /// Set up a GameObject as a player with the correct tag and layer
    /// </summary>
    public static void SetupPlayerObject(GameObject playerObject)
    {
        playerObject.tag = PlayerTag;
        playerObject.layer = PlayerLayer;
    }
} 