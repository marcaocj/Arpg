using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

// Adicionar using para o namespace RPG.UI.Quest
using RPG.UI.Quest;

/// <summary>
/// Item de quest melhorado para UI com hover effects, progress tracking e visual feedback
/// CORRIGIDO: Conflito de namespace com Quest resolvido usando referência global explícita
/// </summary>
public class QuestListItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Components")]
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI questDescriptionText;
    public TextMeshProUGUI questStatusText;
    public TextMeshProUGUI questCategoryText;
    public TextMeshProUGUI questTimeText;
    public Slider progressSlider;
    public TextMeshProUGUI progressText;
    public Button itemButton;
    public Image backgroundImage;
    public Image iconImage;
    public Image priorityIndicator;
    public Image statusIndicator;
    
    [Header("Visual Effects")]
    public GameObject glowEffect;
    public ParticleSystem completeEffect;
    public Image[] objectiveCheckmarks;
    
    [Header("Animation Settings")]
    public float hoverScale = 1.05f;
    public float animationDuration = 0.2f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve glowCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Colors")]
    public Color normalBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color hoverBackgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
    public Color selectedBackgroundColor = new Color(0.4f, 0.4f, 0.1f, 0.9f);
    public Color completedBackgroundColor = new Color(0.1f, 0.4f, 0.1f, 0.8f);
    public Color urgentBackgroundColor = new Color(0.4f, 0.1f, 0.1f, 0.8f);
    
    // State - USANDO REFERÊNCIA GLOBAL EXPLÍCITA
    private global::Quest quest; // EXPLÍCITO: usar Quest do namespace global
    private QuestUI questUI;
    private bool isHovered = false;
    private bool isSelected = false;
    private Vector3 originalScale;
    private Color originalBackgroundColor;
    private Coroutine currentAnimation;
    private Coroutine pulseAnimation;
    
    // Cache for performance
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    
    #region Initialization
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        originalScale = transform.localScale;
        
        if (backgroundImage != null)
            originalBackgroundColor = backgroundImage.color;
        
        // Setup button if exists
        if (itemButton == null)
            itemButton = GetComponent<Button>();
            
        if (itemButton != null)
            itemButton.onClick.AddListener(OnClick);
    }
    
    public void SetupQuest(global::Quest quest, QuestUI questUI) // EXPLÍCITO
    {
        this.quest = quest;
        this.questUI = questUI;
        
        if (quest == null)
        {
            Debug.LogError("QuestListItem: Quest é nulo!");
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        UpdateQuestDisplay();
        UpdateVisualState();
        StartPeriodicUpdate();
    }
    
    #endregion
    
    #region Display Updates
    
    public void UpdateQuestDisplay()
    {
        if (quest == null) return;
        
        UpdateBasicInfo();
        UpdateProgress();
        UpdateTimeRemaining();
        UpdateObjectives();
        UpdateVisualEffects();
    }
    
    private void UpdateBasicInfo()
    {
        // Title
        if (questTitleText != null)
        {
            questTitleText.text = quest.title;
            questTitleText.color = GetQuestPriorityColor(quest.priority);
        }
        
        // Description (truncated for list view)
        if (questDescriptionText != null)
        {
            string description = quest.summary;
            if (string.IsNullOrEmpty(description))
                description = quest.description;
            
            if (description.Length > 80)
                description = description.Substring(0, 77) + "...";
            
            questDescriptionText.text = description;
        }
        
        // Status
        if (questStatusText != null)
        {
            questStatusText.text = GetQuestStatusDisplay();
            questStatusText.color = GetQuestStatusColor(quest.status);
        }
        
        // Category
        if (questCategoryText != null && !string.IsNullOrEmpty(quest.category))
        {
            questCategoryText.text = $"[{quest.category}]";
            questCategoryText.color = GetCategoryColor(quest.category);
        }
        
        // Icon based on quest type
        UpdateQuestIcon();
    }
    
    private void UpdateProgress()
    {
        if (quest.status != QuestStatus.Active)
        {
            if (progressSlider != null)
                progressSlider.gameObject.SetActive(false);
            if (progressText != null)
                progressText.gameObject.SetActive(false);
            return;
        }
        
        float progress = quest.GetOverallProgress();
        
        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(true);
            progressSlider.value = progress;
            
            // Color slider based on progress
            var fillImage = progressSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                if (progress >= 1f)
                    fillImage.color = Color.green;
                else if (progress >= 0.75f)
                    fillImage.color = Color.yellow;
                else if (progress >= 0.5f)
                    fillImage.color = Color.orange;
                else
                    fillImage.color = Color.red;
            }
        }
        
        if (progressText != null)
        {
            progressText.gameObject.SetActive(true);
            progressText.text = $"{progress:P0}";
        }
    }
    
    private void UpdateTimeRemaining()
    {
        if (!quest.hasTimeLimit || quest.status != QuestStatus.Active)
        {
            if (questTimeText != null)
                questTimeText.gameObject.SetActive(false);
            return;
        }
        
        if (questTimeText != null)
        {
            questTimeText.gameObject.SetActive(true);
            questTimeText.text = quest.GetTimeRemainingText();
            
            // Color based on urgency
            float timePercentage = quest.GetTimePercentage();
            if (timePercentage < 0.1f)
            {
                questTimeText.color = Color.red;
                if (!IsUrgentPulseActive())
                    StartUrgentPulse();
            }
            else if (timePercentage < 0.25f)
            {
                questTimeText.color = new Color(1f, 0.5f, 0f); // Orange
                StopUrgentPulse();
            }
            else
            {
                questTimeText.color = Color.white;
                StopUrgentPulse();
            }
        }
    }
    
    private void UpdateObjectives()
    {
        if (objectiveCheckmarks == null || objectiveCheckmarks.Length == 0)
            return;
        
        // Show objective completion status
        for (int i = 0; i < objectiveCheckmarks.Length; i++)
        {
            if (i < quest.objectives.Count)
            {
                objectiveCheckmarks[i].gameObject.SetActive(true);
                
                var objective = quest.objectives[i];
                if (objective.isCompleted)
                {
                    objectiveCheckmarks[i].color = Color.green;
                    objectiveCheckmarks[i].sprite = GetCheckmarkSprite();
                }
                else if (objective.isOptional)
                {
                    objectiveCheckmarks[i].color = Color.gray;
                    objectiveCheckmarks[i].sprite = GetOptionalSprite();
                }
                else
                {
                    objectiveCheckmarks[i].color = Color.white;
                    objectiveCheckmarks[i].sprite = GetIncompleteSprite();
                }
            }
            else
            {
                objectiveCheckmarks[i].gameObject.SetActive(false);
            }
        }
    }
    
    private void UpdateQuestIcon()
    {
        if (iconImage == null) return;
        
        // Set icon based on quest properties
        Sprite iconSprite = GetQuestIconSprite();
        if (iconSprite != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.color = GetQuestTypeColor();
        }
        else
        {
            // Fallback to colored square
            iconImage.color = GetQuestTypeColor();
        }
    }
    
    private void UpdateVisualEffects()
    {
        // Glow effect for important quests
        if (glowEffect != null)
        {
            bool shouldGlow = quest.priority >= QuestPriority.High || 
                             quest.isMainQuest ||
                             (quest.hasTimeLimit && quest.GetTimePercentage() < 0.25f);
            glowEffect.SetActive(shouldGlow);
        }
        
        // Complete effect for recently completed quests
        if (completeEffect != null && quest.status == QuestStatus.Completed)
        {
            if (!completeEffect.isPlaying)
            {
                completeEffect.Play();
            }
        }
        else if (completeEffect != null && completeEffect.isPlaying)
        {
            completeEffect.Stop();
        }
        
        // Priority indicator
        if (priorityIndicator != null)
        {
            priorityIndicator.color = GetQuestPriorityColor(quest.priority);
            priorityIndicator.gameObject.SetActive(quest.priority > QuestPriority.Normal);
        }
        
        // Status indicator
        if (statusIndicator != null)
        {
            statusIndicator.color = GetQuestStatusColor(quest.status);
        }
    }
    
    #endregion
    
    #region Visual State Management
    
    private void UpdateVisualState()
    {
        if (backgroundImage == null) return;
        
        Color targetColor = originalBackgroundColor;
        
        // Determine background color based on state
        if (quest.status == QuestStatus.Completed || quest.status == QuestStatus.TurnedIn)
        {
            targetColor = completedBackgroundColor;
        }
        else if (quest.hasTimeLimit && quest.GetTimePercentage() < 0.1f)
        {
            targetColor = urgentBackgroundColor;
        }
        else if (isSelected)
        {
            targetColor = selectedBackgroundColor;
        }
        else if (isHovered)
        {
            targetColor = hoverBackgroundColor;
        }
        else
        {
            targetColor = normalBackgroundColor;
        }
        
        // Apply color
        backgroundImage.color = targetColor;
    }
    
    public void SetSelected(bool selected)
    {
        if (isSelected != selected)
        {
            isSelected = selected;
            UpdateVisualState();
            
            if (selected)
            {
                PlaySelectionEffect();
            }
        }
    }
    
    private void PlaySelectionEffect()
    {
        // Small scale bump when selected
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);
        
        currentAnimation = StartCoroutine(SelectionAnimation());
    }
    
    private IEnumerator SelectionAnimation()
    {
        Vector3 targetScale = originalScale * 1.02f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        
        // Scale up
        while (elapsed < animationDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (animationDuration * 0.5f);
            transform.localScale = Vector3.Lerp(startScale, targetScale, scaleCurve.Evaluate(progress));
            yield return null;
        }
        
        // Scale back
        elapsed = 0f;
        while (elapsed < animationDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (animationDuration * 0.5f);
            transform.localScale = Vector3.Lerp(targetScale, originalScale, scaleCurve.Evaluate(progress));
            yield return null;
        }
        
        transform.localScale = originalScale;
        currentAnimation = null;
    }
    
    #endregion
    
    #region Hover Effects
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateVisualState();
        
        // Scale animation
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);
        
        currentAnimation = StartCoroutine(HoverAnimation(true));
        
        // Show detailed tooltip after delay
        if (quest != null)
        {
            StartCoroutine(ShowTooltipAfterDelay());
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateVisualState();
        
        // Scale animation
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);
        
        currentAnimation = StartCoroutine(HoverAnimation(false));
        
        // Hide tooltip
        HideTooltip();
    }
    
    private IEnumerator HoverAnimation(bool hovering)
    {
        Vector3 targetScale = hovering ? originalScale * hoverScale : originalScale;
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, scaleCurve.Evaluate(progress));
            yield return null;
        }
        
        transform.localScale = targetScale;
        currentAnimation = null;
    }
    
    private IEnumerator ShowTooltipAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (isHovered && quest != null)
        {
            ShowTooltip();
        }
    }
    
    private void ShowTooltip()
    {
        // Create detailed tooltip
        EventManager.TriggerEvent(new TooltipRequestEvent
        {
            item = null, // Quest tooltips don't use items
            screenPosition = transform.position,
            show = true
        });
        
        // Alternative: Show quest details in a hover panel
        // This would be implemented with a custom quest tooltip system
    }
    
    private void HideTooltip()
    {
        EventManager.TriggerEvent(new TooltipRequestEvent
        {
            item = null,
            screenPosition = Vector3.zero,
            show = false
        });
    }
    
    #endregion
    
    #region Click Handling
    
    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick();
    }
    
    private void OnClick()
    {
        if (questUI != null && quest != null)
        {
            questUI.ShowQuestDetails(quest);
            SetSelected(true);
            
            // Play click sound
            PlayClickSound();
        }
    }
    
    private void PlayClickSound()
    {
        // Play UI click sound
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = Camera.main?.GetComponent<AudioSource>();
        
        if (audioSource != null)
        {
            AudioClip clickSound = Resources.Load<AudioClip>("Audio/UI/Click");
            if (clickSound != null)
            {
                audioSource.PlayOneShot(clickSound, 0.5f);
            }
        }
    }
    
    #endregion
    
    #region Periodic Updates
    
    private void StartPeriodicUpdate()
    {
        // Start periodic update for time-limited quests
        if (quest != null && quest.hasTimeLimit && quest.status == QuestStatus.Active)
        {
            InvokeRepeating(nameof(PeriodicUpdate), 1f, 1f);
        }
    }
    
    private void PeriodicUpdate()
    {
        if (quest == null || quest.status != QuestStatus.Active)
        {
            CancelInvoke(nameof(PeriodicUpdate));
            return;
        }
        
        UpdateTimeRemaining();
        UpdateVisualState();
    }
    
    #endregion
    
    #region Urgent Pulse Effect
    
    private void StartUrgentPulse()
    {
        if (pulseAnimation != null)
            StopCoroutine(pulseAnimation);
        
        pulseAnimation = StartCoroutine(UrgentPulseAnimation());
    }
    
    private void StopUrgentPulse()
    {
        if (pulseAnimation != null)
        {
            StopCoroutine(pulseAnimation);
            pulseAnimation = null;
        }
    }
    
    private bool IsUrgentPulseActive()
    {
        return pulseAnimation != null;
    }
    
    private IEnumerator UrgentPulseAnimation()
    {
        while (true)
        {
            // Pulse the background color
            if (backgroundImage != null)
            {
                Color originalColor = backgroundImage.color;
                Color urgentColor = Color.Lerp(originalColor, Color.red, 0.3f);
                
                // Fade to urgent
                float elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / 0.5f;
                    backgroundImage.color = Color.Lerp(originalColor, urgentColor, progress);
                    yield return null;
                }
                
                // Fade back
                elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / 0.5f;
                    backgroundImage.color = Color.Lerp(urgentColor, originalColor, progress);
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }
    }
    
    #endregion
    
    #region Color & Visual Helpers
    
    private Color GetQuestStatusColor(QuestStatus status)
    {
        switch (status)
        {
            case QuestStatus.NotStarted: return Color.white;
            case QuestStatus.Active: return Color.yellow;
            case QuestStatus.Completed: return Color.green;
            case QuestStatus.TurnedIn: return Color.blue;
            case QuestStatus.Failed: return Color.red;
            case QuestStatus.Abandoned: return Color.gray;
            default: return Color.white;
        }
    }
    
    private Color GetQuestPriorityColor(QuestPriority priority)
    {
        switch (priority)
        {
            case QuestPriority.Low: return Color.gray;
            case QuestPriority.Normal: return Color.white;
            case QuestPriority.High: return Color.yellow;
            case QuestPriority.Critical: return Color.red;
            default: return Color.white;
        }
    }
    
    private Color GetCategoryColor(string category)
    {
        // Simple hash-based color generation for categories
        int hash = category.GetHashCode();
        Random.InitState(hash);
        return new Color(Random.Range(0.5f, 1f), Random.Range(0.5f, 1f), Random.Range(0.5f, 1f));
    }
    
    private Color GetQuestTypeColor()
    {
        if (quest.isMainQuest)
            return new Color(1f, 0.8f, 0.2f); // Gold
        
        switch (quest.category?.ToLower())
        {
            case "combat": return Color.red;
            case "exploration": return Color.blue;
            case "delivery": return Color.green;
            case "collection": return Color.cyan;
            case "main story": return new Color(1f, 0.8f, 0.2f);
            default: return Color.white;
        }
    }
    
    private string GetQuestStatusDisplay()
    {
        string status = quest.GetStatusText();
        
        if (quest.isMainQuest)
            status = "[PRINCIPAL] " + status;
        
        if (quest.hasTimeLimit && quest.status == QuestStatus.Active)
        {
            float timePercentage = quest.GetTimePercentage();
            if (timePercentage < 0.1f)
                status += " [URGENTE]";
            else if (timePercentage < 0.25f)
                status += " [POUCO TEMPO]";
        }
        
        return status;
    }
    
    #endregion
    
    #region Sprite Helpers
    
    private Sprite GetQuestIconSprite()
    {
        // Try to load category-specific icons
        string iconPath = $"UI/QuestIcons/{quest.category}";
        Sprite categoryIcon = Resources.Load<Sprite>(iconPath);
        
        if (categoryIcon != null)
            return categoryIcon;
        
        // Fallback to type-based icons
        if (quest.isMainQuest)
            return Resources.Load<Sprite>("UI/QuestIcons/MainQuest");
        
        return Resources.Load<Sprite>("UI/QuestIcons/SideQuest");
    }
    
    private Sprite GetCheckmarkSprite()
    {
        return Resources.Load<Sprite>("UI/Icons/Checkmark");
    }
    
    private Sprite GetOptionalSprite()
    {
        return Resources.Load<Sprite>("UI/Icons/Optional");
    }
    
    private Sprite GetIncompleteSprite()
    {
        return Resources.Load<Sprite>("UI/Icons/Incomplete");
    }
    
    #endregion
    
    #region Public Interface
    
    public global::Quest GetQuest() // EXPLÍCITO
    {
        return quest;
    }
    
    public bool IsSelected()
    {
        return isSelected;
    }
    
    public void ForceUpdate()
    {
        if (quest != null)
        {
            UpdateQuestDisplay();
            UpdateVisualState();
        }
    }
    
    public void PlayCompletionEffect()
    {
        if (completeEffect != null)
        {
            completeEffect.Play();
        }
        
        // Flash effect
        StartCoroutine(CompletionFlash());
    }
    
    private IEnumerator CompletionFlash()
    {
        if (backgroundImage == null) yield break;
        
        Color originalColor = backgroundImage.color;
        Color flashColor = Color.green;
        
        // Flash to green
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / 0.2f;
            backgroundImage.color = Color.Lerp(originalColor, flashColor, progress);
            yield return null;
        }
        
        // Flash back
        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / 0.3f;
            backgroundImage.color = Color.Lerp(flashColor, originalColor, progress);
            yield return null;
        }
        
        backgroundImage.color = originalColor;
    }
    
    #endregion
    
    #region Cleanup
    
    private void OnDestroy()
    {
        // Stop all coroutines
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);
        
        if (pulseAnimation != null)
            StopCoroutine(pulseAnimation);
        
        // Cancel invoke
        CancelInvoke();
        
        // Clean up events
        if (itemButton != null)
            itemButton.onClick.RemoveAllListeners();
    }
    
    #endregion
    
    #region Debug
    
    [ContextMenu("Debug Quest Info")]
    private void DebugQuestInfo()
    {
        if (quest == null)
        {
            Debug.Log("Quest is null");
            return;
        }
        
        Debug.Log($"=== {quest.title} ===");
        Debug.Log($"Status: {quest.status}");
        Debug.Log($"Priority: {quest.priority}");
        Debug.Log($"Category: {quest.category}");
        Debug.Log($"Progress: {quest.GetOverallProgress():P1}");
        Debug.Log($"Is Main Quest: {quest.isMainQuest}");
        Debug.Log($"Has Time Limit: {quest.hasTimeLimit}");
        
        if (quest.hasTimeLimit)
        {
            Debug.Log($"Time Remaining: {quest.GetTimeRemainingText()}");
            Debug.Log($"Time Percentage: {quest.GetTimePercentage():P1}");
        }
        
        Debug.Log($"Objectives ({quest.objectives.Count}):");
        for (int i = 0; i < quest.objectives.Count; i++)
        {
            var obj = quest.objectives[i];
            Debug.Log($"  {i + 1}. {(obj.isCompleted ? "✓" : "○")} {obj.description} ({obj.currentAmount}/{obj.requiredAmount})");
        }
    }
    
    #endregion
}
