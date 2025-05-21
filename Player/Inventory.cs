using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    public int maxItems = 20;
    public List<Item> items = new List<Item>();
    
    // Equipamento atual
    public Item equippedWeapon;
    public Item equippedHelmet;
    public Item equippedChest;
    public Item equippedGloves;
    public Item equippedBoots;
    
    // Referência ao PlayerController
    private PlayerController player;
    
    private void Awake()
    {
        // Obter referência ao PlayerController
        player = GetComponent<PlayerController>();
        if (player == null)
        {
            Debug.LogError("Inventory: PlayerController não encontrado no mesmo objeto!");
        }
    }
    
    public bool AddItem(Item item)
    {
        if (items.Count < maxItems)
        {
            items.Add(item);
            Debug.Log("Item adicionado: " + item.name);
			FindObjectOfType<UIManager>()?.UpdateInventoryUI();

            return true;
        }
        
        Debug.Log("Inventário cheio!");
        return false;
    }
    
    public void RemoveItem(Item item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
            Debug.Log("Item removido: " + item.name);
        FindObjectOfType<UIManager>()?.UpdateInventoryUI();

        }
    }
    
    public void EquipItem(Item item)
    {
        if (!items.Contains(item))
        {
            Debug.Log("Item não está no inventário!");
            return;
        }
        
        switch (item.type)
        {
            case ItemType.Weapon:
                if (equippedWeapon != null)
                {
                    // Desequipar arma atual
                    UnequipItem(equippedWeapon);
                }
                equippedWeapon = item;
                break;
                
            case ItemType.Helmet:
                if (equippedHelmet != null)
                {
                    UnequipItem(equippedHelmet);
                }
                equippedHelmet = item;
                break;
                
            case ItemType.Chest:
                if (equippedChest != null)
                {
                    UnequipItem(equippedChest);
                }
                equippedChest = item;
                break;
                
            case ItemType.Gloves:
                if (equippedGloves != null)
                {
                    UnequipItem(equippedGloves);
                }
                equippedGloves = item;
                break;
                
            case ItemType.Boots:
                if (equippedBoots != null)
                {
                    UnequipItem(equippedBoots);
                }
                equippedBoots = item;
                break;
        }
        
        // Aplicar modificadores de atributos do item usando o novo sistema
        if (player != null)
        {
            // Usar o novo método AdjustAttribute do PlayerStats
            PlayerStats stats = player.GetStats();
            
            if (item.strengthModifier != 0)
                stats.AdjustAttribute("strength", item.strengthModifier);
            
            if (item.intelligenceModifier != 0)
                stats.AdjustAttribute("intelligence", item.intelligenceModifier);
            
            if (item.dexterityModifier != 0)
                stats.AdjustAttribute("dexterity", item.dexterityModifier);
            
            if (item.vitalityModifier != 0)
                stats.AdjustAttribute("vitality", item.vitalityModifier);
            
            // Não precisamos mais atualizar maxHealth e maxMana manualmente
            // O método RecalculateStats já é chamado internamente pelo AdjustAttribute
        }
        
        Debug.Log("Item equipado: " + item.name);
		FindObjectOfType<UIManager>()?.UpdateInventoryUI();

    }
    
    public void UnequipItem(Item item)
    {
        bool wasEquipped = false;
        
        if (equippedWeapon == item)
        {
            equippedWeapon = null;
            wasEquipped = true;
        }
        else if (equippedHelmet == item)
        {
            equippedHelmet = null;
            wasEquipped = true;
        }
        else if (equippedChest == item)
        {
            equippedChest = null;
            wasEquipped = true;
        }
        else if (equippedGloves == item)
        {
            equippedGloves = null;
            wasEquipped = true;
        }
        else if (equippedBoots == item)
        {
            equippedBoots = null;
            wasEquipped = true;
        }
        
        if (wasEquipped)
        {
            // Remover modificadores de atributos do item usando o novo sistema
            if (player != null)
            {
                // Usar o novo método AdjustAttribute do PlayerStats com valores negativos
                PlayerStats stats = player.GetStats();
                
                if (item.strengthModifier != 0)
                    stats.AdjustAttribute("strength", -item.strengthModifier);
                
                if (item.intelligenceModifier != 0)
                    stats.AdjustAttribute("intelligence", -item.intelligenceModifier);
                
                if (item.dexterityModifier != 0)
                    stats.AdjustAttribute("dexterity", -item.dexterityModifier);
                
                if (item.vitalityModifier != 0)
                    stats.AdjustAttribute("vitality", -item.vitalityModifier);
                
                // Não precisamos mais atualizar maxHealth e maxMana manualmente
                // O método RecalculateStats já é chamado internamente pelo AdjustAttribute
            }
            
            Debug.Log("Item desequipado: " + item.name);
			    FindObjectOfType<UIManager>()?.UpdateInventoryUI();

        }
    }
}