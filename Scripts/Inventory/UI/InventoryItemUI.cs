using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Componente de UI para exibir itens individuais no inventário
/// </summary>
public class InventoryItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Components")]
    public Image itemIcon;
    public Image backgroundImage;
    public Image rarityBorder;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI stackCountText;
    public TextMeshProUGUI levelText;
    public Button itemButton;
    
    [Header("Visual States")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 1f, 1f, 0.8f);
    public Color selectedColor = Color.yellow;
    public Color equippedColor = Color.green;
    
    [Header("Drag Settings")]
    public bool enableDrag = true;
    public float dragAlpha = 0.6f;
    
    // State
    private Item item;
    private PlayerInventoryManager inventoryManager;
    private UIManager uiManager;
    private bool isHovered = false;
    private bool isSelected = false;
    private bool isDragging = false;
    
    // Drag components
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector3 originalPosition;
    
    #region Initialization
    
    private void Awake()
    {
        // Cache components
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Setup button
        if (itemButton == null)
            itemButton = GetComponent<Button>();
        
        if (itemButton != null)
            itemButton.onClick.AddListener(OnItemClicked);
    }
    
    /// <summary>
    /// Inicializa o componente com um item
    /// CORRIGIDO: Método com três parâmetros para compatibilidade com UIManager
    /// </summary>
    public void Initialize(Item item, PlayerInventoryManager inventoryManager, UIManager uiManager)
    {
        this.item = item;
        this.inventoryManager = inventoryManager;
        this.uiManager = uiManager;
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// Sobrecarga para compatibilidade
    /// </summary>
    public void Initialize(Item item, PlayerInventoryManager inventoryManager)
    {
        Initialize(item, inventoryManager, null);
    }
    
    #endregion
    
    #region Display Updates
    
    public void UpdateDisplay()
    {
        if (item == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        
        UpdateIcon();
        UpdateText();
        UpdateColors();
        UpdateStackCount();
        UpdateTooltip();
    }
    
    private void UpdateIcon()
    {
        if (itemIcon != null)
        {
            // Tentar carregar ícone do item
            Sprite icon = LoadItemIcon();
            if (icon != null)
            {
                itemIcon.sprite = icon;
                itemIcon.color = Color.white;
            }
            else
            {
                // Fallback para cor baseada no tipo
                itemIcon.sprite = null;
                itemIcon.color = GetItemTypeColor();
            }
        }
    }
    
    private void UpdateText()
    {
        if (itemNameText != null)
        {
            itemNameText.text = item.name;
            itemNameText.color = item.GetRarityColor();
        }
        
        if (levelText != null)
        {
            if (item.level > 1)
            {
                levelText.text = item.level.ToString();
                levelText.gameObject.SetActive(true);
            }
            else
            {
                levelText.gameObject.SetActive(false);
            }
        }
    }
    
    private void UpdateColors()
    {
        // Background color
        if (backgroundImage != null)
        {
            Color bgColor = normalColor;
            
            if (IsItemEquipped())
            {
                bgColor = equippedColor;
            }
            else if (isSelected)
            {
                bgColor = selectedColor;
            }
            else if (isHovered)
            {
                bgColor = hoverColor;
            }
            
            backgroundImage.color = bgColor;
        }
        
        // Rarity border
        if (rarityBorder != null)
        {
            rarityBorder.color = item.GetRarityColor();
            rarityBorder.gameObject.SetActive(item.rarity > ItemRarity.Common);
        }
    }
    
    private void UpdateStackCount()
    {
        if (stackCountText != null)
        {
            if (item.IsStackable && item.currentStack > 1)
            {
                stackCountText.text = item.currentStack.ToString();
                stackCountText.gameObject.SetActive(true);
            }
            else
            {
                stackCountText.gameObject.SetActive(false);
            }
        }
    }
    
    private void UpdateTooltip()
    {
        // Tooltip será mostrado no hover
    }
    
    #endregion
    
    #region Event Handlers
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging) return;
        
        isHovered = true;
        UpdateColors();
        
        ShowTooltip();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging) return;
        
        isHovered = false;
        UpdateColors();
        
        HideTooltip();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnItemClicked();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnItemRightClicked();
        }
    }
    
    private void OnItemClicked()
    {
        if (item == null || inventoryManager == null) return;
        
        // Equipar/usar item
        if (item.IsEquipment)
        {
            bool success = inventoryManager.EquipItem(item);
            if (success)
            {
                UpdateDisplay(); // Atualizar para mostrar como equipado
            }
        }
        else if (item.IsConsumable)
        {
            // Usar consumível
            if (inventoryManager.GetType().GetMethod("UseConsumableItem") != null)
            {
                var method = inventoryManager.GetType().GetMethod("UseConsumableItem", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(inventoryManager, new object[] { item });
            }
        }
        
        // Mostrar feedback visual
        PlayClickEffect();
    }
    
    private void OnItemRightClicked()
    {
        if (item == null || inventoryManager == null) return;
        
        // Abrir menu de contexto ou ação alternativa
        if (IsItemEquipped())
        {
            // Desequipar
            inventoryManager.UnequipItem(item);
            UpdateDisplay();
        }
        else
        {
            // Mostrar menu de contexto (implementar se necessário)
            Debug.Log($"Menu de contexto para {item.name}");
        }
    }
    
    #endregion
    
    #region Drag and Drop
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!enableDrag || item == null) return;
        
        isDragging = true;
        originalPosition = rectTransform.position;
        
        // Tornar semi-transparente durante drag
        if (canvasGroup != null)
        {
            canvasGroup.alpha = dragAlpha;
            canvasGroup.blocksRaycasts = false;
        }
        
        // Mover para frente na hierarquia
        transform.SetAsLastSibling();
        
        HideTooltip();
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        // Seguir o mouse
        if (rectTransform != null && canvas != null)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out localPoint);
            
            rectTransform.position = canvas.transform.TransformPoint(localPoint);
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        isDragging = false;
        
        // Restaurar transparência
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        
        // Verificar se foi dropado em um slot válido
        bool wasDropped = TryDropItem(eventData);
        
        if (!wasDropped)
        {
            // Voltar para posição original
            rectTransform.position = originalPosition;
        }
        
        UpdateColors();
    }
    
    private bool TryDropItem(PointerEventData eventData)
    {
        // Verificar se foi dropado em outro slot de inventário
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        foreach (var result in results)
        {
            var otherSlot = result.gameObject.GetComponent<InventoryItemUI>();
            if (otherSlot != null && otherSlot != this)
            {
                // Trocar itens
                return SwapItems(otherSlot);
            }
            
            // Verificar drop em equipment slots
            var equipSlot = result.gameObject.GetComponent<EquipmentSlotUI>();
            if (equipSlot != null && item.IsEquipment)
            {
                return TryEquipToSlot(equipSlot);
            }
        }
        
        return false;
    }
    
    private bool SwapItems(InventoryItemUI otherSlot)
    {
        if (otherSlot == null || otherSlot.item == null) return false;
        
        // Implementar troca de itens
        Item tempItem = this.item;
        this.item = otherSlot.item;
        otherSlot.item = tempItem;
        
        // Atualizar displays
        UpdateDisplay();
        otherSlot.UpdateDisplay();
        
        return true;
    }
    
    private bool TryEquipToSlot(EquipmentSlotUI equipSlot)
    {
        if (equipSlot == null || !item.IsEquipment) return false;
        
        // Tentar equipar
        bool success = inventoryManager.EquipItem(item);
        if (success)
        {
            UpdateDisplay();
        }
        
        return success;
    }
    
    #endregion
    
    #region Tooltip
    
    private void ShowTooltip()
    {
        if (item == null) return;
        
        // Usar EventManager para mostrar tooltip
        EventManager.TriggerEvent(new TooltipRequestEvent
        {
            item = item,
            screenPosition = transform.position,
            show = true
        });
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
    
    private bool IsItemEquipped()
    {
        if (inventoryManager == null || item == null) return false;
        
        // Verificar se o item está equipado
        var equippedItems = inventoryManager.GetAllEquippedItems();
        return equippedItems.Contains(item);
    }
    
    private Sprite LoadItemIcon()
    {
        if (item == null) return null;
        
        // Tentar carregar ícone específico do item
        if (!string.IsNullOrEmpty(item.iconPath))
        {
            return Resources.Load<Sprite>(item.iconPath);
        }
        
        // Fallback para ícone por tipo
        string typePath = $"UI/ItemIcons/{item.type}";
        return Resources.Load<Sprite>(typePath);
    }
    
    private Color GetItemTypeColor()
    {
        switch (item.type)
        {
            case ItemType.Weapon: return Color.red;
            case ItemType.Helmet: return new Color(0.5f, 0.3f, 0.1f); // Brown
            case ItemType.Chest: return new Color(0.3f, 0.3f, 0.6f); // Blue
            case ItemType.Gloves: return new Color(0.4f, 0.6f, 0.4f); // Green
            case ItemType.Boots: return new Color(0.6f, 0.4f, 0.2f); // Orange
            case ItemType.Jewelry: return new Color(1f, 0.8f, 0.2f); // Gold
            case ItemType.Consumable: return new Color(0.2f, 0.8f, 0.2f); // Bright Green
            case ItemType.Material: return new Color(0.7f, 0.7f, 0.7f); // Gray
            case ItemType.Quest: return new Color(1f, 1f, 0.4f); // Yellow
            default: return Color.white;
        }
    }
    
    private void PlayClickEffect()
    {
        // Implementar efeito visual/sonoro de clique
        StartCoroutine(ClickEffectCoroutine());
    }
    
    private System.Collections.IEnumerator ClickEffectCoroutine()
    {
        if (backgroundImage == null) yield break;
        
        Color originalColor = backgroundImage.color;
        Color flashColor = Color.white;
        
        // Flash rápido
        backgroundImage.color = flashColor;
        yield return new WaitForSeconds(0.1f);
        backgroundImage.color = originalColor;
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateColors();
    }
    
    public Item GetItem()
    {
        return item;
    }
    
    public void RefreshDisplay()
    {
        UpdateDisplay();
    }
    
    #endregion
    
    #region Cleanup
    
    private void OnDestroy()
    {
        HideTooltip();
        
        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
        }
    }
    
    #endregion
}
