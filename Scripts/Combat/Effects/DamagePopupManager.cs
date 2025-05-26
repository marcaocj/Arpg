using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    // Singleton para acesso fácil
    public static DamagePopupManager Instance { get; private set; }
    
    // Prefab do popup de dano (deve ser configurado no Inspector)
    public GameObject damagePopupPrefab;
    
    // Referência ao Canvas pai dos popups
    public Transform canvasTransform;
    
    // Altura adicional para os popups (offset acima dos personagens)
    public float heightOffset = 1.5f;
    
    private void Awake()
    {
        // Configuração do singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Verificar dependências
        if (damagePopupPrefab == null)
        {
            Debug.LogError("DamagePopupManager: damagePopupPrefab não configurado!");
        }
        
        if (canvasTransform == null)
        {
            // Tentar encontrar automaticamente o canvas principal
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas != null)
            {
                canvasTransform = mainCanvas.transform;
                Debug.Log("DamagePopupManager: Canvas encontrado automaticamente.");
            }
            else
            {
                Debug.LogError("DamagePopupManager: canvasTransform não configurado e nenhum Canvas encontrado!");
            }
        }
    }
    
    private void Start()
    {
        // Registrar para eventos de popup de dano
        SubscribeToEvents();
    }
    
    private void SubscribeToEvents()
    {
        EventManager.Subscribe<DamagePopupRequestEvent>(OnDamagePopupRequested);
        EventManager.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        // REMOVIDO: DamageDealtEvent para evitar duplicação
        // REMOVIDO: EnemyTakeDamageEvent para evitar duplicação
    }
    
    private void OnDestroy()
    {
        // Desregistrar eventos
        EventManager.Unsubscribe<DamagePopupRequestEvent>(OnDamagePopupRequested);
        EventManager.Unsubscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    #region Event Handlers
    
    private void OnDamagePopupRequested(DamagePopupRequestEvent eventData)
    {
        // Adicionar offset de altura
        Vector3 popupPosition = eventData.worldPosition + Vector3.up * heightOffset;
        
        // Criar popup com cor customizada
        DamagePopup.Create(
            popupPosition,
            eventData.amount,
            eventData.isCritical,
            eventData.isHeal,
            eventData.customColor // Passar a cor customizada!
        );
        
        Debug.Log($"Popup criado: {eventData.amount} {(eventData.isHeal ? "cura" : "dano")} {(eventData.isCritical ? "crítico" : "")}");
    }
    
    private void OnPlayerHealthChanged(PlayerHealthChangedEvent eventData)
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null) return;
        
        // Mostrar popup de CURA quando vida aumenta (delta positivo)
        if (eventData.healthDelta > 0)
        {
            ShowHealing(player.transform.position, eventData.healthDelta);
            Debug.Log($"Popup de cura criado: +{eventData.healthDelta} HP no player");
        }
        // REMOVIDO: Popup de dano no player - será criado pelo PlayerCombat quando necessário
    }
    
    #endregion
    
    #region Public Methods (Mantidos para compatibilidade)
    
    /// <summary>
    /// Método para mostrar dano sobre uma entidade
    /// COR PADRÃO: LARANJA para dano normal, VERMELHO para crítico
    /// </summary>
    public void ShowDamage(Vector3 worldPosition, int damageAmount, bool isCritical = false)
    {
        Color damageColor = isCritical ? Color.red : new Color(1f, 0.65f, 0f); // Laranja para dano normal
        
        EventManager.TriggerEvent(new DamagePopupRequestEvent
        {
            worldPosition = worldPosition,
            amount = damageAmount,
            isCritical = isCritical,
            isHeal = false,
            customColor = damageColor
        });
    }
    
    /// <summary>
    /// Método para mostrar cura sobre uma entidade
    /// </summary>
    public void ShowHealing(Vector3 worldPosition, int healAmount)
    {
        EventManager.TriggerEvent(new DamagePopupRequestEvent
        {
            worldPosition = worldPosition,
            amount = healAmount,
            isCritical = false,
            isHeal = true,
            customColor = Color.green // VERDE para cura
        });
    }
    
    /// <summary>
    /// Método para mostrar popup customizado
    /// </summary>
    public void ShowCustomPopup(Vector3 worldPosition, int amount, Color color, bool isCritical = false)
    {
        EventManager.TriggerEvent(new DamagePopupRequestEvent
        {
            worldPosition = worldPosition,
            amount = amount,
            isCritical = isCritical,
            isHeal = false,
            customColor = color
        });
    }
    
    /// <summary>
    /// Método para mostrar popup de mana - COR CIANO
    /// </summary>
    public void ShowManaGain(Vector3 worldPosition, int manaAmount)
    {
        EventManager.TriggerEvent(new DamagePopupRequestEvent
        {
            worldPosition = worldPosition,
            amount = manaAmount,
            isCritical = false,
            isHeal = false,
            customColor = Color.cyan // CIANO para mana
        });
    }
    
    /// <summary>
    /// Método para mostrar popup de texto customizado
    /// </summary>
    public void ShowTextPopup(Vector3 worldPosition, string text, Color color)
    {
        // Para textos customizados, você pode estender o DamagePopup
        // ou criar um sistema separado
        Debug.Log($"Popup de texto: {text} em {worldPosition}");
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Verifica se o sistema está configurado corretamente
    /// </summary>
    public bool IsConfigured()
    {
        return damagePopupPrefab != null && canvasTransform != null;
    }
    
    /// <summary>
    /// Configura automaticamente as dependências se possível
    /// </summary>
    public void AutoConfigure()
    {
        if (canvasTransform == null)
        {
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas != null)
            {
                canvasTransform = mainCanvas.transform;
                Debug.Log("DamagePopupManager: Canvas configurado automaticamente.");
            }
        }
        
        if (damagePopupPrefab == null)
        {
            Debug.LogWarning("DamagePopupManager: damagePopupPrefab ainda precisa ser configurado manualmente no Inspector.");
        }
    }
    
    #endregion
}