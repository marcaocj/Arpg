using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    // Referência ao componente de texto
    private TextMeshProUGUI textMesh;
    
    // Variáveis para animação e movimento
    private float moveYSpeed;
    private float disappearTimer;
    private float disappearSpeed;
    private Color textColor;
    private Vector3 initialScale = new Vector3(1, 1, 1);
    private Vector3 targetScale = new Vector3(1.5f, 1.5f, 1.5f);
    private Vector3 moveDirection;
    
    // Variáveis para tipos diferentes de dano
    private bool isCriticalHit;
    private bool isHealAmount;
    
    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        if (textMesh != null)
        {
            textColor = textMesh.color;
        }

        // Load config values
        var config = GameConfig.Instance;
        moveYSpeed = config.damagePopupMoveSpeed;
        disappearSpeed = config.damagePopupDisappearSpeed;
    }
    
    public void Setup(int damageAmount, bool isCritical, bool isHeal = false, Color? customColor = null)
    {
        string displayText = damageAmount.ToString();
        isCriticalHit = isCritical;
        isHealAmount = isHeal;
        
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshProUGUI>();
        }
        
        // Usar cor customizada se fornecida
        if (customColor.HasValue)
        {
            textMesh.color = customColor.Value;
            textColor = customColor.Value;
        }
        else
        {
            // Configurar o texto baseado no tipo de dano
            if (isHealAmount)
            {
                // Verde para cura
                textMesh.color = new Color(0.2f, 0.8f, 0.2f);
                textColor = textMesh.color;
                displayText = "+" + displayText;
            }
            else if (isCriticalHit)
            {
                // Vermelho brilhante para crítico
                textMesh.color = new Color(1f, 0.1f, 0.1f);
                textColor = textMesh.color;
                displayText = displayText + "!";
                // Críticos são maiores
                initialScale = new Vector3(1.5f, 1.5f, 1.5f);
                targetScale = new Vector3(2.2f, 2.2f, 2.2f);
            }
            else
            {
                // Vermelho normal para dano comum
                textMesh.color = new Color(0.9f, 0.3f, 0.3f);
                textColor = textMesh.color;
            }
        }
        
        // Formatação especial para diferentes tipos
        if (isHealAmount && !customColor.HasValue)
        {
            displayText = "+" + displayText;
        }
        else if (isCriticalHit && !customColor.HasValue)
        {
            displayText = displayText + "!";
            // Críticos são maiores
            initialScale = new Vector3(1.5f, 1.5f, 1.5f);
            targetScale = new Vector3(2.2f, 2.2f, 2.2f);
        }
        
        textMesh.text = displayText;
        
        // Configurar a direção do movimento (leve aleatoriedade para evitar sobreposição)
        moveDirection = new Vector3(Random.Range(-0.5f, 0.5f), 1, 0).normalized;
        
        // Configurar o tempo de desaparecimento
        disappearTimer = GameConfig.Instance.damagePopupLifetime;
        
        // Aplicar escala inicial
        transform.localScale = initialScale;
    }
    
    private void Update()
    {
        // Movimento para cima
        transform.position += moveDirection * moveYSpeed * Time.deltaTime;
        
        // Efeito de escala (aumenta ligeiramente e depois diminui)
        if (disappearTimer > 0.5f)
        {
            // Primeira metade da animação - aumenta
            float scalePercent = (1f - (disappearTimer - 0.5f) * 2);
            transform.localScale = Vector3.Lerp(initialScale, targetScale, scalePercent);
        }
        else
        {
            // Segunda metade da animação - diminui
            float scalePercent = disappearTimer * 2;
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, scalePercent);
        }
        
        // Efeito de desaparecimento gradual
        disappearTimer -= Time.deltaTime;
        if (disappearTimer <= 0)
        {
            // Aumentar a transparência do texto
            textColor.a -= disappearSpeed * Time.deltaTime;
            if (textMesh != null)
            {
                textMesh.color = textColor;
            }
            
            if (textColor.a <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
    
    // Método estático para criar um popup de dano facilmente - OTIMIZADO
    public static DamagePopup Create(Vector3 worldPosition, int damageAmount, bool isCritical, bool isHeal = false, Color? customColor = null)
    {
        // Obter o prefab através do evento - sem FindObjectOfType
        if (DamagePopupManager.Instance == null)
        {
            Debug.LogError("DamagePopupManager.Instance é null!");
            return null;
        }
        
        GameObject damagePopupPrefab = DamagePopupManager.Instance.damagePopupPrefab;
        
        // Verificar se o prefab está disponível
        if (damagePopupPrefab == null)
        {
            Debug.LogError("Prefab de DamagePopup não configurado no DamagePopupManager!");
            return null;
        }
        
        // Obter a câmera principal - cached no manager se possível
        Camera gameCamera = Camera.main;
        if (gameCamera == null)
        {
            Debug.LogError("Camera principal não encontrada!");
            return null;
        }
        
        // Converter posição do mundo para posição na tela/canvas
        Vector3 screenPos = gameCamera.WorldToScreenPoint(worldPosition);
        
        // Instanciar o prefab dentro do canvas
        GameObject damagePopupObject = Instantiate(
            damagePopupPrefab, 
            screenPos, 
            Quaternion.identity, 
            DamagePopupManager.Instance.canvasTransform
        );
        
        // Obter o componente DamagePopup e configurá-lo
        DamagePopup damagePopup = damagePopupObject.GetComponent<DamagePopup>();
        if (damagePopup != null)
        {
            damagePopup.Setup(damageAmount, isCritical, isHeal, customColor);
        }
        
        return damagePopup;
    }
}