using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Loot Table", menuName = "RPG/Loot Table")]
public class LootTable : ScriptableObject
{
    [System.Serializable]
    public class LootEntry
    {
        public Item item;
        public float dropChance; // 0-100%
    }
    
    public List<LootEntry> possibleLoot = new List<LootEntry>();
    public float goldMin = 0;
    public float goldMax = 10;
    
    public void InitializeDefault(int enemyLevel)
    {
        // Inicializar tabela de loot com itens básicos
        possibleLoot.Clear();
        
        // Adicionar algumas poções
        Item healthPotion = new Item("Poção de Vida", "Recupera 50 pontos de vida", ItemType.Consumable, ItemRarity.Common, 1);
        healthPotion.healthRestore = 50;
        
        Item manaPotion = new Item("Poção de Mana", "Recupera 30 pontos de mana", ItemType.Consumable, ItemRarity.Common, 1);
        manaPotion.manaRestore = 30;
        
        // Adicionar equipamento baseado no nível
        Item weapon = new Item("Espada de Ferro", "Uma espada comum", ItemType.Weapon, ItemRarity.Common, enemyLevel);
        weapon.physicalDamage = 5 + enemyLevel * 2;
        weapon.strengthModifier = 1 + enemyLevel / 3;
        
        Item helmet = new Item("Elmo de Couro", "Proteção básica para a cabeça", ItemType.Helmet, ItemRarity.Common, enemyLevel);
        helmet.vitalityModifier = 1 + enemyLevel / 3;
        
        // Adicionar entradas à tabela de loot
        LootEntry healthPotionEntry = new LootEntry();
        healthPotionEntry.item = healthPotion;
        healthPotionEntry.dropChance = 30; // 30% de chance
        possibleLoot.Add(healthPotionEntry);
        
        LootEntry manaPotionEntry = new LootEntry();
        manaPotionEntry.item = manaPotion;
        manaPotionEntry.dropChance = 20; // 20% de chance
        possibleLoot.Add(manaPotionEntry);
        
        LootEntry weaponEntry = new LootEntry();
        weaponEntry.item = weapon;
        weaponEntry.dropChance = 5; // 5% de chance
        possibleLoot.Add(weaponEntry);
        
        LootEntry helmetEntry = new LootEntry();
        helmetEntry.item = helmet;
        helmetEntry.dropChance = 3; // 3% de chance
        possibleLoot.Add(helmetEntry);
        
        // Ajustar ouro baseado no nível
        goldMin = enemyLevel * 5;
        goldMax = enemyLevel * 10;
    }
    
    public List<Item> RollForLoot()
    {
        List<Item> result = new List<Item>();
        
        // Rolar para cada item possível
        foreach (LootEntry entry in possibleLoot)
        {
            float roll = Random.Range(0f, 100f);
            if (roll <= entry.dropChance)
            {
                result.Add(entry.item);
            }
        }
        
        // Adicionar ouro (implementação separada)
        float goldAmount = Random.Range(goldMin, goldMax);
        Debug.Log("Ouro dropado: " + Mathf.RoundToInt(goldAmount));
        
        return result;
    }
}
