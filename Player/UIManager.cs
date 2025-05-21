using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;


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
	public GameObject itemCollectedNotification;
	public TextMeshProUGUI itemCollectedText;
	public float notificationDuration = 2f;
	
    private PlayerController player;
    
    private void Start()
    {
        player = FindObjectOfType<PlayerController>();
        
        // Inicializar UI
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
        
        if (tooltip != null)
        {
            tooltip.SetActive(false);
        }
        
        UpdateUI();
    }
    
    private void Update()
    {
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (player == null)
        {
            return;
        }
        
        // Atualizar barras e textos
        if (healthBar != null)
        {
            healthBar.maxValue = player.maxHealth;
            healthBar.value = player.health;
        }
        
        if (manaBar != null)
        {
            manaBar.maxValue = player.maxMana;
            manaBar.value = player.mana;
        }
        
        if (experienceBar != null)
        {
            experienceBar.maxValue = player.experienceToNextLevel;
            experienceBar.value = player.experiencePoints;
        }
        
        if (levelText != null)
        {
            levelText.text = "Level " + player.level;
        }
        
        if (goldText != null && GameManager.instance != null)
        {
            goldText.text = GameManager.instance.goldCollected.ToString();
        }
        
        // Atualizar ícones de habilidade
        for (int i = 0; i < skillIcons.Length && i < player.skills.Count; i++)
        {
            // Implementar lógica para mostrar ícones de habilidade
            // e indicadores de cooldown
        }
    }
    
    public void UpdateInventoryUI()
    {
        if (player == null || itemContainer == null)
        {
            return;
        }
        
        // Limpar container
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Criar elementos de UI para cada item
        foreach (Item item in player.inventory.items)
        {
            GameObject itemUI = Instantiate(itemPrefab, itemContainer);
            InventoryItemUI itemUIComponent = itemUI.GetComponent<InventoryItemUI>();
            
            if (itemUIComponent != null)
            {
                itemUIComponent.Initialize(item, this);
            }
        }
    }
    
    public void ShowTooltip(Item item, Vector3 position)
    {
        if (tooltip == null || tooltipTitle == null || tooltipDescription == null)
        {
            return;
        }
        
        tooltipTitle.text = item.name;
        
        // Construir descrição detalhada
        string description = item.description + "\n";
        
        // Adicionar estatísticas baseado no tipo de item
        if (item.type == ItemType.Weapon)
        {
            description += "Dano Físico: " + item.physicalDamage + "\n";
            if (item.fireDamage > 0) description += "Dano de Fogo: " + item.fireDamage + "\n";
            if (item.iceDamage > 0) description += "Dano de Gelo: " + item.iceDamage + "\n";
            if (item.lightningDamage > 0) description += "Dano Elétrico: " + item.lightningDamage + "\n";
            if (item.poisonDamage > 0) description += "Dano de Veneno: " + item.poisonDamage + "\n";
        }
        
        // Adicionar modificadores de atributos
        if (item.strengthModifier > 0) description += "Força: +" + item.strengthModifier + "\n";
        if (item.intelligenceModifier > 0) description += "Inteligência: +" + item.intelligenceModifier + "\n";
        if (item.dexterityModifier > 0) description += "Destreza: +" + item.dexterityModifier + "\n";
        if (item.vitalityModifier > 0) description += "Vitalidade: +" + item.vitalityModifier + "\n";
        
        // Itens consumíveis
        if (item.type == ItemType.Consumable)
        {
            if (item.healthRestore > 0) description += "Recupera " + item.healthRestore + " de vida\n";
            if (item.manaRestore > 0) description += "Recupera " + item.manaRestore + " de mana\n";
        }
        
        // Definir raridade com cor
        string rarityText = "";
        switch (item.rarity)
        {
            case ItemRarity.Common:
                rarityText = "<color=white>Comum</color>";
                break;
            case ItemRarity.Uncommon:
                rarityText = "<color=green>Incomum</color>";
                break;
            case ItemRarity.Rare:
                rarityText = "<color=blue>Raro</color>";
                break;
            case ItemRarity.Epic:
                rarityText = "<color=purple>Épico</color>";
                break;
            case ItemRarity.Legendary:
                rarityText = "<color=orange>Lendário</color>";
                break;
        }
        
        description += "Nível: " + item.level + "\n";
        description += rarityText;
        
        tooltipDescription.text = description;
        
        // Posicionar e mostrar tooltip
        tooltip.transform.position = position;
        tooltip.SetActive(true);
    }

// Adicionar este método ao UIManager.cs
public void ShowItemCollectedMessage(string itemName, ItemRarity rarity = ItemRarity.Common)
{
    if (itemCollectedNotification == null || itemCollectedText == null)
        return;
    
    // Configurar o texto
    itemCollectedText.text = "Item Coletado: " + itemName;
    
    // Configurar a cor baseada na raridade
    switch (rarity)
    {
        case ItemRarity.Common:
            itemCollectedText.color = Color.white;
            break;
        case ItemRarity.Uncommon:
            itemCollectedText.color = Color.green;
            break;
        case ItemRarity.Rare:
            itemCollectedText.color = Color.blue;
            break;
        case ItemRarity.Epic:
            itemCollectedText.color = new Color(0.5f, 0, 0.5f); // Roxo
            break;
        case ItemRarity.Legendary:
            itemCollectedText.color = new Color(1.0f, 0.5f, 0); // Laranja
            break;
    }
    
    // Mostrar a notificação
    itemCollectedNotification.SetActive(true);
    
    // Ocultá-la após um tempo
    StartCoroutine(HideNotificationAfterDelay(notificationDuration));
}

private IEnumerator HideNotificationAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);
    itemCollectedNotification.SetActive(false);
}    
    public void HideTooltip()
    {
        if (tooltip != null)
        {
            tooltip.SetActive(false);
        }
    }
}
