using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Sistema avançado de loot table com múltiplas raridades, condições e balanceamento
/// </summary>
[CreateAssetMenu(fileName = "New Loot Table", menuName = "RPG/Advanced Loot Table")]
public class LootTable : ScriptableObject
{
    [System.Serializable]
    public class LootEntry
    {
        [Header("Item Configuration")]
        public Item item;
        public float dropChance = 10f;          // 0-100%
        public int minQuantity = 1;
        public int maxQuantity = 1;
        public bool respectStackLimits = true;
        
        [Header("Conditions")]
        public int minPlayerLevel = 1;
        public int maxPlayerLevel = 999;
        public List<string> requiredTags = new List<string>();
        public List<string> excludedTags = new List<string>();
        
        [Header("Rarity Scaling")]
        public bool scaleWithLuck = true;
        public float rarityMultiplier = 1f;     // Multiplica chance baseado na raridade
        public AnimationCurve levelScaling = AnimationCurve.Linear(1, 1, 100, 2);
        
        [Header("Advanced")]
        public bool isGuaranteed = false;       // Sempre dropa (ignora chance)
        public bool isUnique = false;           // Só pode dropar uma vez
        public string uniqueId = "";            // ID para controle de unique
        
        // Estado interno
        [System.NonSerialized]
        public bool hasDropped = false;
        
        public bool CanDropForPlayer(int playerLevel, float playerLuck, List<string> playerTags = null)
        {
            // Verificar level
            if (playerLevel < minPlayerLevel || playerLevel > maxPlayerLevel)
                return false;
            
            // Verificar se já dropou (para uniques)
            if (isUnique && hasDropped)
                return false;
            
            // Verificar tags obrigatórias e excluídas
            if (requiredTags.Count > 0 && (playerTags == null || !requiredTags.All(tag => playerTags.Contains(tag))))
                return false;
            
            if (excludedTags.Count > 0 && playerTags != null && excludedTags.Any(tag => playerTags.Contains(tag)))
                return false;
            
            return true;
        }
        
        public float GetActualDropChance(int playerLevel, float playerLuck)
        {
            if (isGuaranteed) return 100f;
            
            float chance = dropChance;
            
            // Aplicar scaling de nível
            chance *= levelScaling.Evaluate(playerLevel);
            
            // Aplicar scaling de raridade
            if (item != null)
            {
                chance *= GetRarityChanceMultiplier(item.rarity) * rarityMultiplier;
            }
            
            // Aplicar luck do player
            if (scaleWithLuck)
            {
                chance *= (1f + playerLuck);
            }
            
            return Mathf.Clamp(chance, 0f, 100f);
        }
        
