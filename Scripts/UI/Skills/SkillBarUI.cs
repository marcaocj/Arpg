using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Sistema otimizado de Skill Bar UI integrado com EventManager
/// CORREÇÃO: Tratamento robusto de inicialização e null references
/// </summary>
public class SkillBarUI : MonoBehaviour
{
    [Header("Skill Slots Configuration")]
    public GameObject[] skillSlots;
    public Image[] skillIcons;
    public Image[] cooldownOverlays;
    public TextMeshProUGUI[] hotkeyTexts;
    public TextMeshProUGUI[] cooldownTexts;
    
    [Header("Skill Info Panel")]
    public GameObject skillInfoPanel;
    public TextMeshProUGUI skillNameText;
    public TextMeshProUGUI skillDescriptionText;
    public TextMeshProUGUI skillStatsText;
    public Image skillInfoIcon;
    
    [Header("Selection & Animation")]
    public Image selectionIndicator;
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;
    public Color unavailableColor = Color.gray;
    public Color onCooldownColor = Color.red;
    
    [Header("Visual Effects & Animation")]
    public AnimationCurve iconScaleOnUse = AnimationCurve.EaseInOut(0, 1, 0.2f, 1.2f);
    public float scaleAnimationDuration = 0.2f;
    public float pulseIntensity = 0.1f;
    public float pulseSpeed = 2f;
    
    [Header("Performance Settings")]
    private float uiUpdateInterval;
    private float cooldownUpdateInterval;
    private float tooltipCheckInterval;
    
    [Header("Canvas Optimization")]
    public Canvas dynamicCanvas;
    public Canvas staticCanvas;
    
    [Header("Debug & Safety")]
    public bool enableNullChecks = true;
    public bool showDebugLogs = false;
    
    // === CACHED COMPONENTS ===
    private PlayerController player;
    private PlayerStats playerStats;
    private PlayerSkillController skillController;
    
    // === INITIALIZATION STATE ===
    private bool isInitialized = false;
    private bool isInitializing = false;
    private int initializationAttempts = 0;
    private const int MAX_INIT_ATTEMPTS = 10;
    
    // === PERFORMANCE OPTIMIZATION ===
    private readonly Dictionary<int, SkillSlotData> slotDataCache = new Dictionary<int, SkillSlotData>();
    private readonly Dictionary<SkillType, Color> skillColorCache = new Dictionary<SkillType, Color>();
    private readonly Dictionary<int, Coroutine> activeAnimations = new Dictionary<int, Coroutine>();
    
    // === UPDATE TIMERS ===
    private float uiUpdateTimer = 0f;
    private float cooldownUpdateTimer = 0f;
    private float tooltipTimer = 0f;
    private float pulseTimer = 0f;
    private float initializationRetryTimer = 0f;
    
    // === STATE TRACKING ===
    private int lastCurrentSkillIndex = -1;
    private int lastPlayerMana = -1;
    private bool[] lastSkillAvailability;
    private float[] lastCooldownTimes;
    private bool isTooltipShowing = false;
    private int hoveredSlotIndex = -1;
    
    // === POOLING ===
    private static readonly Queue<GameObject> effectPool = new Queue<GameObject>();
    private const int EFFECT_POOL_SIZE = 10;
    
    #region Data Structures
    
    [System.Serializable]
    private class SkillSlotData
    {
        public GameObject slotObject;
        public Image icon;
        public Image cooldownOverlay;
        public TextMeshProUGUI hotkeyText;
        public TextMeshProUGUI cooldownText;
        public bool isAvailable;
        public float cooldownRemaining;
        public Color currentColor;
        
        public void UpdateAvailability(bool available, Color color)
        {
            if (isAvailable != available || currentColor != color)
            {
                isAvailable = available;
                currentColor = color;
                if (icon != null) icon.color = color;
            }
        }
        
