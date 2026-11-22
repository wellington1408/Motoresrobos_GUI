using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Identificação")]
    [Tooltip("1 = Jogador 1 (WASD / tecladop1), 2 = Jogador 2 (Setas / tecladop2)")]
    public int playerID = 1;

    [Header("Movimento")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f;
    public float gravity = -9.81f;

    [Header("Câmera (opcional se usar PlayerFollowCmra)")]
    public Camera playerCamera;
    public bool controlCameraManually = false;
    public Vector3 cameraOffset = new Vector3(0f, 3f, -4f);
    public float cameraSmoothSpeed = 10f;

    private CharacterController controller;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        GameEvents.OnCoinCollected += HandleCoinCollected;
    }

    private void OnDisable()
    {
        GameEvents.OnCoinCollected -= HandleCoinCollected;
    }

    private void Start()
    {
        ConfigureCameraViewport();
        ConfigureInputScheme();
    }

    private void ConfigureCameraViewport()
    {
        if (playerCamera == null) return;

        if (playerID == 1)
        {
            playerCamera.rect = new Rect(0f, 0f, 0.5f, 1f);
        }
        else if (playerID == 2)
        {
            playerCamera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
        }
    }

    private void ConfigureInputScheme()
    {
        if (playerInput == null) return;

        string targetScheme = playerID == 1 ? "tecladop1" : "tecladop2";

        if (playerInput.actions != null)
        {
            playerInput.actions.Enable();
        }

        if (!string.IsNullOrEmpty(targetScheme) && playerInput.currentControlScheme != targetScheme)
        {
            playerInput.SwitchCurrentControlScheme(targetScheme, Keyboard.current);
        }
    }


    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void Update()
    {
        Move();
    }

    private void LateUpdate()
    {
        if (controlCameraManually)
        {
            UpdateCamera();
        }
    }

    private void Move()
    {
        Transform refTransform = playerCamera != null ? playerCamera.transform : transform;

        Vector3 forward = refTransform.forward;
        Vector3 right = refTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = forward * moveInput.y + right * moveInput.x;
        if (moveDir.sqrMagnitude > 1f)
        {
            moveDir.Normalize();
        }

        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 velocity = moveDir * moveSpeed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateCamera()
    {
        if (playerCamera == null) return;

        Vector3 targetPosition = transform.position + transform.TransformDirection(cameraOffset);
        playerCamera.transform.position = Vector3.Lerp(
            playerCamera.transform.position,
            targetPosition,
            cameraSmoothSpeed * Time.deltaTime
        );
        playerCamera.transform.LookAt(transform.position + Vector3.up * 1f);
    }

    private void HandleCoinCollected(int id)
    {
        if (id == playerID)
        {
            moveSpeed += 1.5f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))
        {
            GameManager.Instance.AddCoin(playerID);
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Star"))
        {
            GameManager.Instance.AddStar(playerID);
            Destroy(other.gameObject);
        }
    }
}