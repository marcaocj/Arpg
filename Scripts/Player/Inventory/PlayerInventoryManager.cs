using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// Gerenciador de inventário avançado com sistema de stacking, filtragem e organização
/// </summary>
public class PlayerInventoryManager : MonoBehaviour
{
    [Header("Configurações Básicas")]
    [SerializeField] private int maxItems = 40;
    [SerializeField] private bool autoSort = false;
    [SerializeField] private bool autoStack = true;          // NOVO: Auto-stack automático
    
    [Header("Configurações Avançadas")]
    [SerializeField] private int quickSlots = 8;             // NOVO: Slots rápidos para consumíveis
    [SerializeField] private float autoSortDelay = 0.5f;     // NOVO: Delay para auto-sort
    
    // NOVO: Sistema de filtros
    [Header("Sistema de Filtros")]
    public ItemFilter currentFilter = ItemFilter.All;
    public SortMode currentSortMode = SortMode.Type;
    
    // Listas de itens organizadas
    public List<Item> items = new List<Item>();
    public List<Item> quickSlotItems = new List<Item>(8);    // NOVO: Slots rápidos
    
    // Equipamentos atuais - expandido
    [Header("Equipamentos")]
    public Item equippedWeapon;
    public Item equippedHelmet;
    public Item equippedChest;
    public Item equippedGloves;
    public Item equippedBoots;
    public Item equippedRing1;        // NOVO: Anéis
    public Item equippedRing2;        // NOVO: Segundo anel
    public Item equippedNecklace;     // NOVO: Colar
    
    // Cache e otimizações
    private Dictionary<string, List<Item>> itemCache = new Dictionary<string, List<Item>>();
    private Dictionary<ItemType, int> typeCount = new Dictionary<ItemType, int>();
    private bool needsResort = false;
    private float lastSortTime = 0f;
    
    // Referências
    private PlayerStatsManager statsManager;
    private PlayerHealthManager healthManager;
    public PlayerController playerController; // Tornado público para acesso
    
    // Eventos
    public event Action<Item, bool> OnItemAdded;
    public event Action<Item> OnItemRemoved;
    public event Action<Item, Item> OnItemEquipped;
    public event Action<Item> OnItemUnequipped;
    public event Action OnInventoryChanged;
    public event Action<Item, int> OnItemStackChanged;       // NOVO: Stack alterado
    public event Action<ItemFilter> OnFilterChanged;        // NOVO: Filtro alterado
    
    // Enums para organização
    public enum ItemFilter
    {
        All,
        Equipment,
        Consumables,
        Materials,
        Quest,
        Weapons,
        Armor,
        Jewelry,
        Tradeable,
        Valuable
    }
    
    public enum SortMode
    {
        Type,
        Rarity,
        Level,
        Name,
        Value,
        Quality,
        Recent
    }
    
    #region Properties
    
    public int MaxItems => maxItems;
    public int CurrentItemCount => items.Count;
    public int FreeSlots => maxItems - items.Count;
    public bool HasSpace => items.Count < maxItems;
    public int TotalValue => items.Sum(item => item.GetCurrentValue() * item.currentStack);
    public int UniqueItemCount => items.Select(item => item.id).Distinct().Count();
    
    // NOVO: Propriedades de estatísticas
    public Dictionary<ItemRarity, int> RarityCount => GetRarityCount();
    public Dictionary<ItemType, int> TypeCount => typeCount;
    public List<Item> EquippedItems => GetAllEquippedItems();
    public List<Item> FilteredItems => GetFilteredItems(currentFilter);
    
    #endregion
    
    #region Initialization
    
    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        statsManager = GetComponent<PlayerStatsManager>();
        healthManager = GetComponent<PlayerHealthManager>();
        
        InitializeQuickSlots();
        InitializeTypeCount();
        
