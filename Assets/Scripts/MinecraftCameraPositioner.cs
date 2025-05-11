using UnityEngine;

[ExecuteInEditMode]
public class MinecraftCameraPositioner : MonoBehaviour
{
    [Header("Minecraft Reference Values")]
    [Tooltip("Total player height in blocks (Minecraft = 1.8)")]
    public float playerHeight = 1.8f;
    
    [Tooltip("Eye height from ground in blocks (Minecraft = 1.62)")]
    public float eyeHeight = 1.62f;
    
    [Header("Camera Positioning")]
    [Tooltip("Force position camera in Update")]
    public bool forcePositionInUpdate = true;
    
    [Tooltip("Set this to the local Y position that works for your setup")]
    public float manualYPosition = 0.72f;
    
    [Tooltip("Try different positioning methods")]
    public PositionMethod positionMethod = PositionMethod.ManualValue;
    
    public enum PositionMethod
    {
        ManualValue,
        StandardFormula,
        DirectEyeHeight,
        WorldPositionOverride
    }
    
    private void Start()
    {
        // Set a different position based on each method
        ApplySelectedPositionMethod();
        
        // Log current position
        Debug.Log($"Camera positioner Start - Current position: {transform.localPosition}");
    }
    
    private void Update()
    {
        if (forcePositionInUpdate || Application.isEditor)
        {
            ApplySelectedPositionMethod();
        }
    }
    
    // Apply the selected position method
    private void ApplySelectedPositionMethod()
    {
        switch (positionMethod)
        {
            case PositionMethod.ManualValue:
                transform.localPosition = new Vector3(0, manualYPosition, 0);
                break;
                
            case PositionMethod.StandardFormula:
                float standardOffset = eyeHeight - (playerHeight / 2);
                transform.localPosition = new Vector3(0, standardOffset, 0);
                break;
                
            case PositionMethod.DirectEyeHeight:
                transform.localPosition = new Vector3(0, eyeHeight, 0);
                break;
                
            case PositionMethod.WorldPositionOverride:
                // Get the world position of the player's feet
                Vector3 worldPos = transform.parent.position;
                // Set the camera directly at eye height from ground
                worldPos.y += eyeHeight;
                transform.position = worldPos;
                break;
        }
    }
    
    // Add a button to apply in editor
    [ContextMenu("Apply Selected Position Method")]
    public void ApplyPositionNow()
    {
        ApplySelectedPositionMethod();
        Debug.Log($"Applied position method: {positionMethod}, new position: {transform.localPosition}");
    }
} 