using UnityEngine;
using System;
using System.Collections.Generic;

public enum ItemType
{
    Weapon,
    Helmet,
    Chest,
    Gloves,
    Boots,
    Consumable,
    Material,    // NOVO: Materiais de crafting
    Quest,       // NOVO: Itens de quest
    Jewelry      // NOVO: Anéis, amuletos
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Artifact     // NOVO: Raridade especial
}

public enum ItemQuality
{
    Poor,
    Normal,
    Good,
    Excellent,
    Perfect
}

[Serializable]
public class Item : IComparable<Item>, IEquatable<Item>
{
    [Header("Basic Info")]
    public string id = "";              // NOVO: ID único para serialização
    public string name;
    public string description;
    public ItemType type;
    public ItemRarity rarity = ItemRarity.Common;
    public ItemQuality quality = ItemQuality.Normal; // NOVO: Qualidade do item
    public int level = 1;
    public int stackSize = 1;           // NOVO: Quantidade máxima em stack
    public int currentStack = 1;        // NOVO: Quantidade atual
    
    [Header("Value & Trade")]
    public int baseValue = 10;          // NOVO: Valor base fixo
    public bool isTradeable = true;     // NOVO: Item pode ser comercializado
    public bool isDroppable = true;     // NOVO: Item pode ser dropado
    public bool isDestroyable = true;   // NOVO: Item pode ser destruído
    
    [Header("Requirements")]
    public int levelRequirement = 1;    // NOVO: Nível mínimo para usar
    public int strengthRequirement = 0; // NOVO: Força mínima
    public int intelligenceRequirement = 0;
    public int dexterityRequirement = 0;
    public int vitalityRequirement = 0;
    
    [Header("Attribute Modifiers")]
    public int strengthModifier;
    public int intelligenceModifier;
    public int dexterityModifier;
    public int vitalityModifier;
    
    [Header("Damage (Weapons)")]
    public int physicalDamage;
    public int fireDamage;
    public int iceDamage;
    public int lightningDamage;
    public int poisonDamage;
    public float criticalChanceBonus = 0f; // NOVO: Bônus de chance crítica
    public float criticalDamageBonus = 0f; // NOVO: Bônus de dano crítico
    
    [Header("Defense (Armor)")]
    public int physicalDefense = 0;     // NOVO: Defesa física
    public int fireResistance = 0;      // NOVO: Resistências elementais
    public int iceResistance = 0;
    public int lightningResistance = 0;
    public int poisonResistance = 0;
    
    [Header("Consumable Effects")]
    public int healthRestore;
    public int manaRestore;
    public float buffDuration = 0f;     // NOVO: Duração de buffs temporários
    public List<ItemEffect> effects = new List<ItemEffect>(); // NOVO: Efeitos customizados
    
    [Header("Visual & Audio")]
    public string prefabPath;
    public string iconPath;
    public string iconSpriteName = "";  // NOVO: Nome do sprite no atlas
    public Color itemColor = Color.white; // NOVO: Cor customizada
    public string useSound = "";        // NOVO: Som ao usar
    
    [Header("Crafting & Materials")]
    public bool isCraftingMaterial = false;  // NOVO: É material de crafting
    public List<string> craftingTags = new List<string>(); // NOVO: Tags para receitas
    
    // NOVO: Sistema de efeitos de item
    [System.Serializable]
    public class ItemEffect
    {
        public string effectType;
        public float value;
        public float duration;
        public bool isPermanent;
    }
    
    #region Constructors
    
    public Item()
    {
        GenerateUniqueId();
    }
    
    public Item(string name, string description, ItemType type, ItemRarity rarity, int level)
    {
        GenerateUniqueId();
        this.name = name;
        this.description = description;
        this.type = type;
        this.rarity = rarity;
        this.level = level;
        this.baseValue = CalculateBaseValue();
    }
    
    // NOVO: Construtor para itens stackables
    public Item(string name, string description, ItemType type, ItemRarity rarity, int level, int stackSize) 
        : this(name, description, type, rarity, level)
    {
        this.stackSize = stackSize;
        this.currentStack = 1;
    }
    
