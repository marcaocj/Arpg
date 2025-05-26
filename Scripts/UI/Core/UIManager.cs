using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UIManager CORRIGIDO - Fixes para level text scale, health/mana bars e outros bugs da UI
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    public Slider healthBar;
    public Slider manaBar;
    public Slider experienceBar;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI goldText;
    
    [Header("Skills")]
    public Image[] skillIcons;
    public Image[] skillCooldowns;
    
    [Header("Inventory")]
    public GameObject inventoryPanel;
    public Transform itemContainer;
    public GameObject itemPrefab;
    
    [Header("Quest")]
    public TextMeshProUGUI questTitle;
    public TextMeshProUGUI questDescription;
    public Slider questProgress;
    
    [Header("Tooltip")]
    public GameObject tooltip;
    public TextMeshProUGUI tooltipTitle;
    public TextMeshProUGUI tooltipDescription;
    
    [Header("Notificações")]
    public GameObject notificationPrefab;
    public Transform notificationContainer;
    public float notificationDuration = 2f;
    
    [Header("Performance Settings")]
    public float uiUpdateInterval = 0.1f;
    public int maxNotifications = 5;
    
    [Header("Animation Fix Settings")]
    public bool fixAnimationBugs = true;
    public float maxLevelUpAnimationScale = 1.2f;
    public float levelUpAnimationDuration = 0.8f;
    
    // Cache para evitar FindObjectOfType - OTIMIZAÇÃO CRÍTICA
    private PlayerController cachedPlayer;
    private PlayerStats cachedPlayerStats;
    private PlayerHealthManager cachedPlayerHealth;
    private GameManager cachedGameManager;
    private bool playerCacheValid = false;
    private bool gameManagerCacheValid = false;
    
    // Cache timer para re-validação periódica
    private float cacheValidationTimer = 0f;
    private const float CACHE_VALIDATION_INTERVAL = 2f;
    
    // Cache para evitar updates desnecessários
    private int cachedHealth = -1;
    private int cachedMaxHealth = -1;
    private int cachedMana = -1;
    private int cachedMaxMana = -1;
    private int cachedExperience = -1;
    private int cachedLevel = -1;
    private int cachedGold = -1;
    
    // Timer para updates
    private float updateTimer = 0f;
    
    // Notification management
    private Queue<GameObject> notificationQueue = new Queue<GameObject>();
    private List<GameObject> activeNotifications = new List<GameObject>();
    
    // Tooltip management
    private bool tooltipVisible = false;
    private Coroutine tooltipCoroutine;
    
    // FIX: Controle de animações para evitar bugs
    private bool isLevelTextAnimating = false;
    private Vector3 originalLevelTextScale = Vector3.one;
    private Coroutine levelUpAnimationCoroutine;
    private bool hasInitializedLevelTextScale = false;
    
    // FIX: Controle de bars update
    private bool isUpdatingHealthBar = false;
    private bool isUpdatingManaBar = false;
    private Coroutine healthBarUpdateCoroutine;
    private Coroutine manaBarUpdateCoroutine;
    
    private void Start()
    {
        SubscribeToEvents();
        InitializeUI();
        
        // FIX: Salvar escala original do level text
        InitializeLevelTextScale();
        
        // Tentar cachear componentes
        TryCacheComponents();
        
        Invoke("DelayedFirstUpdate", 0.2f); // Aumentado delay para garantir inicialização
    }
    
    private void InitializeLevelTextScale()
    {
        if (levelText != null && !hasInitializedLevelTextScale)
        {
            originalLevelTextScale = levelText.transform.localScale;
            hasInitializedLevelTextScale = true;
            Debug.Log($"UIManager: Level text scale original salva: {originalLevelTextScale}");
        }
    }
    
    private void SubscribeToEvents()
    {
        // Registrar ouvintes para eventos do EventManager
        EventManager.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        EventManager.Subscribe<PlayerManaChangedEvent>(OnPlayerManaChanged);
        EventManager.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        EventManager.Subscribe<PlayerExperienceGainedEvent>(OnPlayerExperienceGained);
        EventManager.Subscribe<PlayerStatsRecalculatedEvent>(OnPlayerStatsRecalculated);
        EventManager.Subscribe<ItemAddedEvent>(OnItemAdded);
        EventManager.Subscribe<ItemEquippedEvent>(OnItemEquipped);
        EventManager.Subscribe<ItemUnequippedEvent>(OnItemUnequipped);
        EventManager.Subscribe<NotificationEvent>(OnNotificationRequested);
        EventManager.Subscribe<TooltipRequestEvent>(OnTooltipRequested);
        EventManager.Subscribe<GoldCollectedEvent>(OnGoldCollected);
        EventManager.Subscribe<InventoryFullEvent>(OnInventoryFull);
        EventManager.Subscribe<UIElementToggledEvent>(OnUIElementToggled);
        
        // Eventos para gerenciamento de cache
        EventManager.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        EventManager.Subscribe<PlayerDestroyedEvent>(OnPlayerDestroyed);
        EventManager.Subscribe<SceneTransitionEvent>(OnSceneTransition);
    }
    
    private void InitializeUI()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
        
        if (tooltip != null)
        {
            tooltip.SetActive(false);
        }
        
        // FIX: Inicializar barras com valores seguros
        if (healthBar != null)
        {
            healthBar.minValue = 0;
            healthBar.maxValue = 100;
            healthBar.value = 100;
        }
        
        if (manaBar != null)
        {
            manaBar.minValue = 0;
            manaBar.maxValue = 50;
            manaBar.value = 50;
        }
        
        if (experienceBar != null)
        {
            experienceBar.minValue = 0;
            experienceBar.maxValue = 100;
            experienceBar.value = 0;
        }
        
        // Inicializar lists e queues
        activeNotifications.Clear();
        
        while (notificationQueue.Count > 0)
        {
            notificationQueue.Dequeue();
        }
    }
    
    private void TryCacheComponents()
    {
        // Cache do Player usando singleton
        if (!playerCacheValid && PlayerController.Instance != null)
        {
            cachedPlayer = PlayerController.Instance;
            cachedPlayerStats = cachedPlayer.GetStats();
            cachedPlayerHealth = cachedPlayer.GetHealthManager();
            playerCacheValid = true;
            Debug.Log("UIManager: Player cacheado com sucesso");
        }
        
        // Cache do GameManager
        if (!gameManagerCacheValid && GameManager.instance != null)
        {
            cachedGameManager = GameManager.instance;
            gameManagerCacheValid = true;
            Debug.Log("UIManager: GameManager cacheado com sucesso");
        }
    }
    
    private void DelayedFirstUpdate()
    {
        ForceUpdateAllUI();
    }
    
    private void Update()
    {
        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0f)
        {
            // Tentar cachear se ainda não temos
            if (!playerCacheValid || !gameManagerCacheValid)
            {
                TryCacheComponents();
            }
            
            UpdateNonCriticalUI();
            updateTimer = uiUpdateInterval;
        }
        
        // Validação periódica do cache
        cacheValidationTimer -= Time.deltaTime;
        if (cacheValidationTimer <= 0f)
        {
            ValidateCache();
            cacheValidationTimer = CACHE_VALIDATION_INTERVAL;
        }
        
        // FIX: Verificação aprimorada de sincronização das barras
        if (playerCacheValid && cachedPlayerHealth != null)
        {
            // Verificar health bar
            int currentHealth = cachedPlayerHealth.CurrentHealth;
            int currentMaxHealth = cachedPlayerHealth.MaxHealth;
            
            if (healthBar != null && (currentHealth != cachedHealth || currentMaxHealth != cachedMaxHealth))
            {
                UpdateHealthBarSafely(currentHealth, currentMaxHealth);
            }
            
            // Verificar mana bar
            int currentMana = cachedPlayerHealth.CurrentMana;
            int currentMaxMana = cachedPlayerHealth.MaxMana;
            
            if (manaBar != null && (currentMana != cachedMana || currentMaxMana != cachedMaxMana))
            {
                UpdateManaBarSafely(currentMana, currentMaxMana);
            }
        }
        
        // Gerenciar notifications queue
        ProcessNotificationQueue();
    }
    
    private void ValidateCache()
    {
        // Validar cache do player
        if (playerCacheValid && (cachedPlayer == null || cachedPlayerStats == null))
        {
            playerCacheValid = false;
            cachedPlayer = null;
            cachedPlayerStats = null;
            cachedPlayerHealth = null;
            Debug.LogWarning("UIManager: Cache do player invalidado - objeto destruído");
        }
        
        // Validar cache do GameManager
        if (gameManagerCacheValid && cachedGameManager == null)
        {
            gameManagerCacheValid = false;
            cachedGameManager = null;
            Debug.LogWarning("UIManager: Cache do GameManager invalidado - objeto destruído");
        }
    }
    
    #region Event Handlers
    
    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        cachedPlayer = eventData.player.GetComponent<PlayerController>();
        if (cachedPlayer != null)
        {
            cachedPlayerStats = cachedPlayer.GetStats();
            cachedPlayerHealth = cachedPlayer.GetHealthManager();
            playerCacheValid = true;
            
            // Resetar cache de UI para forçar atualização
            ResetUICache();
            
            Debug.Log("UIManager: Novo player cacheado");
        }
    }
    
    private void OnPlayerDestroyed(PlayerDestroyedEvent eventData)
    {
        cachedPlayer = null;
        cachedPlayerStats = null;
        cachedPlayerHealth = null;
        playerCacheValid = false;
        
        // Resetar cache
        ResetUICache();
        
        Debug.Log("UIManager: Cache do player limpo");
    }
    
    private void OnSceneTransition(SceneTransitionEvent eventData)
    {
        // FIX: Limpar animações ao trocar de cena
        StopAllAnimations();
        
        // Invalidar todos os caches ao trocar de cena
        cachedPlayer = null;
        cachedPlayerStats = null;
        cachedPlayerHealth = null;
        cachedGameManager = null;
        playerCacheValid = false;
        gameManagerCacheValid = false;
        
        // Resetar cache de UI
        ResetUICache();
        
        // Limpar notifications
        ClearAllNotifications();
        
        Debug.Log("UIManager: Cache limpo devido à transição de cena");
    }
    
    private void OnPlayerHealthChanged(PlayerHealthChangedEvent eventData)
    {
        UpdateHealthBarSafely(eventData.currentHealth, eventData.maxHealth);
        
        // Animação visual para mudanças críticas de saúde
        if (eventData.healthDelta < 0) // Tomou dano
        {
            StartCoroutine(FlashHealthBar());
        }
    }
    
    private void OnPlayerManaChanged(PlayerManaChangedEvent eventData)
    {
        UpdateManaBarSafely(eventData.currentMana, eventData.maxMana);
    }
    
    private void OnPlayerLevelUp(PlayerLevelUpEvent eventData)
    {
        // FIX: Level text update with proper animation control
        UpdateLevelTextSafely(eventData.newLevel);
        
        // Mostrar notificação de level up
        ShowNotification($"LEVEL UP! Nível {eventData.newLevel}", NotificationType.LevelUp, 3f);
    }
    
    private void OnPlayerExperienceGained(PlayerExperienceGainedEvent eventData)
    {
        if (experienceBar != null)
        {
            // FIX: Animação suave da barra de experiência com bounds checking
            StartCoroutine(AnimateExperienceBarSafely(eventData.currentExperience, eventData.experienceToNextLevel));
            cachedExperience = eventData.currentExperience;
        }
    }
    
    private void OnPlayerStatsRecalculated(PlayerStatsRecalculatedEvent eventData)
    {
        // FIX: Forçar atualização das barras quando stats são recalculados
        cachedMaxHealth = -1;
        cachedMaxMana = -1;
        
        if (healthBar != null)
        {
            healthBar.maxValue = Mathf.Max(1, eventData.maxHealth); // Evitar maxValue = 0
        }
        
        if (manaBar != null)
        {
            manaBar.maxValue = Mathf.Max(1, eventData.maxMana); // Evitar maxValue = 0
        }
        
        // Forçar atualização imediata
        Invoke("ForceUpdateBars", 0.1f);
    }
    
    private void OnItemAdded(ItemAddedEvent eventData)
    {
        if (eventData.wasSuccessful)
        {
            UpdateInventoryUI();
            ShowNotification($"Item adicionado: {eventData.item.name}", NotificationType.ItemCollected);
        }
        else
        {
            ShowNotification(eventData.failureReason, NotificationType.Error);
        }
    }
    
    private void OnItemEquipped(ItemEquippedEvent eventData)
    {
        UpdateInventoryUI();
        ShowNotification($"Equipado: {eventData.item.name}", NotificationType.Success);
    }
    
    private void OnItemUnequipped(ItemUnequippedEvent eventData)
    {
        UpdateInventoryUI();
        ShowNotification($"Desequipado: {eventData.item.name}", NotificationType.Info);
    }
    
    private void OnNotificationRequested(NotificationEvent eventData)
    {
        ShowNotification(eventData.message, eventData.type, eventData.duration, eventData.color);
    }
    
    private void OnTooltipRequested(TooltipRequestEvent eventData)
    {
        if (eventData.show)
        {
            ShowTooltip(eventData.item, eventData.screenPosition);
        }
        else
        {
            HideTooltip();
        }
    }
    
    private void OnGoldCollected(GoldCollectedEvent eventData)
    {
        if (goldText != null && eventData.totalGold != cachedGold)
        {
            // Animação do texto de ouro
            StartCoroutine(AnimateGoldText(eventData.totalGold));
            cachedGold = eventData.totalGold;
        }
        
        ShowNotification($"+{eventData.amount} ouro", NotificationType.Success, 1.5f);
    }
    
    private void OnInventoryFull(InventoryFullEvent eventData)
    {
        ShowNotification("Inventário cheio!", NotificationType.Warning);
    }
    
    private void OnUIElementToggled(UIElementToggledEvent eventData)
    {
        // Gerenciar diferentes elementos de UI
        switch (eventData.elementName)
        {
            case "Inventory":
                if (inventoryPanel != null)
                {
                    inventoryPanel.SetActive(eventData.isVisible);
                    if (eventData.isVisible)
                    {
                        UpdateInventoryUI();
                    }
                }
                break;
                
            case "Tooltip":
                if (tooltip != null)
                {
                    tooltip.SetActive(eventData.isVisible);
                }
                break;
                
            case "AllUI":
                // Toggle geral da UI
                ToggleAllUI(eventData.isVisible);
                break;
        }
    }
    
    #endregion
    
    #region Fixed UI Update Methods
    
    private void UpdateHealthBarSafely(int currentHealth, int maxHealth)
    {
        if (healthBar == null || isUpdatingHealthBar) return;
        
        // Validar valores
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Verificar mudanças
        bool hasChanged = (currentHealth != cachedHealth || maxHealth != cachedMaxHealth);
        
        if (hasChanged)
        {
            cachedHealth = currentHealth;
            cachedMaxHealth = maxHealth;
            
            // Parar coroutine anterior se existir
            if (healthBarUpdateCoroutine != null)
            {
                StopCoroutine(healthBarUpdateCoroutine);
            }
            
            // Iniciar nova atualização
            healthBarUpdateCoroutine = StartCoroutine(UpdateHealthBarCoroutine(currentHealth, maxHealth));
        }
    }
    
    private IEnumerator UpdateHealthBarCoroutine(int targetHealth, int maxHealth)
    {
        isUpdatingHealthBar = true;
        
        yield return new WaitForEndOfFrame(); // Aguardar um frame
        
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            
            // Animação suave se a diferença for significativa
            float currentValue = healthBar.value;
            float targetValue = targetHealth;
            
            if (Mathf.Abs(currentValue - targetValue) > 1f)
            {
                float duration = 0.3f;
                float elapsed = 0f;
                
                while (elapsed < duration && healthBar != null)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / duration;
                    healthBar.value = Mathf.Lerp(currentValue, targetValue, progress);
                    yield return null;
                }
                
                if (healthBar != null)
                {
                    healthBar.value = targetValue;
                }
            }
            else
            {
                healthBar.value = targetValue;
            }
        }
        
        isUpdatingHealthBar = false;
        healthBarUpdateCoroutine = null;
    }
    
    private void UpdateManaBarSafely(int currentMana, int maxMana)
    {
        if (manaBar == null || isUpdatingManaBar) return;
        
        // Validar valores
        maxMana = Mathf.Max(1, maxMana);
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
        
        // Verificar mudanças
        bool hasChanged = (currentMana != cachedMana || maxMana != cachedMaxMana);
        
        if (hasChanged)
        {
            cachedMana = currentMana;
            cachedMaxMana = maxMana;
            
            // Parar coroutine anterior se existir
            if (manaBarUpdateCoroutine != null)
            {
                StopCoroutine(manaBarUpdateCoroutine);
            }
            
            // Iniciar nova atualização
            manaBarUpdateCoroutine = StartCoroutine(UpdateManaBarCoroutine(currentMana, maxMana));
        }
    }
    
    private IEnumerator UpdateManaBarCoroutine(int targetMana, int maxMana)
    {
        isUpdatingManaBar = true;
        
        yield return new WaitForEndOfFrame(); // Aguardar um frame
        
        if (manaBar != null)
        {
            manaBar.maxValue = maxMana;
            
            // Animação suave se a diferença for significativa
            float currentValue = manaBar.value;
            float targetValue = targetMana;
            
            if (Mathf.Abs(currentValue - targetValue) > 1f)
            {
                float duration = 0.2f;
                float elapsed = 0f;
                
                while (elapsed < duration && manaBar != null)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / duration;
                    manaBar.value = Mathf.Lerp(currentValue, targetValue, progress);
                    yield return null;
                }
                
                if (manaBar != null)
                {
                    manaBar.value = targetValue;
                }
            }
            else
            {
                manaBar.value = targetValue;
            }
        }
        
        isUpdatingManaBar = false;
        manaBarUpdateCoroutine = null;
    }
    
    private void UpdateLevelTextSafely(int newLevel)
    {
        if (levelText == null) return;
        
        // Verificar mudança
        if (newLevel != cachedLevel)
        {
            cachedLevel = newLevel;
            levelText.text = "Level " + newLevel;
            
            // FIX: Controlar animação para evitar scale gigante
            if (fixAnimationBugs)
            {
                StartLevelUpAnimationSafely();
            }
        }
    }
    
    private void StartLevelUpAnimationSafely()
    {
        if (isLevelTextAnimating || levelText == null) return;
        
        // Parar animação anterior se existir
        if (levelUpAnimationCoroutine != null)
        {
            StopCoroutine(levelUpAnimationCoroutine);
        }
        
        // Garantir que temos a escala original
        if (!hasInitializedLevelTextScale)
        {
            InitializeLevelTextScale();
        }
        
        // Resetar escala antes de animar
        levelText.transform.localScale = originalLevelTextScale;
        
        // Iniciar nova animação
        levelUpAnimationCoroutine = StartCoroutine(AnimateLevelUpTextSafely());
    }
    
    private void ForceUpdateBars()
    {
        if (playerCacheValid && cachedPlayerHealth != null)
        {
            UpdateHealthBarSafely(cachedPlayerHealth.CurrentHealth, cachedPlayerHealth.MaxHealth);
            UpdateManaBarSafely(cachedPlayerHealth.CurrentMana, cachedPlayerHealth.MaxMana);
        }
    }
    
    #endregion
    
    #region UI Updates
    
    private void UpdateNonCriticalUI()
    {
        // Atualizar ouro se mudou - usando cache
        if (goldText != null && gameManagerCacheValid && cachedGameManager != null)
        {
            int currentGold = cachedGameManager.goldCollected;
            if (currentGold != cachedGold)
            {
                goldText.text = currentGold.ToString();
                cachedGold = currentGold;
            }
        }
        
        // Atualizar skills se temos referências
        UpdateSkillIcons();
    }
    
    private void UpdateSkillIcons()
    {
        if (!playerCacheValid || cachedPlayer == null || skillIcons == null || skillIcons.Length == 0)
            return;
        
        var skillController = cachedPlayer.GetSkillController();
        if (skillController == null) return;
        
        var skills = skillController.GetAllSkills();
        if (skills == null) return;
        
        for (int i = 0; i < skillIcons.Length; i++)
        {
            if (skillIcons[i] != null)
            {
                if (i < skills.Count)
                {
                    skillIcons[i].gameObject.SetActive(true);
                    // Configurar cor baseada no tipo de skill
                    skillIcons[i].color = GetSkillTypeColor(skills[i].type);
                }
                else
                {
                    skillIcons[i].gameObject.SetActive(false);
                }
            }
            
            // Atualizar cooldown overlays
            if (skillCooldowns != null && i < skillCooldowns.Length && skillCooldowns[i] != null)
            {
                if (i < skills.Count && skillController.IsSkillOnCooldown(i))
                {
                    float remaining = skillController.GetSkillCooldownRemaining(i);
                    float total = skills[i].GetActualCooldown(cachedPlayerStats);
                    
                    skillCooldowns[i].gameObject.SetActive(true);
                    skillCooldowns[i].fillAmount = remaining / total;
                }
                else
                {
                    skillCooldowns[i].gameObject.SetActive(false);
                }
            }
        }
    }
    
    public void ForceUpdateAllUI()
    {
        if (!playerCacheValid || cachedPlayerStats == null || cachedPlayerHealth == null) 
        {
            TryCacheComponents();
            if (!playerCacheValid) return;
        }
        
        // Resetar cache para forçar atualização
        ResetUICache();
        
        // FIX: Atualizar usando dados reais do PlayerHealthManager
        UpdateHealthBarSafely(cachedPlayerHealth.CurrentHealth, cachedPlayerHealth.MaxHealth);
        UpdateManaBarSafely(cachedPlayerHealth.CurrentMana, cachedPlayerHealth.MaxMana);
        UpdateLevelTextSafely(cachedPlayerStats.Level);
        
        UpdateNonCriticalUI();
        
        Debug.Log("UIManager: Force update completo realizado");
    }
    
    public void RefreshUI()
    {
        ForceUpdateAllUI();
    }
    
    public void UpdateInventoryUI()
    {
        if (!playerCacheValid || itemContainer == null)
        {
            return;
        }
        
        // Limpar container
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Criar elementos de UI para cada item usando cache
        if (cachedPlayer?.inventory?.items != null)
        {
            foreach (Item item in cachedPlayer.inventory.items)
            {
                GameObject itemUI = Instantiate(itemPrefab, itemContainer);
                InventoryItemUI itemUIComponent = itemUI.GetComponent<InventoryItemUI>();
                
                if (itemUIComponent != null)
                {
                    itemUIComponent.Initialize(item, this);
                }
            }
        }
    }
    
    private void ResetUICache()
    {
        cachedHealth = -1;
        cachedMaxHealth = -1;
        cachedMana = -1;
        cachedMaxMana = -1;
        cachedExperience = -1;
        cachedLevel = -1;
        cachedGold = -1;
    }
    
    #endregion
    
    #region Notifications
    
    private void ShowNotification(string message, NotificationType type, float duration = -1f, Color? customColor = null)
    {
        if (notificationPrefab == null || notificationContainer == null) return;
        
        if (duration < 0) duration = notificationDuration;
        
        GameObject notification = Instantiate(notificationPrefab, notificationContainer);
        TextMeshProUGUI text = notification.GetComponentInChildren<TextMeshProUGUI>();
        
        if (text != null)
        {
            text.text = message;
            text.color = customColor ?? GetNotificationColor(type);
        }
        
        // Adicionar à lista de notifications ativas
        activeNotifications.Add(notification);
        
        // Remover notifications antigas se excedeu o limite
        while (activeNotifications.Count > maxNotifications)
        {
            GameObject oldNotification = activeNotifications[0];
            activeNotifications.RemoveAt(0);
            if (oldNotification != null)
            {
                Destroy(oldNotification);
            }
        }
        
        StartCoroutine(DestroyNotificationAfterDelay(notification, duration));
    }
    
    private void ProcessNotificationQueue()
    {
        // Processar fila de notifications se necessário
        // (Para futuras expansões do sistema)
    }
    
    private void ClearAllNotifications()
    {
        foreach (GameObject notification in activeNotifications)
        {
            if (notification != null)
            {
                Destroy(notification);
            }
        }
        activeNotifications.Clear();
        
        while (notificationQueue.Count > 0)
        {
            GameObject notification = notificationQueue.Dequeue();
            if (notification != null)
            {
                Destroy(notification);
            }
        }
    }
    
    private Color GetNotificationColor(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Info:
                return Color.white;
            case NotificationType.Success:
                return Color.green;
            case NotificationType.Warning:
                return Color.yellow;
            case NotificationType.Error:
                return Color.red;
            case NotificationType.ItemCollected:
                return Color.cyan;
            case NotificationType.LevelUp:
                return Color.yellow;
            case NotificationType.QuestComplete:
                return Color.green;
            default:
                return Color.white;
        }
    }
    
    private Color GetSkillTypeColor(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Physical:
                return new Color(0.9f, 0.9f, 0.9f);
            case SkillType.Fire:
                return new Color(1f, 0.4f, 0.2f);
            case SkillType.Ice:
                return new Color(0.4f, 0.8f, 1f);
            case SkillType.Lightning:
                return new Color(1f, 1f, 0.4f);
            case SkillType.Poison:
                return new Color(0.6f, 1f, 0.4f);
            default:
                return Color.white;
        }
    }
    
    private IEnumerator DestroyNotificationAfterDelay(GameObject notification, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (notification != null)
        {
            activeNotifications.Remove(notification);
            Destroy(notification);
        }
    }
    
    #endregion
    
    #region Tooltips
    
    public void ShowTooltip(Item item, Vector3 position)
    {
        if (tooltip == null || tooltipTitle == null || tooltipDescription == null)
        {
            return;
        }
        
        tooltipTitle.text = item.name;
        
        string description = item.description + "\n";
        
        if (item.type == ItemType.Weapon)
        {
            description += "Dano Físico: " + item.physicalDamage + "\n";
            if (item.fireDamage > 0) description += "Dano de Fogo: " + item.fireDamage + "\n";
            if (item.iceDamage > 0) description += "Dano de Gelo: " + item.iceDamage + "\n";
            if (item.lightningDamage > 0) description += "Dano Elétrico: " + item.lightningDamage + "\n";
            if (item.poisonDamage > 0) description += "Dano de Veneno: " + item.poisonDamage + "\n";
        }
        
        if (item.strengthModifier > 0) description += "Força: +" + item.strengthModifier + "\n";
        if (item.intelligenceModifier > 0) description += "Inteligência: +" + item.intelligenceModifier + "\n";
        if (item.dexterityModifier > 0) description += "Destreza: +" + item.dexterityModifier + "\n";
        if (item.vitalityModifier > 0) description += "Vitalidade: +" + item.vitalityModifier + "\n";
        
        if (item.type == ItemType.Consumable)
        {
            if (item.healthRestore > 0) description += "Recupera " + item.healthRestore + " de vida\n";
            if (item.manaRestore > 0) description += "Recupera " + item.manaRestore + " de mana\n";
        }
        
        string rarityText = GetRarityColoredText(item.rarity);
        
        description += "Nível: " + item.level + "\n";
        description += rarityText;
        
        tooltipDescription.text = description;
        
        tooltip.transform.position = position;
        tooltip.SetActive(true);
        tooltipVisible = true;
        
        // Auto-hide tooltip após algum tempo
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
        }
        tooltipCoroutine = StartCoroutine(AutoHideTooltip());
    }

    private string GetRarityColoredText(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return "<color=white>Comum</color>";
            case ItemRarity.Uncommon:
                return "<color=green>Incomum</color>";
            case ItemRarity.Rare:
                return "<color=blue>Raro</color>";
            case ItemRarity.Epic:
                return "<color=purple>Épico</color>";
            case ItemRarity.Legendary:
                return "<color=orange>Lendário</color>";
            default:
                return "<color=white>Comum</color>";
        }
    }
    
    public void HideTooltip()
    {
        if (tooltip != null)
        {
            tooltip.SetActive(false);
            tooltipVisible = false;
        }
        
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }
    }
    
    private IEnumerator AutoHideTooltip()
    {
        yield return new WaitForSeconds(5f); // Auto-hide após 5 segundos
        HideTooltip();
    }
    
    #endregion
    
    #region Fixed Animations
    
    private IEnumerator FlashHealthBar()
    {
        if (healthBar == null) yield break;
        
        Image healthFill = healthBar.fillRect.GetComponent<Image>();
        if (healthFill == null) yield break;
        
        Color originalColor = healthFill.color;
        Color flashColor = Color.red;
        
        // Flash rápido
        healthFill.color = flashColor;
        yield return new WaitForSeconds(0.1f);
        healthFill.color = originalColor;
        yield return new WaitForSeconds(0.05f);
        healthFill.color = flashColor;
        yield return new WaitForSeconds(0.1f);
        healthFill.color = originalColor;
    }
    
    // FIX: Animação de level up corrigida para evitar scale gigante
    private IEnumerator AnimateLevelUpTextSafely()
    {
        if (levelText == null || isLevelTextAnimating) yield break;
        
        isLevelTextAnimating = true;
        
        // Garantir escala original
        if (!hasInitializedLevelTextScale)
        {
            originalLevelTextScale = Vector3.one;
        }
        
        // Resetar para escala original
        levelText.transform.localScale = originalLevelTextScale;
        
        float duration = levelUpAnimationDuration;
        float elapsed = 0f;
        float maxScale = maxLevelUpAnimationScale;
        
        // Primeira fase: crescer
        while (elapsed < duration * 0.5f)
        {
            if (levelText == null) break; // Safety check
            
            elapsed += Time.deltaTime;
            float progress = (elapsed / (duration * 0.5f));
            float currentScale = Mathf.Lerp(1f, maxScale, progress);
            
            levelText.transform.localScale = originalLevelTextScale * currentScale;
            yield return null;
        }
        
        // Segunda fase: voltar ao normal
        elapsed = 0f;
        while (elapsed < duration * 0.5f)
        {
            if (levelText == null) break; // Safety check
            
            elapsed += Time.deltaTime;
            float progress = (elapsed / (duration * 0.5f));
            float currentScale = Mathf.Lerp(maxScale, 1f, progress);
            
            levelText.transform.localScale = originalLevelTextScale * currentScale;
            yield return null;
        }
        
        // Garantir que voltou ao tamanho original
        if (levelText != null)
        {
            levelText.transform.localScale = originalLevelTextScale;
        }
        
        isLevelTextAnimating = false;
        levelUpAnimationCoroutine = null;
    }
    
    // FIX: Animação de experience bar com bounds checking
    private IEnumerator AnimateExperienceBarSafely(int targetExp, int maxExp)
    {
        if (experienceBar == null) yield break;
        
        // Validar valores
        maxExp = Mathf.Max(1, maxExp);
        targetExp = Mathf.Clamp(targetExp, 0, maxExp);
        
        float currentValue = experienceBar.value;
        float targetValue = targetExp;
        experienceBar.maxValue = maxExp;
        
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration && experienceBar != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            experienceBar.value = Mathf.Lerp(currentValue, targetValue, progress);
            yield return null;
        }
        
        if (experienceBar != null)
        {
            experienceBar.value = targetValue;
        }
    }
    
    private IEnumerator AnimateGoldText(int targetGold)
    {
        if (goldText == null) yield break;
        
        // Validação do texto atual
        int currentGold;
        if (!int.TryParse(goldText.text, out currentGold))
        {
            currentGold = 0;
        }
        
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration && goldText != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            int displayGold = Mathf.RoundToInt(Mathf.Lerp(currentGold, targetGold, progress));
            goldText.text = displayGold.ToString();
            yield return null;
        }
        
        if (goldText != null)
        {
            goldText.text = targetGold.ToString();
        }
    }
    
    #endregion
    
    #region Utility
    
    private void ToggleAllUI(bool visible)
    {
        // Toggle visibility de todos os elementos principais da UI
        if (healthBar != null) healthBar.gameObject.SetActive(visible);
        if (manaBar != null) manaBar.gameObject.SetActive(visible);
        if (experienceBar != null) experienceBar.gameObject.SetActive(visible);
        if (levelText != null) levelText.gameObject.SetActive(visible);
        if (goldText != null) goldText.gameObject.SetActive(visible);
        
        // Skills
        if (skillIcons != null)
        {
            foreach (var icon in skillIcons)
            {
                if (icon != null) icon.gameObject.SetActive(visible);
            }
        }
        
        // Notifications container
        if (notificationContainer != null)
        {
            notificationContainer.gameObject.SetActive(visible);
        }
    }
    
    // FIX: Método para parar todas as animações
    private void StopAllAnimations()
    {
        // Parar animação de level up
        if (levelUpAnimationCoroutine != null)
        {
            StopCoroutine(levelUpAnimationCoroutine);
            levelUpAnimationCoroutine = null;
        }
        
        // Parar animações das barras
        if (healthBarUpdateCoroutine != null)
        {
            StopCoroutine(healthBarUpdateCoroutine);
            healthBarUpdateCoroutine = null;
        }
        
        if (manaBarUpdateCoroutine != null)
        {
            StopCoroutine(manaBarUpdateCoroutine);
            manaBarUpdateCoroutine = null;
        }
        
        // Resetar flags
        isLevelTextAnimating = false;
        isUpdatingHealthBar = false;
        isUpdatingManaBar = false;
        
        // Garantir que level text volte ao tamanho normal
        if (levelText != null && hasInitializedLevelTextScale)
        {
            levelText.transform.localScale = originalLevelTextScale;
        }
    }
    
    /// <summary>
    /// Força refresh de todos os elementos da UI
    /// </summary>
    public void ForceRefreshAll()
    {
        // Parar animações primeiro
        StopAllAnimations();
        
        // Invalidar todos os caches
        playerCacheValid = false;
        gameManagerCacheValid = false;
        
        // Re-cachear
        TryCacheComponents();
        
        // Atualizar tudo
        ForceUpdateAllUI();
        
        Debug.Log("UIManager: Refresh completo forçado");
    }
    
    /// <summary>
    /// Método público para resetar level text scale (para casos de emergência)
    /// </summary>
    [ContextMenu("Reset Level Text Scale")]
    public void ResetLevelTextScale()
    {
        if (levelText != null)
        {
            // Parar qualquer animação
            if (levelUpAnimationCoroutine != null)
            {
                StopCoroutine(levelUpAnimationCoroutine);
                levelUpAnimationCoroutine = null;
            }
            
            isLevelTextAnimating = false;
            
            // Resetar para escala original ou padrão
            if (hasInitializedLevelTextScale)
            {
                levelText.transform.localScale = originalLevelTextScale;
            }
            else
            {
                levelText.transform.localScale = Vector3.one;
                originalLevelTextScale = Vector3.one;
                hasInitializedLevelTextScale = true;
            }
            
            Debug.Log($"UIManager: Level text scale resetado para {levelText.transform.localScale}");
        }
    }
    
    /// <summary>
    /// Debug para verificar estado do cache
    /// </summary>
    [ContextMenu("Debug Cache Status")]
    public void DebugCacheStatus()
    {
        Debug.Log($"=== UI MANAGER CACHE STATUS ===");
        Debug.Log($"Player Cache Válido: {playerCacheValid}");
        Debug.Log($"GameManager Cache Válido: {gameManagerCacheValid}");
        Debug.Log($"Player Cacheado: {(cachedPlayer != null ? cachedPlayer.name : "null")}");
        Debug.Log($"GameManager Cacheado: {(cachedGameManager != null ? cachedGameManager.name : "null")}");
        Debug.Log($"Notifications Ativas: {activeNotifications.Count}");
        Debug.Log($"Tooltip Visível: {tooltipVisible}");
        Debug.Log($"Próxima validação de cache em: {cacheValidationTimer:F1}s");
        Debug.Log($"Level Text Animando: {isLevelTextAnimating}");
        Debug.Log($"Health Bar Atualizando: {isUpdatingHealthBar}");
        Debug.Log($"Mana Bar Atualizando: {isUpdatingManaBar}");
        Debug.Log($"Level Text Scale Inicializado: {hasInitializedLevelTextScale}");
        Debug.Log($"Level Text Scale Original: {originalLevelTextScale}");
        if (levelText != null)
        {
            Debug.Log($"Level Text Scale Atual: {levelText.transform.localScale}");
        }
    }
    
    /// <summary>
    /// Debug para verificar estado das barras
    /// </summary>
    [ContextMenu("Debug Bars Status")]
    public void DebugBarsStatus()
    {
        Debug.Log($"=== BARS STATUS ===");
        
        if (healthBar != null)
        {
            Debug.Log($"Health Bar - Value: {healthBar.value}, Max: {healthBar.maxValue}, Min: {healthBar.minValue}");
        }
        else
        {
            Debug.Log("Health Bar: NULL");
        }
        
        if (manaBar != null)
        {
            Debug.Log($"Mana Bar - Value: {manaBar.value}, Max: {manaBar.maxValue}, Min: {manaBar.minValue}");
        }
        else
        {
            Debug.Log("Mana Bar: NULL");
        }
        
        if (experienceBar != null)
        {
            Debug.Log($"Experience Bar - Value: {experienceBar.value}, Max: {experienceBar.maxValue}, Min: {experienceBar.minValue}");
        }
        else
        {
            Debug.Log("Experience Bar: NULL");
        }
        
        Debug.Log($"Cached Health: {cachedHealth}/{cachedMaxHealth}");
        Debug.Log($"Cached Mana: {cachedMana}/{cachedMaxMana}");
        Debug.Log($"Cached Experience: {cachedExperience}");
        Debug.Log($"Cached Level: {cachedLevel}");
        
        if (playerCacheValid && cachedPlayerHealth != null)
        {
            Debug.Log($"Real Health: {cachedPlayerHealth.CurrentHealth}/{cachedPlayerHealth.MaxHealth}");
            Debug.Log($"Real Mana: {cachedPlayerHealth.CurrentMana}/{cachedPlayerHealth.MaxMana}");
        }
        else
        {
            Debug.Log("Player Health Manager: NULL ou Cache Inválido");
        }
    }
    
    #endregion
    
    private void OnDestroy()
    {
        // Parar todas as animações antes de destruir
        StopAllAnimations();
        
        // IMPORTANTE: Sempre desregistrar eventos
        EventManager.Unsubscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        EventManager.Unsubscribe<PlayerManaChangedEvent>(OnPlayerManaChanged);
        EventManager.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        EventManager.Unsubscribe<PlayerExperienceGainedEvent>(OnPlayerExperienceGained);
        EventManager.Unsubscribe<PlayerStatsRecalculatedEvent>(OnPlayerStatsRecalculated);
        EventManager.Unsubscribe<ItemAddedEvent>(OnItemAdded);
        EventManager.Unsubscribe<ItemEquippedEvent>(OnItemEquipped);
        EventManager.Unsubscribe<ItemUnequippedEvent>(OnItemUnequipped);
        EventManager.Unsubscribe<NotificationEvent>(OnNotificationRequested);
        EventManager.Unsubscribe<TooltipRequestEvent>(OnTooltipRequested);
        EventManager.Unsubscribe<GoldCollectedEvent>(OnGoldCollected);
        EventManager.Unsubscribe<InventoryFullEvent>(OnInventoryFull);
        EventManager.Unsubscribe<UIElementToggledEvent>(OnUIElementToggled);
        EventManager.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        EventManager.Unsubscribe<PlayerDestroyedEvent>(OnPlayerDestroyed);
        EventManager.Unsubscribe<SceneTransitionEvent>(OnSceneTransition);
        
        // Limpar notifications
        ClearAllNotifications();
        
        // Parar coroutines
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
        }
    }
}