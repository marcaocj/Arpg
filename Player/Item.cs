using UnityEngine; // Adicionando a referência necessária ao UnityEngine
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

[Serializable] // Usando o atributo Serializable para permitir que a classe seja serializada
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
        if (type == ItemType.Consumable)
        {
            // Restaurar saúde e mana
            player.health = Mathf.Min(player.health + healthRestore, player.maxHealth);
            player.mana = Mathf.Min(player.mana + manaRestore, player.maxMana);
            
            // Remover do inventário após uso
            player.inventory.RemoveItem(this);
            
            Debug.Log("Item consumido: " + name);
        }
        else
        {
            // Equipar item
            player.inventory.EquipItem(this);
        }
    }
}