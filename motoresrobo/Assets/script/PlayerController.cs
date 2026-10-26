using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Identificação")]
    [Tooltip("1 para Jogador 1 (WASD), 2 para Jogador 2 (Setas)")]
    public int playerID = 1; 

    [Header("Movimento")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f;

    [Header("Câmera")]
    public Camera playerCamera;
    public Vector3 cameraOffset = new Vector3(0, 3f, -4f);
    public float cameraSmoothSpeed = 10f;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        GameEvents.OnCoinCollected += HandleCoinCollected;
    }

    private void OnDisable()
    {
        GameEvents.OnCoinCollected -= HandleCoinCollected;
    }

    private void Update()
    {
        MovePlayer();
    }

    private void LateUpdate()
    {
        UpdateCamera();
    }

    private void MovePlayer()
    {
        float moveX = 0f;
        float moveZ = 0f;

        
        if (playerID == 1)
        {
            if (Input.GetKey(KeyCode.W)) moveZ = 1f;
            if (Input.GetKey(KeyCode.S)) moveZ = -1f;
            if (Input.GetKey(KeyCode.A)) moveX = -1f;
            if (Input.GetKey(KeyCode.D)) moveX = 1f;
        }
        else if (playerID == 2)
        {
            if (Input.GetKey(KeyCode.UpArrow)) moveZ = 1f;
            if (Input.GetKey(KeyCode.DownArrow)) moveZ = -1f;
            if (Input.GetKey(KeyCode.LeftArrow)) moveX = -1f;
            if (Input.GetKey(KeyCode.RightArrow)) moveX = 1f;
        }

        Vector3 forward = playerCamera != null ? playerCamera.transform.forward : Vector3.forward;
        Vector3 right = playerCamera != null ? playerCamera.transform.right : Vector3.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = (forward * moveZ + right * moveX).normalized;

        if (moveDir.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            controller.Move(moveDir * moveSpeed * Time.deltaTime);
        }

        controller.Move(Vector3.down * 9.81f * Time.deltaTime);
    }

    private void UpdateCamera()
    {
        if (playerCamera == null) return;

        Vector3 targetPosition = transform.position + transform.TransformDirection(cameraOffset);
        playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, targetPosition, cameraSmoothSpeed * Time.deltaTime);
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