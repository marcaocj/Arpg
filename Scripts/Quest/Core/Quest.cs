using UnityEngine;
using System.Collections.Generic;

public enum QuestType
{
    KillEnemies,
    CollectItems,
    ExploreArea,
    DefeatBoss
}

[System.Serializable]
public class QuestReward
{
    public int gold;
    public int experience;
    public Item item;
}

[System.Serializable]
public class Quest
{
    // Adicionado o campo id
    public string id;
    public string title;
    public string description;
    public QuestType type;
    public int requiredAmount;
    public int currentAmount;
    public bool isCompleted;
    public List<QuestReward> rewards = new List<QuestReward>();
    
    // Construtor atualizado para incluir id
    public Quest(string id, string title, string description, QuestType type, int requiredAmount)
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.type = type;
        this.requiredAmount = requiredAmount;
        this.currentAmount = 0;
        this.isCompleted = false;
    }
    
    public void UpdateProgress(int amount)
    {
        currentAmount += amount;
        
        if (currentAmount >= requiredAmount && !isCompleted)
        {
            CompleteQuest();
        }
    }
    
    public void CompleteQuest()
    {
        isCompleted = true;
        Debug.Log("Quest completa: " + title);
        
        // Dar recompensas ao jogador
        PlayerController player = Object.FindObjectOfType<PlayerController>();
        if (player != null)
        {
            foreach (QuestReward reward in rewards)
            {
                // Dar ouro
                if (reward.gold > 0 && GameManager.instance != null)
                {
                    GameManager.instance.AddGold(reward.gold);
                }
                
                // Dar experiência
                if (reward.experience > 0)
                {
                    player.GainExperience(reward.experience);
                }
                
                // Dar item
                if (reward.item != null)
                {
                    player.inventory.AddItem(reward.item);
                }
            }
        }
    }
}