    #endregion
    
    #region Properties & Calculations
    
    public bool IsStackable => stackSize > 1;
    public bool IsFullStack => currentStack >= stackSize;
    public bool IsEquipment => type != ItemType.Consumable && type != ItemType.Material && type != ItemType.Quest;
    public bool IsConsumable => type == ItemType.Consumable;
    public bool CanStack(Item other) => other != null && other.id == this.id && !IsFullStack;
    public int AvailableStackSpace => stackSize - currentStack;
    
    // NOVO: Cálculo dinâmico de valor
    public int GetCurrentValue()
    {
        float qualityMultiplier = GetQualityMultiplier();
        float rarityMultiplier = GetRarityMultiplier();
        float levelMultiplier = 1f + (level * 0.1f);
        
        int finalValue = Mathf.RoundToInt(baseValue * qualityMultiplier * rarityMultiplier * levelMultiplier);
        return Mathf.Max(1, finalValue); // Valor mínimo de 1
    }
    
    private float GetQualityMultiplier()
    {
        switch (quality)
        {
            case ItemQuality.Poor: return 0.5f;
            case ItemQuality.Normal: return 1f;
            case ItemQuality.Good: return 1.5f;
            case ItemQuality.Excellent: return 2f;
            case ItemQuality.Perfect: return 3f;
            default: return 1f;
        }
    }
    
    private float GetRarityMultiplier()
    {
        switch (rarity)
        {
            case ItemRarity.Common: return 1f;
            case ItemRarity.Uncommon: return 2f;
            case ItemRarity.Rare: return 4f;
            case ItemRarity.Epic: return 8f;
            case ItemRarity.Legendary: return 16f;
            case ItemRarity.Artifact: return 32f;
            default: return 1f;
        }
    }
    
    private int CalculateBaseValue()
    {
        int value = 10 + (level * 5);
        
        // Ajustar baseado no tipo
        switch (type)
        {
            case ItemType.Weapon: value *= 2; break;
            case ItemType.Chest: value = Mathf.RoundToInt(value * 1.5f); break;
            case ItemType.Consumable: value = Mathf.RoundToInt(value * 0.5f); break;
        }
        
        return value;
    }
    
    #endregion
    
    #region Requirements & Validation
    
    public bool CanPlayerUse(PlayerStats playerStats)
    {
        if (playerStats == null) return false;
        
        return playerStats.Level >= levelRequirement &&
               playerStats.Strength >= strengthRequirement &&
               playerStats.Intelligence >= intelligenceRequirement &&
               playerStats.Dexterity >= dexterityRequirement &&
               playerStats.Vitality >= vitalityRequirement;
    }
    
