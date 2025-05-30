using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI melhorada para itens do inventário com suporte a drag & drop, context menu e animações
/// </summary>
public class InventoryItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, 
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Components")]
    public Image backgroundImage;
    public Image icon;
    public Image rarityBorder;
    public Image qualityOverlay;
    public TextMeshProUGUI itemName;
    public TextMeshProUGUI stackText;
    public TextMeshProUGUI levelText;
    public Button useButton;
    public Button equipButton;
    public Button destroyButton;
    
    [Header("Visual Effects")]
    public GameObject glowEffect;
    public ParticleSystem rarityParticles;
    public AudioSource audioSource;
    
    [Header("Drag & Drop")]
    public Transform dragParent;
    public CanvasGroup canvasGroup;
    
    [Header("Animation Settings")]
    public float hoverScale = 1.1f;
    public float animationDuration = 0.2f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoveredColor = new Color(1f, 1f, 1f, 0.8f);
    public Color selectedColor = Color.yellow;
    public Color equippedColor = Color.green;
    public Color cannotUseColor = Color.red;
    
    // State
    private Item item;
    private PlayerInventoryManager inventoryManager;
    private UIManager uiManager;
    private bool isHovered = false;
    private bool isSelected = false;
    private bool isDragging = false;
    private bool isEquipped = false;
    private bool canPlayerUse = true;
    
    // Components
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Transform originalParent;
    private int originalSiblingIndex;
    
    // Animation
    private Coroutine currentAnimation;
    
    // Cache de sprites
    private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    
    #region Initialization
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        originalScale = transform.localScale;
        
        SetupButtons();
    }
    
    private void SetupButtons()
    {
        if (useButton != null)
            useButton.onClick.AddListener(UseItem);
        
        if (equipButton != null)
            equipButton.onClick.AddListener(EquipItem);
        
        if (destroyButton != null)
            destroyButton.onClick.AddListener(ShowDestroyConfirmation);
    }
    
    public void Initialize(Item item, PlayerInventoryManager inventoryManager, UIManager uiManager)
    {
        this.item = item;
        this.inventoryManager = inventoryManager;
        this.uiManager = uiManager;
        
        if (item == null)
        {
            Debug.LogError("InventoryItemUI: Item é nulo!");
            return;
        }
        
        UpdateUI();
        CheckPlayerRequirements();
        CheckEquippedStatus();
    }
    
    #endregion
    
    #region UI Updates
    
    private void UpdateUI()
    {
        if (item == null) return;
        
        // Nome do item
        if (itemName != null)
        {
            itemName.text = item.name;
            itemName.color = item.GetRarityColor();
        }
        
        // Ícone e cor de raridade
        UpdateIcon();
        UpdateRarityBorder();
        UpdateQualityOverlay();
        
        // Stack count
        UpdateStackText();
        
        // Level
        if (levelText != null)
        {
            levelText.text = item.level > 1 ? item.level.ToString() : "";
            levelText.gameObject.SetActive(item.level > 1);
        }
        
        // Background color baseado no estado
        UpdateBackgroundColor();
        
        // Botões
        UpdateButtons();
        
        // Efeitos especiais para itens raros
        UpdateSpecialEffects();
    }
    
    private void UpdateIcon()
    {
        if (icon == null) return;
        
        // Tentar carregar sprite específico
        if (!string.IsNullOrEmpty(item.iconSpriteName))
        {
            Sprite sprite;
            if (!spriteCache.TryGetValue(item.iconSpriteName, out sprite))
            {
                sprite = Resources.Load<Sprite>($"ItemIcons/{item.iconSpriteName}");
                if (sprite != null)
                {
                    spriteCache[item.iconSpriteName] = sprite;
                }
            }
            
            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.color = Color.white;
                return;
            }
        }
        
        // Usar cor baseada no tipo se não houver sprite
        icon.color = GetTypeColor(item.type);
        
        // Aplicar cor customizada do item se definida
        if (item.itemColor != Color.white)
        {
            icon.color = item.itemColor;
        }
    }
    
    private void UpdateRarityBorder()
    {
        if (rarityBorder == null) return;
        
        rarityBorder.color = item.GetRarityColor();
        rarityBorder.gameObject.SetActive(item.rarity != ItemRarity.Common);
        
        // Animação especial para itens lendários+ - apenas se estiver visível
        if (item.rarity >= ItemRarity.Legendary && rarityBorder.gameObject.activeInHierarchy && gameObject.activeInHierarchy)
        {
            if (currentAnimation == null)
            {
                currentAnimation = StartCoroutine(AnimateRarityBorder());
            }
        }
        else if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
    }
    
    private void UpdateQualityOverlay()
    {
        if (qualityOverlay == null) return;
        
        switch (item.quality)
        {
            case ItemQuality.Poor:
                qualityOverlay.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                break;
            case ItemQuality.Normal:
                qualityOverlay.gameObject.SetActive(false);
                return;
            case ItemQuality.Good:
                qualityOverlay.color = new Color(0f, 1f, 0f, 0.3f);
                break;
            case ItemQuality.Excellent:
                qualityOverlay.color = new Color(0f, 0f, 1f, 0.3f);
                break;
            case ItemQuality.Perfect:
                qualityOverlay.color = new Color(1f, 1f, 0f, 0.4f);
                break;
        }
        
        qualityOverlay.gameObject.SetActive(true);
    }
    
    private void UpdateStackText()
    {
        if (stackText == null) return;
        
        if (item.IsStackable && item.currentStack > 1)
        {
            stackText.text = item.currentStack.ToString();
            stackText.gameObject.SetActive(true);
        }
        else
        {
            stackText.gameObject.SetActive(false);
        }
    }
    
    private void UpdateBackgroundColor()
    {
        if (backgroundImage == null) return;
        
        Color targetColor = normalColor;
        
        if (!canPlayerUse)
        {
            targetColor = cannotUseColor;
        }
        else if (isEquipped)
        {
            targetColor = equippedColor;
        }
        else if (isSelected)
        {
            targetColor = selectedColor;
        }
        else if (isHovered)
        {
            targetColor = hoveredColor;
        }
        
        backgroundImage.color = targetColor;
    }
    
    private void UpdateButtons()
    {
        if (useButton != null)
        {
            useButton.gameObject.SetActive(item.IsConsumable && canPlayerUse);
        }
        
        if (equipButton != null)
        {
            bool showEquip = item.IsEquipment && canPlayerUse && !isEquipped;
            equipButton.gameObject.SetActive(showEquip);
            
            if (showEquip && equipButton.GetComponentInChildren<TextMeshProUGUI>() != null)
            {
                equipButton.GetComponentInChildren<TextMeshProUGUI>().text = "Equipar";
            }
        }
        
        if (destroyButton != null)
        {
            destroyButton.gameObject.SetActive(item.isDestroyable);
        }
    }
    
    private void UpdateSpecialEffects()
    {
        // Glow effect para itens raros
        if (glowEffect != null)
        {
            bool shouldGlow = item.rarity >= ItemRarity.Epic;
            glowEffect.SetActive(shouldGlow);
        }
        
        // Particle effects para itens lendários
        if (rarityParticles != null)
        {
            if (item.rarity >= ItemRarity.Legendary)
            {
                if (!rarityParticles.isPlaying)
                {
                    var main = rarityParticles.main;
                    main.startColor = item.GetRarityColor();
                    rarityParticles.Play();
                }
            }
            else
            {
                rarityParticles.Stop();
            }
        }
    }
    
    #endregion
    
    #region State Management
    
    private void CheckPlayerRequirements()
    {
        if (inventoryManager?.playerController == null) return;
        
        var playerStats = inventoryManager.playerController.GetStats();
        canPlayerUse = item.CanPlayerUse(playerStats);
    }
    
    private void CheckEquippedStatus()
    {
        if (inventoryManager == null) return;
        
        isEquipped = inventoryManager.GetAllEquippedItems().Contains(item);
    }
    
    public void SetSelected(bool selected)
    {
        if (isSelected != selected)
        {
            isSelected = selected;
            UpdateBackgroundColor();
            
            if (selected)
            {
                PlaySelectSound();
            }
        }
    }
    
    public void RefreshUI()
    {
        if (!gameObject.activeInHierarchy) return;
        
        CheckPlayerRequirements();
        CheckEquippedStatus();
        UpdateUI();
    }
    
    private void OnEnable()
    {
        // Atualizar UI apenas quando o objeto for ativado
        if (item != null)
        {
            RefreshUI();
        }
    }
    
    private void OnDisable()
    {
        // Parar animações quando desativado
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
    }
    
    #endregion
    
    #region Event Handlers
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging) return;
        
        isHovered = true;
        UpdateBackgroundColor();
        
        // Animação de hover
        AnimateScale(hoverScale);
        
        // Mostrar tooltip
        if (uiManager != null && item != null)
        {
            var playerStats = inventoryManager?.playerController?.GetStats();
            string tooltip = item.GetDetailedDescription(playerStats);
            ShowTooltip(tooltip);
        }
        
        PlayHoverSound();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging) return;
        
        isHovered = false;
        UpdateBackgroundColor();
        
        // Voltar escala normal
        AnimateScale(1f);
        
        // Esconder tooltip
        HideTooltip();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Single click - selecionar
            SetSelected(!isSelected);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right click - context menu
            ShowContextMenu();
        }
        
        // Double click - usar/equipar
        if (eventData.clickCount == 2)
        {
            if (item.IsConsumable)
            {
                UseItem();
            }
            else if (item.IsEquipment)
            {
                EquipItem();
            }
        }
    }
    
    #endregion
    
    #region Drag & Drop
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!item.isDroppable) return;
        
        isDragging = true;
        isHovered = false;
        
        // Setup drag
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalPosition = rectTransform.anchoredPosition;
        
        if (dragParent != null)
        {
            transform.SetParent(dragParent);
        }
        
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        
        // Esconder tooltip
        HideTooltip();
        
        PlayDragSound();
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        rectTransform.anchoredPosition += eventData.delta / transform.lossyScale.x;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        isDragging = false;
        
        // Restore settings
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        // Check drop target
        GameObject dropTargetObj = eventData.pointerCurrentRaycast.gameObject;
        var dropTarget = dropTargetObj?.GetComponent<IDropHandler>();
        
        if (dropTarget == null)
        {
            // Return to original position
            ReturnToOriginalPosition();
        }
        
        PlayDropSound();
    }
    
    private void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        rectTransform.anchoredPosition = originalPosition;
    }
    
    #endregion
    
    #region Actions
    
    public void UseItem()
    {
        if (item == null || !canPlayerUse) return;
        
        if (item.IsConsumable)
        {
            if (inventoryManager == null || inventoryManager.playerController == null)
            {
                Debug.LogError("InventoryItemUI: inventoryManager or playerController is null!");
                return;
            }

            try
            {
                // Guardar o stack atual para verificar se o item foi consumido
                int oldStack = item.currentStack;
                
                item.Use(inventoryManager.playerController);
                PlayUseSound();
                
                // Verificar se o item foi consumido
                if (item.currentStack < oldStack)
                {
                    // Atualizar UI
                    RefreshUI();
                    
                    // Se o item acabou, destruir o GameObject
                    if (item.currentStack <= 0)
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
                
                // Animação de uso
                if (gameObject.activeInHierarchy)
                {
                    StartCoroutine(UseItemAnimation());
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error using item {item.name}: {e.Message}");
            }
        }
    }
    
    public void EquipItem()
    {
        if (item == null || !canPlayerUse || !item.IsEquipment) return;
        
        bool success = inventoryManager.EquipItem(item);
        if (success)
        {
            PlayEquipSound();
            StartCoroutine(EquipAnimation());
        }
    }
    
    private void ShowDestroyConfirmation()
    {
        if (!item.isDestroyable) return;
        
        // Implementar diálogo de confirmação
        Debug.Log($"Deseja destruir {item.name}?");
        
        // Por enquanto, destruir diretamente
        DestroyItem();
    }
    
    private void DestroyItem()
    {
        if (inventoryManager.RemoveItem(item))
        {
            StartCoroutine(DestroyAnimation());
        }
    }
    
    #endregion
    
    #region Context Menu
    
    private void ShowContextMenu()
    {
        // Implementar context menu
        Debug.Log($"Context menu para {item.name}");
        
        // Opções básicas:
        // - Usar (se consumível)
        // - Equipar (se equipamento)
        // - Dropar
        // - Destruir
        // - Informações detalhadas
    }
    
    #endregion
    
    #region Animations
    
    private void AnimateScale(float targetScale)
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        currentAnimation = StartCoroutine(ScaleAnimation(targetScale));
    }
    
    private IEnumerator ScaleAnimation(float targetScale)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = originalScale * targetScale;
        
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;
            float curveValue = scaleCurve.Evaluate(progress);
            
            transform.localScale = Vector3.Lerp(startScale, endScale, curveValue);
            yield return null;
        }
        
        transform.localScale = endScale;
        currentAnimation = null;
    }
    
    private IEnumerator AnimateRarityBorder()
    {
        if (rarityBorder == null) yield break;
        
        float pulseSpeed = 2f;
        Color originalColor = rarityBorder.color;
        
        while (rarityBorder.gameObject.activeInHierarchy)
        {
            float alpha = 0.5f + (Mathf.Sin(Time.time * pulseSpeed) * 0.3f);
            Color pulseColor = originalColor;
            pulseColor.a = alpha;
            rarityBorder.color = pulseColor;
            
            yield return null;
        }
    }
    
    private IEnumerator UseItemAnimation()
    {
        if (backgroundImage == null)
        {
            Debug.LogWarning("InventoryItemUI: backgroundImage is null, skipping animation");
            yield break;
        }

        // Animação de "consumir" - escala pequena e fade
        Vector3 originalScale = transform.localScale;
        Color originalColor = backgroundImage.color;
        
        float duration = 0.3f;
        float elapsed = 0f;
        
        // Primeira parte da animação - diminuir
        while (elapsed < duration)
        {
            if (!gameObject.activeInHierarchy) yield break;
            
            try
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.8f, progress);
                
                Color fadeColor = originalColor;
                fadeColor.a = Mathf.Lerp(1f, 0.5f, progress);
                backgroundImage.color = fadeColor;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in UseItemAnimation (first part): {e.Message}");
                RestoreOriginalState(originalScale, originalColor);
                yield break;
            }
            
            yield return null;
        }
        
        // Segunda parte da animação - voltar ao normal
        elapsed = 0f;
        while (elapsed < duration)
        {
            if (!gameObject.activeInHierarchy) yield break;
            
            try
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                transform.localScale = Vector3.Lerp(originalScale * 0.8f, originalScale, progress);
                
                Color fadeColor = originalColor;
                fadeColor.a = Mathf.Lerp(0.5f, 1f, progress);
                backgroundImage.color = fadeColor;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in UseItemAnimation (second part): {e.Message}");
                RestoreOriginalState(originalScale, originalColor);
                yield break;
            }
            
            yield return null;
        }
        
        RestoreOriginalState(originalScale, originalColor);
    }
    
    private void RestoreOriginalState(Vector3 originalScale, Color originalColor)
    {
        try
        {
            transform.localScale = originalScale;
            if (backgroundImage != null)
            {
                backgroundImage.color = originalColor;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error restoring original state: {e.Message}");
        }
    }
    
    private IEnumerator EquipAnimation()
    {
        // Animação de "equipar" - brilho verde
        if (backgroundImage == null) yield break;
        
        Color originalColor = backgroundImage.color;
        Color equipColor = Color.green;
        
        float duration = 0.5f;
        float elapsed = 0f;
        
        // Fade para verde
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = (elapsed / (duration / 2));
            
            backgroundImage.color = Color.Lerp(originalColor, equipColor, progress);
            yield return null;
        }
        
        // Fade de volta
        elapsed = 0f;
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = (elapsed / (duration / 2));
            
            backgroundImage.color = Color.Lerp(equipColor, originalColor, progress);
            yield return null;
        }
        
        backgroundImage.color = originalColor;
        RefreshUI(); // Atualizar estado equipado
    }
    
    private IEnumerator DestroyAnimation()
    {
        // Animação de destruição - desaparecer com rotação
        float duration = 0.5f;
        float elapsed = 0f;
        
        Vector3 originalScale = transform.localScale;
        Vector3 originalRotation = transform.eulerAngles;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, progress);
            transform.eulerAngles = originalRotation + new Vector3(0, 0, progress * 360f);
            
            canvasGroup.alpha = 1f - progress;
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
    
    #endregion
    
    #region Audio
    
    private void PlayHoverSound()
    {
        PlaySound("UI_Hover");
    }
    
    private void PlaySelectSound()
    {
        PlaySound("UI_Select");
    }
    
    private void PlayUseSound()
    {
        if (!string.IsNullOrEmpty(item.useSound))
        {
            PlaySound(item.useSound);
        }
        else
        {
            PlaySound("Item_Use");
        }
    }
    
    private void PlayEquipSound()
    {
        PlaySound("Item_Equip");
    }
    
    private void PlayDragSound()
    {
        PlaySound("UI_Drag");
    }
    
    private void PlayDropSound()
    {
        PlaySound("UI_Drop");
    }
    
    private void PlaySound(string soundName)
    {
        if (audioSource == null) return;
        
        AudioClip clip = Resources.Load<AudioClip>($"Audio/UI/{soundName}");
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    #endregion
    
    #region Tooltip System
    
    private void ShowTooltip(string content)
    {
        if (uiManager != null)
        {
            Vector3 tooltipPosition = transform.position;
            
            // Ajustar posição para não sair da tela
            RectTransform canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, tooltipPosition);
                
                if (screenPoint.x > Screen.width * 0.7f)
                {
                    tooltipPosition += Vector3.left * 200f;
                }
                else
                {
                    tooltipPosition += Vector3.right * 200f;
                }
                
                if (screenPoint.y < Screen.height * 0.3f)
                {
                    tooltipPosition += Vector3.up * 100f;
                }
            }
            
            // Usar sistema de tooltip customizado se disponível
            EventManager.TriggerEvent(new TooltipRequestEvent
            {
                item = item,
                screenPosition = tooltipPosition,
                show = true
            });
        }
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
    
    #region Utility Methods
    
    private Color GetTypeColor(ItemType type)
    {
        switch (type)
        {
            case ItemType.Weapon: return new Color(0.8f, 0.4f, 0.4f);
            case ItemType.Helmet: return new Color(0.6f, 0.6f, 0.8f);
            case ItemType.Chest: return new Color(0.7f, 0.5f, 0.3f);
            case ItemType.Gloves: return new Color(0.5f, 0.7f, 0.5f);
            case ItemType.Boots: return new Color(0.4f, 0.6f, 0.4f);
            case ItemType.Consumable: return new Color(0.3f, 0.8f, 0.3f);
            case ItemType.Material: return new Color(0.7f, 0.7f, 0.4f);
            case ItemType.Quest: return new Color(1f, 0.8f, 0.2f);
            case ItemType.Jewelry: return new Color(0.9f, 0.7f, 0.9f);
            default: return Color.white;
        }
    }
    
    public bool IsItemValid()
    {
        return item != null && item.IsValid();
    }
    
    public void ForceRefresh()
    {
        if (item != null)
        {
            RefreshUI();
        }
    }
    
    #endregion
    
    #region Comparison & Highlight
    
    public void HighlightAsUpgrade(bool isUpgrade)
    {
        if (backgroundImage == null) return;
        
        if (isUpgrade)
        {
            // Destacar em verde como upgrade
            backgroundImage.color = Color.Lerp(normalColor, Color.green, 0.3f);
            
            if (glowEffect != null)
            {
                glowEffect.SetActive(true);
            }
        }
        else
        {
            UpdateBackgroundColor();
            
            if (glowEffect != null && item.rarity < ItemRarity.Epic)
            {
                glowEffect.SetActive(false);
            }
        }
    }
    
    public void HighlightAsDowngrade(bool isDowngrade)
    {
        if (backgroundImage == null) return;
        
        if (isDowngrade)
        {
            // Destacar em vermelho como downgrade
            backgroundImage.color = Color.Lerp(normalColor, Color.red, 0.3f);
        }
        else
        {
            UpdateBackgroundColor();
        }
    }
    
    #endregion
    
    #region Interface Implementations for Drop Zones
    
    public Item GetItem()
    {
        return item;
    }
    
    public bool CanAcceptItem(Item otherItem)
    {
        // Lógica para determinar se pode aceitar outro item (para swap)
        return otherItem != null && otherItem != item;
    }
    
    public void SwapItems(InventoryItemUI otherItemUI)
    {
        if (otherItemUI == null || otherItemUI.item == null) return;
        
        Item tempItem = this.item;
        this.item = otherItemUI.item;
        otherItemUI.item = tempItem;
        
        // Atualizar ambas as UIs
        this.RefreshUI();
        otherItemUI.RefreshUI();
        
        PlayDropSound();
    }
    
    #endregion
    
    #region Debug
    
    [ContextMenu("Debug Item Info")]
    private void DebugItemInfo()
    {
        if (item == null)
        {
            Debug.Log("Item is null");
            return;
        }
        
        Debug.Log($"=== {item.name} ===");
        Debug.Log($"Type: {item.type}");
        Debug.Log($"Rarity: {item.rarity}");
        Debug.Log($"Quality: {item.quality}");
        Debug.Log($"Level: {item.level}");
        Debug.Log($"Value: {item.GetCurrentValue()}");
        Debug.Log($"Can Player Use: {canPlayerUse}");
        Debug.Log($"Is Equipped: {isEquipped}");
        Debug.Log($"Stack: {item.currentStack}/{item.stackSize}");
    }
    
    #endregion
}