using UnityEngine;

public class SimpleTextureTest : MonoBehaviour
{
    public Material baseMaterial; // Assign your BlockMaterial here
    
    void Start()
    {
        // Wait for textures to initialize
        if (!BlockTextureManager.isInitialized)
        {
            BlockTextureManager.Initialize();
        }
        
        if (BlockTextureManager.textureArray == null)
        {
            Debug.LogError("TextureArray is null!");
            return;
        }
        
        // Create a test cube
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = new Vector3(0, 2, 0);
        cube.transform.SetParent(transform);
        
        // Create a simple unlit material directly using Shader.Find
        Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.name = "TestBlockMaterial";
        
        // Assign the texture to the _BaseMap property
        material.mainTexture = BlockTextureManager.textureArray;
        
        // Apply the material
        cube.GetComponent<MeshRenderer>().material = material;
        
        // Log success
        Debug.Log($"Created test cube with texture array (depth: {BlockTextureManager.textureArray.depth})");
    }
} 