# Infinite World System Setup

This document guides you through setting up the infinite world generation system for your Minecraft clone.

## 1. Setting Up the World Manager

1. Create a new empty GameObject in your scene and name it "World"
2. Add the `WorldManager` component to it
3. Configure the settings in the Inspector:
   - **Player**: Drag your player GameObject here
   - **View Distance**: How many chunks to load in each direction (recommended: 5-8)
   - **Block Material**: Assign your block material
   - **Chunk Prefab**: Assign your chunk prefab (see below)
   - **Seed**: Set a random seed or leave at 0 for random generation
   - **Generate Caves**: Toggle to enable/disable cave generation

## 2. Creating a Chunk Prefab

1. Create a new empty GameObject and name it "ChunkPrefab"
2. Add the following components:
   - MeshFilter
   - MeshRenderer
   - MeshCollider
   - Chunk
   - ChunkPrefab
3. In the Chunk component, assign your block material
4. In the ChunkPrefab component, assign your block material
5. Set the Tag to "Block"
6. Set the Layer to your block layer (if you have one set up)
7. Drag the GameObject to your Project window to create a prefab
8. Assign this prefab to the World Manager's "Chunk Prefab" field

## 3. Player Setup

1. Make sure your player is positioned high enough above the terrain
   - Default terrain is generated around y=8, so position player at y=10+
2. Ensure the player has a camera setup correctly
3. Ensure block interaction scripts reference the WorldManager

## 4. Playing the Game

Once everything is set up:

1. Press Play in the Unity Editor
2. Your player should be standing on a generated world
3. As you move, new chunks will load around you
4. Chunks far from the player will automatically unload

## 5. Customizing Terrain Generation

You can customize the terrain by modifying the TerrainGenerator.cs script:

- **terrainScale**: Controls how stretched the terrain is horizontally
- **heightScale**: Controls the overall height of the terrain
- **baseHeight**: The minimum height of the terrain
- **mountainScale/mountainHeight**: Control mountain generation
- **valleyScale/valleyDepth**: Control valley generation
- **caveScale/caveThreshold**: Control cave generation

## 6. Performance Considerations

- Higher view distances will generate more chunks, which may impact performance
- Consider implementing chunk threading or object pooling for better performance
- On slower machines, reduce view distance to improve frame rate

## 7. Extending the System

You can extend the system with:

- Different biomes
- Structures like trees, villages, etc.
- Underground features like caves, dungeons, etc.
- Water bodies like rivers, lakes, and oceans
- Different block types (ores, wood, etc.)

## Troubleshooting

- **No chunks appear**: Ensure your player reference is set correctly
- **Chunks have no textures**: Check your block material settings
- **Holes between chunks**: Ensure cross-chunk face culling is working properly
- **Performance issues**: Reduce view distance or chunk size

For detailed technical information, refer to the code documentation in each script. 