        private float GetRarityChanceMultiplier(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return 1f;
                case ItemRarity.Uncommon: return 0.7f;
                case ItemRarity.Rare: return 0.4f;
                case ItemRarity.Epic: return 0.2f;
                case ItemRarity.Legendary: return 0.1f;
                case ItemRarity.Artifact: return 0.05f;
                default: return 1f;
            }
        }
    }
    
    [Header("Basic Settings")]
    public string tableName = "Loot Table";
    public string description = "";
    
    [Header("Gold Settings")]
    public bool dropGold = true;
    public float goldMin = 5;
    public float goldMax = 25;
    public AnimationCurve goldScaling = AnimationCurve.Linear(1, 1, 100, 10);
    
    [Header("Drop Settings")]
    public int minItems = 0;                    // Mínimo de itens a dropar
    public int maxItems = 3;                    // Máximo de itens a dropar
    public bool allowDuplicates = true;         // Permitir mesmo item várias vezes
    public float globalLuckMultiplier = 1f;     // Multiplicador global de sorte
    
    [Header("Loot Entries")]
    public List<LootEntry> possibleLoot = new List<LootEntry>();
    
    [Header("Debug")]
    public bool enableDebugLog = false;
    public bool showDropStatistics = false;
    
    // Estatísticas de debug
    [System.NonSerialized]
    private Dictionary<string, int> dropStatistics = new Dictionary<string, int>();
    
    #region Initialization
    
    public void InitializeDefault(int enemyLevel)
    {
        possibleLoot.Clear();
        
        // Criar itens básicos baseados no nível
        CreateHealthPotions(enemyLevel);
        CreateManaPotions(enemyLevel);
        CreateWeapons(enemyLevel);
        CreateArmor(enemyLevel);
        CreateMaterials(enemyLevel);
        
        // Configurar gold scaling
        goldMin = enemyLevel * 2f;
        goldMax = enemyLevel * 8f;
        
        Debug.Log($"Loot table inicializada para nível {enemyLevel} com {possibleLoot.Count} entradas");
    }
    
    private void CreateHealthPotions(int level)
    {
        // Poção básica
        var basicHealthPotion = CreateConsumableItem("Poção de Vida Menor", "Recupera 25 pontos de vida", 
            ItemRarity.Common, level, 25, 0);
        AddLootEntry(basicHealthPotion, 35f, 1, 2);
        
        // Poção média (para níveis mais altos)
        if (level >= 5)
        {
            var mediumHealthPotion = CreateConsumableItem("Poção de Vida", "Recupera 75 pontos de vida", 
                ItemRarity.Uncommon, level, 75, 0);
            AddLootEntry(mediumHealthPotion, 20f, 1, 1);
        }
        
        // Poção maior (para níveis muito altos)
        if (level >= 15)
        {
            var majorHealthPotion = CreateConsumableItem("Poção de Vida Maior", "Recupera 150 pontos de vida", 
                ItemRarity.Rare, level, 150, 0);
            AddLootEntry(majorHealthPotion, 8f, 1, 1);
        }
    }
    
    private void CreateManaPotions(int level)
    {
        var basicManaPotion = CreateConsumableItem("Poção de Mana Menor", "Recupera 20 pontos de mana", 
            ItemRarity.Common, level, 0, 20);
        AddLootEntry(basicManaPotion, 25f, 1, 2);
        
        if (level >= 5)
        {
            var mediumManaPotion = CreateConsumableItem("Poção de Mana", "Recupera 50 pontos de mana", 
                ItemRarity.Uncommon, level, 0, 50);
            AddLootEntry(mediumManaPotion, 15f, 1, 1);
        }
    }
    
    private void CreateWeapons(int level)
    {
        // Arma comum
        var commonWeapon = new Item("Espada Enferrujada", "Uma espada desgastada pelo tempo", 
            ItemType.Weapon, ItemRarity.Common, level);
        commonWeapon.physicalDamage = 8 + level * 2;
        commonWeapon.strengthModifier = 1;
        AddLootEntry(commonWeapon, 12f);
        
        // Arma incomum
        if (level >= 3)
        {
            var uncommonWeapon = new Item("Lâmina de Ferro", "Uma espada bem forjada", 
                ItemType.Weapon, ItemRarity.Uncommon, level);
            uncommonWeapon.physicalDamage = 15 + level * 3;
            uncommonWeapon.strengthModifier = 2 + level / 5;
            uncommonWeapon.criticalChanceBonus = 0.02f;
            AddLootEntry(uncommonWeapon, 5f);
        }
        
        // Arma rara
        if (level >= 8)
        {
            var rareWeapon = new Item("Espada Élfica", "Uma lâmina encantada pelos elfos", 
                ItemType.Weapon, ItemRarity.Rare, level);
            rareWeapon.physicalDamage = 25 + level * 4;
            rareWeapon.strengthModifier = 3 + level / 3;
            rareWeapon.dexterityModifier = 1 + level / 5;
            rareWeapon.criticalChanceBonus = 0.05f;
            rareWeapon.criticalDamageBonus = 0.1f;
            AddLootEntry(rareWeapon, 2f);
        }
    }
    
    private void CreateArmor(int level)
    {
        // Elmo comum
        var helmet = new Item("Elmo de Couro", "Proteção básica para a cabeça", 
            ItemType.Helmet, ItemRarity.Common, level);
        helmet.vitalityModifier = 1 + level / 5;
        helmet.physicalDefense = 3 + level;
        AddLootEntry(helmet, 8f);
        
        // Peitoral incomum
        if (level >= 4)
        {
            var chest = new Item("Armadura de Malha", "Proteção robusta para o torso", 
                ItemType.Chest, ItemRarity.Uncommon, level);
            chest.vitalityModifier = 2 + level / 3;
            chest.physicalDefense = 8 + level * 2;
            AddLootEntry(chest, 4f);
        }
    }
    
    private void CreateMaterials(int level)
    {
        // Material comum
        var leather = new Item("Couro Curtido", "Material básico para crafting", 
            ItemType.Material, ItemRarity.Common, level, 10);
        leather.isCraftingMaterial = true;
        leather.craftingTags.Add("leather");
        leather.craftingTags.Add("basic");
        AddLootEntry(leather, 40f, 1, 5);
        
        // Material raro
        if (level >= 10)
        {
            var crystal = new Item("Cristal Mágico", "Cristal imbuído com energia arcana", 
                ItemType.Material, ItemRarity.Rare, level, 5);
            crystal.isCraftingMaterial = true;
            crystal.craftingTags.Add("crystal");
            crystal.craftingTags.Add("magic");
            AddLootEntry(crystal, 3f, 1, 2);
        }
    }
    
    #endregion
    
    #region Loot Generation
    
    // Método de compatibilidade - mantém funcionalidade anterior
    public List<Item> RollForLoot()
    {
        var result = new List<Item>();
        
        // Rolar para cada item possível
        foreach (LootEntry entry in possibleLoot)
        {
            float roll = Random.Range(0f, 100f);
            if (roll <= entry.dropChance)
            {
                result.Add(entry.item);
            }
        }
        
        // Debug do gold (implementação separada como era antes)
        float goldAmount = Random.Range(goldMin, goldMax);
        Debug.Log("Ouro dropado: " + Mathf.RoundToInt(goldAmount));
        
        return result;
    }
    
    public LootResult RollForLootAdvanced(int playerLevel = 1, float playerLuck = 0f, List<string> playerTags = null)
    {
        var result = new LootResult();
        
        if (enableDebugLog)
        {
            Debug.Log($"[LootTable] Rolling loot for level {playerLevel} player with {playerLuck:F2} luck");
        }
        
        result = RollFromMainTable(playerLevel, playerLuck, playerTags);
        
        // Dropar gold
        if (dropGold)
        {
            result.goldAmount = CalculateGoldDrop(playerLevel, playerLuck);
        }
        
        // Atualizar estatísticas
        UpdateDropStatistics(result);
        
        if (enableDebugLog)
        {
            Debug.Log($"[LootTable] Generated {result.items.Count} items and {result.goldAmount} gold");
        }
        
        return result;
    }
    
    private LootResult RollFromMainTable(int playerLevel, float playerLuck, List<string> playerTags)
    {
        var result = new LootResult();
        var availableEntries = possibleLoot.Where(entry => 
            entry.CanDropForPlayer(playerLevel, playerLuck, playerTags)).ToList();
        
        int itemsToGenerate = Random.Range(minItems, maxItems + 1);
        var alreadyDropped = new HashSet<string>();
        
        for (int i = 0; i < itemsToGenerate && availableEntries.Count > 0; i++)
        {
            var validEntries = availableEntries.Where(entry => 
                allowDuplicates || !alreadyDropped.Contains(entry.item.name)).ToList();
            
            if (validEntries.Count == 0) break;
            
            var selectedEntry = SelectWeightedRandom(validEntries, playerLevel, playerLuck);
            if (selectedEntry != null)
            {
                var droppedItem = CreateDroppedItem(selectedEntry, playerLevel);
                if (droppedItem != null)
                {
                    result.items.Add(droppedItem);
                    alreadyDropped.Add(selectedEntry.item.name);
                    selectedEntry.hasDropped = true;
                    
                    // Remover unique items da lista
                    if (selectedEntry.isUnique)
                    {
                        availableEntries.Remove(selectedEntry);
                    }
                }
            }
        }
        
        return result;
    }
    
    private LootEntry SelectWeightedRandom(List<LootEntry> entries, int playerLevel, float playerLuck)
    {
        if (entries.Count == 0) return null;
        
        // Calcular pesos totais
        float totalWeight = 0f;
        var weights = new List<float>();
        
        foreach (var entry in entries)
        {
            float weight = entry.GetActualDropChance(playerLevel, playerLuck * globalLuckMultiplier);
            weights.Add(weight);
            totalWeight += weight;
        }
        
        if (totalWeight <= 0f) return null;
        
        // Selecionar baseado no peso
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        for (int i = 0; i < entries.Count; i++)
        {
            currentWeight += weights[i];
            if (randomValue <= currentWeight)
            {
                return entries[i];
            }
        }
        
        return entries[entries.Count - 1]; // Fallback
    }
    
    private Item CreateDroppedItem(LootEntry entry, int playerLevel)
    {
        if (entry.item == null) return null;
        
        var droppedItem = entry.item.Clone();
        
        // Determinar quantidade
        int quantity = Random.Range(entry.minQuantity, entry.maxQuantity + 1);
        
        if (droppedItem.IsStackable)
        {
            if (entry.respectStackLimits)
            {
                quantity = Mathf.Min(quantity, droppedItem.stackSize);
            }
            droppedItem.currentStack = quantity;
        }
        
        // Aplicar variações baseadas no nível
        ApplyLevelScaling(droppedItem, playerLevel);
        
        return droppedItem;
    }
    
    private void ApplyLevelScaling(Item item, int playerLevel)
    {
        if (item == null) return;
        
        // Ajustar level do item baseado no player
        int levelVariation = Random.Range(-2, 3); // -2 a +2
        item.level = Mathf.Max(1, playerLevel + levelVariation);
        
        // Scaling de stats baseado no nível
        float levelMultiplier = 1f + (item.level - 1) * 0.1f;
        
        if (item.type == ItemType.Weapon)
        {
            item.physicalDamage = Mathf.RoundToInt(item.physicalDamage * levelMultiplier);
            item.fireDamage = Mathf.RoundToInt(item.fireDamage * levelMultiplier);
            item.iceDamage = Mathf.RoundToInt(item.iceDamage * levelMultiplier);
            item.lightningDamage = Mathf.RoundToInt(item.lightningDamage * levelMultiplier);
            item.poisonDamage = Mathf.RoundToInt(item.poisonDamage * levelMultiplier);
        }
        
        if (item.IsEquipment)
        {
            item.strengthModifier = Mathf.RoundToInt(item.strengthModifier * levelMultiplier);
            item.intelligenceModifier = Mathf.RoundToInt(item.intelligenceModifier * levelMultiplier);
            item.dexterityModifier = Mathf.RoundToInt(item.dexterityModifier * levelMultiplier);
            item.vitalityModifier = Mathf.RoundToInt(item.vitalityModifier * levelMultiplier);
            item.physicalDefense = Mathf.RoundToInt(item.physicalDefense * levelMultiplier);
        }
    }
    
    private int CalculateGoldDrop(int playerLevel, float playerLuck)
    {
        float baseGold = Random.Range(goldMin, goldMax);
        
        // Aplicar scaling de nível
        baseGold *= goldScaling.Evaluate(playerLevel);
        
        // Aplicar luck multiplier
        baseGold *= (1f + playerLuck * globalLuckMultiplier);
        
        // Variação aleatória ±20%
        float variation = Random.Range(0.8f, 1.2f);
        baseGold *= variation;
        
        return Mathf.RoundToInt(Mathf.Max(0, baseGold));
    }
    
    #endregion
    
    #region Helper Methods
    
    private Item CreateConsumableItem(string name, string description, ItemRarity rarity, int level, int healthRestore, int manaRestore)
    {
        var item = new Item(name, description, ItemType.Consumable, rarity, level, 10);
        item.healthRestore = healthRestore;
        item.manaRestore = manaRestore;
        return item;
    }
    
    private void AddLootEntry(Item item, float dropChance, int minQty = 1, int maxQty = 1)
    {
        var entry = new LootEntry
        {
            item = item,
            dropChance = dropChance,
            minQuantity = minQty,
            maxQuantity = maxQty
        };
        
        possibleLoot.Add(entry);
    }
    
    #endregion
    
    #region Statistics & Debug
    
    private void UpdateDropStatistics(LootResult result)
    {
        if (!showDropStatistics) return;
        
        foreach (var item in result.items)
        {
            string key = $"{item.rarity} {item.type}";
            if (dropStatistics.ContainsKey(key))
            {
                dropStatistics[key]++;
            }
            else
            {
                dropStatistics[key] = 1;
            }
        }
    }
    
    public void PrintDropStatistics()
    {
        if (dropStatistics.Count == 0)
        {
            Debug.Log("Nenhuma estatística de drop disponível");
            return;
        }
        
        Debug.Log("=== ESTATÍSTICAS DE DROP ===");
        foreach (var kvp in dropStatistics.OrderByDescending(x => x.Value))
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} drops");
        }
    }
    
    public void ResetStatistics()
    {
        dropStatistics.Clear();
    }
    
    [ContextMenu("Validate Loot Table")]
    public void ValidateLootTable()
    {
        int validEntries = 0;
        int invalidEntries = 0;
        
        foreach (var entry in possibleLoot)
        {
            if (entry.item == null)
            {
                Debug.LogError($"Entrada com item nulo encontrada!");
                invalidEntries++;
                continue;
            }
            
            if (!entry.item.IsValid())
            {
                Debug.LogError($"Item inválido: {entry.item.name}");
                invalidEntries++;
                continue;
            }
            
            if (entry.dropChance <= 0)
            {
                Debug.LogWarning($"Item {entry.item.name} tem chance de drop 0 ou negativa");
            }
            
            validEntries++;
        }
        
        Debug.Log($"Validação completa: {validEntries} válidas, {invalidEntries} inválidas");
    }
    
    #endregion
}

/// <summary>
/// Resultado da geração de loot
/// </summary>
[System.Serializable]
public class LootResult
{
    public List<Item> items = new List<Item>();
    public int goldAmount = 0;
    public float totalValue = 0f;
    public Dictionary<ItemRarity, int> rarityBreakdown = new Dictionary<ItemRarity, int>();
    
    public void CalculateTotalValue()
    {
        totalValue = 0f;
        rarityBreakdown.Clear();
        
        foreach (var item in items)
        {
            totalValue += item.GetCurrentValue() * item.currentStack;
            
            if (rarityBreakdown.ContainsKey(item.rarity))
            {
                rarityBreakdown[item.rarity]++;
            }
            else
            {
                rarityBreakdown[item.rarity] = 1;
            }
        }
        
        totalValue += goldAmount;
    }
    
    public bool HasItems => items.Count > 0;
    public bool HasGold => goldAmount > 0;
    public bool IsEmpty => !HasItems && !HasGold;
}