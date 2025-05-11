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

        // Debug the camera position at start
        Debug.Log($"Camera Awake - Local position: {transform.localPosition}, World position: {transform.position}");
        Debug.Log($"Parent position: {(playerBody != null ? playerBody.position : Vector3.zero)}");
        
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
        // Debug camera position again
        Debug.Log($"Camera OnEnable - Local position: {transform.localPosition}, World position: {transform.position}");
        
        // Make sure cursor is locked when script is enabled
        LockCursor();
    }
    
    private void Start()
    {
        Debug.Log($"Camera Start - Local position: {transform.localPosition}, World position: {transform.position}");
    }
    
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
        Debug.Log($"OnLook called: Input = ({lookInput.x}, {lookInput.y})");
    }
    
    // Prevent any accidental resets of camera position
    private Vector3 initialLocalPosition;
    private void LateUpdate()
    {
        // Store the initial local position on first frame
        if (initialLocalPosition == Vector3.zero && transform.localPosition != Vector3.zero)
        {
            initialLocalPosition = transform.localPosition;
            Debug.Log($"Stored initial camera position: {initialLocalPosition}");
        }
        
        // Handle rotation
        HandleRotation();
        
        // If camera position has changed from what's expected, log a warning
        if (initialLocalPosition != Vector3.zero && 
            Vector3.Distance(transform.localPosition, initialLocalPosition) > 0.01f)
        {
            Debug.LogWarning($"Camera position changed from {initialLocalPosition} to {transform.localPosition}");
            // Option: Force position back to original setting
            // transform.localPosition = initialLocalPosition;
        }
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