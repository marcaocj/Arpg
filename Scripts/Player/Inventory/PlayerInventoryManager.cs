using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Gerencia o inventário e equipamentos do jogador
/// </summary>
public class PlayerInventoryManager : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private int maxItems = 40;
    [SerializeField] private bool autoSort = false;
    
    // Itens do inventário
    public List<Item> items = new List<Item>();
    
    // Equipamentos atuais
    public Item equippedWeapon;
    public Item equippedHelmet;
    public Item equippedChest;
    public Item equippedGloves;
    public Item equippedBoots;
    
    // Referências
    private PlayerStatsManager statsManager;
    private PlayerHealthManager healthManager;
    
    // Eventos
    public event Action<Item, bool> OnItemAdded; // item, wasSuccessful
    public event Action<Item> OnItemRemoved;
    public event Action<Item, Item> OnItemEquipped; // newItem, oldItem
    public event Action<Item> OnItemUnequipped;
    public event Action OnInventoryChanged;
    
    // Propriedades
    public int MaxItems => maxItems;
    public int CurrentItemCount => items.Count;
    public int FreeSlots => maxItems - items.Count;
    public bool HasSpace => items.Count < maxItems;
    
    private void Awake()
    {
        statsManager = GetComponent<PlayerStatsManager>();
        healthManager = GetComponent<PlayerHealthManager>();
        
        if (statsManager == null)
        {
            Debug.LogError("PlayerInventoryManager: PlayerStatsManager não encontrado!");
        }
    }
    
    #region Item Management
    
    public bool AddItem(Item item)
    {
        if (item == null)
        {
            Debug.LogWarning("Tentativa de adicionar item nulo ao inventário!");
            return false;
        }
        
        if (!HasSpace)
        {
            Debug.Log("Inventário cheio!");
            
            // Disparar eventos de falha
            EventManager.TriggerEvent(new ItemAddedEvent
            {
                item = item,
                wasSuccessful = false,
                failureReason = "Inventário cheio"
            });
            
            EventManager.TriggerEvent(new InventoryFullEvent
            {
                attemptedItem = item,
                maxCapacity = maxItems
            });
            
            OnItemAdded?.Invoke(item, false);
            return false;
        }
        
        // Adicionar item
        items.Add(item);
        
        // Auto-sort se ativado
        if (autoSort)
        {
            SortByRarity();
        }
        
        // Disparar eventos de sucesso
        EventManager.TriggerEvent(new ItemAddedEvent
        {
            item = item,
            wasSuccessful = true,
            failureReason = ""
        });
        
        OnItemAdded?.Invoke(item, true);
        OnInventoryChanged?.Invoke();
        
        Debug.Log($"Item adicionado: {item.name}");
        return true;
    }
    
    public bool RemoveItem(Item item)
    {
        if (item == null || !items.Contains(item))
        {
            return false;
        }
        
        bool wasEquipped = IsItemEquipped(item);
        
        // Desequipar se necessário
        if (wasEquipped)
        {
            UnequipItem(item);
        }
        
        // Remover do inventário
        items.Remove(item);
        
        // Disparar eventos
        EventManager.TriggerEvent(new ItemRemovedEvent
        {
            item = item,
            wasEquipped = wasEquipped
        });
        
        OnItemRemoved?.Invoke(item);
        OnInventoryChanged?.Invoke();
        
        Debug.Log($"Item removido: {item.name}");
        return true;
    }
    
    public bool RemoveItem(string itemName)
    {
        Item item = FindItemByName(itemName);
        return item != null && RemoveItem(item);
    }
    
    public bool RemoveItems(ItemType type, int count)
    {
        var itemsToRemove = new List<Item>();
        int removed = 0;
        
        foreach (Item item in items)
        {
            if (item.type == type && removed < count)
            {
                itemsToRemove.Add(item);
                removed++;
            }
        }
        
        foreach (Item item in itemsToRemove)
        {
            RemoveItem(item);
        }
        
        return removed == count;
    }
    
    #endregion
    
    #region Equipment Management
    
    public bool EquipItem(Item item)
    {
        if (item == null || !items.Contains(item))
        {
            Debug.Log("Item não está no inventário!");
            return false;
        }
        
        if (item.type == ItemType.Consumable)
        {
            // Consumir item em vez de equipar
            UseConsumableItem(item);
            return true;
        }
        
        Item previousItem = GetEquippedItem(item.type);
        
        // Desequipar item anterior se houver
        if (previousItem != null)
        {
            UnequipItem(previousItem);
        }
        
        // Equipar novo item
        SetEquippedItem(item);
        
        // Aplicar modificadores
        ApplyItemModifiers(item, true);
        
        // Disparar eventos
        EventManager.TriggerEvent(new ItemEquippedEvent
        {
            item = item,
            previousItem = previousItem,
            slotType = item.type
        });
        
        OnItemEquipped?.Invoke(item, previousItem);
        OnInventoryChanged?.Invoke();
        
        Debug.Log($"Item equipado: {item.name}");
        return true;
    }
    
    public bool UnequipItem(Item item)
    {
        if (item == null || !IsItemEquipped(item))
        {
            return false;
        }
        
        // Remover modificadores
        ApplyItemModifiers(item, false);
        
        // Desequipar
        ClearEquippedItem(item.type);
        
        // Disparar eventos
        EventManager.TriggerEvent(new ItemUnequippedEvent
        {
            item = item,
            slotType = item.type
        });
        
        OnItemUnequipped?.Invoke(item);
        OnInventoryChanged?.Invoke();
        
        Debug.Log($"Item desequipado: {item.name}");
        return true;
    }
    
    public bool UnequipItem(ItemType type)
    {
        Item item = GetEquippedItem(type);
        return item != null && UnequipItem(item);
    }
    
    private void UseConsumableItem(Item item)
    {
        if (item.type != ItemType.Consumable || healthManager == null)
        {
            return;
        }
        
        int oldHealth = healthManager.CurrentHealth;
        int oldMana = healthManager.CurrentMana;
        
        // Aplicar efeitos do consumível
        if (item.healthRestore > 0)
        {
            healthManager.Heal(item.healthRestore);
        }
        
        if (item.manaRestore > 0)
        {
            healthManager.RestoreMana(item.manaRestore);
        }
        
        // Calcular quanto foi realmente restaurado
        int actualHealthRestored = healthManager.CurrentHealth - oldHealth;
        int actualManaRestored = healthManager.CurrentMana - oldMana;
        
        // Criar popups para o que foi restaurado
        if (actualHealthRestored > 0)
        {
            EventManager.TriggerEvent(new DamagePopupRequestEvent
            {
                worldPosition = transform.position,
                amount = actualHealthRestored,
                isCritical = false,
                isHeal = true,
                customColor = Color.green
            });
        }
        
        if (actualManaRestored > 0)
        {
            EventManager.TriggerEvent(new DamagePopupRequestEvent
            {
                worldPosition = transform.position + Vector3.up * 0.5f,
                amount = actualManaRestored,
                isCritical = false,
                isHeal = false,
                customColor = Color.cyan
            });
        }
        
        // Remover item do inventário
        RemoveItem(item);
        
        Debug.Log($"Item consumido: {item.name}");
    }
    
    #endregion
    
    #region Equipment Helpers
    
    private void SetEquippedItem(Item item)
    {
        switch (item.type)
        {
            case ItemType.Weapon:
                equippedWeapon = item;
                break;
            case ItemType.Helmet:
                equippedHelmet = item;
                break;
            case ItemType.Chest:
                equippedChest = item;
                break;
            case ItemType.Gloves:
                equippedGloves = item;
                break;
            case ItemType.Boots:
                equippedBoots = item;
                break;
        }
    }
    
    private void ClearEquippedItem(ItemType type)
    {
        switch (type)
        {
            case ItemType.Weapon:
                equippedWeapon = null;
                break;
            case ItemType.Helmet:
                equippedHelmet = null;
                break;
            case ItemType.Chest:
                equippedChest = null;
                break;
            case ItemType.Gloves:
                equippedGloves = null;
                break;
            case ItemType.Boots:
                equippedBoots = null;
                break;
        }
    }
    
    private bool IsItemEquipped(Item item)
    {
        return equippedWeapon == item ||
               equippedHelmet == item ||
               equippedChest == item ||
               equippedGloves == item ||
               equippedBoots == item;
    }
    
    private void ApplyItemModifiers(Item item, bool equipping)
    {
        if (item == null || statsManager == null) return;
        
        int multiplier = equipping ? 1 : -1;
        
        if (item.strengthModifier != 0)
            statsManager.AdjustAttribute("strength", item.strengthModifier * multiplier);
        
        if (item.intelligenceModifier != 0)
            statsManager.AdjustAttribute("intelligence", item.intelligenceModifier * multiplier);
        
        if (item.dexterityModifier != 0)
            statsManager.AdjustAttribute("dexterity", item.dexterityModifier * multiplier);
        
        if (item.vitalityModifier != 0)
            statsManager.AdjustAttribute("vitality", item.vitalityModifier * multiplier);
    }
    
    #endregion
    
    #region Query Methods
    
    public Item GetEquippedItem(ItemType type)
    {
        switch (type)
        {
            case ItemType.Weapon:
                return equippedWeapon;
            case ItemType.Helmet:
                return equippedHelmet;
            case ItemType.Chest:
                return equippedChest;
            case ItemType.Gloves:
                return equippedGloves;
            case ItemType.Boots:
                return equippedBoots;
            default:
                return null;
        }
    }
    
    public List<Item> GetAllEquippedItems()
    {
        List<Item> equippedItems = new List<Item>();
        
        if (equippedWeapon != null) equippedItems.Add(equippedWeapon);
        if (equippedHelmet != null) equippedItems.Add(equippedHelmet);
        if (equippedChest != null) equippedItems.Add(equippedChest);
        if (equippedGloves != null) equippedItems.Add(equippedGloves);
        if (equippedBoots != null) equippedItems.Add(equippedBoots);
        
        return equippedItems;
    }
    
    public Item FindItemByName(string itemName)
    {
        foreach (Item item in items)
        {
            if (item.name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }
        return null;
    }
    
    public List<Item> FindItemsByType(ItemType type)
    {
        List<Item> foundItems = new List<Item>();
        
        foreach (Item item in items)
        {
            if (item.type == type)
            {
                foundItems.Add(item);
            }
        }
        
        return foundItems;
    }
    
    public int CountItemsOfType(ItemType type)
    {
        int count = 0;
        foreach (Item item in items)
        {
            if (item.type == type)
            {
                count++;
            }
        }
        return count;
    }
    
    public bool HasItem(string itemName)
    {
        return FindItemByName(itemName) != null;
    }
    
    public bool HasItem(Item item)
    {
        return item != null && items.Contains(item);
    }
    
    public bool HasItemOfType(ItemType type)
    {
        return CountItemsOfType(type) > 0;
    }
    
    #endregion
    
    #region Sorting and Organization
    
    public void SortByRarity()
    {
        items.Sort((item1, item2) => item2.rarity.CompareTo(item1.rarity));
        
        EventManager.TriggerEvent(new NotificationEvent
        {
            message = "Inventário ordenado por raridade",
            type = NotificationType.Info,
            duration = 1f,
            color = Color.white
        });
        
        OnInventoryChanged?.Invoke();
    }
    
    public void SortByType()
    {
        items.Sort((item1, item2) => item1.type.CompareTo(item2.type));
        
        EventManager.TriggerEvent(new NotificationEvent
        {
            message = "Inventário ordenado por tipo",
            type = NotificationType.Info,
            duration = 1f,
            color = Color.white
        });
        
        OnInventoryChanged?.Invoke();
    }
    
    public void SortByLevel()
    {
        items.Sort((item1, item2) => item2.level.CompareTo(item1.level));
        
        EventManager.TriggerEvent(new NotificationEvent
        {
            message = "Inventário ordenado por nível",
            type = NotificationType.Info,
            duration = 1f,
            color = Color.white
        });
        
        OnInventoryChanged?.Invoke();
    }
    
    public void SortByName()
    {
        items.Sort((item1, item2) => string.Compare(item1.name, item2.name, StringComparison.OrdinalIgnoreCase));
        
        EventManager.TriggerEvent(new NotificationEvent
        {
            message = "Inventário ordenado por nome",
            type = NotificationType.Info,
            duration = 1f,
            color = Color.white
        });
        
        OnInventoryChanged?.Invoke();
    }
    
    #endregion
    
    #region Utility
    
    public void ClearInventory()
    {
        // Desequipar todos os itens
        UnequipAllItems();
        
        // Limpar inventário
        items.Clear();
        
        OnInventoryChanged?.Invoke();
        
        Debug.Log("Inventário limpo!");
    }
    
    public void UnequipAllItems()
    {
        var equippedItems = GetAllEquippedItems();
        
        foreach (Item item in equippedItems)
        {
            UnequipItem(item);
        }
    }
    
    public int GetTotalValue()
    {
        int totalValue = 0;
        
        foreach (Item item in items)
        {
            // Calcular valor baseado na raridade e nível
            int itemValue = CalculateItemValue(item);
            totalValue += itemValue;
        }
        
        return totalValue;
    }
    
    private int CalculateItemValue(Item item)
    {
        int baseValue = item.level * 10;
        
        switch (item.rarity)
        {
            case ItemRarity.Common:
                return baseValue;
            case ItemRarity.Uncommon:
                return baseValue * 2;
            case ItemRarity.Rare:
                return baseValue * 4;
            case ItemRarity.Epic:
                return baseValue * 8;
            case ItemRarity.Legendary:
                return baseValue * 16;
            default:
                return baseValue;
        }
    }
    
    public void ExpandInventory(int additionalSlots)
    {
        if (additionalSlots > 0)
        {
            maxItems += additionalSlots;
            
            EventManager.TriggerEvent(new NotificationEvent
            {
                message = $"Inventário expandido! +{additionalSlots} slots",
                type = NotificationType.Success,
                duration = 2f,
                color = Color.green
            });
            
            Debug.Log($"Inventário expandido para {maxItems} slots");
        }
    }
    
    #endregion
    
    #region Serialization
    
    [System.Serializable]
    public class InventoryData
    {
        public List<Item> items;
        public Item equippedWeapon;
        public Item equippedHelmet;
        public Item equippedChest;
        public Item equippedGloves;
        public Item equippedBoots;
        public int maxItems;
    }
    
    public InventoryData ToSaveData()
    {
        return new InventoryData
        {
            items = new List<Item>(items),
            equippedWeapon = equippedWeapon,
            equippedHelmet = equippedHelmet,
            equippedChest = equippedChest,
            equippedGloves = equippedGloves,
            equippedBoots = equippedBoots,
            maxItems = maxItems
        };
    }
    
    public void LoadFromSaveData(InventoryData data)
    {
        if (data == null) return;
        
        ClearInventory();
        
        maxItems = data.maxItems;
        items = new List<Item>(data.items ?? new List<Item>());
        
        // Reequipar itens
        if (data.equippedWeapon != null && items.Contains(data.equippedWeapon))
            EquipItem(data.equippedWeapon);
        if (data.equippedHelmet != null && items.Contains(data.equippedHelmet))
            EquipItem(data.equippedHelmet);
        if (data.equippedChest != null && items.Contains(data.equippedChest))
            EquipItem(data.equippedChest);
        if (data.equippedGloves != null && items.Contains(data.equippedGloves))
            EquipItem(data.equippedGloves);
        if (data.equippedBoots != null && items.Contains(data.equippedBoots))
            EquipItem(data.equippedBoots);
        
        OnInventoryChanged?.Invoke();
        
        Debug.Log("Inventário carregado do save!");
    }
    
    #endregion
    
    #region Debug
    
    public void DebugPrintInventory()
    {
        Debug.Log("=== INVENTÁRIO ===");
        Debug.Log($"Slots: {items.Count}/{maxItems}");
        
        Debug.Log("Itens:");
        foreach (Item item in items)
        {
            Debug.Log($"- {item.name} ({item.type}, {item.rarity}, Nível {item.level})");
        }
        
        Debug.Log("Equipamentos:");
        Debug.Log($"Arma: {(equippedWeapon?.name ?? "Nenhuma")}");
        Debug.Log($"Elmo: {(equippedHelmet?.name ?? "Nenhum")}");
        Debug.Log($"Peitoral: {(equippedChest?.name ?? "Nenhum")}");
        Debug.Log($"Luvas: {(equippedGloves?.name ?? "Nenhuma")}");
        Debug.Log($"Botas: {(equippedBoots?.name ?? "Nenhuma")}");
    }
    
    #endregion
}