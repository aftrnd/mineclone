using UnityEngine;

/// <summary>
/// Helper script to attach to a chunk prefab. Ensures all required components are added.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(Chunk))]
public class ChunkPrefab : MonoBehaviour
{
    [Header("Material Settings")]
    [SerializeField] private Material blockMaterial;
    
    private void Reset()
    {
        // This is called when the script is added to a GameObject in the editor
        // or when Reset is selected from the context menu
        
        // If no material is set, try to find a default one
        if (blockMaterial == null)
        {
            blockMaterial = Resources.Load<Material>("BlockMaterial");
        }
        
        // Make sure we have all required components
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        Chunk chunk = GetComponent<Chunk>();
        
        // Setup the renderer
        if (meshRenderer != null && blockMaterial != null)
        {
            meshRenderer.material = blockMaterial;
        }
        
        // Give the GameObject a proper tag
        gameObject.tag = "Block";
        
        // Set the layer
        if (System.Type.GetType("MinecraftLayers") != null)
        {
            gameObject.layer = MinecraftLayers.BlockLayer;
        }
    }
} 