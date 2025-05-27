using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UIManager OTIMIZADO - Performance melhorada com updates condicionais, pooling e lazy loading
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
    
    [Header("Performance Settings - NOVOS")]
    [SerializeField] private float uiUpdateInterval = 0.1f;
    [SerializeField] private float skillUpdateInterval = 0.2f;
    [SerializeField] private float goldUpdateInterval = 0.5f;
    [SerializeField] private int maxNotifications = 5;
    [SerializeField] private int notificationPoolSize = 10;
    [SerializeField] private bool enableLazyLoading = true;
    [SerializeField] private bool enableSmartUpdates = true;
    
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
    
    // NOVO: Cache melhorado para evitar updates desnecessários
    private int cachedHealth = -1;
    private int cachedMaxHealth = -1;
    private int cachedMana = -1;
    private int cachedMaxMana = -1;
    private int cachedExperience = -1;
    private int cachedLevel = -1;
    private int cachedGold = -1;
    
    // NOVO: Hash para detectar mudanças reais
//    private int lastHealthHash = 0;
//    private int lastManaHash = 0;
    private int lastSkillHash = 0;
    
    // NOVO: Update timers staggered
    private float uiUpdateTimer = 0f;
    private float skillUpdateTimer = 0f;
    private float goldUpdateTimer = 0f;
    
    // NOVO: Notification Pool
    private Queue<GameObject> notificationPool = new Queue<GameObject>();
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
    
    // NOVO: Lazy Loading Components
    private Dictionary<string, bool> componentLoadStatus = new Dictionary<string, bool>();
    private Dictionary<string, Component> lazyComponents = new Dictionary<string, Component>();
    
    // NOVO: Performance Monitoring
    private float frameTime = 0f;
    private float avgFrameTime = 0f;
    private int frameCount = 0;
    
    // NOVO: Smart Update Flags
    private bool needsHealthUpdate = false;
    private bool needsManaUpdate = false;
    private bool needsSkillUpdate = false;
    private bool needsGoldUpdate = false;
    
    private void Start()
    {
        SubscribeToEvents();
        InitializeUI();
        InitializeLevelTextScale();
        InitializeNotificationPool();
        InitializeLazyLoading();
        
        TryCacheComponents();
        Invoke("DelayedFirstUpdate", 0.2f);
    }
    
    // NOVO: Inicialização do Pool de Notificações
    private void InitializeNotificationPool()
    {
        if (notificationPrefab == null || notificationContainer == null) return;
        
        for (int i = 0; i < notificationPoolSize; i++)
        {
            GameObject notification = Instantiate(notificationPrefab, notificationContainer);
            notification.SetActive(false);
            notificationPool.Enqueue(notification);
        }
    }
    
    // NOVO: Sistema de Lazy Loading
    private void InitializeLazyLoading()
    {
        if (!enableLazyLoading) return;
        
        // Marcar componentes para lazy loading
        componentLoadStatus["InventoryUI"] = false;
        componentLoadStatus["QuestUI"] = false;
        componentLoadStatus["SkillTree"] = false;
        componentLoadStatus["Settings"] = false;
    }
    
    private T GetLazyComponent<T>(string key) where T : Component
    {
        if (!enableLazyLoading) return FindObjectOfType<T>();
        
        if (lazyComponents.ContainsKey(key))
        {
            return lazyComponents[key] as T;
        }
        
        // Load on demand
        T component = FindObjectOfType<T>();
        if (component != null)
        {
            lazyComponents[key] = component;
            componentLoadStatus[key] = true;
        }
        
        return component;
    }
    
    private void InitializeLevelTextScale()
    {
        if (levelText != null && !hasInitializedLevelTextScale)
        {
            originalLevelTextScale = levelText.transform.localScale;
            hasInitializedLevelTextScale = true;
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
        InitializeBars();
        
        // Inicializar collections
        activeNotifications.Clear();
    }
    
    private void InitializeBars()
    {
        if (healthBar != null)
        {
            healthBar.minValue = 0;
            healthBar.maxValue = 100;
            healthBar.value = 100;
            
            // NOVO: Disable raycast em fill areas para performance
            var fillImage = healthBar.fillRect?.GetComponent<Image>();
            if (fillImage != null) fillImage.raycastTarget = false;
        }
        
        if (manaBar != null)
        {
            manaBar.minValue = 0;
            manaBar.maxValue = 50;
            manaBar.value = 50;
            
            // NOVO: Disable raycast
            var fillImage = manaBar.fillRect?.GetComponent<Image>();
            if (fillImage != null) fillImage.raycastTarget = false;
        }
        
        if (experienceBar != null)
        {
            experienceBar.minValue = 0;
            experienceBar.maxValue = 100;
            experienceBar.value = 0;
            
            // NOVO: Disable raycast
            var fillImage = experienceBar.fillRect?.GetComponent<Image>();
            if (fillImage != null) fillImage.raycastTarget = false;
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
        }
        
        // Cache do GameManager
        if (!gameManagerCacheValid && GameManager.instance != null)
        {
            cachedGameManager = GameManager.instance;
            gameManagerCacheValid = true;
        }
    }
    
    private void DelayedFirstUpdate()
    {
        ForceUpdateAllUI();
    }
    
    // NOVO: Update System Otimizado
    private void Update()
    {
        // Performance monitoring
        if (enableSmartUpdates)
        {
            MonitorPerformance();
        }
        
        // Tentar cachear se ainda não temos
        if (!playerCacheValid || !gameManagerCacheValid)
        {
            TryCacheComponents();
        }
        
        // NOVO: Staggered Updates para melhor performance
        UpdateTimers();
        
        // Critical updates (sempre executar)
        if (needsHealthUpdate || needsManaUpdate)
        {
            UpdateCriticalUI();
        }
        
        // Non-critical updates (com throttling)
        if (uiUpdateTimer <= 0f && (needsSkillUpdate || needsGoldUpdate))
        {
            UpdateNonCriticalUI();
            uiUpdateTimer = uiUpdateInterval;
        }
        
        // Skill-specific updates (menos frequentes)
        if (skillUpdateTimer <= 0f && needsSkillUpdate)
        {
            UpdateSkillIcons();
            skillUpdateTimer = skillUpdateInterval;
            needsSkillUpdate = false;
        }
        
        // Gold updates (ainda menos frequentes)
        if (goldUpdateTimer <= 0f && needsGoldUpdate)
        {
            UpdateGoldDisplay();
            goldUpdateTimer = goldUpdateInterval;
            needsGoldUpdate = false;
        }
        
        // Validação periódica do cache
        cacheValidationTimer -= Time.deltaTime;
        if (cacheValidationTimer <= 0f)
        {
            ValidateCache();
            cacheValidationTimer = CACHE_VALIDATION_INTERVAL;
        }
        
        // Gerenciar notifications
        ProcessNotificationQueue();
    }
    
    // NOVO: Performance Monitoring
    private void MonitorPerformance()
    {
        frameTime = Time.deltaTime;
        frameCount++;
        avgFrameTime = (avgFrameTime * (frameCount - 1) + frameTime) / frameCount;
        
        // Auto-adjust update intervals based on performance
        if (avgFrameTime > 0.02f) // Se FPS < 50
        {
            uiUpdateInterval = Mathf.Min(uiUpdateInterval * 1.1f, 0.5f);
            skillUpdateInterval = Mathf.Min(skillUpdateInterval * 1.1f, 1f);
        }
        else if (avgFrameTime < 0.01f) // Se FPS > 100
        {
            uiUpdateInterval = Mathf.Max(uiUpdateInterval * 0.9f, 0.05f);
            skillUpdateInterval = Mathf.Max(skillUpdateInterval * 0.9f, 0.1f);
        }
    }
    
    private void UpdateTimers()
    {
        uiUpdateTimer -= Time.deltaTime;
        skillUpdateTimer -= Time.deltaTime;
        goldUpdateTimer -= Time.deltaTime;
    }
    
    // NOVO: Updates Críticos (sempre executar)
    private void UpdateCriticalUI()
    {
        if (!playerCacheValid || cachedPlayerHealth == null) return;
        
        if (needsHealthUpdate)
        {
            int currentHealth = cachedPlayerHealth.CurrentHealth;
            int currentMaxHealth = cachedPlayerHealth.MaxHealth;
            
            if (healthBar != null && (currentHealth != cachedHealth || currentMaxHealth != cachedMaxHealth))
            {
                UpdateHealthBarSafely(currentHealth, currentMaxHealth);
            }
            needsHealthUpdate = false;
        }
        
        if (needsManaUpdate)
        {
            int currentMana = cachedPlayerHealth.CurrentMana;
            int currentMaxMana = cachedPlayerHealth.MaxMana;
            
            if (manaBar != null && (currentMana != cachedMana || currentMaxMana != cachedMaxMana))
            {
                UpdateManaBarSafely(currentMana, currentMaxMana);
            }
            needsManaUpdate = false;
        }
    }
    
    private void UpdateNonCriticalUI()
    {
        // Updates menos críticos
        if (needsGoldUpdate && gameManagerCacheValid && cachedGameManager != null)
        {
            int currentGold = cachedGameManager.goldCollected;
            if (currentGold != cachedGold && goldText != null)
            {
                // NOVO: Animate gold change
                StartCoroutine(AnimateGoldChange(cachedGold, currentGold));
                cachedGold = currentGold;
            }
        }
    }
    
    // NOVO: Animação suave para mudança de ouro
    private IEnumerator AnimateGoldChange(int fromValue, int toValue)
    {
        if (goldText == null) yield break;
        
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            int currentValue = Mathf.RoundToInt(Mathf.Lerp(fromValue, toValue, progress));
            goldText.text = currentValue.ToString();
            yield return null;
        }
        
        goldText.text = toValue.ToString();
    }
    
    // NOVO: Hash-based change detection para skills
    private void UpdateSkillIcons()
    {
        if (!playerCacheValid || cachedPlayer == null) return;
        
        var skillController = cachedPlayer.GetSkillController();
        if (skillController == null) return;
        
        var skills = skillController.GetAllSkills();
        if (skills == null) return;
        
        // NOVO: Calcular hash para detectar mudanças
        int newSkillHash = CalculateSkillHash(skillController, skills);
        
        if (newSkillHash != lastSkillHash)
        {
            UpdateSkillIconsActual(skillController, skills);
            lastSkillHash = newSkillHash;
        }
    }
    
    // NOVO: Função para calcular hash das skills
    private int CalculateSkillHash(PlayerSkillController skillController, List<Skill> skills)
    {
        int hash = skillController.currentSkillIndex;
        
        for (int i = 0; i < skills.Count && i < (skillIcons?.Length ?? 0); i++)
        {
            if (skillController.IsSkillOnCooldown(i))
            {
                hash ^= (i + 1) * 31;
            }
        }
        
        return hash;
    }
    
    private void UpdateSkillIconsActual(PlayerSkillController skillController, List<Skill> skills)
    {
        if (skillIcons == null) return;
        
        for (int i = 0; i < skillIcons.Length; i++)
        {
            if (skillIcons[i] != null)
            {
                if (i < skills.Count)
                {
                    skillIcons[i].gameObject.SetActive(true);
                    skillIcons[i].color = GetSkillTypeColor(skills[i].type);
                }
                else
                {
                    skillIcons[i].gameObject.SetActive(false);
                }
            }
            
            // Update cooldown overlays
            if (skillCooldowns != null && i < skillCooldowns.Length && skillCooldowns[i] != null)
            {
                UpdateSkillCooldown(i, skillController, skills);
            }
        }
    }
    
    private void UpdateSkillCooldown(int index, PlayerSkillController skillController, List<Skill> skills)
    {
        if (index < skills.Count && skillController.IsSkillOnCooldown(index))
        {
            float remaining = skillController.GetSkillCooldownRemaining(index);
            float total = skills[index].GetActualCooldown(cachedPlayerStats);
            
            skillCooldowns[index].gameObject.SetActive(true);
            skillCooldowns[index].fillAmount = remaining / total;
        }
        else
        {
            skillCooldowns[index].gameObject.SetActive(false);
        }
    }
    
    private void UpdateGoldDisplay()
    {
        // Implementação simplificada - já tratada em UpdateNonCriticalUI
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
        }
        
        // Validar cache do GameManager
        if (gameManagerCacheValid && cachedGameManager == null)
        {
            gameManagerCacheValid = false;
            cachedGameManager = null;
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
            ResetUICache();
        }
    }
    
    private void OnPlayerDestroyed(PlayerDestroyedEvent eventData)
    {
        cachedPlayer = null;
        cachedPlayerStats = null;
        cachedPlayerHealth = null;
        playerCacheValid = false;
        ResetUICache();
    }
    
    private void OnSceneTransition(SceneTransitionEvent eventData)
    {
        StopAllAnimations();
        
        // Invalidar todos os caches ao trocar de cena
        cachedPlayer = null;
        cachedPlayerStats = null;
        cachedPlayerHealth = null;
        cachedGameManager = null;
        playerCacheValid = false;
        gameManagerCacheValid = false;
        
        ResetUICache();
        ClearAllNotifications();
        
        // Reset lazy loading
        componentLoadStatus.Clear();
        lazyComponents.Clear();
    }
    
    private void OnPlayerHealthChanged(PlayerHealthChangedEvent eventData)
    {
        // NOVO: Smart update flag
        needsHealthUpdate = true;
        
        // Animação visual para mudanças críticas de saúde
        if (eventData.healthDelta < 0)
        {
            StartCoroutine(FlashHealthBar());
        }
    }
    
    private void OnPlayerManaChanged(PlayerManaChangedEvent eventData)
    {
        // NOVO: Smart update flag
        needsManaUpdate = true;
    }
    
    private void OnPlayerLevelUp(PlayerLevelUpEvent eventData)
    {
        UpdateLevelTextSafely(eventData.newLevel);
        
        // Auto-update flags
        needsHealthUpdate = true;
        needsManaUpdate = true;
        
        ShowLevelUpEffects();
    }
    
    private void OnPlayerExperienceGained(PlayerExperienceGainedEvent eventData)
    {
        if (experienceBar != null)
        {
            StartCoroutine(AnimateExperienceBarSafely(eventData.currentExperience, eventData.experienceToNextLevel));
            cachedExperience = eventData.currentExperience;
        }
    }
    
    private void OnPlayerStatsRecalculated(PlayerStatsRecalculatedEvent eventData)
    {
        cachedMaxHealth = -1;
        cachedMaxMana = -1;
        
        // Set update flags
        needsHealthUpdate = true;
        needsManaUpdate = true;
        
        Invoke("ForceUpdateBars", 0.1f);
    }
    
    private void OnItemAdded(ItemAddedEvent eventData)
    {
        if (eventData.wasSuccessful)
        {
            // NOVO: Lazy load inventory UI only when needed
            if (inventoryPanel != null && inventoryPanel.activeSelf)
            {
                UpdateInventoryUI();
            }
            ShowNotificationFromPool($"Item adicionado: {eventData.item.name}", NotificationType.ItemCollected);
        }
        else
        {
            ShowNotificationFromPool(eventData.failureReason, NotificationType.Error);
        }
    }
    
    private void OnItemEquipped(ItemEquippedEvent eventData)
    {
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            UpdateInventoryUI();
        }
        ShowNotificationFromPool($"Equipado: {eventData.item.name}", NotificationType.Success);
    }
    
    private void OnItemUnequipped(ItemUnequippedEvent eventData)
    {
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            UpdateInventoryUI();
        }
        ShowNotificationFromPool($"Desequipado: {eventData.item.name}", NotificationType.Info);
    }
    
    private void OnNotificationRequested(NotificationEvent eventData)
    {
        ShowNotificationFromPool(eventData.message, eventData.type, eventData.duration, eventData.color);
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
        // NOVO: Smart update flag
        needsGoldUpdate = true;
        
        ShowNotificationFromPool($"+{eventData.amount} ouro", NotificationType.Success, 1.5f);
    }
    
    private void OnInventoryFull(InventoryFullEvent eventData)
    {
        ShowNotificationFromPool("Inventário cheio!", NotificationType.Warning);
    }
    
    private void OnUIElementToggled(UIElementToggledEvent eventData)
    {
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
                ToggleAllUI(eventData.isVisible);
                break;
        }
    }
    
    #endregion
    
    #region Fixed UI Update Methods
    
    private void UpdateHealthBarSafely(int currentHealth, int maxHealth)
    {
        if (healthBar == null || isUpdatingHealthBar) return;
        
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        bool hasChanged = (currentHealth != cachedHealth || maxHealth != cachedMaxHealth);
        
        if (hasChanged)
        {
            cachedHealth = currentHealth;
            cachedMaxHealth = maxHealth;
            
            if (healthBarUpdateCoroutine != null)
            {
                StopCoroutine(healthBarUpdateCoroutine);
            }
            
            healthBarUpdateCoroutine = StartCoroutine(UpdateHealthBarCoroutine(currentHealth, maxHealth));
        }
    }
    
    private IEnumerator UpdateHealthBarCoroutine(int targetHealth, int maxHealth)
    {
        isUpdatingHealthBar = true;
        
        yield return new WaitForEndOfFrame();
        
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            
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
        
        maxMana = Mathf.Max(1, maxMana);
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
        
        bool hasChanged = (currentMana != cachedMana || maxMana != cachedMaxMana);
        
        if (hasChanged)
        {
            cachedMana = currentMana;
            cachedMaxMana = maxMana;
            
            if (manaBarUpdateCoroutine != null)
            {
                StopCoroutine(manaBarUpdateCoroutine);
            }
            
            manaBarUpdateCoroutine = StartCoroutine(UpdateManaBarCoroutine(currentMana, maxMana));
        }
    }
    
    private IEnumerator UpdateManaBarCoroutine(int targetMana, int maxMana)
    {
        isUpdatingManaBar = true;
        
        yield return new WaitForEndOfFrame();
        
        if (manaBar != null)
        {
            manaBar.maxValue = maxMana;
            
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
        
        if (newLevel != cachedLevel)
        {
            cachedLevel = newLevel;
            levelText.text = "Level " + newLevel;
            
            if (fixAnimationBugs)
            {
                StartLevelUpAnimationSafely();
            }
        }
    }
    
    private void StartLevelUpAnimationSafely()
    {
        if (isLevelTextAnimating || levelText == null) return;
        
        if (levelUpAnimationCoroutine != null)
        {
            StopCoroutine(levelUpAnimationCoroutine);
        }
        
        if (!hasInitializedLevelTextScale)
        {
            InitializeLevelTextScale();
        }
        
        levelText.transform.localScale = originalLevelTextScale;
        levelUpAnimationCoroutine = StartCoroutine(AnimateLevelUpTextSafely());
    }
    
    private void ForceUpdateBars()
    {
        if (playerCacheValid && cachedPlayerHealth != null)
        {
            needsHealthUpdate = true;
            needsManaUpdate = true;
        }
    }
    
    #endregion
    
    #region Notifications com Pooling
    
    // NOVO: Sistema de Notifications com Object Pooling
    private void ShowNotificationFromPool(string message, NotificationType type, float duration = -1f, Color? customColor = null)
    {
        GameObject notification = GetPooledNotification();
        if (notification == null) return;
        
        if (duration < 0) duration = notificationDuration;
        
        TextMeshProUGUI text = notification.GetComponentInChildren<TextMeshProUGUI>();
        
        if (text != null)
        {
            text.text = message;
            text.color = customColor ?? GetNotificationColor(type);
        }
        
        notification.SetActive(true);
        activeNotifications.Add(notification);
        
        // Remover notifications antigas se excedeu o limite
        while (activeNotifications.Count > maxNotifications)
        {
            ReturnNotificationToPool(activeNotifications[0]);
            activeNotifications.RemoveAt(0);
        }
        
        StartCoroutine(ReturnNotificationAfterDelay(notification, duration));
    }
    
    private GameObject GetPooledNotification()
    {
        if (notificationPool.Count > 0)
        {
            return notificationPool.Dequeue();
        }
        
        // Se pool está vazio, criar novo (fallback)
        if (notificationPrefab != null && notificationContainer != null)
        {
            return Instantiate(notificationPrefab, notificationContainer);
        }
        
        return null;
    }
    
    private void ReturnNotificationToPool(GameObject notification)
    {
        if (notification != null)
        {
            notification.SetActive(false);
            notificationPool.Enqueue(notification);
        }
    }
    
    private IEnumerator ReturnNotificationAfterDelay(GameObject notification, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (notification != null && activeNotifications.Contains(notification))
        {
            activeNotifications.Remove(notification);
            ReturnNotificationToPool(notification);
        }
    }
    
    // NOVO: Método depreciado para compatibilidade
    private void ShowNotification(string message, NotificationType type, float duration = -1f, Color? customColor = null)
    {
        ShowNotificationFromPool(message, type, duration, customColor);
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
        
        // NOVO: String Builder para evitar allocations
        var description = new System.Text.StringBuilder();
        description.AppendLine(item.description);
        
        if (item.type == ItemType.Weapon)
        {
            description.AppendLine($"Dano Físico: {item.physicalDamage}");
            if (item.fireDamage > 0) description.AppendLine($"Dano de Fogo: {item.fireDamage}");
            if (item.iceDamage > 0) description.AppendLine($"Dano de Gelo: {item.iceDamage}");
            if (item.lightningDamage > 0) description.AppendLine($"Dano Elétrico: {item.lightningDamage}");
            if (item.poisonDamage > 0) description.AppendLine($"Dano de Veneno: {item.poisonDamage}");
        }
        
        if (item.strengthModifier > 0) description.AppendLine($"Força: +{item.strengthModifier}");
        if (item.intelligenceModifier > 0) description.AppendLine($"Inteligência: +{item.intelligenceModifier}");
        if (item.dexterityModifier > 0) description.AppendLine($"Destreza: +{item.dexterityModifier}");
        if (item.vitalityModifier > 0) description.AppendLine($"Vitalidade: +{item.vitalityModifier}");
        
        if (item.type == ItemType.Consumable)
        {
            if (item.healthRestore > 0) description.AppendLine($"Recupera {item.healthRestore} de vida");
            if (item.manaRestore > 0) description.AppendLine($"Recupera {item.manaRestore} de mana");
        }
        
        description.AppendLine($"Nível: {item.level}");
        description.AppendLine(GetRarityColoredText(item.rarity));
        
        tooltipDescription.text = description.ToString();
        
        tooltip.transform.position = position;
        tooltip.SetActive(true);
        tooltipVisible = true;
        
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
        yield return new WaitForSeconds(5f);
        HideTooltip();
    }
    
    #endregion
    
    #region Animations
    
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
    
    private IEnumerator AnimateLevelUpTextSafely()
    {
        if (levelText == null || isLevelTextAnimating) yield break;
        
        isLevelTextAnimating = true;
        
        if (!hasInitializedLevelTextScale)
        {
            originalLevelTextScale = Vector3.one;
        }
        
        levelText.transform.localScale = originalLevelTextScale;
        
        float duration = levelUpAnimationDuration;
        float elapsed = 0f;
        float maxScale = maxLevelUpAnimationScale;
        
        // Primeira fase: crescer
        while (elapsed < duration * 0.5f)
        {
            if (levelText == null) break;
            
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
            if (levelText == null) break;
            
            elapsed += Time.deltaTime;
            float progress = (elapsed / (duration * 0.5f));
            float currentScale = Mathf.Lerp(maxScale, 1f, progress);
            
            levelText.transform.localScale = originalLevelTextScale * currentScale;
            yield return null;
        }
        
        if (levelText != null)
        {
            levelText.transform.localScale = originalLevelTextScale;
        }
        
        isLevelTextAnimating = false;
        levelUpAnimationCoroutine = null;
    }
    
    private IEnumerator AnimateExperienceBarSafely(int targetExp, int maxExp)
    {
        if (experienceBar == null) yield break;
        
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
    
    private void ShowLevelUpEffects()
    {
        // Add particle effects, screen shake, etc. here
        Debug.Log("Level Up Effects!");
    }
    
    #endregion
    
    #region UI Management
    
    public void ForceUpdateAllUI()
    {
        if (!playerCacheValid || cachedPlayerStats == null || cachedPlayerHealth == null) 
        {
            TryCacheComponents();
            if (!playerCacheValid) return;
        }
        
        ResetUICache();
        
        // Force update all
        needsHealthUpdate = true;
        needsManaUpdate = true;
        needsSkillUpdate = true;
        needsGoldUpdate = true;
        
        UpdateCriticalUI();
        UpdateNonCriticalUI();
        UpdateSkillIcons();
        UpdateGoldDisplay();
        
        if (cachedPlayerStats != null)
        {
            UpdateLevelTextSafely(cachedPlayerStats.Level);
        }
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
        
        // NOVO: Clear usando pool se disponível
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }
        
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
        
        // Reset hashes
      //  lastHealthHash = 0;
       // lastManaHash = 0;
        lastSkillHash = 0;
        
        // Set update flags
        needsHealthUpdate = true;
        needsManaUpdate = true;
        needsSkillUpdate = true;
        needsGoldUpdate = true;
    }
    
    #endregion
    
    #region Notifications Processing
    
    private void ProcessNotificationQueue()
    {
        // NOVO: Automatic cleanup of expired notifications
        for (int i = activeNotifications.Count - 1; i >= 0; i--)
        {
            if (activeNotifications[i] == null)
            {
                activeNotifications.RemoveAt(i);
            }
        }
    }
    
    private void ClearAllNotifications()
    {
        foreach (GameObject notification in activeNotifications)
        {
            if (notification != null)
            {
                ReturnNotificationToPool(notification);
            }
        }
        activeNotifications.Clear();
    }
    
    #endregion
    
    #region Color Utilities
    
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
    
    #endregion
    
    #region Utility Methods
    
    private void ToggleAllUI(bool visible)
    {
        if (healthBar != null) healthBar.gameObject.SetActive(visible);
        if (manaBar != null) manaBar.gameObject.SetActive(visible);
        if (experienceBar != null) experienceBar.gameObject.SetActive(visible);
        if (levelText != null) levelText.gameObject.SetActive(visible);
        if (goldText != null) goldText.gameObject.SetActive(visible);
        
        if (skillIcons != null)
        {
            foreach (var icon in skillIcons)
            {
                if (icon != null) icon.gameObject.SetActive(visible);
            }
        }
        
        if (notificationContainer != null)
        {
            notificationContainer.gameObject.SetActive(visible);
        }
    }
    
    private void StopAllAnimations()
    {
        if (levelUpAnimationCoroutine != null)
        {
            StopCoroutine(levelUpAnimationCoroutine);
            levelUpAnimationCoroutine = null;
        }
        
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
        
        isLevelTextAnimating = false;
        isUpdatingHealthBar = false;
        isUpdatingManaBar = false;
        
        if (levelText != null && hasInitializedLevelTextScale)
        {
            levelText.transform.localScale = originalLevelTextScale;
        }
    }
    
    public void ForceRefreshAll()
    {
        StopAllAnimations();
        
        playerCacheValid = false;
        gameManagerCacheValid = false;
        
        TryCacheComponents();
        ForceUpdateAllUI();
    }
    
    [ContextMenu("Reset Level Text Scale")]
    public void ResetLevelTextScale()
    {
        if (levelText != null)
        {
            if (levelUpAnimationCoroutine != null)
            {
                StopCoroutine(levelUpAnimationCoroutine);
                levelUpAnimationCoroutine = null;
            }
            
            isLevelTextAnimating = false;
            
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
        }
    }
    
    #endregion
    
    #region Debug Methods
    
    [ContextMenu("Debug Performance Stats")]
    public void DebugPerformanceStats()
    {
        Debug.Log($"=== UI MANAGER PERFORMANCE ===");
        Debug.Log($"Average Frame Time: {avgFrameTime:F4}s ({(1f/avgFrameTime):F1} FPS)");
        Debug.Log($"UI Update Interval: {uiUpdateInterval:F3}s");
        Debug.Log($"Skill Update Interval: {skillUpdateInterval:F3}s");
        Debug.Log($"Gold Update Interval: {goldUpdateInterval:F3}s");
        Debug.Log($"Active Notifications: {activeNotifications.Count}");
        Debug.Log($"Notification Pool Size: {notificationPool.Count}");
        Debug.Log($"Player Cache Valid: {playerCacheValid}");
        Debug.Log($"GameManager Cache Valid: {gameManagerCacheValid}");
        Debug.Log($"Smart Updates Enabled: {enableSmartUpdates}");
        Debug.Log($"Lazy Loading Enabled: {enableLazyLoading}");
    }
    
    [ContextMenu("Force Performance Optimization")]
    public void ForcePerformanceOptimization()
    {
        // Increase update intervals for better performance
        uiUpdateInterval = Mathf.Min(uiUpdateInterval * 1.5f, 0.5f);
        skillUpdateInterval = Mathf.Min(skillUpdateInterval * 1.5f, 1f);
        goldUpdateInterval = Mathf.Min(goldUpdateInterval * 1.5f, 2f);
        
        Debug.Log("Performance optimization applied!");
    }
    
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
    
    #endregion
    
    private void OnDestroy()
    {
        StopAllAnimations();
        
        // Unsubscribe from all events
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
        
        ClearAllNotifications();
        
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
        }
    }
}
