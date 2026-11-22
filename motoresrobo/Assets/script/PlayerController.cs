using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Identificação")]
    [Tooltip("1 = Jogador 1, 2 = Jogador 2 (as teclas vêm do Control Scheme do PlayerInput)")]
    public int playerID = 1;

    [Header("Movimento")]
    public float moveSpeed = 6f;
    public float rotationSpeed = 720f;
    public float gravity = -9.81f;
    [Tooltip("Se marcado, W/Cima anda para onde a câmera do jogador olha. Se desmarcado, usa os eixos do mundo.")]
    public bool useCameraDirection = true;
    [Tooltip("Desativa o ThirdPersonController do Starter Assets, que também move o personagem.")]
    public bool disableStarterAssetsController = true;

    [Header("Câmera")]
    [Tooltip("PlayerCameraRoot (alvo do Cinemachine). Se vazio, é procurado pelo nome dentro do jogador.")]
    public Transform cameraTarget;
    [Tooltip("Mantém o alvo da câmera com rotação fixa no mundo, para a câmera não girar junto com o personagem.")]
    public bool lockCameraTargetRotation = true;
    public Transform cameraReference;
    public bool controlCameraManually = false;
    public Vector3 cameraOffset = new Vector3(0f, 3f, -4f);
    public float cameraSmoothSpeed = 10f;

    [Header("Animação (opcional)")]
    [Tooltip("Se vazio, procura um Animator nos filhos.")]
    public Animator animator;
    public float animationBlendSpeed = 10f;

    private CharacterController controller;
    private PlayerInput playerInput;
    private float verticalVelocity;
    private float animSpeed;
    private Vector2 moveInput;
    private Quaternion cameraTargetWorldRotation = Quaternion.identity;

    // Hashes dos parâmetros do Animator (Starter Assets)
    private bool hasSpeed, hasMotionSpeed, hasGrounded;
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimMotionSpeed = Animator.StringToHash("MotionSpeed");
    private static readonly int AnimGrounded = Animator.StringToHash("Grounded");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = GetComponentInParent<CharacterController>();
        }

        playerInput = GetComponent<PlayerInput>();

        DisableConflictingControllers();

        if (cameraTarget == null)
        {
            foreach (Transform t in transform.root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "PlayerCameraRoot")
                {
                    cameraTarget = t;
                    break;
                }
            }
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.nameHash == AnimSpeed) hasSpeed = true;
                else if (p.nameHash == AnimMotionSpeed) hasMotionSpeed = true;
                else if (p.nameHash == AnimGrounded) hasGrounded = true;
            }
        }
    }

    // O Starter Assets (ThirdPersonController) também move o personagem com o mesmo input
    // (via StarterAssetsInputs, que recebe o mesmo OnMove do Send Messages). Com os dois
    // scripts movendo o mesmo CharacterController, o personagem anda em direções erradas.
    private void DisableConflictingControllers()
    {
        if (!disableStarterAssetsController) return;

        foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
        {
            if (mb != null && mb != this && mb.enabled && mb.GetType().Name == "ThirdPersonController")
            {
                // Desativar não basta: os eventos de animação (OnFootstep/OnLand) ainda chamam
                // o componente desativado, que dá NullReferenceException por não ter iniciado.
                // Por isso o componente é removido; os métodos abaixo recebem os eventos.
                mb.enabled = false;
                Destroy(mb);
                Debug.Log($"[PlayerController] {name}: ThirdPersonController removido para não competir com o movimento deste script.", this);
            }
        }
    }

    // O PlayerCameraRoot é filho do robô. Quando o robô gira para andar, a câmera do
    // Cinemachine que segue esse alvo girava junto (antes o ThirdPersonController desfazia
    // isso). Fixamos a rotação do alvo no mundo para a câmera ficar estável.
    private void LockCameraTarget()
    {
        if (lockCameraTargetRotation && cameraTarget != null)
        {
            cameraTarget.rotation = cameraTargetWorldRotation;
        }
    }

    private void Start()
    {
        if (cameraTarget != null)
        {
            cameraTargetWorldRotation = cameraTarget.rotation;
        }

        // Dois PlayerInput na cena compartilhando o mesmo teclado: por padrão o primeiro
        // a iniciar fica com o teclado e o segundo fica sem dispositivo (não recebe input).
        // Aqui cada jogador pareia o teclado explicitamente com o seu Control Scheme.
        if (playerInput == null) return;

        // Se os dois PlayerInput estiverem usando a MESMA instância do asset de actions,
        // trocar o Control Scheme de um troca para o outro também (o último vence), e as
        // setas passam a mover os dois. Aqui cada jogador fica com a sua própria cópia.
        bool sharedActions = false;
        foreach (PlayerController other in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (other != this && other.playerInput != null && other.playerInput.actions == playerInput.actions)
            {
                sharedActions = true;
                break;
            }
        }

        if (sharedActions)
        {
            playerInput.actions = Instantiate(playerInput.actions);
        }

        string scheme = playerInput.defaultControlScheme;
        if (Keyboard.current != null && !string.IsNullOrEmpty(scheme))
        {
            playerInput.SwitchCurrentControlScheme(scheme, Keyboard.current);
        }

        Debug.Log($"[PlayerController] {name} (ID {playerID}): scheme = '{playerInput.currentControlScheme}', actions compartilhadas = {sharedActions}", this);
    }

    private void OnEnable()
    {
        GameEvents.OnCoinCollected += HandleCoinCollected;
    }

    private void OnDisable()
    {
        GameEvents.OnCoinCollected -= HandleCoinCollected;
        moveInput = Vector2.zero;
    }

    // Chamado pelo PlayerInput (Behavior = Send Messages) com a action "Move".
    // Cada PlayerInput só envia para o próprio GameObject, então cada jogador
    // recebe apenas o input do seu Control Scheme.
    public void OnMove(InputValue value)
    {
        Vector2 v = value.Get<Vector2>();
        moveInput = v.sqrMagnitude > 1f ? v.normalized : v;
    }

    // Eventos de animação do Starter Assets (passos e pouso). Ficam vazios aqui só para
    // não dar erro de "no receiver" depois que o ThirdPersonController é removido.
    public void OnFootstep(AnimationEvent animationEvent) { }
    public void OnLand(AnimationEvent animationEvent) { }

    // Se o dispositivo for perdido/trocado, para o personagem
    public void OnDeviceLost()
    {
        moveInput = Vector2.zero;
    }

    private Transform GetDirectionCamera()
    {
        if (cameraReference != null) return cameraReference;
        if (playerInput != null && playerInput.camera != null) return playerInput.camera.transform;
        if (Camera.main != null) return Camera.main.transform;
        return null;
    }

    private void Update()
    {
        Move(moveInput);
    }

    private void LateUpdate()
    {
        LockCameraTarget();

        if (controlCameraManually)
        {
            UpdateCamera();
        }
    }

    private void Move(Vector2 input)
    {
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (useCameraDirection)
        {
            Transform cam = GetDirectionCamera();
            if (cam != null)
            {
                forward = cam.forward;
                right = cam.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
            }
        }

        Vector3 moveDir = forward * input.y + right * input.x;
        if (moveDir.sqrMagnitude > 1f)
        {
            moveDir.Normalize();
        }

        Transform targetTransform = controller != null ? controller.transform : transform;

        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            targetTransform.rotation = Quaternion.RotateTowards(
                targetTransform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        bool grounded = true;

        if (controller != null && controller.enabled)
        {
            grounded = controller.isGrounded;

            if (grounded && verticalVelocity < 0f)
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
        else
        {
            transform.position += moveDir * (moveSpeed * Time.deltaTime);
        }

        LockCameraTarget();

        UpdateAnimator(moveDir.magnitude * moveSpeed, input.magnitude, grounded);
    }

    private void UpdateAnimator(float targetSpeed, float inputMagnitude, bool grounded)
    {
        if (animator == null) return;

        animSpeed = Mathf.Lerp(animSpeed, targetSpeed, Time.deltaTime * animationBlendSpeed);
        if (animSpeed < 0.01f) animSpeed = 0f;

        if (hasSpeed) animator.SetFloat(AnimSpeed, animSpeed);
        if (hasMotionSpeed) animator.SetFloat(AnimMotionSpeed, inputMagnitude > 0.01f ? inputMagnitude : 1f);
        if (hasGrounded) animator.SetBool(AnimGrounded, grounded);
    }

    private void UpdateCamera()
    {
        if (cameraReference == null) return;

        Vector3 targetPosition = transform.position + transform.TransformDirection(cameraOffset);
        cameraReference.position = Vector3.Lerp(
            cameraReference.position,
            targetPosition,
            cameraSmoothSpeed * Time.deltaTime
        );
        cameraReference.LookAt(transform.position + Vector3.up * 1f);
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