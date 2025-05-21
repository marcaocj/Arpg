using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventoryItemUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI itemName;
    public Button useButton;
    
    private Item item;
    private UIManager uiManager;
    
 public void Initialize(Item item, UIManager uiManager)
{
    this.item = item;
    this.uiManager = uiManager;
    
    if (itemName != null)
    {
        itemName.text = item.name;
    }
    
    // Implementar configuração do ícone
    if (icon != null)
    {
        // Definir cor baseada na raridade
        switch (item.rarity)
        {
            case ItemRarity.Common:
                icon.color = Color.white;
                break;
            case ItemRarity.Uncommon:
                icon.color = Color.green;
                break;
            case ItemRarity.Rare:
                icon.color = Color.blue;
                break;
            case ItemRarity.Epic:
                icon.color = new Color(0.5f, 0f, 0.5f); // Roxo
                break;
            case ItemRarity.Legendary:
                icon.color = new Color(1f, 0.5f, 0f); // Laranja
                break;
        }
        
        // Se tiver sprites específicos (futuro):
        // icon.sprite = Resources.Load<Sprite>("ItemIcons/" + item.iconPath);
    }
    
    // Configurar botão
    if (useButton != null)
    {
        useButton.onClick.AddListener(UseItem);
    }
}
    
    public void UseItem()
    {
        if (item != null)
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                item.Use(player);
                uiManager.UpdateInventoryUI();
            }
        }
    }
    
    public void OnPointerEnter()
    {
        if (uiManager != null && item != null)
        {
            uiManager.ShowTooltip(item, transform.position);
        }
    }
    
    public void OnPointerExit()
    {
        if (uiManager != null)
        {
            uiManager.HideTooltip();
        }
    }
}