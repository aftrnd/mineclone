using UnityEngine;

public class MinecraftHotbar : MonoBehaviour
{
    [Header("Hotbar Settings")]
    [SerializeField] private int slotCount = 9;
    [SerializeField] private Texture2D hotbarTexture;
    [SerializeField] private Texture2D selectorTexture;
    [SerializeField] private Texture2D[] blockTextures;
    
    [Header("References")]
    [SerializeField] private BlockInteraction blockInteraction;

    private Rect hotbarRect;
    private Rect[] slotRects;
    private Rect selectorRect;
    private int selectedSlot = 0;
    private float slotSize = 40f;
    private float padding = 4f;
    
    private void Start()
    {
        // Create default textures if none assigned
        if (hotbarTexture == null)
        {
            hotbarTexture = CreateDefaultHotbarTexture();
        }
        if (selectorTexture == null)
        {
            selectorTexture = CreateDefaultSelectorTexture();
        }
        
        // Initialize hotbar rectangles
        InitializeRects();
        
        // Find block interaction if not assigned
        if (blockInteraction == null)
        {
            blockInteraction = GetComponent<BlockInteraction>();
            if (blockInteraction == null)
            {
                blockInteraction = FindObjectOfType<BlockInteraction>();
            }
        }
    }
    
    private void Update()
    {
        // Handle number key input (1-9)
        for (int i = 0; i < Mathf.Min(slotCount, 9); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                selectedSlot = i;
                
                // Notify block interaction about selected type
                if (blockInteraction != null)
                {
                    // Update block interaction to match selected slot
                    // Here we assume slot index + 1 corresponds to block type, skip 0 (Air)
                    int blockType = i + 1;
                    blockInteraction.SelectBlockType(blockType);
                }
            }
        }
        
        // Handle scroll wheel input
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (scrollDelta != 0)
        {
            if (scrollDelta > 0)
            {
                selectedSlot = (selectedSlot + 1) % slotCount;
            }
            else
            {
                selectedSlot = (selectedSlot - 1 + slotCount) % slotCount;
            }
            
            // Notify block interaction about selected type
            if (blockInteraction != null)
            {
                // Update block interaction to match selected slot
                int blockType = selectedSlot + 1;
                blockInteraction.SelectBlockType(blockType);
            }
        }
    }
    
    private void OnGUI()
    {
        DrawHotbar();
    }
    
    private void InitializeRects()
    {
        // Calculate total width of hotbar
        float totalWidth = slotCount * slotSize + (slotCount - 1) * padding;
        
        // Create hotbar rectangle centered at bottom of screen
        hotbarRect = new Rect(
            (Screen.width - totalWidth) / 2,
            Screen.height - slotSize - 10,
            totalWidth,
            slotSize
        );
        
        // Create individual slot rectangles
        slotRects = new Rect[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            slotRects[i] = new Rect(
                hotbarRect.x + i * (slotSize + padding),
                hotbarRect.y,
                slotSize,
                slotSize
            );
        }
        
        // Create selector rectangle (same size as a slot)
        selectorRect = new Rect(
            slotRects[selectedSlot].x - 2,
            slotRects[selectedSlot].y - 2,
            slotSize + 4,
            slotSize + 4
        );
    }
    
    private void DrawHotbar()
    {
        // Draw hotbar background
        GUI.DrawTexture(hotbarRect, hotbarTexture);
        
        // Draw slot items (blocks)
        for (int i = 0; i < slotCount; i++)
        {
            // Only draw block textures if we have them
            if (blockTextures != null && i < blockTextures.Length && blockTextures[i] != null)
            {
                GUI.DrawTexture(slotRects[i], blockTextures[i]);
            }
            
            // Draw slot number
            GUI.Label(slotRects[i], (i + 1).ToString());
        }
        
        // Update selector position
        selectorRect.x = slotRects[selectedSlot].x - 2;
        
        // Draw selector
        GUI.DrawTexture(selectorRect, selectorTexture);
    }
    
    private Texture2D CreateDefaultHotbarTexture()
    {
        // Create a simple dark gray texture for hotbar background
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f, 0.8f));
        texture.Apply();
        return texture;
    }
    
    private Texture2D CreateDefaultSelectorTexture()
    {
        // Create a simple white border texture for selector
        Texture2D texture = new Texture2D(3, 3);
        Color white = Color.white;
        Color transparent = new Color(1, 1, 1, 0);
        
        // Set border pixels to white, center to transparent
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (x == 0 || x == 2 || y == 0 || y == 2)
                {
                    texture.SetPixel(x, y, white);
                }
                else
                {
                    texture.SetPixel(x, y, transparent);
                }
            }
        }
        
        texture.Apply();
        return texture;
    }
} 