        if (statsManager == null)
        {
            Debug.LogError("PlayerInventoryManager: PlayerStatsManager não encontrado!");
        }
    }
    
    private void Start()
    {
        SubscribeToEvents();
        RefreshItemCache();
    }
    
    private void SubscribeToEvents()
    {
        // Eventos do sistema para otimizações
        EventManager.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        EventManager.Subscribe<GamePausedEvent>(OnGamePaused);
    }
    
    private void InitializeQuickSlots()
    {
        quickSlotItems.Clear();
        for (int i = 0; i < quickSlots; i++)
        {
            quickSlotItems.Add(null);
        }
    }
    
    private void InitializeTypeCount()
    {
        typeCount.Clear();
        foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
        {
            typeCount[type] = 0;
        }
    }
    
    #endregion
    
    #region Item Management - Core
    
    public bool AddItem(Item item)
    {
        if (item == null)
        {
            Debug.LogWarning("Tentativa de adicionar item nulo ao inventário!");
            return false;
        }
        
        if (!item.IsValid())
        {
            Debug.LogWarning($"Item inválido: {item.name}");
            return false;
        }
        
        // Tentar fazer stack primeiro se ativado
        if (autoStack && TryStackItem(item))
        {
            TriggerSuccessEvents(item, "Item empilhado");
            return true;
        }
        
        // Verificar espaço
        if (!HasSpace)
        {
            TriggerFailureEvents(item, "Inventário cheio");
            return false;
        }
        
        // Adicionar item
        items.Add(item);
        UpdateTypeCount(item.type, 1);
        
        // Auto-sort se ativado
        if (autoSort)
        {
            ScheduleSort();
        }
        
        // Atualizar cache
        RefreshItemCache();
        
        TriggerSuccessEvents(item, "Item adicionado");
        return true;
    }
    
    public bool AddItems(List<Item> itemsToAdd)
    {
        if (itemsToAdd == null || itemsToAdd.Count == 0) return false;
        
        var failedItems = new List<Item>();
        int successCount = 0;
        
        foreach (var item in itemsToAdd)
        {
            if (AddItem(item))
            {
                successCount++;
            }
            else
            {
                failedItems.Add(item);
            }
        }
        
        if (failedItems.Count > 0)
        {
            Debug.LogWarning($"Falha ao adicionar {failedItems.Count} itens. {successCount} adicionados com sucesso.");
        }
        
        return failedItems.Count == 0;
    }
    
    private bool TryStackItem(Item newItem)
    {
        if (!newItem.IsStackable) return false;
        
        foreach (var existingItem in items)
        {
            if (existingItem.CanStack(newItem))
            {
                int leftOver;
                if (existingItem.TryStack(newItem, out leftOver))
                {
                    OnItemStackChanged?.Invoke(existingItem, existingItem.currentStack);
                    
                    if (leftOver > 0)
                    {
                        // Ainda sobrou, criar novo item com o restante
                        newItem.currentStack = leftOver;
                        return AddItem(newItem); // Recursivo para tentar stack novamente
                    }
                    return true;
                }
            }
        }
        
        return false;
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
        
        // Remover dos quick slots se estiver lá
        RemoveFromQuickSlots(item);
        
        // Remover do inventário
        items.Remove(item);
        UpdateTypeCount(item.type, -1);
        
        // Atualizar cache
        RefreshItemCache();
        
        TriggerRemovalEvents(item, wasEquipped);
        return true;
    }
    
    public bool RemoveItem(string itemId, int amount = 1)
    {
        var item = FindItemById(itemId);
        if (item == null) return false;
        
        return RemoveItemAmount(item, amount);
    }
    
    public bool RemoveItemAmount(Item item, int amount)
    {
        if (item == null || amount <= 0) return false;
        
        if (!item.IsStackable)
        {
            return amount == 1 ? RemoveItem(item) : false;
        }
        
        if (item.currentStack <= amount)
        {
            return RemoveItem(item);
        }
        
        item.currentStack -= amount;
        OnItemStackChanged?.Invoke(item, item.currentStack);
        RefreshItemCache();
        
        return true;
    }
    
    #endregion
    
    #region Equipment Management - Expandido
    
    public bool EquipItem(Item item)
    {
        if (item == null || !items.Contains(item))
        {
            Debug.Log("Item não está no inventário!");
            return false;
        }
        
        if (item.IsConsumable)
        {
            UseConsumableItem(item);
            return true;
        }
        
        if (!item.IsEquipment)
        {
            Debug.Log("Item não é equipável!");
            return false;
        }

        // Verificar se o item já está equipado
        if (IsItemEquipped(item))
        {
            // Se já estiver equipado, desequipar
            return UnequipItem(item);
        }
        
        // Verificar requirements
        var playerStats = statsManager?.Stats;
        if (!item.CanPlayerUse(playerStats))
        {
            var missing = item.GetMissingRequirements(playerStats);
            EventManager.TriggerEvent(new NotificationEvent
            {
                message = $"Não é possível equipar {item.name}: {string.Join(", ", missing)}",
                type = NotificationType.Warning,
                duration = 3f,
                color = Color.yellow
            });
            return false;
        }
        
        Item previousItem = GetEquippedItem(item.type);
        
        // Para joias, verificar slots disponíveis
        if (item.type == ItemType.Jewelry)
        {
            previousItem = FindAvailableJewelrySlot(item);
        }
        
        // Desequipar item anterior se houver
        if (previousItem != null)
        {
            UnequipItem(previousItem);
        }
        
        // Equipar novo item
        SetEquippedItem(item);
        
        // Aplicar modificadores
        ApplyItemModifiers(item, true);
        
        TriggerEquipEvents(item, previousItem);
        
        Debug.Log($"Item equipado: {item.name}");
        return true;
    }
    
    private Item FindAvailableJewelrySlot(Item jewelry)
    {
        // Tentar primeiro slot vazio
        if (equippedRing1 == null) return null;
        if (equippedRing2 == null) return null;
        if (equippedNecklace == null) return null;
        
        // Se todos ocupados, substituir o primeiro
        return equippedRing1;
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
        ClearEquippedItem(item);
        
        TriggerUnequipEvents(item);
        
        Debug.Log($"Item desequipado: {item.name}");
        return true;
    }
    
    public void UnequipAll()
    {
        var equippedItems = GetAllEquippedItems();
        foreach (var item in equippedItems)
        {
            UnequipItem(item);
        }
    }
    
    #endregion
    
    #region Quick Slots Management - NOVO
    
    public bool AddToQuickSlot(Item item, int slotIndex)
    {
        if (!item.IsConsumable) return false;
        if (slotIndex < 0 || slotIndex >= quickSlots) return false;
        if (!items.Contains(item)) return false;
        
        quickSlotItems[slotIndex] = item;
        OnInventoryChanged?.Invoke();
        
        return true;
    }
    
    public bool RemoveFromQuickSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= quickSlots) return false;
        
        quickSlotItems[slotIndex] = null;
        OnInventoryChanged?.Invoke();
        
        return true;
    }
    
    private void RemoveFromQuickSlots(Item item)
    {
        for (int i = 0; i < quickSlots; i++)
        {
            if (quickSlotItems[i] == item)
            {
                quickSlotItems[i] = null;
            }
        }
    }
    
    public Item GetQuickSlotItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= quickSlots) return null;
        return quickSlotItems[slotIndex];
    }
    
    public bool UseQuickSlotItem(int slotIndex)
    {
        var item = GetQuickSlotItem(slotIndex);
        if (item == null) return false;
        
        if (item.IsConsumable)
        {
            UseConsumableItem(item);
            return true;
        }
        
        return false;
    }
    
    #endregion
    
    #region Consumable System - Melhorado
    
    private void UseConsumableItem(Item item)
    {
        if (item.type != ItemType.Consumable || healthManager == null)
        {
            return;
        }
        
        // Verificar cooldown global de consumíveis (opcional)
        if (IsOnConsumableCooldown())
        {
            EventManager.TriggerEvent(new NotificationEvent
            {
                message = "Aguarde antes de usar outro consumível",
                type = NotificationType.Warning,
                duration = 1f,
                color = new Color(1f, 0.5f, 0f) // Laranja
            });
            return;
        }
        
        int oldHealth = healthManager.CurrentHealth;
        int oldMana = healthManager.CurrentMana;
        
        // Aplicar efeitos básicos
        if (item.healthRestore > 0)
        {
            healthManager.Heal(item.healthRestore);
        }
        
        if (item.manaRestore > 0)
        {
            healthManager.RestoreMana(item.manaRestore);
        }
        
        // Criar popups de feedback
        CreateUsagePopups(item, oldHealth, oldMana);
        
        // Reduzir stack ou remover item
        if (RemoveItemAmount(item, 1))
        {
            Debug.Log($"Item consumido: {item.name}");
        }
        
        // Aplicar cooldown se necessário
        ApplyConsumableCooldown();
    }
    
    private bool IsOnConsumableCooldown()
    {
        // Implementar sistema de cooldown se necessário
        return false;
    }
    
    private void ApplyConsumableCooldown()
    {
        // Implementar cooldown global para consumíveis
    }
    
    private void CreateUsagePopups(Item item, int oldHealth, int oldMana)
    {
        int actualHealthRestored = healthManager.CurrentHealth - oldHealth;
        int actualManaRestored = healthManager.CurrentMana - oldMana;
        
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
    }
    
    #endregion
    
    #region Equipment Helpers - Expandido
    
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
            case ItemType.Jewelry:
                SetJewelryItem(item);
                break;
        }
    }
    
    private void SetJewelryItem(Item jewelry)
    {
        // Lógica para determinar onde colocar a joia
        if (jewelry.name.Contains("Ring") || jewelry.name.Contains("Anel"))
        {
            if (equippedRing1 == null)
                equippedRing1 = jewelry;
            else if (equippedRing2 == null)
                equippedRing2 = jewelry;
            else
                equippedRing1 = jewelry; // Substituir primeiro anel
        }
        else
        {
            equippedNecklace = jewelry;
        }
    }
    
    private void ClearEquippedItem(Item item)
    {
        if (equippedWeapon == item) equippedWeapon = null;
        else if (equippedHelmet == item) equippedHelmet = null;
        else if (equippedChest == item) equippedChest = null;
        else if (equippedGloves == item) equippedGloves = null;
        else if (equippedBoots == item) equippedBoots = null;
        else if (equippedRing1 == item) equippedRing1 = null;
        else if (equippedRing2 == item) equippedRing2 = null;
        else if (equippedNecklace == item) equippedNecklace = null;
    }
    
    private bool IsItemEquipped(Item item)
    {
        return equippedWeapon == item ||
               equippedHelmet == item ||
               equippedChest == item ||
               equippedGloves == item ||
               equippedBoots == item ||
               equippedRing1 == item ||
               equippedRing2 == item ||
               equippedNecklace == item;
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
    
    #region Query Methods - Expandido
    
    public Item GetEquippedItem(ItemType type)
    {
        switch (type)
        {
            case ItemType.Weapon: return equippedWeapon;
            case ItemType.Helmet: return equippedHelmet;
            case ItemType.Chest: return equippedChest;
            case ItemType.Gloves: return equippedGloves;
            case ItemType.Boots: return equippedBoots;
            default: return null;
        }
    }
    
    public List<Item> GetAllEquippedItems()
    {
        var equippedItems = new List<Item>();
        
        if (equippedWeapon != null) equippedItems.Add(equippedWeapon);
        if (equippedHelmet != null) equippedItems.Add(equippedHelmet);
        if (equippedChest != null) equippedItems.Add(equippedChest);
        if (equippedGloves != null) equippedItems.Add(equippedGloves);
        if (equippedBoots != null) equippedItems.Add(equippedBoots);
        if (equippedRing1 != null) equippedItems.Add(equippedRing1);
        if (equippedRing2 != null) equippedItems.Add(equippedRing2);
        if (equippedNecklace != null) equippedItems.Add(equippedNecklace);
        
        return equippedItems;
    }
    
    public Item FindItemById(string itemId)
    {
        return items.FirstOrDefault(item => item.id == itemId);
    }
    
    public Item FindItemByName(string itemName)
    {
        return items.FirstOrDefault(item => 
            item.name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
    }
    
    public List<Item> FindItemsByType(ItemType type)
    {
        return items.Where(item => item.type == type).ToList();
    }
    
    public List<Item> FindItemsByRarity(ItemRarity rarity)
    {
        return items.Where(item => item.rarity == rarity).ToList();
    }
    
    public List<Item> FindItemsByLevel(int minLevel, int maxLevel = int.MaxValue)
    {
        return items.Where(item => item.level >= minLevel && item.level <= maxLevel).ToList();
    }
    
    public int CountItemsOfType(ItemType type)
    {
        return typeCount.ContainsKey(type) ? typeCount[type] : 0;
    }
    
    public int CountItemById(string itemId)
    {
        var item = FindItemById(itemId);
        return item?.currentStack ?? 0;
    }
    
    public bool HasItem(string itemId, int amount = 1)
    {
        return CountItemById(itemId) >= amount;
    }
    
    public bool HasItemOfType(ItemType type, int amount = 1)
    {
        return CountItemsOfType(type) >= amount;
    }
    
    #endregion
    
    #region Filtering & Sorting - NOVO Sistema Avançado
    
    public void SetFilter(ItemFilter filter)
    {
        if (currentFilter != filter)
        {
            currentFilter = filter;
            OnFilterChanged?.Invoke(filter);
            RefreshItemCache();
        }
    }
    
    public List<Item> GetFilteredItems(ItemFilter filter)
    {
        switch (filter)
        {
            case ItemFilter.All:
                return new List<Item>(items);
                
            case ItemFilter.Equipment:
                return items.Where(item => item.IsEquipment).ToList();
                
            case ItemFilter.Consumables:
                return items.Where(item => item.IsConsumable).ToList();
                
            case ItemFilter.Materials:
                return items.Where(item => item.type == ItemType.Material).ToList();
                
            case ItemFilter.Quest:
                return items.Where(item => item.type == ItemType.Quest).ToList();
                
            case ItemFilter.Weapons:
                return items.Where(item => item.type == ItemType.Weapon).ToList();
                
            case ItemFilter.Armor:
                return items.Where(item => item.type == ItemType.Helmet || 
                                          item.type == ItemType.Chest || 
                                          item.type == ItemType.Gloves || 
                                          item.type == ItemType.Boots).ToList();
                
            case ItemFilter.Jewelry:
                return items.Where(item => item.type == ItemType.Jewelry).ToList();
                
            case ItemFilter.Tradeable:
                return items.Where(item => item.isTradeable).ToList();
                
            case ItemFilter.Valuable:
                return items.Where(item => item.GetCurrentValue() >= 100).ToList();
                
            default:
                return new List<Item>(items);
        }
    }
    
    public void SetSortMode(SortMode mode)
    {
        if (currentSortMode != mode)
        {
            currentSortMode = mode;
            SortInventory(mode);
        }
    }
    
    public void SortInventory(SortMode mode)
    {
        switch (mode)
        {
            case SortMode.Type:
                SortByType();
                break;
            case SortMode.Rarity:
                SortByRarity();
                break;
            case SortMode.Level:
                SortByLevel();
                break;
            case SortMode.Name:
                SortByName();
                break;
            case SortMode.Value:
                SortByValue();
                break;
            case SortMode.Quality:
                SortByQuality();
                break;
        }
        
        RefreshItemCache();
        OnInventoryChanged?.Invoke();
    }
    
    public void SortByRarity()
    {
        items.Sort((item1, item2) => item2.rarity.CompareTo(item1.rarity));
        TriggerSortNotification("raridade");
    }
    
    public void SortByType()
    {
        items.Sort((item1, item2) => item1.type.CompareTo(item2.type));
        TriggerSortNotification("tipo");
    }
    
    public void SortByLevel()
    {
        items.Sort((item1, item2) => item2.level.CompareTo(item1.level));
        TriggerSortNotification("nível");
    }
    
    public void SortByName()
    {
        items.Sort((item1, item2) => string.Compare(item1.name, item2.name, StringComparison.OrdinalIgnoreCase));
        TriggerSortNotification("nome");
    }
    
    public void SortByValue()
    {
        items.Sort((item1, item2) => item2.GetCurrentValue().CompareTo(item1.GetCurrentValue()));
        TriggerSortNotification("valor");
    }
    
    public void SortByQuality()
    {
        items.Sort((item1, item2) => item2.quality.CompareTo(item1.quality));
        TriggerSortNotification("qualidade");
    }
    
    private void ScheduleSort()
    {
        needsResort = true;
    }
    
    private void Update()
    {
        if (needsResort && Time.time - lastSortTime >= autoSortDelay)
        {
            SortInventory(currentSortMode);
            needsResort = false;
            lastSortTime = Time.time;
        }
    }
    
    #endregion
    
    #region Cache & Performance - NOVO
    
    private void RefreshItemCache()
    {
        itemCache.Clear();
        
        // Cache por tipo
        foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
        {
            itemCache[type.ToString()] = FindItemsByType(type);
        }
        
        // Cache por raridade
        foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
        {
            itemCache[rarity.ToString()] = FindItemsByRarity(rarity);
        }
        
        // Atualizar contadores
        UpdateAllTypeCounts();
    }
    
    private void UpdateTypeCount(ItemType type, int delta)
    {
        if (typeCount.ContainsKey(type))
        {
            typeCount[type] = Mathf.Max(0, typeCount[type] + delta);
        }
        else
        {
            typeCount[type] = Mathf.Max(0, delta);
        }
    }
    
    private void UpdateAllTypeCounts()
    {
        InitializeTypeCount();
        foreach (var item in items)
        {
            typeCount[item.type]++;
        }
    }
    
    private Dictionary<ItemRarity, int> GetRarityCount()
    {
        var rarityCount = new Dictionary<ItemRarity, int>();
        
        foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
        {
            rarityCount[rarity] = 0;
        }
        
        foreach (var item in items)
        {
            rarityCount[item.rarity]++;
        }
        
        return rarityCount;
    }
    
    #endregion
    
    #region Utility Methods - Expandido
    
    public void ClearInventory()
    {
        UnequipAll();
        items.Clear();
        InitializeQuickSlots();
        InitializeTypeCount();
        RefreshItemCache();
        
        OnInventoryChanged?.Invoke();
        
        Debug.Log("Inventário limpo!");
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
    
    public List<Item> GetItemsOfValue(int minValue)
    {
        return items.Where(item => item.GetCurrentValue() >= minValue).ToList();
    }
    
    public List<Item> GetEquippableItems(PlayerStats playerStats)
    {
        return items.Where(item => item.IsEquipment && item.CanPlayerUse(playerStats)).ToList();
    }
    
    public void AutoEquipBestItems()
    {
        if (statsManager?.Stats == null) return;
        
        var playerStats = statsManager.Stats;
        var equipableItems = GetEquippableItems(playerStats);
        
        foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
        {
            if (type == ItemType.Consumable || type == ItemType.Material || type == ItemType.Quest) continue;
            
            var bestItem = equipableItems
                .Where(item => item.type == type)
                .OrderByDescending(item => item.rarity)
                .ThenByDescending(item => item.level)
                .FirstOrDefault();
            
            if (bestItem != null && (GetEquippedItem(type)?.IsBetterThan(bestItem) != true))
            {
                EquipItem(bestItem);
            }
        }
    }
    
    public void CompactInventory()
    {
        // Compactar stacks primeiro
        var stackableItems = items.Where(item => item.IsStackable).GroupBy(item => item.name).ToList();
        
        foreach (var group in stackableItems)
        {
            var itemList = group.OrderBy(item => item.currentStack).ToList();
            
            for (int i = 0; i < itemList.Count - 1; i++)
            {
                for (int j = i + 1; j < itemList.Count; j++)
                {
                    if (itemList[i].CanStack(itemList[j]))
                    {
                        int leftOver;
                        if (itemList[i].TryStack(itemList[j], out leftOver))
                        {
                            if (leftOver == 0)
                            {
                                items.Remove(itemList[j]);
                                itemList.RemoveAt(j);
                                j--;
                            }
                        }
                    }
                }
            }
        }
        
        RefreshItemCache();
        OnInventoryChanged?.Invoke();
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnPlayerLevelUp(PlayerLevelUpEvent eventData)
    {
        // Verificar se há itens que agora podem ser equipados
        var playerStats = statsManager?.Stats;
        if (playerStats == null) return;
        
        var newlyEquippable = items.Where(item => 
            item.IsEquipment && 
            item.levelRequirement <= playerStats.Level && 
            item.levelRequirement > eventData.oldLevel).ToList();
        
        if (newlyEquippable.Count > 0)
        {
            EventManager.TriggerEvent(new NotificationEvent
            {
                message = $"{newlyEquippable.Count} novos itens disponíveis para equipar!",
                type = NotificationType.Info,
                duration = 3f,
                color = Color.cyan
            });
        }
    }
    
    private void OnGamePaused(GamePausedEvent eventData)
    {
        if (eventData.isPaused)
        {
            // Salvar estado do inventário quando pausar
            SaveInventoryState();
        }
    }
    
    #endregion
    
    #region Event Triggers
    
    private void TriggerSuccessEvents(Item item, string message)
    {
        EventManager.TriggerEvent(new ItemAddedEvent
        {
            item = item,
            wasSuccessful = true,
            failureReason = ""
        });
        
        OnItemAdded?.Invoke(item, true);
        OnInventoryChanged?.Invoke();
        
        Debug.Log($"{message}: {item.name}");
    }
    
    private void TriggerFailureEvents(Item item, string reason)
    {
        EventManager.TriggerEvent(new ItemAddedEvent
        {
            item = item,
            wasSuccessful = false,
            failureReason = reason
        });
        
        EventManager.TriggerEvent(new InventoryFullEvent
        {
            attemptedItem = item,
            maxCapacity = maxItems
        });
        
        OnItemAdded?.Invoke(item, false);
        
        Debug.Log($"Falha ao adicionar item: {reason}");
    }
    
    private void TriggerRemovalEvents(Item item, bool wasEquipped)
    {
        EventManager.TriggerEvent(new ItemRemovedEvent
        {
            item = item,
            wasEquipped = wasEquipped
        });
        
        OnItemRemoved?.Invoke(item);
        OnInventoryChanged?.Invoke();
        
        Debug.Log($"Item removido: {item.name}");
    }
    
    private void TriggerEquipEvents(Item newItem, Item previousItem)
    {
        EventManager.TriggerEvent(new ItemEquippedEvent
        {
            item = newItem,
            previousItem = previousItem,
            slotType = newItem.type
        });
        
        OnItemEquipped?.Invoke(newItem, previousItem);
        OnInventoryChanged?.Invoke();
    }
    
    private void TriggerUnequipEvents(Item item)
    {
        EventManager.TriggerEvent(new ItemUnequippedEvent
        {
            item = item,
            slotType = item.type
        });
        
        OnItemUnequipped?.Invoke(item);
        OnInventoryChanged?.Invoke();
    }
    
    private void TriggerSortNotification(string sortType)
    {
        EventManager.TriggerEvent(new NotificationEvent
        {
            message = $"Inventário ordenado por {sortType}",
            type = NotificationType.Info,
            duration = 1f,
            color = Color.white
        });
    }
    
    #endregion
    
    #region Serialization - Melhorado
    
    [System.Serializable]
    public class InventoryData
    {
        public List<Item> items;
        public List<Item> quickSlotItems;
        public Item equippedWeapon;
        public Item equippedHelmet;
        public Item equippedChest;
        public Item equippedGloves;
        public Item equippedBoots;
        public Item equippedRing1;
        public Item equippedRing2;
        public Item equippedNecklace;
        public int maxItems;
        public ItemFilter currentFilter;
        public SortMode currentSortMode;
        public bool autoSort;
        public bool autoStack;
    }
    
    public InventoryData ToSaveData()
    {
        return new InventoryData
        {
            items = new List<Item>(items),
            quickSlotItems = new List<Item>(quickSlotItems),
            equippedWeapon = equippedWeapon,
            equippedHelmet = equippedHelmet,
            equippedChest = equippedChest,
            equippedGloves = equippedGloves,
            equippedBoots = equippedBoots,
            equippedRing1 = equippedRing1,
            equippedRing2 = equippedRing2,
            equippedNecklace = equippedNecklace,
            maxItems = maxItems,
            currentFilter = currentFilter,
            currentSortMode = currentSortMode,
            autoSort = autoSort,
            autoStack = autoStack
        };
    }
    
    public void LoadFromSaveData(InventoryData data)
    {
        if (data == null) return;
        
        ClearInventory();
        
        maxItems = data.maxItems;
        currentFilter = data.currentFilter;
        currentSortMode = data.currentSortMode;
        autoSort = data.autoSort;
        autoStack = data.autoStack;
        
        items = new List<Item>(data.items ?? new List<Item>());
        quickSlotItems = new List<Item>(data.quickSlotItems ?? new List<Item>());
        
        // Garantir tamanho correto dos quick slots
        while (quickSlotItems.Count < quickSlots)
        {
            quickSlotItems.Add(null);
        }
        
        // Reequipar itens
        TryEquipItem(data.equippedWeapon);
        TryEquipItem(data.equippedHelmet);
        TryEquipItem(data.equippedChest);
        TryEquipItem(data.equippedGloves);
        TryEquipItem(data.equippedBoots);
        TryEquipItem(data.equippedRing1);
        TryEquipItem(data.equippedRing2);
        TryEquipItem(data.equippedNecklace);
        
        RefreshItemCache();
        OnInventoryChanged?.Invoke();
        
        Debug.Log("Inventário carregado do save!");
    }
    
    private void TryEquipItem(Item item)
    {
        if (item != null && items.Contains(item))
        {
            EquipItem(item);
        }
    }
    
    private void SaveInventoryState()
    {
        var saveData = ToSaveData();
        var json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString("InventoryData", json);
        PlayerPrefs.Save();
    }
    
    public void LoadInventoryState()
    {
        if (PlayerPrefs.HasKey("InventoryData"))
        {
            var json = PlayerPrefs.GetString("InventoryData");
            var saveData = JsonUtility.FromJson<InventoryData>(json);
            LoadFromSaveData(saveData);
        }
    }
    
    #endregion
    
    #region Debug & Statistics
    
    public void DebugPrintInventory()
    {
        Debug.Log("=== INVENTÁRIO AVANÇADO ===");
        Debug.Log($"Slots: {items.Count}/{maxItems} | Valor Total: {TotalValue}");
        Debug.Log($"Filtro: {currentFilter} | Ordenação: {currentSortMode}");
        
        Debug.Log("\n=== CONTAGEM POR TIPO ===");
        foreach (var kvp in typeCount)
        {
            if (kvp.Value > 0)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value}");
            }
        }
        
        Debug.Log("\n=== CONTAGEM POR RARIDADE ===");
        var rarityCount = GetRarityCount();
        foreach (var kvp in rarityCount)
        {
            if (kvp.Value > 0)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value}");
            }
        }
        
        Debug.Log("\n=== ITENS (Filtrados) ===");
        var filteredItems = GetFilteredItems(currentFilter);
        foreach (Item item in filteredItems.Take(10)) // Mostrar apenas 10 primeiros
        {
            string stackInfo = item.IsStackable ? $" x{item.currentStack}" : "";
            Debug.Log($"- {item.name}{stackInfo} ({item.type}, {item.rarity}, Nível {item.level}, Valor: {item.GetCurrentValue()})");
        }
        
        if (filteredItems.Count > 10)
        {
            Debug.Log($"... e mais {filteredItems.Count - 10} itens");
        }
        
        Debug.Log("\n=== EQUIPAMENTOS ===");
        Debug.Log($"Arma: {(equippedWeapon?.name ?? "Nenhuma")}");
        Debug.Log($"Elmo: {(equippedHelmet?.name ?? "Nenhum")}");
        Debug.Log($"Peitoral: {(equippedChest?.name ?? "Nenhum")}");
        Debug.Log($"Luvas: {(equippedGloves?.name ?? "Nenhuma")}");
        Debug.Log($"Botas: {(equippedBoots?.name ?? "Nenhuma")}");
        Debug.Log($"Anel 1: {(equippedRing1?.name ?? "Nenhum")}");
        Debug.Log($"Anel 2: {(equippedRing2?.name ?? "Nenhum")}");
        Debug.Log($"Colar: {(equippedNecklace?.name ?? "Nenhum")}");
        
        Debug.Log("\n=== QUICK SLOTS ===");
        for (int i = 0; i < quickSlots; i++)
        {
            var item = quickSlotItems[i];
            Debug.Log($"Slot {i + 1}: {(item?.name ?? "Vazio")}");
        }
    }
    
    public InventoryStatistics GetStatistics()
    {
        return new InventoryStatistics
        {
            totalItems = items.Count,
            maxItems = maxItems,
            totalValue = TotalValue,
            uniqueItems = UniqueItemCount,
            rarityCount = GetRarityCount(),
            typeCount = new Dictionary<ItemType, int>(typeCount),
            averageItemLevel = items.Count > 0 ? (float)items.Average(item => item.level) : 0f, // CORRIGIDO: Cast para float
            mostValuableItem = items.OrderByDescending(item => item.GetCurrentValue()).FirstOrDefault(),
            equippedItemsCount = GetAllEquippedItems().Count
        };
    }
    
    [System.Serializable]
    public class InventoryStatistics
    {
        public int totalItems;
        public int maxItems;
        public int totalValue;
        public int uniqueItems;
        public Dictionary<ItemRarity, int> rarityCount;
        public Dictionary<ItemType, int> typeCount;
        public float averageItemLevel;
        public Item mostValuableItem;
        public int equippedItemsCount;
    }
    
    #endregion
    
    #region Cleanup
    
    private void OnDestroy()
    {
        EventManager.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        EventManager.Unsubscribe<GamePausedEvent>(OnGamePaused);
    }
    
    #endregion
}