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
                // Face indexes in MeshBuilder:
                // 0 = Front (Z+)
                // 1 = Back (Z-)
                // 2 = Top (Y+)
                // 3 = Bottom (Y-)
                // 4 = Left (X-)
                // 5 = Right (X+)
                if (face == 2) return BlockTextureManager.GetTextureIndex("grass_top");     // Top
                if (face == 3) return BlockTextureManager.GetTextureIndex("dirt");          // Bottom
                return BlockTextureManager.GetTextureIndex("grass_side");                   // Sides

            default:
                return 0;
        }
    }
}