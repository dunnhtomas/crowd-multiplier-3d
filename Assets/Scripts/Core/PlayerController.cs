using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Mathematics;

namespace CrowdMultiplier.Core
{
    /// <summary>
    /// High-performance player controller with touch/mouse input and smooth movement
    /// Supports both mobile touch input and desktop mouse/keyboard
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float smoothTime = 0.1f;
        [SerializeField] private float maxSpeed = 15f;
        [SerializeField] private bool constrainToPath = true;
        [SerializeField] private float pathWidth = 10f;
        
        [Header("Input Settings")]
        [SerializeField] private bool useNewInputSystem = true;
        [SerializeField] private bool enableKeyboardInput = true;
        [SerializeField] private bool enableTouchInput = true;
        [SerializeField] private float touchSensitivity = 2f;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem movementTrail;
        [SerializeField] private GameObject playerModel;
        [SerializeField] private Animator playerAnimator;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] footstepSounds;
        [SerializeField] private float footstepInterval = 0.5f;
        
        // Input components
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction touchPositionAction;
        private InputAction touchPressAction;
        
        // Movement variables
        private Vector3 targetPosition;
        private Vector3 velocity;
        private Vector3 lastPosition;
        private bool isMoving = false;
        private float lastFootstepTime;
        
        // Touch input
        private bool isTouching = false;
        private Vector2 touchStartPosition;
        private Vector2 currentTouchPosition;
        
        // Camera reference
        private Camera mainCamera;
        private CrowdController crowdController;
        
        // Events
        public event System.Action<Vector3> OnPlayerMoved;
        public event System.Action OnPlayerStopped;
        
        // Properties
        public Vector3 Position => transform.position;
        public bool IsMoving => isMoving;
        public float CurrentSpeed => velocity.magnitude;
        
        private void Awake()
        {
            InitializeComponents();
            SetupInput();
        }
        
        private void Start()
        {
            InitializeReferences();
            SetupInitialPosition();
        }
        
        private void InitializeComponents()
        {
            // Get or add required components
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            
            // Setup audio source
            audioSource.volume = 0.5f;
            audioSource.spatialBlend = 0.8f; // 3D sound
            
            // Initialize target position
            targetPosition = transform.position;
            lastPosition = transform.position;
        }
        
        private void SetupInput()
        {
            if (useNewInputSystem)
            {
                playerInput = GetComponent<PlayerInput>();
                if (playerInput == null)
                {
                    playerInput = gameObject.AddComponent<PlayerInput>();
                }
                
                // Get input actions
                moveAction = playerInput.actions["Move"];
                touchPositionAction = playerInput.actions["TouchPosition"];
                touchPressAction = playerInput.actions["TouchPress"];
                
                // Enable input actions
                if (enableKeyboardInput && moveAction != null)
                {
                    moveAction.Enable();
                }
                
                if (enableTouchInput)
                {
                    if (touchPositionAction != null) touchPositionAction.Enable();
                    if (touchPressAction != null)
                    {
                        touchPressAction.Enable();
                        touchPressAction.performed += OnTouchPress;
                        touchPressAction.canceled += OnTouchRelease;
                    }
                }
            }
        }
        
        private void InitializeReferences()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
            
            crowdController = FindObjectOfType<CrowdController>();
            
            // Tag this as Player for other systems
            if (gameObject.tag != "Player")
            {
                gameObject.tag = "Player";
            }
        }
        
        private void SetupInitialPosition()
        {
            // Position player at the start of the level
            var levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null)
            {
                transform.position = levelManager.GetPlayerStartPosition();
                targetPosition = transform.position;
            }
        }
        
        private void Update()
        {
            HandleInput();
            UpdateMovement();
            UpdateVisualEffects();
            UpdateAudio();
            
            // Track movement for analytics
            TrackMovementAnalytics();
        }
        
        private void HandleInput()
        {
            Vector2 inputVector = Vector2.zero;
            
            if (useNewInputSystem)
            {
                HandleNewInputSystem(ref inputVector);
            }
            else
            {
                HandleLegacyInput(ref inputVector);
            }
            
            // Apply input to target position
            if (inputVector != Vector2.zero)
            {
                Vector3 inputDirection = new Vector3(inputVector.x, 0f, inputVector.y);
                
                // Convert to world space if using camera-relative movement
                if (mainCamera != null)
                {
                    Vector3 cameraForward = Vector3.Scale(mainCamera.transform.forward, new Vector3(1, 0, 1)).normalized;
                    Vector3 cameraRight = Vector3.Scale(mainCamera.transform.right, new Vector3(1, 0, 1)).normalized;
                    
                    inputDirection = cameraForward * inputDirection.z + cameraRight * inputDirection.x;
                }
                
                targetPosition += inputDirection * moveSpeed * Time.deltaTime;
                
                // Constrain to path if enabled
                if (constrainToPath)
                {
                    targetPosition.x = Mathf.Clamp(targetPosition.x, -pathWidth * 0.5f, pathWidth * 0.5f);
                }
            }
        }
        
        private void HandleNewInputSystem(ref Vector2 inputVector)
        {
            // Keyboard/Gamepad input
            if (enableKeyboardInput && moveAction != null)
            {
                inputVector = moveAction.ReadValue<Vector2>();
            }
            
            // Touch input
            if (enableTouchInput && isTouching && touchPositionAction != null)
            {
                currentTouchPosition = touchPositionAction.ReadValue<Vector2>();
                Vector2 touchDelta = (currentTouchPosition - touchStartPosition) * touchSensitivity * Time.deltaTime;
                inputVector += touchDelta;
                touchStartPosition = currentTouchPosition; // Update for continuous movement
            }
        }
        
        private void HandleLegacyInput()
        {
            // Legacy input system fallback
            Vector2 inputVector = Vector2.zero;
            
            if (enableKeyboardInput)
            {
                inputVector.x = Input.GetAxis("Horizontal");
                inputVector.y = Input.GetAxis("Vertical");
            }
            
            if (enableTouchInput && Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                if (touch.phase == TouchPhase.Began)
                {
                    touchStartPosition = touch.position;
                    isTouching = true;
                }
                else if (touch.phase == TouchPhase.Moved && isTouching)
                {
                    Vector2 touchDelta = (touch.position - touchStartPosition) * touchSensitivity * Time.deltaTime;
                    inputVector += touchDelta;
                    touchStartPosition = touch.position;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    isTouching = false;
                }
            }
            
            // Apply legacy input
            if (inputVector != Vector2.zero)
            {
                Vector3 inputDirection = new Vector3(inputVector.x, 0f, inputVector.y);
                targetPosition += inputDirection * moveSpeed * Time.deltaTime;
                
                if (constrainToPath)
                {
                    targetPosition.x = Mathf.Clamp(targetPosition.x, -pathWidth * 0.5f, pathWidth * 0.5f);
                }
            }
        }
        
        private void UpdateMovement()
        {
            // Smooth movement towards target
            Vector3 newPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime, maxSpeed);
            
            // Check if we're moving
            bool wasMoving = isMoving;
            isMoving = Vector3.Distance(transform.position, newPosition) > 0.01f;
            
            // Update position
            transform.position = newPosition;
            
            // Handle movement state changes
            if (isMoving && !wasMoving)
            {
                OnPlayerMoved?.Invoke(transform.position);
            }
            else if (!isMoving && wasMoving)
            {
                OnPlayerStopped?.Invoke();
            }
            
            // Update rotation to face movement direction
            if (isMoving)
            {
                Vector3 movementDirection = (transform.position - lastPosition).normalized;
                if (movementDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                }
            }
            
            lastPosition = transform.position;
        }
        
        private void UpdateVisualEffects()
        {
            // Animator updates
            if (playerAnimator != null)
            {
                playerAnimator.SetBool("IsMoving", isMoving);
                playerAnimator.SetFloat("Speed", CurrentSpeed / maxSpeed);
            }
            
            // Movement trail effect
            if (movementTrail != null)
            {
                var emission = movementTrail.emission;
                emission.enabled = isMoving && CurrentSpeed > 1f;
                
                if (isMoving)
                {
                    var velocityOverLifetime = movementTrail.velocityOverLifetime;
                    velocityOverLifetime.enabled = true;
                    velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
                }
            }
        }
        
        private void UpdateAudio()
        {
            // Footstep sounds
            if (isMoving && footstepSounds.Length > 0 && Time.time - lastFootstepTime > footstepInterval)
            {
                int randomIndex = UnityEngine.Random.Range(0, footstepSounds.Length);
                audioSource.PlayOneShot(footstepSounds[randomIndex], 0.3f);
                lastFootstepTime = Time.time;
            }
        }
        
        private void TrackMovementAnalytics()
        {
            // Track movement metrics for analytics
            if (GameManager.Instance != null)
            {
                var analyticsManager = GameManager.Instance.GetComponent<AnalyticsManager>();
                if (analyticsManager != null && isMoving)
                {
                    // Track movement every few seconds to avoid spam
                    if (Time.time % 5f < Time.deltaTime)
                    {
                        analyticsManager.TrackEvent("player_movement", new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "position_x", transform.position.x },
                            { "position_z", transform.position.z },
                            { "speed", CurrentSpeed },
                            { "level", GameManager.Instance.CurrentLevel }
                        });
                    }
                }
            }
        }
        
        private void OnTouchPress(InputAction.CallbackContext context)
        {
            isTouching = true;
            touchStartPosition = touchPositionAction.ReadValue<Vector2>();
        }
        
        private void OnTouchRelease(InputAction.CallbackContext context)
        {
            isTouching = false;
        }
        
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = Mathf.Clamp(speed, 1f, 20f);
        }
        
        public void SetPathConstraints(bool constrain, float width = 10f)
        {
            constrainToPath = constrain;
            pathWidth = width;
        }
        
        public void Teleport(Vector3 position)
        {
            transform.position = position;
            targetPosition = position;
            velocity = Vector3.zero;
        }
        
        public void AddForce(Vector3 force)
        {
            targetPosition += force;
        }
        
        private void OnEnable()
        {
            if (useNewInputSystem)
            {
                if (moveAction != null) moveAction.Enable();
                if (touchPositionAction != null) touchPositionAction.Enable();
                if (touchPressAction != null) touchPressAction.Enable();
            }
        }
        
        private void OnDisable()
        {
            if (useNewInputSystem)
            {
                if (moveAction != null) moveAction.Disable();
                if (touchPositionAction != null) touchPositionAction.Disable();
                if (touchPressAction != null) touchPressAction.Disable();
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw path constraints
            if (constrainToPath)
            {
                Gizmos.color = Color.yellow;
                Vector3 center = transform.position;
                center.y += 0.1f;
                
                Vector3 leftBound = center + Vector3.left * (pathWidth * 0.5f);
                Vector3 rightBound = center + Vector3.right * (pathWidth * 0.5f);
                
                Gizmos.DrawLine(leftBound, rightBound);
                Gizmos.DrawWireCube(center, new Vector3(pathWidth, 0.2f, 1f));
            }
            
            // Draw target position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition, 0.5f);
            
            // Draw velocity vector
            if (isMoving)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, velocity);
            }
        }
    }
}