    public List<string> GetMissingRequirements(PlayerStats playerStats)
    {
        var missing = new List<string>();
        
        if (playerStats == null)
        {
            missing.Add("Player stats unavailable");
            return missing;
        }
        
        if (playerStats.Level < levelRequirement)
            missing.Add($"Level {levelRequirement} required (current: {playerStats.Level})");
        if (playerStats.Strength < strengthRequirement)
            missing.Add($"Strength {strengthRequirement} required (current: {playerStats.Strength})");
        if (playerStats.Intelligence < intelligenceRequirement)
            missing.Add($"Intelligence {intelligenceRequirement} required (current: {playerStats.Intelligence})");
        if (playerStats.Dexterity < dexterityRequirement)
            missing.Add($"Dexterity {dexterityRequirement} required (current: {playerStats.Dexterity})");
        if (playerStats.Vitality < vitalityRequirement)
            missing.Add($"Vitality {vitalityRequirement} required (current: {playerStats.Vitality})");
        
        return missing;
    }
    
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(name) && 
               !string.IsNullOrEmpty(id) && 
               level >= 1 && 
               stackSize >= 1 && 
               currentStack >= 1 && 
               currentStack <= stackSize &&
               baseValue >= 0;
    }
    
    #endregion
    
    #region Stack Management
    
    public bool TryStack(Item other, out int leftOver)
    {
        leftOver = 0;
        
        if (!CanStack(other))
            return false;
        
        int spaceAvailable = AvailableStackSpace;
        int amountToAdd = Mathf.Min(other.currentStack, spaceAvailable);
        
        currentStack += amountToAdd;
        other.currentStack -= amountToAdd;
        leftOver = other.currentStack;
        
        return amountToAdd > 0;
    }
    
    public Item Split(int amount)
    {
        if (!IsStackable || amount <= 0 || amount >= currentStack)
            return null;
        
        Item newItem = Clone();
        newItem.currentStack = amount;
        this.currentStack -= amount;
        
        return newItem;
    }
    
    #endregion
    
    #region Usage & Effects
    
    public void Use(PlayerController player)
    {
        if (player == null)
        {
            Debug.LogWarning("Item.Use: PlayerController é nulo!");
            return;
        }
        
        // Verificar requirements
        var playerStats = player.GetStats();
        if (!CanPlayerUse(playerStats))
        {
            var missing = GetMissingRequirements(playerStats);
            EventManager.TriggerEvent(new NotificationEvent
            {
                message = $"Não é possível usar {name}: {string.Join(", ", missing)}",
                type = NotificationType.Warning,
                duration = 3f,
                color = Color.yellow
            });
            return;
        }
        
        if (IsConsumable)
        {
            UseConsumable(player);
        }
        else if (IsEquipment)
        {
            var inventoryManager = player.GetInventoryManager();
            if (inventoryManager != null)
            {
                inventoryManager.EquipItem(this);
            }
        }
        
        // Tocar som se especificado
        if (!string.IsNullOrEmpty(useSound))
        {
            AudioSource.PlayClipAtPoint(Resources.Load<AudioClip>(useSound), player.transform.position);
        }
    }
    
    private void UseConsumable(PlayerController player)
    {
        var healthManager = player.GetHealthManager();
        var inventoryManager = player.GetInventoryManager();
        
        if (healthManager == null || inventoryManager == null) return;
        
        int oldHealth = healthManager.CurrentHealth;
        int oldMana = healthManager.CurrentMana;
        
        // Aplicar efeitos básicos
        if (healthRestore > 0)
        {
            healthManager.Heal(healthRestore);
        }
        
        if (manaRestore > 0)
        {
            healthManager.RestoreMana(manaRestore);
        }
        
        // Aplicar efeitos customizados
        ApplyCustomEffects(player);
        
        // Criar popups
        CreateUsagePopups(player, oldHealth, oldMana, healthManager);
        
        // Reduzir stack ou remover item
        currentStack--;
        if (currentStack <= 0)
        {
            inventoryManager.RemoveItem(this);
        }
        
        Debug.Log($"Item consumido: {name} (restante: {currentStack})");
    }
    
    private void ApplyCustomEffects(PlayerController player)
    {
        foreach (var effect in effects)
        {
            switch (effect.effectType.ToLower())
            {
                case "speed":
                    // Implementar buff de velocidade
                    break;
                case "strength":
                    // Implementar buff temporário de força
                    break;
                case "damage":
                    // Implementar buff de dano
                    break;
            }
        }
    }
    
    private void CreateUsagePopups(PlayerController player, int oldHealth, int oldMana, PlayerHealthManager healthManager)
    {
        int actualHealthRestored = healthManager.CurrentHealth - oldHealth;
        int actualManaRestored = healthManager.CurrentMana - oldMana;
        
        if (actualHealthRestored > 0)
        {
            EventManager.TriggerEvent(new DamagePopupRequestEvent
            {
                worldPosition = player.transform.position,
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
                worldPosition = player.transform.position + Vector3.up * 0.5f,
                amount = actualManaRestored,
                isCritical = false,
                isHeal = false,
                customColor = Color.cyan
            });
        }
    }
    
    #endregion
    
    #region Visual & UI
    
    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case ItemRarity.Common: return Color.white;
            case ItemRarity.Uncommon: return Color.green;
            case ItemRarity.Rare: return Color.blue;
            case ItemRarity.Epic: return new Color(0.5f, 0f, 0.5f); // Roxo
            case ItemRarity.Legendary: return new Color(1f, 0.5f, 0f); // Laranja
            case ItemRarity.Artifact: return new Color(1f, 0f, 0f); // Vermelho
            default: return Color.white;
        }
    }
    
    public string GetRarityText()
    {
        Color color = GetRarityColor();
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        
        switch (rarity)
        {
            case ItemRarity.Common: return $"<color=#{colorHex}>Comum</color>";
            case ItemRarity.Uncommon: return $"<color=#{colorHex}>Incomum</color>";
            case ItemRarity.Rare: return $"<color=#{colorHex}>Raro</color>";
            case ItemRarity.Epic: return $"<color=#{colorHex}>Épico</color>";
            case ItemRarity.Legendary: return $"<color=#{colorHex}>Lendário</color>";
            case ItemRarity.Artifact: return $"<color=#{colorHex}>Artefato</color>";
            default: return $"<color=#{colorHex}>Comum</color>";
        }
    }
    
    public string GetQualityText()
    {
        switch (quality)
        {
            case ItemQuality.Poor: return "<color=gray>Pobre</color>";
            case ItemQuality.Normal: return "<color=white>Normal</color>";
            case ItemQuality.Good: return "<color=green>Bom</color>";
            case ItemQuality.Excellent: return "<color=blue>Excelente</color>";
            case ItemQuality.Perfect: return "<color=gold>Perfeito</color>";
            default: return "<color=white>Normal</color>";
        }
    }
    
    public string GetDetailedDescription(PlayerStats playerStats = null)
    {
        var desc = new System.Text.StringBuilder();
        
        desc.AppendLine($"<b><size=18>{name}</size></b>");
        desc.AppendLine($"{GetRarityText()} | {GetQualityText()}");
        desc.AppendLine($"Nível: {level}");
        
        if (IsStackable && stackSize > 1)
        {
            desc.AppendLine($"Quantidade: {currentStack}/{stackSize}");
        }
        
        desc.AppendLine();
        desc.AppendLine(description);
        
        // Requirements
        if (HasRequirements())
        {
            desc.AppendLine();
            desc.AppendLine("<color=red>Requisitos:</color>");
            if (levelRequirement > 1) desc.AppendLine($"Nível: {levelRequirement}");
            if (strengthRequirement > 0) desc.AppendLine($"Força: {strengthRequirement}");
            if (intelligenceRequirement > 0) desc.AppendLine($"Inteligência: {intelligenceRequirement}");
            if (dexterityRequirement > 0) desc.AppendLine($"Destreza: {dexterityRequirement}");
            if (vitalityRequirement > 0) desc.AppendLine($"Vitalidade: {vitalityRequirement}");
            
            // Show if player meets requirements
            if (playerStats != null)
            {
                var missing = GetMissingRequirements(playerStats);
                if (missing.Count > 0)
                {
                    desc.AppendLine("<color=red>Requisitos não atendidos!</color>");
                }
                else
                {
                    desc.AppendLine("<color=green>Requisitos atendidos</color>");
                }
            }
        }
        
        // Equipment stats
        if (IsEquipment)
        {
            AppendEquipmentStats(desc);
        }
        
        // Consumable effects
        if (IsConsumable)
        {
            AppendConsumableEffects(desc);
        }
        
        // Value
        desc.AppendLine();
        desc.AppendLine($"<color=yellow>Valor: {GetCurrentValue()} moedas</color>");
        
        // Additional info
        if (!isTradeable) desc.AppendLine("<color=red>Não comercializável</color>");
        if (!isDroppable) desc.AppendLine("<color=red>Não pode ser dropado</color>");
        
        return desc.ToString();
    }
    
    private void AppendEquipmentStats(System.Text.StringBuilder desc)
    {
        desc.AppendLine();
        
        if (HasAttributeModifiers())
        {
            desc.AppendLine("<color=yellow>Atributos:</color>");
            if (strengthModifier != 0) desc.AppendLine($"Força: {(strengthModifier > 0 ? "+" : "")}{strengthModifier}");
            if (intelligenceModifier != 0) desc.AppendLine($"Inteligência: {(intelligenceModifier > 0 ? "+" : "")}{intelligenceModifier}");
            if (dexterityModifier != 0) desc.AppendLine($"Destreza: {(dexterityModifier > 0 ? "+" : "")}{dexterityModifier}");
            if (vitalityModifier != 0) desc.AppendLine($"Vitalidade: {(vitalityModifier > 0 ? "+" : "")}{vitalityModifier}");
        }
        
        if (type == ItemType.Weapon && HasDamage())
        {
            desc.AppendLine();
            desc.AppendLine("<color=red>Dano:</color>");
            if (physicalDamage > 0) desc.AppendLine($"Físico: {physicalDamage}");
            if (fireDamage > 0) desc.AppendLine($"Fogo: {fireDamage}");
            if (iceDamage > 0) desc.AppendLine($"Gelo: {iceDamage}");
            if (lightningDamage > 0) desc.AppendLine($"Raio: {lightningDamage}");
            if (poisonDamage > 0) desc.AppendLine($"Veneno: {poisonDamage}");
            
            if (criticalChanceBonus > 0) desc.AppendLine($"Chance Crítica: +{criticalChanceBonus:P}");
            if (criticalDamageBonus > 0) desc.AppendLine($"Dano Crítico: +{criticalDamageBonus:P}");
        }
        
        if (IsArmor() && HasDefense())
        {
            desc.AppendLine();
            desc.AppendLine("<color=blue>Defesa:</color>");
            if (physicalDefense > 0) desc.AppendLine($"Defesa Física: {physicalDefense}");
            if (fireResistance > 0) desc.AppendLine($"Resistência ao Fogo: {fireResistance}");
            if (iceResistance > 0) desc.AppendLine($"Resistência ao Gelo: {iceResistance}");
            if (lightningResistance > 0) desc.AppendLine($"Resistência ao Raio: {lightningResistance}");
            if (poisonResistance > 0) desc.AppendLine($"Resistência ao Veneno: {poisonResistance}");
        }
    }
    
    private void AppendConsumableEffects(System.Text.StringBuilder desc)
    {
        desc.AppendLine();
        desc.AppendLine("<color=green>Efeitos:</color>");
        
        if (healthRestore > 0) desc.AppendLine($"Restaura {healthRestore} de vida");
        if (manaRestore > 0) desc.AppendLine($"Restaura {manaRestore} de mana");
        
        foreach (var effect in effects)
        {
            string duration = effect.isPermanent ? "permanente" : $"{effect.duration}s";
            desc.AppendLine($"{effect.effectType}: +{effect.value} ({duration})");
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    private bool HasRequirements()
    {
        return levelRequirement > 1 || strengthRequirement > 0 || intelligenceRequirement > 0 || 
               dexterityRequirement > 0 || vitalityRequirement > 0;
    }
    
    private bool HasAttributeModifiers()
    {
        return strengthModifier != 0 || intelligenceModifier != 0 || 
               dexterityModifier != 0 || vitalityModifier != 0;
    }
    
    private bool HasDamage()
    {
        return physicalDamage > 0 || fireDamage > 0 || iceDamage > 0 || 
               lightningDamage > 0 || poisonDamage > 0;
    }
    
    private bool HasDefense()
    {
        return physicalDefense > 0 || fireResistance > 0 || iceResistance > 0 || 
               lightningResistance > 0 || poisonResistance > 0;
    }
    
    private bool IsArmor()
    {
        return type == ItemType.Helmet || type == ItemType.Chest || 
               type == ItemType.Gloves || type == ItemType.Boots;
    }
    
    private void GenerateUniqueId()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
        }
    }
    
    #endregion
    
    #region Comparison & Utility
    
    public bool IsBetterThan(Item other)
    {
        if (other == null || type != other.type)
            return true;
        
        // Comparar por raridade primeiro
        if (rarity != other.rarity)
            return rarity > other.rarity;
        
        // Depois por qualidade
        if (quality != other.quality)
            return quality > other.quality;
        
        // Depois por nível
        if (level != other.level)
            return level > other.level;
        
        // Finalmente por stats totais
        return GetTotalStats() > other.GetTotalStats();
    }
    
    private int GetTotalStats()
    {
        return strengthModifier + intelligenceModifier + dexterityModifier + vitalityModifier +
               physicalDamage + fireDamage + iceDamage + lightningDamage + poisonDamage +
               physicalDefense + fireResistance + iceResistance + lightningResistance + poisonResistance +
               healthRestore + manaRestore;
    }
    
    public Item Clone()
    {
        var clone = new Item
        {
            id = System.Guid.NewGuid().ToString(), // Novo ID único
            name = this.name,
            description = this.description,
            type = this.type,
            rarity = this.rarity,
            quality = this.quality,
            level = this.level,
            stackSize = this.stackSize,
            currentStack = this.currentStack,
            baseValue = this.baseValue,
            isTradeable = this.isTradeable,
            isDroppable = this.isDroppable,
            isDestroyable = this.isDestroyable,
            levelRequirement = this.levelRequirement,
            strengthRequirement = this.strengthRequirement,
            intelligenceRequirement = this.intelligenceRequirement,
            dexterityRequirement = this.dexterityRequirement,
            vitalityRequirement = this.vitalityRequirement,
            strengthModifier = this.strengthModifier,
            intelligenceModifier = this.intelligenceModifier,
            dexterityModifier = this.dexterityModifier,
            vitalityModifier = this.vitalityModifier,
            physicalDamage = this.physicalDamage,
            fireDamage = this.fireDamage,
            iceDamage = this.iceDamage,
            lightningDamage = this.lightningDamage,
            poisonDamage = this.poisonDamage,
            criticalChanceBonus = this.criticalChanceBonus,
            criticalDamageBonus = this.criticalDamageBonus,
            physicalDefense = this.physicalDefense,
            fireResistance = this.fireResistance,
            iceResistance = this.iceResistance,
            lightningResistance = this.lightningResistance,
            poisonResistance = this.poisonResistance,
            healthRestore = this.healthRestore,
            manaRestore = this.manaRestore,
            buffDuration = this.buffDuration,
            prefabPath = this.prefabPath,
            iconPath = this.iconPath,
            iconSpriteName = this.iconSpriteName,
            itemColor = this.itemColor,
            useSound = this.useSound,
            isCraftingMaterial = this.isCraftingMaterial,
            effects = new List<ItemEffect>(this.effects),
            craftingTags = new List<string>(this.craftingTags)
        };
        
        return clone;
    }
    
    #endregion
    
    #region IComparable & IEquatable
    
    public int CompareTo(Item other)
    {
        if (other == null) return 1;
        
        // Comparar por raridade primeiro
        int rarityComparison = rarity.CompareTo(other.rarity);
        if (rarityComparison != 0) return rarityComparison;
        
        // Depois por nível
        int levelComparison = level.CompareTo(other.level);
        if (levelComparison != 0) return levelComparison;
        
        // Finalmente por nome
        return string.Compare(name, other.name, StringComparison.OrdinalIgnoreCase);
    }
    
    public bool Equals(Item other)
    {
        if (other == null) return false;
        return id == other.id;
    }
    
    public override bool Equals(object obj)
    {
        return Equals(obj as Item);
    }
    
    public override int GetHashCode()
    {
        return id?.GetHashCode() ?? 0;
    }
    
    public override string ToString()
    {
        string stackInfo = IsStackable ? $" ({currentStack}/{stackSize})" : "";
        return $"{name} ({rarity} {type}, Nível {level}){stackInfo}";
    }
    
    #endregion
}