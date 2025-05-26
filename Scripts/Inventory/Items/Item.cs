using UnityEngine;
using System;

public enum ItemType
{
    Weapon,
    Helmet,
    Chest,
    Gloves,
    Boots,
    Consumable
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[Serializable]
public class Item
{
    public string name;
    public string description;
    public ItemType type;
    public ItemRarity rarity;
    public int level;
    
    // Modificadores de atributos
    public int strengthModifier;
    public int intelligenceModifier;
    public int dexterityModifier;
    public int vitalityModifier;
    
    // Modificadores de dano (para armas)
    public int physicalDamage;
    public int fireDamage;
    public int iceDamage;
    public int lightningDamage;
    public int poisonDamage;
    
    // Para itens consumíveis
    public int healthRestore;
    public int manaRestore;
    
    // Visual (será referenciado pelo prefab)
    public string prefabPath;
    public string iconPath;
    
    public Item(string name, string description, ItemType type, ItemRarity rarity, int level)
    {
        this.name = name;
        this.description = description;
        this.type = type;
        this.rarity = rarity;
        this.level = level;
    }
    
    public void Use(PlayerController player)
    {
        if (player == null)
        {
            Debug.LogWarning("Item.Use: PlayerController é nulo!");
            return;
        }
        
        if (type == ItemType.Consumable)
        {
            UseConsumable(player);
        }
        else
        {
            // Equipar item - agora usando o manager específico
            var inventoryManager = player.GetInventoryManager();
            if (inventoryManager != null)
            {
                inventoryManager.EquipItem(this);
            }
        }
    }
    
    private void UseConsumable(PlayerController player)
    {
        var healthManager = player.GetHealthManager();
        if (healthManager == null)
        {
            Debug.LogWarning("Item.UseConsumable: PlayerHealthManager não encontrado!");
            return;
        }
        
        var inventoryManager = player.GetInventoryManager();
        if (inventoryManager == null)
        {
            Debug.LogWarning("Item.UseConsumable: PlayerInventoryManager não encontrado!");
            return;
        }
        
        int oldHealth = healthManager.CurrentHealth;
        int oldMana = healthManager.CurrentMana;
        
        // Aplicar efeitos do consumível
        if (healthRestore > 0)
        {
            healthManager.Heal(healthRestore);
        }
        
        if (manaRestore > 0)
        {
            healthManager.RestoreMana(manaRestore);
        }
        
        // Calcular quanto foi realmente restaurado
        int actualHealthRestored = healthManager.CurrentHealth - oldHealth;
        int actualManaRestored = healthManager.CurrentMana - oldMana;
        
        // Criar popups para o que foi restaurado
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
            Debug.Log($"Item de cura usado: +{actualHealthRestored} HP");
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
            Debug.Log($"Item de mana usado: +{actualManaRestored} MP");
        }
        
        // Remover do inventário após uso
        inventoryManager.RemoveItem(this);
        
        Debug.Log($"Item consumido: {name}");
    }
    
