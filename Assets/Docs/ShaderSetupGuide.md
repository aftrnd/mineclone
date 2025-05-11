# BlockShader Setup Guide

## Issue: Textures not displaying in BlockShader

If your BlockShader is showing only gray instead of the block textures, follow these steps to fix the issue:

## 1. Check Shader Graph Setup

1. Open the BlockShader.shadergraph in the Unity Editor
2. Check if the "Sample Texture 2D Array" node is properly connected:
   - The "Texture Array" input should be connected to a property node
   - The "Index" input should be connected to the UV1.x channel (which holds our texture indices)
   - The "UV" input should be connected to the UV0 channel (the regular UVs)
   - The RGBA output should be connected to the Base Color input of the Fragment node

## 2. Fix Property References

1. Check the property reference for the Texture Array:
   - Find your Texture2DArray property node in the shader graph
   - Make sure its Reference name is exactly "_BlockTextureArray" (matching the name used in the scripts)
   - The property name should be "TextureArray" or similar for clarity

## 3. Ensure Correct UV Channels

1. Check that your shader is reading from the correct UV channels:
   - Use a UV node set to channel 0 for the regular UVs (connected to the UV input of the Sample Texture 2D Array node)
   - Use a UV node set to channel 1 for the texture indices (connected to the Index input of the Sample Texture 2D Array node)
   - Make sure the UV1.x component is isolated using a Split node and connected to the Index input

## 4. Debug With Preview Windows

1. Add preview windows to each stage of your shader graph:
   - Preview the UV0 output
   - Preview the UV1 output after the Split node
   - Preview the Sample Texture 2D Array node output

## 5. Blackboard Property Setup

1. In the Shader Graph Blackboard (properties panel):
   - Make sure the Texture2DArray property is exposed
   - Set its Default value to None
   - Make sure "Override Property Declaration" is unchecked

## 6. Example Shader Graph Structure

Your shader graph structure should look something like this:

```
[UV Node (UV0)] ------> [Sample Texture 2D Array] ------> [Fragment Node (Base Color)]
                            ^        ^
                            |        |
[UV Node (UV1)] --> [Split Node] --> |
                                     |
[Texture2DArray Property] -----------+
```

## 7. Check Material Inspector

After fixing the shader, check your material in the Inspector:
- Make sure the TextureArray property is visible
- You should see the "_BlockTextureArray" property and it should be populated
- If you don't see this property, there's a mismatch between your shader property name and the name used in the scripts

## 8. Test With Debug Tools

1. Add the CreateTestBlocks script to an empty GameObject 
2. Assign your BlockMaterial
3. Play the scene to see if the test blocks display with different textures

This should help identify and fix the issue with your shader not displaying the block textures. 