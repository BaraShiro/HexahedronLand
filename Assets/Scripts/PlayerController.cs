using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private LayerMask groundLayers = new LayerMask();
    [SerializeField] private Transform cameraFollowPoint;
    [SerializeField, Range(1f,20f)] private float mouseSensitivity = 5f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float climbSpeed = 6f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravityValue = 9.81f;
    [SerializeField] private float terminalVelocity = 50f;
    [SerializeField, Range(0f,90f)] private float maxLookAngle = 85f;
    [SerializeField, Range(-90,0f)] private float minLookAngle = -85f;

    private Transform playerTransform;
    private Vector3 horizontalDirection = Vector3.zero;
    private Vector3 horizontalVelocity = Vector3.zero;
    private Vector3 verticalVelocity = Vector3.zero;
    private Vector2 lookDelta = Vector2.zero;
    private Vector3 newRotation = Vector3.zero;
    private bool jump = false;
    private bool climbing = false;
    private bool mousePointerOn = false;

    private static Dictionary<string, float> inventory = new Dictionary<string, float>(); // TODO: Make separate class
    

    private void Start()
    {
        playerTransform = transform;
        mousePointerOn = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        newRotation.x = cameraFollowPoint.localRotation.eulerAngles.x;
        newRotation.y = playerTransform.localRotation.eulerAngles.y;
    }

    private void Update()
    {
        HandleHorizontalMovement();
        HandleVerticalMovement();
        HandleRotation();
    }

    private void HandleHorizontalMovement()
    {
        horizontalVelocity = playerTransform.forward * horizontalDirection.z + playerTransform.right * horizontalDirection.x;
        horizontalVelocity = horizontalVelocity.normalized * moveSpeed;
        controller.Move(horizontalVelocity * Time.deltaTime);
    }

    private void HandleVerticalMovement()
    {
        if (!climbing && CanClimb())
        {
            climbing = true;
            verticalVelocity.y = climbSpeed * horizontalDirection.z;
        }
        
        if (jump)
        {
            jump = false;
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * 2.0f * gravityValue);
        }

        verticalVelocity.y -= gravityValue * Time.deltaTime;
        verticalVelocity.y = Mathf.Clamp(verticalVelocity.y, -terminalVelocity, terminalVelocity);
        controller.Move(verticalVelocity * Time.deltaTime);
        
        if ((controller.collisionFlags & CollisionFlags.Above) != 0)
        {
            verticalVelocity.y = 0f;
        }

        if (controller.isGrounded)
        {
            // TODO: Fall damage
            verticalVelocity.y = 0f;
            climbing = false;
        }
    }

    private void HandleRotation()
    {
        if (mousePointerOn)
        {
            return;
        }

        newRotation.x += lookDelta.y * mouseSensitivity;
        newRotation.x = Mathf.Clamp(newRotation.x, minLookAngle, maxLookAngle);
        cameraFollowPoint.localRotation = Quaternion.Euler(newRotation.x, 0f, 0f);

        newRotation.y += lookDelta.x * mouseSensitivity;
        newRotation.y = Mathf.Repeat(newRotation.y, 359f);
        playerTransform.localRotation = Quaternion.Euler(0f, newRotation.y, 0f);
    }

    private bool CanClimb()
    {
        // Object in front
        if (Physics.Raycast(
                playerTransform.position + new Vector3(0f, 0.5f, 0f), 
                playerTransform.forward, 
                0.5f,
                groundLayers))
        {
            // Free space to climb to
            if (!Physics.CapsuleCast(
                    playerTransform.position + new Vector3(0f, 2.5f, 0f),
                    playerTransform.position + new Vector3(0f, 1.5f, 0f),
                    0.25f,
                    playerTransform.forward,
                    0.5f,
                    groundLayers))
            {
                return true;
            }
        }
        
        return false;
    }

    public Vector3 GetPlayerPosition()
    {
        return playerTransform.position;
    }
    
    public static void AddToInventory(string resourceName, float amount)
    {
        if (string.IsNullOrEmpty(resourceName) || amount <= 0f)
        {
            return;
        }
        
        if (inventory.ContainsKey(resourceName))
        {
            inventory[resourceName] += amount;
        }
        else
        {
            inventory.Add(resourceName, amount);
        }
    }

    public static void PrintInventory()
    {
        foreach (var res in inventory)
        {
            print($"{res.Key}: {res.Value}");
        }
    }

    public void DebugInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            print("Debug!");
            PrintInventory();
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // velocity.x = context.ReadValue<Vector2>().x;
            // velocity.z = context.ReadValue<Vector2>().y;
            horizontalDirection.x = context.ReadValue<Vector2>().x;
            horizontalDirection.z = context.ReadValue<Vector2>().y;
        }
    
        if (context.canceled)
        {
            horizontalDirection = Vector3.zero;
        }
    }
    
    public void Look(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            lookDelta.x = context.ReadValue<Vector2>().x;
            lookDelta.y = -context.ReadValue<Vector2>().y;
        }
        
        if (context.canceled)
        {
            lookDelta = Vector2.zero;
        }
    }

    public void Fire(InputAction.CallbackContext context)
    {
        if (context.performed) // TODO: Put in separate class
        {
            Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            
            Vector3 rayOrigin = new Vector3(0.5f, 0.5f, 0f); // center of the screen
            float rayLength = 3f;

            // actual Ray
            Ray ray = Camera.main.ViewportPointToRay(rayOrigin);

            // debug Ray
            Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red, 1.0f);

            if (Physics.Raycast(ray, out RaycastHit hit, rayLength, groundLayers))
            {
                if (hit.transform.GetComponent<IHarvestable>() is IHarvestable harvestable)
                {
                    print("hit harvestable");
                    harvestable.Harvest(1);
                }
                else if(hit.transform.GetComponent<Chunk>() is Chunk chunk)
                {
                    Debug.Log("hit chunk");
                    Landscape.MineBlock(hit);
                }
                // // our Ray intersected a collider
                // print(hit.collider.name);
                //
                // IBreakable breakable = hit.collider.GetComponent<IBreakable>();
                // breakable?.ApplyDamage(1, gameObject);
                
                // Landscape.SetBlock(hit, new BlockAir());
                
            }
            
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (controller.isGrounded)
            {
                Debug.Log("Jump!");
                jump = true;
                // verticalVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
                // verticalVelocity.y += jumpHeight;
            }
        }
    }
    
    public void Menu(InputAction.CallbackContext context)
    {
        if (mousePointerOn)
        {
            mousePointerOn = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
        }
        else
        {
            mousePointerOn = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
}