    /// <summary>
    /// Calcula o valor monetário do item
    /// </summary>
    public int GetValue()
    {
        int baseValue = level * 10;
        
        // Multiplicador baseado na raridade
        float rarityMultiplier = GetRarityMultiplier();
        
        // Multiplicador baseado no tipo
        float typeMultiplier = GetTypeMultiplier();
        
        // Multiplicador baseado nos stats
        float statsMultiplier = GetStatsMultiplier();
        
        return Mathf.RoundToInt(baseValue * rarityMultiplier * typeMultiplier * statsMultiplier);
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
            default: return 1f;
        }
    }
    
    private float GetTypeMultiplier()
    {
        switch (type)
        {
            case ItemType.Weapon: return 1.5f;
            case ItemType.Chest: return 1.3f;
            case ItemType.Helmet:
            case ItemType.Gloves:
            case ItemType.Boots: return 1.1f;
            case ItemType.Consumable: return 0.5f;
            default: return 1f;
        }
    }
    
    private float GetStatsMultiplier()
    {
        float multiplier = 1f;
        
        // Modificadores de atributos
        multiplier += (strengthModifier + intelligenceModifier + dexterityModifier + vitalityModifier) * 0.1f;
        
        // Dano físico e elemental
        multiplier += (physicalDamage + fireDamage + iceDamage + lightningDamage + poisonDamage) * 0.05f;
        
        // Efeitos de consumível
        multiplier += (healthRestore + manaRestore) * 0.02f;
        
        return Mathf.Max(multiplier, 1f);
    }
    
    /// <summary>
    /// Retorna a cor associada à raridade do item
    /// </summary>
    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case ItemRarity.Common: return Color.white;
            case ItemRarity.Uncommon: return Color.green;
            case ItemRarity.Rare: return Color.blue;
            case ItemRarity.Epic: return new Color(0.5f, 0f, 0.5f); // Roxo
            case ItemRarity.Legendary: return new Color(1f, 0.5f, 0f); // Laranja
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// Retorna o texto formatado da raridade
    /// </summary>
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
            default: return $"<color=#{colorHex}>Comum</color>";
        }
    }
    
    /// <summary>
    /// Gera uma descrição detalhada do item para tooltips
    /// </summary>
    public string GetDetailedDescription()
    {
        var desc = new System.Text.StringBuilder();
        
        desc.AppendLine($"<b>{name}</b>");
        desc.AppendLine(GetRarityText());
        desc.AppendLine($"Nível: {level}");
        desc.AppendLine();
        desc.AppendLine(description);
        
        if (type != ItemType.Consumable)
        {
            desc.AppendLine();
            desc.AppendLine("<color=yellow>Atributos:</color>");
            
            if (strengthModifier > 0) desc.AppendLine($"Força: +{strengthModifier}");
            if (intelligenceModifier > 0) desc.AppendLine($"Inteligência: +{intelligenceModifier}");
            if (dexterityModifier > 0) desc.AppendLine($"Destreza: +{dexterityModifier}");
            if (vitalityModifier > 0) desc.AppendLine($"Vitalidade: +{vitalityModifier}");
        }
        
        if (type == ItemType.Weapon)
        {
            desc.AppendLine();
            desc.AppendLine("<color=red>Dano:</color>");
            
            if (physicalDamage > 0) desc.AppendLine($"Físico: {physicalDamage}");
            if (fireDamage > 0) desc.AppendLine($"Fogo: {fireDamage}");
            if (iceDamage > 0) desc.AppendLine($"Gelo: {iceDamage}");
            if (lightningDamage > 0) desc.AppendLine($"Raio: {lightningDamage}");
            if (poisonDamage > 0) desc.AppendLine($"Veneno: {poisonDamage}");
        }
        
        if (type == ItemType.Consumable)
        {
            desc.AppendLine();
            desc.AppendLine("<color=green>Efeitos:</color>");
            
            if (healthRestore > 0) desc.AppendLine($"Restaura {healthRestore} de vida");
            if (manaRestore > 0) desc.AppendLine($"Restaura {manaRestore} de mana");
        }
        
        desc.AppendLine();
        desc.AppendLine($"<color=yellow>Valor: {GetValue()} moedas</color>");
        
        return desc.ToString();
    }
    
    /// <summary>
    /// Verifica se este item é melhor que outro do mesmo tipo
    /// </summary>
    public bool IsBetterThan(Item other)
    {
        if (other == null || type != other.type)
            return true;
        
        // Comparar por raridade primeiro
        if (rarity != other.rarity)
            return rarity > other.rarity;
        
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
               healthRestore + manaRestore;
    }
    
    /// <summary>
    /// Cria uma cópia do item
    /// </summary>
    public Item Clone()
    {
        var clone = new Item(name, description, type, rarity, level)
        {
            strengthModifier = this.strengthModifier,
            intelligenceModifier = this.intelligenceModifier,
            dexterityModifier = this.dexterityModifier,
            vitalityModifier = this.vitalityModifier,
            physicalDamage = this.physicalDamage,
            fireDamage = this.fireDamage,
            iceDamage = this.iceDamage,
            lightningDamage = this.lightningDamage,
            poisonDamage = this.poisonDamage,
            healthRestore = this.healthRestore,
            manaRestore = this.manaRestore,
            prefabPath = this.prefabPath,
            iconPath = this.iconPath
        };
        
        return clone;
    }
    
    /// <summary>
    /// Valida se o item está configurado corretamente
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(name))
            return false;
        
        if (level < 1)
            return false;
        
        if (type == ItemType.Consumable)
        {
            return healthRestore > 0 || manaRestore > 0;
        }
        
        // Para equipamentos, deve ter pelo menos um modificador positivo
        return strengthModifier > 0 || intelligenceModifier > 0 || 
               dexterityModifier > 0 || vitalityModifier > 0 ||
               physicalDamage > 0 || fireDamage > 0 || 
               iceDamage > 0 || lightningDamage > 0 || poisonDamage > 0;
    }
    
    public override string ToString()
    {
        return $"{name} ({rarity} {type}, Nível {level})";
    }
    
    public override bool Equals(object obj)
    {
        if (obj is Item other)
        {
            return name == other.name && 
                   type == other.type && 
                   rarity == other.rarity && 
                   level == other.level;
        }
        return false;
    }
    
    public override int GetHashCode()
    {
        return (name?.GetHashCode() ?? 0) ^ 
               type.GetHashCode() ^ 
               rarity.GetHashCode() ^ 
               level.GetHashCode();
    }
}