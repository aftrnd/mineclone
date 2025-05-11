using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    public Material blockMaterial;

    private void Awake()
    {
        // Initialize the BlockTextureManager
        BlockTextureManager.Initialize();

        // Assign the texture array to the material
        if (blockMaterial != null)
        {
            // Try both property names to ensure one works
            blockMaterial.SetTexture("_BlockTextureArray", BlockTextureManager.textureArray);
            blockMaterial.SetTexture("_TextureArray", BlockTextureManager.textureArray);
            
            Debug.Log($"Assigned texture array to material: {blockMaterial.name}");
            
            // Check if the shader is expecting a different property name
            var shader = blockMaterial.shader;
            if (shader != null)
            {
                Debug.Log($"Material is using shader: {shader.name}");
                
                // Try to get the textures to see if they're assigned
                Texture blockTexArray = blockMaterial.GetTexture("_BlockTextureArray");
                Texture texArray = blockMaterial.GetTexture("_TextureArray");
                
                if (blockTexArray != null)
                {
                    Debug.Log("_BlockTextureArray is assigned in the material");
                }
                else
                {
                    Debug.LogWarning("_BlockTextureArray is NOT assigned in the material!");
                }
                
                if (texArray != null)
                {
                    Debug.Log("_TextureArray is assigned in the material");
                }
                else
                {
                    Debug.LogWarning("_TextureArray is NOT assigned in the material!");
                }
            }
        }
        else
        {
            Debug.LogError("Block material not assigned to GameInitializer!");
        }
    }
} 