        public void UpdateCooldown(float remaining, float total)
        {
            if (Mathf.Abs(cooldownRemaining - remaining) > 0.01f)
            {
                cooldownRemaining = remaining;
                
                if (cooldownOverlay != null)
                {
                    bool showOverlay = remaining > 0;
                    cooldownOverlay.gameObject.SetActive(showOverlay);
                    
                    if (showOverlay && total > 0)
                    {
                        cooldownOverlay.fillAmount = remaining / total;
                    }
                }
                
                if (cooldownText != null)
                {
                    bool showText = remaining > 0;
                    cooldownText.gameObject.SetActive(showText);
                    
                    if (showText)
                    {
                        cooldownText.text = remaining.ToString("F1");
                    }
                }
            }
        }
    }
    
    #endregion
    
    #region Initialization
    
    private void Awake()
    {
        if (showDebugLogs) Debug.Log("[SkillBarUI] Awake - Iniciando inicialização");
        
        // Load config values
        var config = GameConfig.Instance;
        uiUpdateInterval = config.uiUpdateInterval;
        cooldownUpdateInterval = config.skillBarUpdateInterval;
        tooltipCheckInterval = 0.2f; // Keep this fixed as it's UI-specific

        InitializeColorCache();
        InitializeEffectPool();
        
        // Tentar inicialização imediata
        if (!TryInitializeComponents())
        {
            if (showDebugLogs) Debug.Log("[SkillBarUI] Inicialização adiada para Start");
        }
    }
    
    private void Start()
    {
        if (showDebugLogs) Debug.Log("[SkillBarUI] Start - Verificando inicialização");
        
        // Tentar inicializar novamente se não foi bem-sucedido no Awake
        if (!isInitialized)
        {
            StartCoroutine(InitializeWithRetry());
        }
        else
        {
            CompleteInitialization();
        }
    }
    
