using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// AttributeDistributionUI otimizado - elimina FindObjectOfType, usa cache
/// </summary>
public class AttributeDistributionUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject attributePanel;
    public GameObject levelUpNotification;
    
    [Header("Attribute Display")]
    public TextMeshProUGUI strengthText;
    public TextMeshProUGUI intelligenceText;
    public TextMeshProUGUI dexterityText;
    public TextMeshProUGUI vitalityText;
    public TextMeshProUGUI availablePointsText;
    
    [Header("Attribute Buttons")]
    public Button strengthPlusButton;
    public Button intelligencePlusButton;
    public Button dexterityPlusButton;
    public Button vitalityPlusButton;
    
    [Header("Stats Preview")]
    public TextMeshProUGUI healthPreview;
    public TextMeshProUGUI manaPreview;
    public TextMeshProUGUI critChancePreview;
    public TextMeshProUGUI physicalResPreview;
    
    [Header("Controls")]
    public Button confirmButton;
    public Button resetButton;
    public Button closeButton;
    public KeyCode toggleKey = KeyCode.C; // Character screen
    
    [Header("Level Up Effect")]
    public GameObject levelUpParticleEffect;
    public AudioClip levelUpSound;
    public float notificationDuration = 3f;
    
    // Cache para evitar FindObjectOfType
    private PlayerController cachedPlayer;
    private PlayerStats cachedPlayerStats;
    private AudioSource audioSource;
    private bool playerCacheValid = false;
    
    // Temporary attribute allocation (before confirming)
    private int tempStrengthPoints = 0;
    private int tempIntelligencePoints = 0;
    private int tempDexterityPoints = 0;
    private int tempVitalityPoints = 0;
    private int tempPointsUsed = 0;
    
    private void Awake()
    {
        // Setup button listeners
        if (strengthPlusButton != null)
            strengthPlusButton.onClick.AddListener(() => AddTempAttribute("strength"));
        if (intelligencePlusButton != null)
            intelligencePlusButton.onClick.AddListener(() => AddTempAttribute("intelligence"));
        if (dexterityPlusButton != null)
            dexterityPlusButton.onClick.AddListener(() => AddTempAttribute("dexterity"));
        if (vitalityPlusButton != null)
            vitalityPlusButton.onClick.AddListener(() => AddTempAttribute("vitality"));
        
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmAttributeDistribution);
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetTempAttributes);
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
        
        // Get audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    private void Start()
    {
        // Tentar encontrar player e stats usando cache
        TryCachePlayer();
        
        // Subscribe to EventManager events instead of PlayerStats events
        SubscribeToEvents();
        
        // Initialize UI
        if (attributePanel != null)
            attributePanel.SetActive(false);
        if (levelUpNotification != null)
            levelUpNotification.SetActive(false);
        
        UpdateUI();
    }
    
    private void SubscribeToEvents()
    {
        // Subscribe to EventManager events
        EventManager.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        EventManager.Subscribe<PlayerAttributeChangedEvent>(OnAttributeChanged);
        EventManager.Subscribe<PlayerStatsRecalculatedEvent>(OnStatsRecalculated);
        EventManager.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        EventManager.Subscribe<PlayerDestroyedEvent>(OnPlayerDestroyed);
        EventManager.Subscribe<SceneTransitionEvent>(OnSceneTransition);
    }
    
    private void TryCachePlayer()
    {
        // Usar singleton em vez de FindObjectOfType
        if (PlayerController.Instance != null)
        {
            cachedPlayer = PlayerController.Instance;
            cachedPlayerStats = cachedPlayer.GetStats();
            playerCacheValid = true;
        }
    }
    
    private void Update()
    {
        // Toggle character screen
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleAttributePanel();
        }
        
        // Tentar cachear player se ainda não temos
        if (!playerCacheValid)
        {
            TryCachePlayer();
        }
        
        // Update button states
        UpdateButtonStates();
    }
    
    #region Event Handlers
    
    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        cachedPlayer = eventData.player.GetComponent<PlayerController>();
        if (cachedPlayer != null)
        {
            cachedPlayerStats = cachedPlayer.GetStats();
            playerCacheValid = true;
        }
        UpdateUI();
    }
    
    private void OnPlayerDestroyed(PlayerDestroyedEvent eventData)
    {
        cachedPlayer = null;
        cachedPlayerStats = null;
        playerCacheValid = false;
    }
    
    private void OnSceneTransition(SceneTransitionEvent eventData)
    {
        cachedPlayer = null;
        cachedPlayerStats = null;
        playerCacheValid = false;
    }
    
    private void OnPlayerLevelUp(PlayerLevelUpEvent eventData)
    {
        // Show level up notification
        ShowLevelUpNotification();
        
        // Play effects
        PlayLevelUpEffects();
        
        // Auto-open attribute panel if player gained points
        if (cachedPlayerStats != null && cachedPlayerStats.AvailableAttributePoints > 0)
        {
            StartCoroutine(AutoOpenAttributePanel());
        }
        
        UpdateUI();
    }
    
    private void OnAttributeChanged(PlayerAttributeChangedEvent eventData)
    {
        UpdateUI();
    }
    
    private void OnStatsRecalculated(PlayerStatsRecalculatedEvent eventData)
    {
        UpdateUI();
    }
    
    #endregion
    
    private IEnumerator AutoOpenAttributePanel()
    {
        // Wait a bit after level up notification
        yield return new WaitForSeconds(2f);
        
        if (attributePanel != null && !attributePanel.activeSelf)
        {
            ToggleAttributePanel();
        }
    }
    
    private void ShowLevelUpNotification()
    {
        if (levelUpNotification != null && cachedPlayerStats != null)
        {
            levelUpNotification.SetActive(true);
            
            // Find the level text in the notification
            TextMeshProUGUI levelText = levelUpNotification.GetComponentInChildren<TextMeshProUGUI>();
            if (levelText != null)
            {
                levelText.text = $"LEVEL UP!\nLevel {cachedPlayerStats.Level}";
            }
            
            // Hide after duration
            StartCoroutine(HideNotificationAfterDelay());
        }
    }
    
    private IEnumerator HideNotificationAfterDelay()
    {
        yield return new WaitForSeconds(notificationDuration);
        if (levelUpNotification != null)
        {
            levelUpNotification.SetActive(false);
        }
    }
    
    private void PlayLevelUpEffects()
    {
        // Play sound
        if (audioSource != null && levelUpSound != null)
        {
            audioSource.PlayOneShot(levelUpSound);
        }
        
        // Play particle effect usando cache
        if (levelUpParticleEffect != null && cachedPlayer != null)
        {
            Instantiate(levelUpParticleEffect, cachedPlayer.transform.position, Quaternion.identity);
        }
    }
    
    public void ToggleAttributePanel()
    {
        if (attributePanel != null)
        {
            bool isActive = attributePanel.activeSelf;
            attributePanel.SetActive(!isActive);
            
            if (!isActive)
            {
                // Opening panel
                ResetTempAttributes();
                UpdateUI();
                
                // Pause the game slightly
                Time.timeScale = 0.1f;
            }
            else
            {
                // Closing panel
                Time.timeScale = 1f;
            }
        }
    }
    
    private void ClosePanel()
    {
        if (attributePanel != null)
        {
            attributePanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }
    
    private void AddTempAttribute(string attribute)
    {
        if (!playerCacheValid || cachedPlayerStats == null) return;
        
        if (tempPointsUsed >= cachedPlayerStats.AvailableAttributePoints)
            return;
        
        switch (attribute.ToLower())
        {
            case "strength":
                tempStrengthPoints++;
                break;
            case "intelligence":
                tempIntelligencePoints++;
                break;
            case "dexterity":
                tempDexterityPoints++;
                break;
            case "vitality":
                tempVitalityPoints++;
                break;
        }
        
        tempPointsUsed++;
        UpdateUI();
        
        // Play UI sound
        // AudioManager.Instance?.PlayUISound("AttributePoint");
    }
    
    private void ResetTempAttributes()
    {
        tempStrengthPoints = 0;
        tempIntelligencePoints = 0;
        tempDexterityPoints = 0;
        tempVitalityPoints = 0;
        tempPointsUsed = 0;
        UpdateUI();
    }
    
    private void ConfirmAttributeDistribution()
    {
        if (!playerCacheValid || cachedPlayerStats == null) return;
        
        // Apply all temp points
        for (int i = 0; i < tempStrengthPoints; i++)
            cachedPlayerStats.SpendAttributePoint("strength");
        for (int i = 0; i < tempIntelligencePoints; i++)
            cachedPlayerStats.SpendAttributePoint("intelligence");
        for (int i = 0; i < tempDexterityPoints; i++)
            cachedPlayerStats.SpendAttributePoint("dexterity");
        for (int i = 0; i < tempVitalityPoints; i++)
            cachedPlayerStats.SpendAttributePoint("vitality");
        
        // Reset temp points
        ResetTempAttributes();
        
        // Close panel if no more points
        if (cachedPlayerStats.AvailableAttributePoints == 0)
        {
            ClosePanel();
        }
        
        // Show confirmation notification
        EventManager.TriggerEvent(new NotificationEvent
        {
            message = "Atributos distribuídos com sucesso!",
            type = NotificationType.Success,
            duration = 2f,
            color = Color.green
        });
        
        Debug.Log("Atributos distribuídos com sucesso!");
    }
    
    private void UpdateUI()
    {
        if (!playerCacheValid || cachedPlayerStats == null) return;
        
        // Update attribute displays with temp points
        if (strengthText != null)
        {
            int totalStr = cachedPlayerStats.Strength + tempStrengthPoints;
            strengthText.text = tempStrengthPoints > 0 ? 
                $"<color=white>{cachedPlayerStats.Strength}</color> <color=yellow>(+{tempStrengthPoints}) = {totalStr}</color>" : 
                $"{cachedPlayerStats.Strength}";
        }
        
        if (intelligenceText != null)
        {
            int totalInt = cachedPlayerStats.Intelligence + tempIntelligencePoints;
            intelligenceText.text = tempIntelligencePoints > 0 ? 
                $"<color=white>{cachedPlayerStats.Intelligence}</color> <color=yellow>(+{tempIntelligencePoints}) = {totalInt}</color>" : 
                $"{cachedPlayerStats.Intelligence}";
        }
        
        if (dexterityText != null)
        {
            int totalDex = cachedPlayerStats.Dexterity + tempDexterityPoints;
            dexterityText.text = tempDexterityPoints > 0 ? 
                $"<color=white>{cachedPlayerStats.Dexterity}</color> <color=yellow>(+{tempDexterityPoints}) = {totalDex}</color>" : 
                $"{cachedPlayerStats.Dexterity}";
        }
        
        if (vitalityText != null)
        {
            int totalVit = cachedPlayerStats.Vitality + tempVitalityPoints;
            vitalityText.text = tempVitalityPoints > 0 ? 
                $"<color=white>{cachedPlayerStats.Vitality}</color> <color=yellow>(+{tempVitalityPoints}) = {totalVit}</color>" : 
                $"{cachedPlayerStats.Vitality}";
        }
        
        // Update available points
        if (availablePointsText != null)
        {
            int remainingPoints = cachedPlayerStats.AvailableAttributePoints - tempPointsUsed;
            availablePointsText.text = $"Pontos Disponíveis: {remainingPoints}";
            
            // Change color based on points
            if (remainingPoints > 0)
                availablePointsText.color = Color.yellow;
            else
                availablePointsText.color = Color.white;
        }
        
        // Update stat previews (showing what stats would be with temp points)
        UpdateStatPreviews();
    }
    
    private void UpdateStatPreviews()
    {
        if (!playerCacheValid || cachedPlayerStats == null) return;
        
        // Calculate temporary stats
        int tempStrTotal = cachedPlayerStats.Strength + tempStrengthPoints;
        int tempIntTotal = cachedPlayerStats.Intelligence + tempIntelligencePoints;
        int tempDexTotal = cachedPlayerStats.Dexterity + tempDexterityPoints;
        int tempVitTotal = cachedPlayerStats.Vitality + tempVitalityPoints;
        
        // Calculate preview values
        int previewMaxHealth = 100 + (cachedPlayerStats.Level * 5) + (tempVitTotal * 8);
        int previewMaxMana = 50 + (cachedPlayerStats.Level * 3) + (tempIntTotal * 6);
        float previewCritChance = Mathf.Min(0.05f + (tempDexTotal * 0.002f), 0.75f);
        float previewPhysRes = Mathf.Min(tempVitTotal * 0.003f, 0.75f);
        
        // Update preview texts
        if (healthPreview != null)
        {
            healthPreview.text = previewMaxHealth != cachedPlayerStats.MaxHealth ? 
                $"<color=white>{cachedPlayerStats.MaxHealth}</color> → <color=green>{previewMaxHealth}</color>" : 
                $"{cachedPlayerStats.MaxHealth}";
        }
        
        if (manaPreview != null)
        {
            manaPreview.text = previewMaxMana != cachedPlayerStats.MaxMana ? 
                $"<color=white>{cachedPlayerStats.MaxMana}</color> → <color=blue>{previewMaxMana}</color>" : 
                $"{cachedPlayerStats.MaxMana}";
        }
        
        if (critChancePreview != null)
        {
            critChancePreview.text = !Mathf.Approximately(previewCritChance, cachedPlayerStats.CriticalChance) ? 
                $"<color=white>{cachedPlayerStats.CriticalChance:P1}</color> → <color=red>{previewCritChance:P1}</color>" : 
                $"{cachedPlayerStats.CriticalChance:P1}";
        }
        
        if (physicalResPreview != null)
        {
            physicalResPreview.text = !Mathf.Approximately(previewPhysRes, cachedPlayerStats.PhysicalResistance) ? 
                $"<color=white>{cachedPlayerStats.PhysicalResistance:P1}</color> → <color=orange>{previewPhysRes:P1}</color>" : 
                $"{cachedPlayerStats.PhysicalResistance:P1}";
        }
    }
    
    private void UpdateButtonStates()
    {
        if (!playerCacheValid || cachedPlayerStats == null) return;
        
        bool canAddPoints = tempPointsUsed < cachedPlayerStats.AvailableAttributePoints;
        
        if (strengthPlusButton != null)
            strengthPlusButton.interactable = canAddPoints;
        if (intelligencePlusButton != null)
            intelligencePlusButton.interactable = canAddPoints;
        if (dexterityPlusButton != null)
            dexterityPlusButton.interactable = canAddPoints;
        if (vitalityPlusButton != null)
            vitalityPlusButton.interactable = canAddPoints;
        
        // Enable/disable confirm and reset buttons
        if (confirmButton != null)
            confirmButton.interactable = tempPointsUsed > 0;
        if (resetButton != null)
            resetButton.interactable = tempPointsUsed > 0;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        EventManager.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        EventManager.Unsubscribe<PlayerAttributeChangedEvent>(OnAttributeChanged);
        EventManager.Unsubscribe<PlayerStatsRecalculatedEvent>(OnStatsRecalculated);
        EventManager.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        EventManager.Unsubscribe<PlayerDestroyedEvent>(OnPlayerDestroyed);
        EventManager.Unsubscribe<SceneTransitionEvent>(OnSceneTransition);
    }
}