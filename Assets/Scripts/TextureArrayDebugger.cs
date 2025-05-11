using UnityEngine;

public class TextureArrayDebugger : MonoBehaviour
{
    public Material blockMaterial;
    public GameObject debugCube;

    private void Start()
    {
        // Log information about the texture array
        var textureArray = BlockTextureManager.textureArray;
        if (textureArray != null)
        {
            Debug.Log($"Texture array initialized: {textureArray.depth} textures, {textureArray.width}x{textureArray.height}");
            
            // Create test cubes with different texture indices if debugCube is assigned
            if (debugCube != null)
            {
                for (int i = 0; i < textureArray.depth; i++)
                {
                    GameObject cube = Instantiate(debugCube, new Vector3(i * 1.5f, 0, 0), Quaternion.identity);
                    // Set the texture index on the material for this instance
                    MaterialPropertyBlock props = new MaterialPropertyBlock();
                    props.SetFloat("_TextureIndex", i);
                    cube.GetComponent<Renderer>().SetPropertyBlock(props);
                }
            }
        }
        else
        {
            Debug.LogError("BlockTextureManager.textureArray is null! Make sure it's initialized before use.");
        }

        // Check if the material has the texture array assigned
        if (blockMaterial != null)
        {
            var materialArray = blockMaterial.GetTexture("_BlockTextureArray");
            if (materialArray != null)
            {
                Debug.Log("Block material has texture array assigned");
            }
            else
            {
                Debug.LogError("Block material does not have _BlockTextureArray assigned!");
            }

            // Try to assign the texture array to the material
            if (textureArray != null)
            {
                blockMaterial.SetTexture("_BlockTextureArray", textureArray);
                blockMaterial.SetTexture("_TextureArray", textureArray);
                Debug.Log("Manually assigned texture array to material");
            }
        }
        else
        {
            Debug.LogError("Block material not assigned to TextureArrayDebugger");
        }
    }
} 