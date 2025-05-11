# Minecraft-Style World Generation

This document explains the enhanced world generation system with biomes and proper Minecraft-style vertical scaling.

## World Height System

The world follows Minecraft Beta's vertical scale:

- **Y = 0**: Bedrock layer (bottom of the world)
- **Y = 64**: Sea level (baseline terrain)
- **Y = 128**: Maximum height (mountain peaks)

## Biome System

The world now generates multiple biomes based on temperature and humidity values:

1. **Plains**: Flat grasslands with gentle hills
2. **Forest**: More varied, slightly hillier terrain with grass
3. **Desert**: Sandy terrain with dunes
4. **Mountains**: Steep, high-elevation terrain with stone and snow
5. **Ocean**: Deep water bodies below sea level

Each biome has its own:
- Terrain height and variation
- Surface block types
- Generation parameters

## Block Types

The system now supports multiple block types:

- **Air**: Empty space
- **Grass**: Grassy top layer for plains/forest
- **Dirt**: Soil beneath grass
- **Stone**: Common underground material
- **Sand**: For beaches and deserts
- **Gravel**: Occasional areas near water
- **SnowGrass**: Snow-covered grass for mountains
- **Clay**: Found under ocean floors
- **Bedrock**: Indestructible bottom layer

## Setting Up Chunks

The chunk system now loads chunks vertically as well as horizontally:
- Each chunk is 16×16×16 blocks
- Chunks are instantiated as needed based on the player's position
- Vertical chunks automatically stack to accommodate the full 0-128 height range

## Debug Tools

1. Add a **WorldDebugger** component to any GameObject to see:
   - Current position and elevation (relative to sea level)
   - Current biome
   - Terrain height
   - Current block

2. Press **F3** to toggle the debug display
3. Press **F4** to regenerate all visible chunks

## Customizing Terrain

You can modify the terrain generation by adjusting values in the TerrainGenerator:

### Global Parameters
- `MIN_HEIGHT`: Bottom of the world (default: 0)
- `SEA_LEVEL`: Water level (default: 64)
- `MAX_HEIGHT`: Maximum height (default: 128)

### Biome Control
- `biomeScale`: Controls how large biomes are (smaller value = larger biomes)
- `temperatureScale`/`humidityScale`: Control climate variation

### Per-Biome Parameters
Each biome has its own settings in the `InitializeBiomeSettings()` method:
- `terrainScale`: Controls how stretched the terrain is
- `heightScale`: Controls how tall hills/mountains are
- `baseHeight`: The average elevation for the biome
- `additionalTerrainNoise`: How much additional detail to add

### Cave Generation
- `caveScale`: Controls cave size (smaller = larger caves)
- `caveThreshold`: Controls cave frequency (higher = fewer caves)

## Future Enhancements

Planned features for future implementation:
- Actual water blocks at sea level
- Trees and vegetation
- Ore deposits underground
- Structure generation (villages, dungeons)
- Block lighting and day/night cycle 