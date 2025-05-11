using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class MinecraftPlayerController : MonoBehaviour
{
    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 4.317f; // Minecraft walking speed in blocks/second
    [SerializeField] private float sprintSpeed = 5.612f; // Minecraft sprint speed in blocks/second
    [SerializeField] private float jumpForce = 9.0f; // Initial jump velocity
    [SerializeField] private float gravity = 30.0f; // Higher than normal gravity for Minecraft feel

    [Header("Player Parameters")]
    [SerializeField] private float playerHeight = 1.8f; // Player is 1.8 blocks tall in Minecraft
    [SerializeField] private float playerWidth = 0.6f; // Player is 0.6 blocks wide in Minecraft
    [SerializeField] private float eyeHeight = 1.62f; // Eye height from ground in Minecraft

    [Header("Gameplay")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;

    // References
    private CharacterController characterController;
    private Transform cameraTransform;
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 velocity = Vector3.zero;
    private bool isGrounded;
    private bool isSprinting;

    // Input actions
    private Vector2 movementInput;
    private bool jumpInput;
    private bool sprintInput;

    private void Awake()
    {
        Debug.Log("MinecraftPlayerController Awake called");
        characterController = GetComponent<CharacterController>();
        
        // Find camera - don't rely on Camera.main which might be null
        // Try to find a camera in our children first
        cameraTransform = null;
        Camera childCamera = GetComponentInChildren<Camera>();
        if (childCamera != null)
        {
            cameraTransform = childCamera.transform;
            Debug.Log("Found camera in children: " + childCamera.name);
        }
        else
        {
            // Fall back to Camera.main as a last resort
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
                Debug.Log("Using Camera.main: " + mainCamera.name);
            }
            else
            {
                Debug.LogError("No camera found! Movement will not work correctly.");
                // Create a temporary transform to avoid null reference
                GameObject tempCam = new GameObject("TempCameraReference");
                tempCam.transform.SetParent(transform);
                cameraTransform = tempCam.transform;
            }
        }
        
        // Set controller height and radius to match Minecraft player dimensions
        if (characterController != null)
        {
            characterController.height = playerHeight;
            characterController.radius = playerWidth / 2.0f;
            
            // Position camera at eye height if it's our child
            if (cameraTransform != null && cameraTransform.parent == transform)
            {
                cameraTransform.localPosition = new Vector3(0, eyeHeight - (playerHeight / 2), 0);
                Debug.Log("Set camera position to: " + cameraTransform.localPosition);
            }
        }
        else
        {
            Debug.LogError("No CharacterController component found! Please add one.");
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        jumpInput = context.ReadValueAsButton();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        sprintInput = context.ReadValueAsButton();
    }

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        isGrounded = characterController.isGrounded;
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -1f; // Small negative value instead of zero to keep grounded
        }

        // Get movement direction from input
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        
        // Project vectors onto the horizontal plane (y = 0)
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 movement = (forward * movementInput.y + right * movementInput.x);
        
        // Determine speed based on sprint state
        isSprinting = canSprint && sprintInput && movementInput.y > 0.1f;
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        
        // Apply movement
        if (movement.magnitude > 1f)
            movement.Normalize(); // Prevent diagonal movement from being faster
            
        characterController.Move(movement * currentSpeed * Time.deltaTime);

        // Handle jumping
        if (canJump && isGrounded && jumpInput)
        {
            velocity.y = jumpForce;
        }

        // Apply gravity
        velocity.y -= gravity * Time.deltaTime;
        
        // Apply vertical movement
        characterController.Move(velocity * Time.deltaTime);
    }
} 