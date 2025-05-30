using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimento")]
    private float moveSpeed;
    private float rotationSpeed;
    private float backwardSpeedMultiplier;
    private float strafeSpeedMultiplier;
    
    // Componentes cached
    private CharacterController characterController;
    private Animator animator;
    private Camera mainCamera;
    private PlayerController playerController;
    
    // Para direcionamento do mouse
    private Plane groundPlane;
    private Ray ray;
    private float rayDistance;
    
    // Para movimento estilo PoE2
    private Vector3 inputDirection = Vector3.zero;
    private Vector3 worldMoveDirection = Vector3.zero;
    private Vector3 lastMouseWorldPosition;
    private bool wasPreviouslyMoving = false;
    
    // Variáveis para direções relativas ao personagem
    private bool isMovingForward = false;
    private bool isMovingBackward = false;
    private bool isMovingLeft = false;
    private bool isMovingRight = false;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        mainCamera = Camera.main;
        playerController = GetComponent<PlayerController>();
        groundPlane = new Plane(Vector3.up, Vector3.zero);

        // Load config values
        var config = GameConfig.Instance;
        moveSpeed = config.playerMoveSpeed;
        rotationSpeed = config.playerRotationSpeed;
        backwardSpeedMultiplier = config.backwardSpeedMultiplier;
        strafeSpeedMultiplier = config.strafeSpeedMultiplier;
    }
    
    private void Start()
    {
        // Inicializar componentes após o PlayerController ter inicializado
        if (playerController != null)
        {
            characterController = playerController.GetCharacterController();
            animator = playerController.GetAnimator();
            mainCamera = playerController.GetMainCamera();
            
            // Fallback para encontrar camera se não encontrada
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("PlayerMovement: Nenhuma câmera encontrada! Certifique-se de que existe uma câmera com a tag 'MainCamera'.");
                }
            }
            
            // Verificar componentes essenciais
            if (characterController == null)
            {
                Debug.LogError("PlayerMovement: CharacterController não encontrado!");
            }
        }
    }

    public void HandleInput()
    {
        // Verificar se os componentes essenciais existem
        if (characterController == null || mainCamera == null)
        {
            return;
        }
        
        // Prioridade: sempre olhar para o mouse
        LookAtMouse();
        
        // Gerenciar movimento
        HandleMovement();
    }
    
    private void LookAtMouse()
    {
        // Verificar se a câmera existe
        if (mainCamera == null)
        {
            return;
        }
        
        // Lançar raio da câmera para o mouse
        ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (groundPlane.Raycast(ray, out rayDistance))
        {
            // Ponto no mundo onde o mouse "está"
            Vector3 mouseWorldPosition = ray.GetPoint(rayDistance);
            lastMouseWorldPosition = mouseWorldPosition;
            
            // Ignorar altura Y - rotação apenas no eixo Y como em PoE
            Vector3 playerPosition = transform.position;
            mouseWorldPosition.y = playerPosition.y;
            
            // Direção do jogador para o mouse
            Vector3 lookDirection = mouseWorldPosition - playerPosition;
            
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                // Criar rotação olhando para o ponto do mouse (apenas no eixo Y)
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                
                // Aplicar rotação suave
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }

    private void HandleMovement()
    {
        // Input do jogador (WASD)
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical");     // W/S
        
        // Criar direção de input baseada no WASD
        inputDirection = new Vector3(horizontal, 0, vertical).normalized;
        
        // Verifica se há input de movimento
        bool hasMovementInput = inputDirection.magnitude > 0.1f;
        
        if (hasMovementInput)
        {
            // Converter input para direção mundial baseada na câmera
            ConvertInputToWorldDirection();
            
            // Calcular direção relativa ao personagem (para animações)
            CalculateRelativeMovementDirections();
            
            // Calcular velocidade baseada na direção
            float currentSpeed = CalculateMovementSpeed();
            
            // Aplicar movimento
            characterController.Move(worldMoveDirection * currentSpeed * Time.deltaTime);
            
            // Animar movimento
            UpdateMovementAnimation();
            
            wasPreviouslyMoving = true;
        }
        else
        {
            // Parado
            inputDirection = Vector3.zero;
            worldMoveDirection = Vector3.zero;
            
            // Resetar flags de direção
            isMovingForward = false;
            isMovingBackward = false;
            isMovingLeft = false;
            isMovingRight = false;
            
            if (animator != null)
            {
                animator.SetFloat("Speed", 0);
                animator.SetFloat("Forward", 0);
                animator.SetFloat("Right", 0);
                animator.SetBool("IsMoving", false);
            }
            
            wasPreviouslyMoving = false;
        }
        
        // Aplicar gravidade se não estiver no chão
        if (!characterController.isGrounded)
        {
            characterController.Move(Physics.gravity * Time.deltaTime);
        }
    }
    
    private void ConvertInputToWorldDirection()
    {
        // Verificar se a câmera existe
        if (mainCamera == null)
        {
            // Fallback: usar direção baseada no transform do jogador
            worldMoveDirection = transform.TransformDirection(inputDirection);
            return;
        }
        
        // Obter direções da câmera (sem a rotação Y)
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        
        // Garantir que estamos movendo no plano X/Z apenas
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();
        
        // Calcular direção mundial final
        worldMoveDirection = (cameraForward * inputDirection.z + cameraRight * inputDirection.x).normalized;
    }
    
    private void CalculateRelativeMovementDirections()
    {
        // Calcular direção relativa ao forward do personagem
        Vector3 playerForward = transform.forward;
        Vector3 playerRight = transform.right;
        
        // Produto escalar para determinar direção relativa
        float forwardDot = Vector3.Dot(worldMoveDirection, playerForward);
        float rightDot = Vector3.Dot(worldMoveDirection, playerRight);
        
        // Determinar direções (com threshold para evitar oscilação)
        isMovingForward = forwardDot > 0.5f;
        isMovingBackward = forwardDot < -0.5f;
        isMovingRight = rightDot > 0.5f;
        isMovingLeft = rightDot < -0.5f;
    }
    
    private float CalculateMovementSpeed()
    {
        float speed = moveSpeed;
        
        // Aplicar modificadores de velocidade baseados na direção
        if (isMovingBackward)
        {
            speed *= backwardSpeedMultiplier;
        }
        else if (isMovingLeft || isMovingRight)
        {
            // Se está se movendo principalmente para os lados (strafe)
            if (!isMovingForward && !isMovingBackward)
            {
                speed *= strafeSpeedMultiplier;
            }
        }
        
        return speed;
    }
    
    private void UpdateMovementAnimation()
    {
        if (animator == null) return;
        
        // Configurar parâmetros do Animator baseados na direção relativa
        float animationSpeed = worldMoveDirection.magnitude;
        
        // Valores para blend tree de movimento
        float forwardValue = 0f;
        float rightValue = 0f;
        
        if (isMovingForward)
        {
            forwardValue = 1f;
        }
        else if (isMovingBackward)
        {
            forwardValue = -1f;
        }
        
        if (isMovingRight)
        {
            rightValue = 1f;
        }
        else if (isMovingLeft)
        {
            rightValue = -1f;
        }
        
        // Definir parâmetros do Animator
        animator.SetFloat("Speed", animationSpeed);
        animator.SetFloat("Forward", forwardValue);
        animator.SetFloat("Right", rightValue);
        animator.SetBool("IsMoving", animationSpeed > 0.1f);
        
        // Se começou a mover agora
        if (!wasPreviouslyMoving)
        {
            animator.SetBool("IsMoving", true);
        }
    }

    // Gizmos para debug
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && worldMoveDirection.sqrMagnitude > 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, worldMoveDirection * 2);
        }
        
        if (Application.isPlaying && inputDirection.sqrMagnitude > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, inputDirection * 2);
        }
    }

    // Getters para acesso externo
    public Vector3 GetLastMouseWorldPosition() => lastMouseWorldPosition;
    public bool IsMoving() => wasPreviouslyMoving;
}