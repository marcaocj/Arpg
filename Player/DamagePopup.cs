using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    // Referência ao componente de texto
    private TextMeshProUGUI textMesh;
    
    // Variáveis para animação e movimento
    private float moveYSpeed = 1.5f;
    private float disappearTimer;
    private float disappearSpeed = 3f;
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
        textColor = textMesh.color;
    }
    
    public void Setup(int damageAmount, bool isCritical, bool isHeal = false)
    {
        string displayText = damageAmount.ToString();
        isCriticalHit = isCritical;
        isHealAmount = isHeal;
        
        // Configurar o texto baseado no tipo de dano
        if (isHealAmount)
        {
            // Verde para cura
            textMesh.color = new Color(0.2f, 0.8f, 0.2f);
            displayText = "+" + displayText;
        }
        else if (isCriticalHit)
        {
            // Vermelho brilhante para crítico
            textMesh.color = new Color(1f, 0.1f, 0.1f);
            displayText = displayText + "!";
            // Críticos são maiores
            initialScale = new Vector3(1.5f, 1.5f, 1.5f);
            targetScale = new Vector3(2.2f, 2.2f, 2.2f);
        }
        else
        {
            // Vermelho normal para dano comum
            textMesh.color = new Color(0.9f, 0.3f, 0.3f);
        }
        
        textMesh.text = displayText;
        
        // Configurar a direção do movimento (leve aleatoriedade para evitar sobreposição)
        moveDirection = new Vector3(Random.Range(-0.5f, 0.5f), 1, 0).normalized;
        
        // Configurar o tempo de desaparecimento
        disappearTimer = 1f;
        
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
            textMesh.color = textColor;
            
            if (textColor.a <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
    
    // Método estático para criar um popup de dano facilmente
    public static DamagePopup Create(Vector3 worldPosition, int damageAmount, bool isCritical, bool isHeal = false)
    {
        // Obter o prefab do DamagePopup a partir do DamagePopupManager
        GameObject damagePopupPrefab = DamagePopupManager.Instance.damagePopupPrefab;
        
        // Verificar se o prefab está disponível
        if (damagePopupPrefab == null)
        {
            Debug.LogError("Prefab de DamagePopup não configurado no DamagePopupManager!");
            return null;
        }
        
        // Obter a câmera para conversão de posição de mundo para tela
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
        damagePopup.Setup(damageAmount, isCritical, isHeal);
        
        return damagePopup;
    }
}