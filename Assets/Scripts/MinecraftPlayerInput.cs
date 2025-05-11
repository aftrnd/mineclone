using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MinecraftPlayerController), typeof(PlayerInput))]
public class MinecraftPlayerInput : MonoBehaviour
{
    [SerializeField] private MinecraftCameraController cameraController;
    [SerializeField] private BlockInteraction blockInteraction;
    
    private MinecraftPlayerController playerController;
    private PlayerInput playerInput;
    
    private void Awake()
    {
        playerController = GetComponent<MinecraftPlayerController>();
        playerInput = GetComponent<PlayerInput>();
        
        // Auto-find the camera controller if not assigned
        if (cameraController == null)
        {
            // Try to find in children first
            cameraController = GetComponentInChildren<MinecraftCameraController>();
            
            if (cameraController == null)
            {
                // Look for any camera in children
                Camera childCamera = GetComponentInChildren<Camera>();
                if (childCamera != null)
                {
                    // Check if that camera has the controller
                    cameraController = childCamera.GetComponent<MinecraftCameraController>();
                    
                    // If not, add the controller to the camera
                    if (cameraController == null)
                    {
                        Debug.LogWarning("Found camera but no MinecraftCameraController. Adding component to camera.");
                        cameraController = childCamera.gameObject.AddComponent<MinecraftCameraController>();
                    }
                    else
                    {
                        Debug.Log("Found MinecraftCameraController on child camera.");
                    }
                }
                else
                {
                    Debug.LogError("No camera found in children! Mouse look will not work.");
                }
            }
            else
            {
                Debug.Log("Found MinecraftCameraController in children.");
            }
        }
        
        // Auto-find the block interaction if not assigned
        if (blockInteraction == null)
        {
            blockInteraction = GetComponent<BlockInteraction>();
            if (blockInteraction == null)
            {
                Debug.LogError("No BlockInteraction found! Please assign one manually.");
            }
        }
    }
    
    private void OnEnable()
    {
        // Get references to the action maps
        var playerActions = playerInput.actions.FindActionMap("Player");
        
        // Check if camera controller is assigned
        if (cameraController == null)
        {
            Debug.LogError("Camera controller is null! Mouse look will not work.");
            return;
        }
        else
        {
            Debug.Log("Camera controller found: " + cameraController.name);
        }
        
        // Check if actions are found
        var lookAction = playerActions.FindAction("Look");
        if (lookAction == null)
        {
            Debug.LogError("Look action not found in input actions!");
        }
        else
        {
            Debug.Log("Look action found. Binding to camera controller.");
            lookAction.performed += cameraController.OnLook;
            lookAction.canceled += cameraController.OnLook;
        }
        
        // Register callbacks for player actions
        playerActions.FindAction("Move").performed += playerController.OnMove;
        playerActions.FindAction("Move").canceled += playerController.OnMove;
        
        playerActions.FindAction("Jump").performed += playerController.OnJump;
        playerActions.FindAction("Jump").canceled += playerController.OnJump;
        
        playerActions.FindAction("Sprint").performed += playerController.OnSprint;
        playerActions.FindAction("Sprint").canceled += playerController.OnSprint;
        
        playerActions.FindAction("Look").performed += cameraController.OnLook;
        playerActions.FindAction("Look").canceled += cameraController.OnLook;
        
        // Block interaction
        playerActions.FindAction("Attack").performed += blockInteraction.OnAttack;
        playerActions.FindAction("Attack").canceled += blockInteraction.OnAttack;
        
        playerActions.FindAction("Interact").performed += blockInteraction.OnInteract;
        playerActions.FindAction("Interact").canceled += blockInteraction.OnInteract;
        
        playerActions.FindAction("Next").performed += blockInteraction.OnNextBlock;
        playerActions.FindAction("Previous").performed += blockInteraction.OnPreviousBlock;
        
        Debug.Log("Setting up input actions - Block interaction should use Attack (left-click) and Interact (right-click)");
    }
    
    private void OnDisable()
    {
        // Get references to the action maps
        var playerActions = playerInput.actions.FindActionMap("Player");
        
        // Unregister callbacks for player actions
        playerActions.FindAction("Move").performed -= playerController.OnMove;
        playerActions.FindAction("Move").canceled -= playerController.OnMove;
        
        playerActions.FindAction("Jump").performed -= playerController.OnJump;
        playerActions.FindAction("Jump").canceled -= playerController.OnJump;
        
        playerActions.FindAction("Sprint").performed -= playerController.OnSprint;
        playerActions.FindAction("Sprint").canceled -= playerController.OnSprint;
        
        playerActions.FindAction("Look").performed -= cameraController.OnLook;
        playerActions.FindAction("Look").canceled -= cameraController.OnLook;
        
        // Block interaction
        playerActions.FindAction("Attack").performed -= blockInteraction.OnAttack;
        playerActions.FindAction("Attack").canceled -= blockInteraction.OnAttack;
        
        playerActions.FindAction("Interact").performed -= blockInteraction.OnInteract;
        playerActions.FindAction("Interact").canceled -= blockInteraction.OnInteract;
        
        playerActions.FindAction("Next").performed -= blockInteraction.OnNextBlock;
        playerActions.FindAction("Previous").performed -= blockInteraction.OnPreviousBlock;
    }

    private void Start()
    {
        // Auto-connect BlockHighlighter to BlockInteraction
        if (blockInteraction != null)
        {
            BlockHighlighter highlighter = GetComponentInChildren<BlockHighlighter>();
            if (highlighter != null)
            {
                // Use reflection to set the blockHighlighter field in BlockInteraction
                System.Type type = blockInteraction.GetType();
                System.Reflection.FieldInfo field = type.GetField("blockHighlighter", 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Public);
                
                if (field != null)
                {
                    field.SetValue(blockInteraction, highlighter);
                    Debug.Log("Successfully connected BlockHighlighter to BlockInteraction via code");
                }
            }
        }
    }

    private void Update() 
    {
        if (blockInteraction != null)
        {
            // Debug the current selected block type
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2) || 
                Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Alpha4))
            {
                Debug.Log("Number key pressed for block selection");
            }
        }
    }
} 