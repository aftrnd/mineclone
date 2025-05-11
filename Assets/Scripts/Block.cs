using UnityEngine;

public enum BlockType {
    Air,
    Dirt,
    Grass,
    Stone
}

public class Block {
    public BlockType type;

    public Block(BlockType type) {
        this.type = type;
    }

    public bool IsSolid() {
        return type != BlockType.Air;
    }

    public int GetTextureID(int face) {
        switch (type) {
            case BlockType.Dirt:
                return BlockTextureManager.GetTextureIndex("dirt");

            case BlockType.Stone:
                return BlockTextureManager.GetTextureIndex("stone");

            case BlockType.Grass:
                if (face == 4) return BlockTextureManager.GetTextureIndex("grass_top");     // Top
                if (face == 5) return BlockTextureManager.GetTextureIndex("dirt");          // Bottom
                return BlockTextureManager.GetTextureIndex("grass_side");                   // Sides

            default:
                return 0;
        }
    }
}