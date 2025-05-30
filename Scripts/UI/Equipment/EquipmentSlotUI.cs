using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// UI para slots individuais de equipamento
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Components")]
    public Image slotBackground;
    public Image itemIcon;
    public Image rarityBorder;
    public TextMeshProUGUI slotName;
    public GameObject emptySlotIcon;

    [Header("Visual Effects")]
    public GameObject hoverEffect;
    public GameObject selectedEffect;
    public AudioSource audioSource;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 1f, 1f, 0.8f);
    public Color selectedColor = Color.yellow;

    // State
    private Item currentItem;
    private ItemType slotType;
    private bool isHovered;
    private bool isSelected;

    public void Initialize(ItemType type, string name)
    {
        slotType = type;
        if (slotName != null)
        {
            slotName.text = name;
        }
    }

    public void UpdateSlot(Item item)
    {
        currentItem = item;

        if (item == null)
        {
            // Slot vazio
            if (itemIcon != null) itemIcon.gameObject.SetActive(false);
            if (rarityBorder != null) rarityBorder.gameObject.SetActive(false);
            if (emptySlotIcon != null) emptySlotIcon.SetActive(true);
            if (slotBackground != null) slotBackground.color = normalColor;
        }
        else
        {
            // Slot com item
            if (itemIcon != null)
            {
                itemIcon.gameObject.SetActive(true);
                itemIcon.sprite = Resources.Load<Sprite>(item.iconPath);
            }

            if (rarityBorder != null)
            {
                rarityBorder.gameObject.SetActive(true);
                rarityBorder.color = GetRarityColor(item.rarity);
            }

            if (emptySlotIcon != null) emptySlotIcon.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateVisuals();

        if (currentItem != null)
        {
            ShowTooltip();
            PlayHoverSound();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateVisuals();
        HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            // Desequipar item
            var inventoryManager = PlayerController.Instance?.GetInventoryManager();
            if (inventoryManager != null)
            {
                inventoryManager.UnequipItem(currentItem);
                PlayClickSound();
            }
        }
    }

    private void UpdateVisuals()
    {
        if (slotBackground != null)
        {
            Color targetColor = normalColor;
            if (isSelected) targetColor = selectedColor;
            else if (isHovered) targetColor = hoverColor;
            slotBackground.color = targetColor;
        }

        if (hoverEffect != null)
        {
            hoverEffect.SetActive(isHovered);
        }
    }

    private void ShowTooltip()
    {
        if (currentItem == null) return;

        var playerStats = PlayerController.Instance?.GetStats();
        string tooltip = currentItem.GetDetailedDescription(playerStats);

        EventManager.TriggerEvent(new TooltipRequestEvent
        {
            item = currentItem,
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

    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return Color.white;
            case ItemRarity.Uncommon: return Color.green;
            case ItemRarity.Rare: return Color.blue;
            case ItemRarity.Epic: return new Color(0.5f, 0, 0.5f); // Roxo
            case ItemRarity.Legendary: return new Color(1f, 0.5f, 0); // Laranja
            default: return Color.white;
        }
    }

    private void PlayHoverSound()
    {
        if (audioSource != null)
        {
            audioSource.PlayOneShot(Resources.Load<AudioClip>("Audio/UI/UI_Hover"));
        }
    }

    private void PlayClickSound()
    {
        if (audioSource != null)
        {
            audioSource.PlayOneShot(Resources.Load<AudioClip>("Audio/UI/UI_Click"));
        }
    }
} 