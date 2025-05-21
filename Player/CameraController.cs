using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Alvo e Posicionamento")]
    public Transform target;                // O alvo que a câmera seguirá (geralmente o jogador)
    public float distance = 10f;            // Distância inicial da câmera até o alvo
    public float minDistance = 5f;          // Distância mínima para zoom
    public float maxDistance = 20f;         // Distância máxima para zoom
    public float height = 5f;               // Altura inicial da câmera acima do alvo
    public float minHeight = 2f;            // Altura mínima permitida
    public float maxHeight = 15f;           // Altura máxima permitida
    
    [Header("Controles")]
    public float rotationSpeed = 100f;      // Velocidade de rotação com o mouse
    public float zoomSpeed = 5f;            // Velocidade de zoom com o scroll
    public float smoothSpeed = 0.125f;      // Suavidade do movimento da câmera (menor = mais suave)
    public float heightAdjustSpeed = 3f;    // Velocidade de ajuste de altura (com Shift+Scroll)
    
    [Header("Botões")]
    public KeyCode rotateButton = KeyCode.Mouse1;  // Botão DIREITO do mouse para rotacionar a câmera
    public KeyCode heightAdjustKey = KeyCode.LeftShift; // Tecla para ajustar altura em vez de zoom
    
    // Variáveis de controle interno
    private float currentRotation = 0f;     // Rotação atual ao redor do alvo
    private Vector3 desiredPosition;        // Posição alvo para suavização
    private bool isDragging = false;        // Controla se o jogador está arrastando o mouse
    private Vector3 previousMousePosition;  // Posição anterior do mouse para calcular o delta
    
    private void Start()
    {
        // Se nenhum alvo for definido, procurar pelo jogador
        if (target == null)
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                target = player.transform;
                Debug.Log("Câmera: Alvo definido automaticamente para o jogador");
            }
            else
            {
                Debug.LogWarning("Câmera: Nenhum alvo encontrado! Por favor, defina um alvo no Inspector.");
            }
        }
        
        // Posicionar a câmera inicialmente
        UpdateCameraPosition();
    }
    
    private void LateUpdate()
    {
        if (target == null)
            return;
        
        // Controle de rotação com o botão DIREITO do mouse
        HandleRotation();
        
        // Controle de zoom com o scroll do mouse
        HandleZoom();
        
        // Movimento suave para a posição desejada
        UpdateCameraPosition();
    }
    
    private void HandleRotation()
    {
        // Iniciar rotação quando o botão DIREITO é pressionado
        if (Input.GetKeyDown(rotateButton))
        {
            isDragging = true;
            previousMousePosition = Input.mousePosition;
        }
        
        // Terminar rotação quando o botão é solto
        if (Input.GetKeyUp(rotateButton))
        {
            isDragging = false;
        }
        
        // Executar rotação enquanto o botão está pressionado
        if (isDragging)
        {
            Vector3 currentMousePosition = Input.mousePosition;
            Vector3 mouseDelta = currentMousePosition - previousMousePosition;
            
            // Rotação horizontal ao redor do alvo
            currentRotation -= mouseDelta.x * rotationSpeed * Time.deltaTime;
            
            // Limitar a rotação para ficar entre 0 e 360 graus
            currentRotation = currentRotation % 360f;
            
            previousMousePosition = currentMousePosition;
        }
    }
    
    private void HandleZoom()
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        
        if (scrollDelta != 0)
        {
            if (Input.GetKey(heightAdjustKey))
            {
                // Ajustar altura se a tecla shift estiver pressionada
                height -= scrollDelta * heightAdjustSpeed;
                height = Mathf.Clamp(height, minHeight, maxHeight);
            }
            else
            {
                // Ajustar distância (zoom)
                distance -= scrollDelta * zoomSpeed;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }
        }
    }
    
    private void UpdateCameraPosition()
    {
        if (target == null)
            return;
        
        // Calcular posição da câmera baseada na rotação, distância e altura
        float radians = currentRotation * Mathf.Deg2Rad;
        float x = target.position.x + distance * Mathf.Sin(radians);
        float z = target.position.z + distance * Mathf.Cos(radians);
        
        desiredPosition = new Vector3(x, target.position.y + height, z);
        
        // Movimento suave para a posição calculada
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        // Fazer a câmera olhar para o alvo
        transform.LookAt(target.position);
    }
    
    public void ResetCamera()
    {
        // Resetar para os valores padrão
        currentRotation = 0f;
        UpdateCameraPosition();
    }
}