using UnityEngine;
using UnityEngine.InputSystem;

public class MinecraftCameraController : MonoBehaviour
{
    [Header("Look Parameters")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float gamepadSensitivity = 100f;
    [SerializeField] private bool invertY = false;
    
    [Header("Camera Settings")]
    [SerializeField] private float fieldOfView = 70f; // Default Minecraft FOV
    [SerializeField] private float cameraSmoothing = 10f;
    
    // Min/max vertical look angle (in degrees)
    [SerializeField] private float minVerticalLookAngle = -90f;
    [SerializeField] private float maxVerticalLookAngle = 90f;
    
    private float rotationX = 0f;
    private float rotationY = 0f;
    private Transform playerBody;
    private Vector2 lookInput;
    private Camera mainCamera;
    
    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
        playerBody = transform.parent;
        
        if (playerBody == null)
        {
            Debug.LogError("Camera must be a child of the player body for rotation to work correctly!");
        }
        
        // Set camera FOV to match Minecraft
        if (mainCamera != null)
        {
            mainCamera.fieldOfView = fieldOfView;
            Debug.Log("Set camera FOV to: " + fieldOfView);
        }
        else
        {
            Debug.LogError("No Camera component found on this GameObject!");
        }
        
        // Lock and hide cursor
        LockCursor();
    }
    
    private void LockCursor()
    {
        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("Cursor locked and hidden");
    }
    
    private void OnEnable()
    {
        // Make sure cursor is locked when script is enabled
        LockCursor();
    }
    
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
        Debug.Log($"OnLook called: Input = ({lookInput.x}, {lookInput.y})");
    }
    
    private void LateUpdate()
    {
        HandleRotation();
    }
    
    private void HandleRotation()
    {
        // Apply sensitivity
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity * (invertY ? 1 : -1);
        
        // Calculate new rotation values
        rotationY += mouseX;
        rotationX = Mathf.Clamp(rotationX + mouseY, minVerticalLookAngle, maxVerticalLookAngle);
        
        // Apply the rotation to the camera (vertical) and player (horizontal)
        transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        playerBody.rotation = Quaternion.Euler(0f, rotationY, 0f);
    }
    
    public void SetFOV(float newFOV)
    {
        fieldOfView = newFOV;
        mainCamera.fieldOfView = fieldOfView;
    }
    
    public void ResetLook()
    {
        rotationX = 0f;
        rotationY = transform.eulerAngles.y;
    }
} 