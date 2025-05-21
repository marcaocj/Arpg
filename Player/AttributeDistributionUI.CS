using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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
    
    private PlayerController player;
    private PlayerStats playerStats;
    private AudioSource audioSource;
    
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
        // Find player and stats
        player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            playerStats = player.GetStats();
            
            // Subscribe to events
            playerStats.OnLevelUp += OnPlayerLevelUp;
            playerStats.OnAttributePointsGained += OnAttributePointsGained;
            playerStats.OnAttributeChanged += UpdateUI;
        }
        
        // Initialize UI
        if (attributePanel != null)
            attributePanel.SetActive(false);
        if (levelUpNotification != null)
            levelUpNotification.SetActive(false);
        
        UpdateUI();
    }
    
    private void Update()
    {
        // Toggle character screen
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleAttributePanel();
        }
        
        // Update button states
        UpdateButtonStates();
    }
    
    private void OnPlayerLevelUp(int newLevel)
    {
        // Show level up notification
        ShowLevelUpNotification();
        
        // Play effects
        PlayLevelUpEffects();
        
        // Auto-open attribute panel if player gained points
        if (playerStats.AvailableAttributePoints > 0)
        {
            StartCoroutine(AutoOpenAttributePanel());
        }
    }
    
    private void OnAttributePointsGained(int points)
    {
        Debug.Log($"Jogador ganhou {points} pontos de atributo!");
        UpdateUI();
    }
    
    private IEnumerator AutoOpenAttributePanel()
    {
        // Wait a bit after level up notification
        yield return new WaitForSeconds(2f);
        
        if (!attributePanel.activeSelf)
        {
            ToggleAttributePanel();
        }
    }
    
    private void ShowLevelUpNotification()
    {
        if (levelUpNotification != null)
        {
            levelUpNotification.SetActive(true);
            
            // Find the level text in the notification
            TextMeshProUGUI levelText = levelUpNotification.GetComponentInChildren<TextMeshProUGUI>();
            if (levelText != null)
            {
                levelText.text = $"LEVEL UP!\nLevel {playerStats.Level}";
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
        
        // Play particle effect
        if (levelUpParticleEffect != null && player != null)
        {
            Instantiate(levelUpParticleEffect, player.transform.position, Quaternion.identity);
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
        if (tempPointsUsed >= playerStats.AvailableAttributePoints)
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
        if (playerStats == null) return;
        
        // Apply all temp points
        for (int i = 0; i < tempStrengthPoints; i++)
            playerStats.SpendAttributePoint("strength");
        for (int i = 0; i < tempIntelligencePoints; i++)
            playerStats.SpendAttributePoint("intelligence");
        for (int i = 0; i < tempDexterityPoints; i++)
            playerStats.SpendAttributePoint("dexterity");
        for (int i = 0; i < tempVitalityPoints; i++)
            playerStats.SpendAttributePoint("vitality");
        
        // Reset temp points
        ResetTempAttributes();
        
        // Close panel if no more points
        if (playerStats.AvailableAttributePoints == 0)
        {
            ClosePanel();
        }
        
        // Play confirmation sound
        // AudioManager.Instance?.PlayUISound("Confirm");
        
        Debug.Log("Atributos distribuídos com sucesso!");
    }
    
    private void UpdateUI()
    {
        if (playerStats == null) return;
        
        // Update attribute displays with temp points
        if (strengthText != null)
        {
            int totalStr = playerStats.Strength + tempStrengthPoints;
            strengthText.text = tempStrengthPoints > 0 ? 
                $"<color=white>{playerStats.Strength}</color> <color=yellow>(+{tempStrengthPoints}) = {totalStr}</color>" : 
                $"{playerStats.Strength}";
        }
        
        if (intelligenceText != null)
        {
            int totalInt = playerStats.Intelligence + tempIntelligencePoints;
            intelligenceText.text = tempIntelligencePoints > 0 ? 
                $"<color=white>{playerStats.Intelligence}</color> <color=yellow>(+{tempIntelligencePoints}) = {totalInt}</color>" : 
                $"{playerStats.Intelligence}";
        }
        
        if (dexterityText != null)
        {
            int totalDex = playerStats.Dexterity + tempDexterityPoints;
            dexterityText.text = tempDexterityPoints > 0 ? 
                $"<color=white>{playerStats.Dexterity}</color> <color=yellow>(+{tempDexterityPoints}) = {totalDex}</color>" : 
                $"{playerStats.Dexterity}";
        }
        
        if (vitalityText != null)
        {
            int totalVit = playerStats.Vitality + tempVitalityPoints;
            vitalityText.text = tempVitalityPoints > 0 ? 
                $"<color=white>{playerStats.Vitality}</color> <color=yellow>(+{tempVitalityPoints}) = {totalVit}</color>" : 
                $"{playerStats.Vitality}";
        }
        
        // Update available points
        if (availablePointsText != null)
        {
            int remainingPoints = playerStats.AvailableAttributePoints - tempPointsUsed;
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
        // Calculate temporary stats
        int tempStrTotal = playerStats.Strength + tempStrengthPoints;
        int tempIntTotal = playerStats.Intelligence + tempIntelligencePoints;
        int tempDexTotal = playerStats.Dexterity + tempDexterityPoints;
        int tempVitTotal = playerStats.Vitality + tempVitalityPoints;
        
        // Calculate preview values
        int previewMaxHealth = 100 + (playerStats.Level * 5) + (tempVitTotal * 8);
        int previewMaxMana = 50 + (playerStats.Level * 3) + (tempIntTotal * 6);
        float previewCritChance = Mathf.Min(0.05f + (tempDexTotal * 0.002f), 0.75f);
        float previewPhysRes = Mathf.Min(tempVitTotal * 0.003f, 0.75f);
        
        // Update preview texts
        if (healthPreview != null)
        {
            healthPreview.text = previewMaxHealth != playerStats.MaxHealth ? 
                $"<color=white>{playerStats.MaxHealth}</color> → <color=green>{previewMaxHealth}</color>" : 
                $"{playerStats.MaxHealth}";
        }
        
        if (manaPreview != null)
        {
            manaPreview.text = previewMaxMana != playerStats.MaxMana ? 
                $"<color=white>{playerStats.MaxMana}</color> → <color=blue>{previewMaxMana}</color>" : 
                $"{playerStats.MaxMana}";
        }
        
        if (critChancePreview != null)
        {
            critChancePreview.text = !Mathf.Approximately(previewCritChance, playerStats.CriticalChance) ? 
                $"<color=white>{playerStats.CriticalChance:P1}</color> → <color=red>{previewCritChance:P1}</color>" : 
                $"{playerStats.CriticalChance:P1}";
        }
        
        if (physicalResPreview != null)
        {
            physicalResPreview.text = !Mathf.Approximately(previewPhysRes, playerStats.PhysicalResistance) ? 
                $"<color=white>{playerStats.PhysicalResistance:P1}</color> → <color=orange>{previewPhysRes:P1}</color>" : 
                $"{playerStats.PhysicalResistance:P1}";
        }
    }
    
    private void UpdateButtonStates()
    {
        bool canAddPoints = tempPointsUsed < playerStats.AvailableAttributePoints;
        
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
        if (playerStats != null)
        {
            playerStats.OnLevelUp -= OnPlayerLevelUp;
            playerStats.OnAttributePointsGained -= OnAttributePointsGained;
            playerStats.OnAttributeChanged -= UpdateUI;
        }
    }
}