    private bool TryInitializeComponents()
    {
        if (isInitializing) return false;
        
        isInitializing = true;
        initializationAttempts++;
        
        try
        {
            // Buscar PlayerController
            if (player == null)
            {
                player = FindObjectOfType<PlayerController>();
                if (player == null)
                {
                    if (showDebugLogs) Debug.Log($"[SkillBarUI] PlayerController não encontrado (tentativa {initializationAttempts})");
                    return false;
                }
            }
            
            // Buscar PlayerStats
            if (playerStats == null && player != null)
            {
                playerStats = player.GetStats();
                if (playerStats == null)
                {
                    if (showDebugLogs) Debug.Log($"[SkillBarUI] PlayerStats não encontrado (tentativa {initializationAttempts})");
                    return false;
                }
            }
            
            // Buscar SkillController
            if (skillController == null && player != null)
            {
                skillController = player.GetComponent<PlayerSkillController>();
                if (skillController == null)
                {
                    if (showDebugLogs) Debug.Log($"[SkillBarUI] PlayerSkillController não encontrado (tentativa {initializationAttempts})");
                    return false;
                }
            }
            
            // Verificar se tudo foi encontrado
            if (player != null && playerStats != null && skillController != null)
            {
                if (showDebugLogs) Debug.Log("[SkillBarUI] Todos os componentes encontrados - inicialização bem-sucedida");
                
                InitializeSlotData();
                isInitialized = true;
                return true;
            }
            
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SkillBarUI] Erro durante inicialização: {e.Message}");
            return false;
        }
        finally
        {
            isInitializing = false;
        }
    }
    
    private IEnumerator InitializeWithRetry()
    {
        while (!isInitialized && initializationAttempts < MAX_INIT_ATTEMPTS)
        {
            yield return new WaitForSeconds(0.1f); // Aguardar 100ms entre tentativas
            
            if (TryInitializeComponents())
            {
                CompleteInitialization();
                yield break;
            }
        }
        
        if (!isInitialized)
        {
            Debug.LogError($"[SkillBarUI] Falha na inicialização após {MAX_INIT_ATTEMPTS} tentativas. Componentes necessários não encontrados.");
        }
    }
    
    private void CompleteInitialization()
    {
        if (!isInitialized) return;
        
        SubscribeToEvents();
        SetupUI();
        
        // Aguardar um frame antes de fazer o refresh inicial
        StartCoroutine(DelayedInitialRefresh());
    }
    
    private IEnumerator DelayedInitialRefresh()
    {
        yield return new WaitForEndOfFrame();
        RefreshSkillBar();
    }
    
    private void InitializeColorCache()
    {
        skillColorCache[SkillType.Physical] = new Color(0.9f, 0.9f, 0.9f);
        skillColorCache[SkillType.Fire] = new Color(1f, 0.4f, 0.2f);
        skillColorCache[SkillType.Ice] = new Color(0.4f, 0.8f, 1f);
        skillColorCache[SkillType.Lightning] = new Color(1f, 1f, 0.4f);
        skillColorCache[SkillType.Poison] = new Color(0.6f, 1f, 0.4f);
    }
    
    private void InitializeSlotData()
    {
        int slotCount = Mathf.Min(skillSlots?.Length ?? 0, skillIcons?.Length ?? 0);
        lastSkillAvailability = new bool[slotCount];
        lastCooldownTimes = new float[slotCount];
        
        slotDataCache.Clear(); // Limpar cache anterior
        
        for (int i = 0; i < slotCount; i++)
        {
            var slotData = new SkillSlotData
            {
                slotObject = skillSlots?[i],
                icon = skillIcons?[i],
                cooldownOverlay = cooldownOverlays?[i],
                hotkeyText = hotkeyTexts?[i],
                cooldownText = cooldownTexts?[i]
            };
            
            slotDataCache[i] = slotData;
            
            // Disable raycast target for non-interactive elements (OPTIMIZATION)
            if (slotData.icon != null)
                slotData.icon.raycastTarget = false;
            if (slotData.cooldownOverlay != null)
                slotData.cooldownOverlay.raycastTarget = false;
            if (slotData.hotkeyText != null)
                slotData.hotkeyText.raycastTarget = false;
            if (slotData.cooldownText != null)
                slotData.cooldownText.raycastTarget = false;
        }
    }
    
    private void InitializeEffectPool()
    {
        for (int i = 0; i < EFFECT_POOL_SIZE; i++)
        {
            GameObject effect = new GameObject("PooledEffect");
            effect.SetActive(false);
            effectPool.Enqueue(effect);
        }
    }
    
    private void SubscribeToEvents()
    {
        EventManager.Subscribe<SkillUsedEvent>(OnSkillUsed);
        EventManager.Subscribe<SkillCooldownChangedEvent>(OnSkillCooldownChanged);
        EventManager.Subscribe<PlayerManaChangedEvent>(OnPlayerManaChanged);
    }
    
    private void SetupUI()
    {
        SetupHotkeys();
        SetupButtonEvents();
        
        if (skillInfoPanel != null)
            skillInfoPanel.SetActive(false);
        
        // Auto-configure canvases if not set
        if (dynamicCanvas == null)
            dynamicCanvas = GetComponentInParent<Canvas>();
        if (staticCanvas == null)
            staticCanvas = dynamicCanvas;
    }
    
    #endregion
    
    #region Safe Component Access
    
    private bool ValidateComponents()
    {
        if (!isInitialized)
        {
            if (enableNullChecks && Time.time > initializationRetryTimer)
            {
                initializationRetryTimer = Time.time + 1f; // Tentar novamente em 1 segundo
                TryInitializeComponents();
            }
            return false;
        }
        
        if (enableNullChecks)
        {
            if (player == null || playerStats == null || skillController == null)
            {
                if (showDebugLogs) Debug.LogWarning("[SkillBarUI] Componentes críticos são null durante validação");
                isInitialized = false;
                return false;
            }
        }
        
        return true;
    }
    
    private List<Skill> GetSkillsSafely()
    {
        if (!ValidateComponents()) return new List<Skill>();
        
        try
        {
            var skills = skillController.GetAllSkills();
            return skills ?? new List<Skill>();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SkillBarUI] Erro ao obter skills: {e.Message}");
            return new List<Skill>();
        }
    }
    
    #endregion
    
    #region Event Handlers
    
    public void OnSkillUsed(SkillUsedEvent eventData)
    {
        if (!ValidateComponents()) return;
        
        var skills = GetSkillsSafely();
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] == eventData.skill)
            {
                QueueSkillAnimation(i);
                break;
            }
        }
    }
    
    private void OnSkillCooldownChanged(SkillCooldownChangedEvent eventData)
    {
        if (slotDataCache.ContainsKey(eventData.skillIndex))
        {
            var slotData = slotDataCache[eventData.skillIndex];
            slotData.UpdateCooldown(eventData.remainingCooldown, eventData.totalCooldown);
        }
    }
    
    private void OnPlayerManaChanged(PlayerManaChangedEvent eventData)
    {
        if (eventData.currentMana != lastPlayerMana)
        {
            lastPlayerMana = eventData.currentMana;
            MarkForAvailabilityUpdate();
        }
    }
    
    #endregion
    
    #region Update Loops (Optimized)
    
    private void Update()
    {
        if (!ValidateComponents()) return;
        
        UpdateTimers();
        ProcessAnimationQueue();
        
        // Staggered updates for performance
        if (uiUpdateTimer <= 0f)
        {
            UpdateSelection();
            UpdateSkillAvailability();
            uiUpdateTimer = uiUpdateInterval;
        }
        
        if (cooldownUpdateTimer <= 0f)
        {
            UpdateCooldowns();
            cooldownUpdateTimer = cooldownUpdateInterval;
        }
        
        if (tooltipTimer <= 0f)
        {
            CheckSkillHover();
            tooltipTimer = tooltipCheckInterval;
        }
        
        UpdatePulseEffect();
    }
    
    private void UpdateTimers()
    {
        uiUpdateTimer -= Time.deltaTime;
        cooldownUpdateTimer -= Time.deltaTime;
        tooltipTimer -= Time.deltaTime;
        pulseTimer += Time.deltaTime;
    }
    
    private void ProcessAnimationQueue()
    {
        // Sistema simplificado sem fila - animações são tratadas diretamente
    }
    
    #endregion
    
    #region Core UI Updates
    
    public void RefreshSkillBar()
    {
        if (!ValidateComponents())
        {
            if (showDebugLogs) Debug.Log("[SkillBarUI] RefreshSkillBar - Componentes não validados");
            return;
        }
        
        var skills = GetSkillsSafely();
        if (skills == null || skills.Count == 0)
        {
            if (showDebugLogs) Debug.Log("[SkillBarUI] RefreshSkillBar - Nenhuma skill encontrada");
            return;
        }
        
        // Update each slot efficiently
        for (int i = 0; i < slotDataCache.Count; i++)
        {
            var slotData = slotDataCache[i];
            bool hasSkill = i < skills.Count;
            
            if (slotData.slotObject != null)
            {
                slotData.slotObject.SetActive(hasSkill);
            }
            
            if (hasSkill)
            {
                Skill skill = skills[i];
                
                // CORREÇÃO: Verificar se skill é válida
                if (skill == null)
                {
                    if (showDebugLogs) Debug.LogWarning($"[SkillBarUI] Skill no índice {i} é null");
                    continue;
                }
                
                // Update icon color
                if (slotData.icon != null && skillColorCache.ContainsKey(skill.type))
                {
                    Color skillColor = skillColorCache[skill.type];
                    
                    // CORREÇÃO: Usar método seguro para CanUse
                    bool canUse = CanSkillBeUsedSafely(skill);
                    slotData.UpdateAvailability(canUse, skillColor);
                }
                
                // Configure cooldown overlay
                if (slotData.cooldownOverlay != null)
                {
                    slotData.cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
                    slotData.cooldownOverlay.fillOrigin = 0;
                    slotData.cooldownOverlay.gameObject.SetActive(false);
                }
            }
        }
        
        UpdateSelection();
        MarkForAvailabilityUpdate();
    }
    
    // MÉTODO SEGURO para verificar se a skill pode ser usada
    private bool CanSkillBeUsedSafely(Skill skill)
    {
        if (skill == null) return false;
        if (playerStats == null) return false;
        
        try
        {
            return skill.CanUse(playerStats);
        }
        catch (System.Exception e)
        {
            if (showDebugLogs) Debug.LogError($"[SkillBarUI] Erro ao verificar CanUse para skill {skill.name}: {e.Message}");
            return false;
        }
    }
    
    public void UpdateSelection()
    {
        if (!ValidateComponents()) return;
        
        if (skillController.currentSkillIndex != lastCurrentSkillIndex)
        {
            MoveSelectionIndicator();
            lastCurrentSkillIndex = skillController.currentSkillIndex;
        }
    }
    
    private void UpdateSkillAvailability()
    {
        var skills = GetSkillsSafely();
        if (skills == null || skills.Count == 0) return;
        
        for (int i = 0; i < skills.Count && i < slotDataCache.Count; i++)
        {
            if (skills[i] == null) continue;
            
            bool canUse = CanSkillBeUsedSafely(skills[i]) && !skillController.IsSkillOnCooldown(i);
            
            if (i < lastSkillAvailability.Length && lastSkillAvailability[i] != canUse)
            {
                lastSkillAvailability[i] = canUse;
                UpdateSlotAvailability(i, skills[i], canUse);
            }
        }
    }
    
    private void UpdateCooldowns()
    {
        var skills = GetSkillsSafely();
        if (skills == null || skills.Count == 0) return;
        
        for (int i = 0; i < skills.Count && i < slotDataCache.Count; i++)
        {
            if (skills[i] == null) continue;
            
            float cooldownRemaining = skillController.GetSkillCooldownRemaining(i);
            
            if (i < lastCooldownTimes.Length && Mathf.Abs(lastCooldownTimes[i] - cooldownRemaining) > 0.01f)
            {
                lastCooldownTimes[i] = cooldownRemaining;
                
                if (slotDataCache.ContainsKey(i))
                {
                    float maxCooldown = skills[i].GetActualCooldown(playerStats);
                    slotDataCache[i].UpdateCooldown(cooldownRemaining, maxCooldown);
                }
                
                // Trigger cooldown event for other systems
                EventManager.TriggerEvent(new SkillCooldownChangedEvent
                {
                    skillIndex = i,
                    remainingCooldown = cooldownRemaining,
                    totalCooldown = skills[i].GetActualCooldown(playerStats)
                });
            }
        }
    }
    
    #endregion
    
    #region Visual Updates
    
    private void UpdateSlotAvailability(int index, Skill skill, bool canUse)
    {
        if (!slotDataCache.ContainsKey(index) || skill == null) return;
        
        var slotData = slotDataCache[index];
        Color baseColor = skillColorCache.ContainsKey(skill.type) ? skillColorCache[skill.type] : Color.white;
        
        Color finalColor = canUse ? baseColor : Color.Lerp(baseColor, unavailableColor, 0.7f);
        slotData.UpdateAvailability(canUse, finalColor);
    }
    
    private void MoveSelectionIndicator()
    {
        if (!ValidateComponents()) return;
        
        if (selectionIndicator != null && 
            skillController.currentSkillIndex < slotDataCache.Count && 
            slotDataCache.ContainsKey(skillController.currentSkillIndex))
        {
            var targetSlot = slotDataCache[skillController.currentSkillIndex];
            if (targetSlot.slotObject != null)
            {
                selectionIndicator.transform.position = targetSlot.slotObject.transform.position;
                selectionIndicator.gameObject.SetActive(true);
            }
        }
        else if (selectionIndicator != null)
        {
            selectionIndicator.gameObject.SetActive(false);
        }
    }
    
    private void UpdatePulseEffect()
    {
        if (!ValidateComponents() || selectionIndicator == null) return;
        
        // Subtle pulse effect on selected skill
        float pulse = 1f + (Mathf.Sin(pulseTimer * pulseSpeed) * pulseIntensity);
        selectionIndicator.transform.localScale = Vector3.one * pulse;
    }
    
    #endregion
    
    #region Animation System
    
    private void QueueSkillAnimation(int skillIndex)
    {
        if (!slotDataCache.ContainsKey(skillIndex)) return;
        
        var slotData = slotDataCache[skillIndex];
        if (slotData.icon == null) return;
        
        // Parar qualquer animação anterior
        if (activeAnimations.ContainsKey(skillIndex) && activeAnimations[skillIndex] != null)
        {
            StopCoroutine(activeAnimations[skillIndex]);
        }
        
        // Resetar escala antes de iniciar nova animação
        slotData.icon.transform.localScale = Vector3.one;
        
        // Iniciar nova animação e armazenar referência
        activeAnimations[skillIndex] = StartCoroutine(AnimateSkillUseCoroutine(skillIndex));
    }
    
    private IEnumerator AnimateSkillUseCoroutine(int skillIndex)
    {
        if (!slotDataCache.ContainsKey(skillIndex)) yield break;
        
        var slotData = slotDataCache[skillIndex];
        if (slotData.icon == null) yield break;
        
        Transform iconTransform = slotData.icon.transform;
        Vector3 originalScale = Vector3.one;
        
        iconTransform.localScale = originalScale;
        
        float elapsed = 0f;
        while (elapsed < scaleAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / scaleAnimationDuration;
            float scaleMultiplier = iconScaleOnUse.Evaluate(progress);
            
            iconTransform.localScale = originalScale * scaleMultiplier;
            yield return null;
        }
        
        iconTransform.localScale = originalScale;
        
        if (activeAnimations.ContainsKey(skillIndex))
        {
            activeAnimations.Remove(skillIndex);
        }
    }
    
    #endregion
    
    #region Tooltip System
    
    private void CheckSkillHover()
    {
        if (!ValidateComponents()) return;
        
        Vector2 mousePos = Input.mousePosition;
        bool foundHover = false;
        
        var skills = GetSkillsSafely();
        if (skills == null || skills.Count == 0) return;
        
        for (int i = 0; i < slotDataCache.Count && i < skills.Count; i++)
        {
            if (skills[i] == null) continue;
            
            var slotData = slotDataCache[i];
            if (slotData.slotObject != null)
            {
                RectTransform rectTransform = slotData.slotObject.GetComponent<RectTransform>();
                if (rectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePos))
                {
                    if (hoveredSlotIndex != i)
                    {
                        ShowSkillInfo(skills[i], i);
                        hoveredSlotIndex = i;
                    }
                    foundHover = true;
                    break;
                }
            }
        }
        
        if (!foundHover && isTooltipShowing)
        {
            HideSkillInfo();
            hoveredSlotIndex = -1;
        }
    }
    
    private void ShowSkillInfo(Skill skill, int slotIndex)
    {
        if (skillInfoPanel == null || skill == null) return;
        
        skillInfoPanel.SetActive(true);
        isTooltipShowing = true;
        
        if (skillNameText != null)
            skillNameText.text = skill.name;
        
        if (skillDescriptionText != null)
            skillDescriptionText.text = skill.GetDetailedDescription(playerStats);
        
        if (skillStatsText != null)
        {
            skillStatsText.text = BuildSkillStatsText(skill);
        }
        
        if (skillInfoIcon != null && skillColorCache.ContainsKey(skill.type))
        {
            skillInfoIcon.color = skillColorCache[skill.type];
        }
        
        PositionTooltip();
    }
    
    private string BuildSkillStatsText(Skill skill)
    {
        if (skill == null) return "Skill inválida";
        
        var stats = new System.Text.StringBuilder();
        stats.AppendLine("<color=yellow>Estatísticas:</color>");
        
        try
        {
            stats.AppendLine($"Dano: {skill.GetActualDamage(playerStats, player?.inventory?.equippedWeapon)}");
            stats.AppendLine($"Custo de Mana: {skill.GetActualManaCost(playerStats)}");
            stats.AppendLine($"Cooldown: {skill.GetActualCooldown(playerStats):F1}s");
            stats.AppendLine($"Alcance: {skill.GetActualRange(playerStats):F1}m");
            
            if (skill.areaRadius > 0)
            {
                stats.AppendLine($"Área: {skill.GetActualAreaRadius(playerStats):F1}m");
            }
        }
        catch (System.Exception e)
        {
            stats.AppendLine("<color=red>Erro ao calcular estatísticas</color>");
            if (showDebugLogs) Debug.LogError($"[SkillBarUI] Erro ao construir stats text: {e.Message}");
        }
        
        return stats.ToString();
    }
    
    private void PositionTooltip()
    {
        if (skillInfoPanel == null) return;
        
        Vector2 mousePos = Input.mousePosition;
        RectTransform panelRect = skillInfoPanel.GetComponent<RectTransform>();
        RectTransform canvasRect = transform.root.GetComponent<RectTransform>();
        
        if (panelRect != null && canvasRect != null)
        {
            Vector2 localMousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, null, out localMousePos);
            
            Vector2 offset = new Vector2(10, -10);
            Vector2 tooltipPos = localMousePos + offset;
            
            // Clamp to screen bounds
            Vector2 panelSize = panelRect.sizeDelta;
            Vector2 canvasSize = canvasRect.sizeDelta;
            
            if (tooltipPos.x + panelSize.x > canvasSize.x / 2)
            {
                tooltipPos.x = localMousePos.x - panelSize.x - 10;
            }
            
            if (tooltipPos.y - panelSize.y < -canvasSize.y / 2)
            {
                tooltipPos.y = localMousePos.y + panelSize.y + 10;
            }
            
            panelRect.anchoredPosition = tooltipPos;
        }
    }
    
    private void HideSkillInfo()
    {
        if (skillInfoPanel != null)
        {
            skillInfoPanel.SetActive(false);
            isTooltipShowing = false;
        }
    }
    
    #endregion
    
    #region Setup Methods
    
    private void SetupHotkeys()
    {
        string[] hotkeys = { "1", "2", "3", "4", "5", "6", "7", "8" };
        
        for (int i = 0; i < slotDataCache.Count && i < hotkeys.Length; i++)
        {
            var slotData = slotDataCache[i];
            if (slotData.hotkeyText != null)
            {
                slotData.hotkeyText.text = hotkeys[i];
            }
        }
    }
    
    private void SetupButtonEvents()
    {
        for (int i = 0; i < slotDataCache.Count; i++)
        {
            var slotData = slotDataCache[i];
            int skillIndex = i; // Capture for closure
            
            Button slotButton = slotData.slotObject?.GetComponent<Button>();
            if (slotButton != null)
            {
                slotButton.onClick.AddListener(() => SelectSkill(skillIndex));
            }
        }
    }
    
    private void SelectSkill(int skillIndex)
    {
        if (ValidateComponents())
        {
            skillController.SelectSkill(skillIndex);
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    private void MarkForAvailabilityUpdate()
    {
        uiUpdateTimer = 0f;
    }
    
    public void ForceRefresh()
    {
        if (!ValidateComponents()) return;
        
        uiUpdateTimer = 0f;
        cooldownUpdateTimer = 0f;
        RefreshSkillBar();
    }
    
    private static GameObject GetPooledEffect()
    {
        return effectPool.Count > 0 ? effectPool.Dequeue() : new GameObject("Effect");
    }
    
    private static void ReturnPooledEffect(GameObject effect)
    {
        effect.SetActive(false);
        effectPool.Enqueue(effect);
    }
    
    #endregion
    
    #region Cleanup
    
    private void OnDestroy()
    {
        // Parar todas as animações ativas
        foreach (var animation in activeAnimations.Values)
        {
            if (animation != null)
                StopCoroutine(animation);
        }
        activeAnimations.Clear();
        
        EventManager.Unsubscribe<SkillUsedEvent>(OnSkillUsed);
        EventManager.Unsubscribe<SkillCooldownChangedEvent>(OnSkillCooldownChanged);
        EventManager.Unsubscribe<PlayerManaChangedEvent>(OnPlayerManaChanged);
    }
    
    #endregion
}