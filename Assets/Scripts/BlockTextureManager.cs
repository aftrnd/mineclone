using UnityEngine;
using System.Collections.Generic;

public static class BlockTextureManager {
    public static Texture2DArray textureArray;
    public static bool isInitialized = false;

    private static Dictionary<string, int> textureIndices = new Dictionary<string, int>();

    public static void Initialize() {
        if (isInitialized) {
            Debug.Log("BlockTextureManager already initialized");
            return;
        }
        
        string[] textureNames = {
            "dirt",          // 0
            "grass_top",     // 1
            "grass_side",    // 2
            "stone"          // 3
        };

        // First, check the format of our textures to determine what format to use
        string path = "Assets/Blocks/" + textureNames[0] + ".png";
        Texture2D sampleTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        
        if (sampleTex == null) {
            Debug.LogError("Failed to load sample texture to determine format: " + path);
            return;
        }
        
        // Create a texture array that matches the format of our source textures
        TextureFormat textureFormat = sampleTex.format;
        Debug.Log($"Creating Texture2DArray with format: {textureFormat} to match source textures");
        
        textureArray = new Texture2DArray(16, 16, textureNames.Length, textureFormat, false);
        textureArray.filterMode = FilterMode.Point;

        for (int i = 0; i < textureNames.Length; i++) {
            // Load textures directly from Assets/Blocks instead of Resources
            path = "Assets/Blocks/" + textureNames[i] + ".png";
            Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            
            if (tex == null) {
                Debug.LogError("Missing texture: " + path);
                continue;
            }
            
            // Ensure the loaded texture format matches what we need
            if (tex.format != textureFormat) {
                Debug.LogWarning($"Texture {textureNames[i]} has format {tex.format} but we need {textureFormat}");
                
                // Create a temporary texture with the correct format and copy the data
                Texture2D tempTex = new Texture2D(tex.width, tex.height, textureFormat, false);
                tempTex.SetPixels(tex.GetPixels());
                tempTex.Apply();
                
                // Copy from the temp texture to the array
                Graphics.CopyTexture(tempTex, 0, 0, textureArray, i, 0);
            } else {
                // Copy directly if formats match
                Graphics.CopyTexture(tex, 0, 0, textureArray, i, 0);
            }
            
            textureIndices[textureNames[i]] = i;
        }

        textureArray.Apply();
        
        // Add debug output to verify texture array has content
        Debug.Log($"Texture array created with {textureArray.depth} textures, dimensions: {textureArray.width}x{textureArray.height}");
        
        // Make the texture array available to the shader
        Shader.SetGlobalTexture("_BlockTextureArray", textureArray);
        Shader.SetGlobalTexture("_TextureArray", textureArray);
        
        isInitialized = true;
    }

    public static int GetTextureIndex(string name) {
        return textureIndices.TryGetValue(name, out int index) ? index : 0;
    }
}