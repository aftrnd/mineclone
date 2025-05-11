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
    [SerializeField] private float flightSpeed = 8.0f; // Creative mode flight speed
    [SerializeField] private float flightSprintSpeed = 16.0f; // Creative mode sprint flight speed

    [Header("Player Parameters")]
    [SerializeField] private float playerHeight = 1.8f; // Player is 1.8 blocks tall in Minecraft
    [SerializeField] private float playerWidth = 0.6f; // Player is 0.6 blocks wide in Minecraft
    [SerializeField] private float eyeHeight = 1.62f; // Eye height from ground in Minecraft

    [Header("Gameplay")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canFly = true; // Enable/disable flying ability

    // References
    private CharacterController characterController;
    private Transform cameraTransform;
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 velocity = Vector3.zero;
    private bool isGrounded;
    private bool isSprinting;
    private bool isFlying = false; // Track if player is in flight mode

    // Input actions
    private Vector2 movementInput;
    private bool jumpInput;
    private bool sprintInput;
    private bool flyToggleInput;
    private bool flyUpInput;
    private bool flyDownInput;

    private bool firstUpdateDone = false;

    private void Awake()
    {
        Debug.Log("MinecraftPlayerController Awake called - Player height: " + playerHeight + ", Eye height: " + eyeHeight);
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
            
            // Also fix the character controller center to ensure the bottom is at the ground
            characterController.center = new Vector3(0, playerHeight / 2, 0);
            Debug.Log($"Set character controller height: {playerHeight}, radius: {playerWidth/2}, center Y: {playerHeight/2}");
            
            // Position camera at eye height if it's our child
            PositionCameraAtEyeHeight();
        }
        else
        {
            Debug.LogError("No CharacterController component found! Please add one.");
        }
    }

    private void PositionCameraAtEyeHeight()
    {
        if (cameraTransform != null && cameraTransform.parent == transform)
        {
            // Calculate the offset - The camera should be at eye level (1.62 blocks from ground)
            // When character pivot is at the bottom, camera should be at eyeHeight
            // When character pivot is at the center, camera should be at eyeHeight - playerHeight/2
            
            // OPTION 1: Direct positioning at eye height from ground (assuming player pivot at ground level)
            //cameraTransform.localPosition = new Vector3(0, eyeHeight, 0);
            
            // OPTION 2: Positioning relative to character controller center
            float cameraOffset = eyeHeight - (playerHeight / 2);
            cameraTransform.localPosition = new Vector3(0, cameraOffset, 0);
            
            // OPTION 3: Force absolute world height directly
            //Vector3 worldPos = transform.position;
            //worldPos.y += eyeHeight;
            //cameraTransform.position = worldPos;
            
            Debug.Log($"CAMERA POSITION SET - Player total height: {playerHeight}, Eye height from ground: {eyeHeight}");
            Debug.Log($"Character pivot at {transform.position.y}, Character center at {transform.position.y + playerHeight/2}");
            Debug.Log($"Camera local position set to: {cameraTransform.localPosition}, World position: {cameraTransform.position}");
        }
        else
        {
            Debug.LogWarning("Cannot position camera - camera transform not found or not a child of player");
        }
    }

    private void Start()
    {
        Debug.Log("MinecraftPlayerController Start called");
        // Position camera again in Start to make sure it's set
        PositionCameraAtEyeHeight();
    }

    private void Update()
    {
        if (!firstUpdateDone)
        {
            firstUpdateDone = true;
            Debug.Log("First Update - Setting camera position again");
            PositionCameraAtEyeHeight();
        }
        
        // Handle flight toggle with F key
        if (flyToggleInput && canFly)
        {
            isFlying = !isFlying;
            velocity = Vector3.zero; // Reset velocity when toggling flight
            Debug.Log("Flight mode: " + (isFlying ? "Enabled" : "Disabled"));
        }
        
        HandleMovement();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        jumpInput = context.ReadValueAsButton();
        
        // In flight mode, jump is used to fly up
        if (isFlying)
        {
            flyUpInput = context.ReadValueAsButton();
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        sprintInput = context.ReadValueAsButton();
    }
    
    public void OnFlyToggle(InputAction.CallbackContext context)
    {
        // Only trigger on button press, not hold
        if (context.performed)
        {
            flyToggleInput = true;
        }
        else
        {
            flyToggleInput = false;
        }
    }
    
    public void OnFlyDown(InputAction.CallbackContext context)
    {
        flyDownInput = context.ReadValueAsButton();
    }

    private void HandleMovement()
    {
        isGrounded = characterController.isGrounded;
        
        if (isGrounded && velocity.y < 0 && !isFlying)
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
        
        // Determine speed based on sprint state and flight mode
        isSprinting = canSprint && sprintInput && movementInput.y > 0.1f;
        float currentSpeed;
        
        if (isFlying)
        {
            currentSpeed = isSprinting ? flightSprintSpeed : flightSpeed;
        }
        else
        {
            currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        }
        
        // Apply movement
        if (movement.magnitude > 1f)
            movement.Normalize(); // Prevent diagonal movement from being faster
            
        characterController.Move(movement * currentSpeed * Time.deltaTime);

        // Handle vertical movement
        if (isFlying)
        {
            // In flight mode, control vertical movement directly
            velocity.y = 0; // Reset gravity effect
            
            if (flyUpInput)
            {
                velocity.y = flightSpeed;
            }
            else if (flyDownInput)
            {
                velocity.y = -flightSpeed;
            }
        }
        else
        {
            // Normal jumping in non-flight mode
            if (canJump && isGrounded && jumpInput)
            {
                velocity.y = jumpForce;
            }

            // Apply gravity in non-flight mode
            velocity.y -= gravity * Time.deltaTime;
        }
        
        // Apply vertical movement
        characterController.Move(velocity * Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        // Draw player height outline
        Gizmos.color = Color.blue;
        // Draw a wireframe cylinder to represent the player's collider
        Vector3 bottom = transform.position;
        Vector3 top = transform.position + new Vector3(0, playerHeight, 0);
        
        // Draw vertical line for height
        Gizmos.DrawLine(bottom, top);
        
        // Draw circles at bottom and top
        DrawWireCircle(bottom, playerWidth / 2, 16);
        DrawWireCircle(top, playerWidth / 2, 16);
        
        // Draw eye level
        Gizmos.color = Color.red;
        Vector3 eyePos = transform.position + new Vector3(0, eyeHeight, 0);
        
        // Draw a horizontal cross at eye level
        float crossSize = playerWidth / 2;
        Gizmos.DrawLine(eyePos - new Vector3(crossSize, 0, 0), eyePos + new Vector3(crossSize, 0, 0));
        Gizmos.DrawLine(eyePos - new Vector3(0, 0, crossSize), eyePos + new Vector3(0, 0, crossSize));
        
        // Draw small circle at eye level
        DrawWireCircle(eyePos, playerWidth / 4, 8);
        
        // If we have a camera child, draw its position as well
        if (cameraTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(cameraTransform.position, 0.1f);
        }
    }

    private void DrawWireCircle(Vector3 position, float radius, int segments)
    {
        float angle = 0f;
        Vector3 lastPoint = position;
        Vector3 firstPoint = position;
        
        for (int i = 0; i < segments + 1; i++)
        {
            angle = (float)i / segments * 360 * Mathf.Deg2Rad;
            Vector3 newPoint = position + new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
            
            if (i > 0)
                Gizmos.DrawLine(lastPoint, newPoint);
            
            if (i == 1)
                firstPoint = newPoint;
            
            lastPoint = newPoint;
        }
        
        // Close the circle
        Gizmos.DrawLine(lastPoint, firstPoint);
    }
} 