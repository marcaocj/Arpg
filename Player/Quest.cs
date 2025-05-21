using UnityEngine;
using System.Collections.Generic;
using System;

public enum QuestType
{
    KillEnemies,
    CollectItems,
    ExploreArea,
    DefeatBoss,
    TalkToNPC,      // Novo tipo: conversar com NPC
    EscortNPC,      // Novo tipo: escoltar NPC
    DefendLocation  // Novo tipo: defender local
}

public enum QuestPriority
{
    Low,
    Normal,
    High,
    Critical
}

[Serializable]
public class QuestReward
{
    public int gold;
    public int experience;
    public Item item;
    public Skill skillReward; // NOVO: Recompensa de habilidade
}

[Serializable]
public class QuestObjective
{
    public string description;
    public QuestType type;
    public int requiredAmount;
    public int currentAmount;
    public bool isCompleted;
    public string targetId; // ID do alvo (inimigo, item, localização)
    
    public QuestObjective(string description, QuestType type, int requiredAmount, string targetId = "")
    {
        this.description = description;
        this.type = type;
        this.requiredAmount = requiredAmount;
        this.currentAmount = 0;
        this.isCompleted = false;
        this.targetId = targetId;
    }
    
    public void UpdateProgress(int amount)
    {
        currentAmount += amount;
        
        if (currentAmount >= requiredAmount && !isCompleted)
        {
            isCompleted = true;
        }
    }
    
    public float GetProgress()
    {
        return Mathf.Clamp01((float)currentAmount / requiredAmount);
    }
}

[Serializable]
public class Quest
{
    // Identificação
    public string id;
    public string title;
    public string description;
    
    // Tipo e prioridade
    public QuestType type;
    public QuestPriority priority = QuestPriority.Normal;
    
    // Progresso
    public int requiredAmount;
    public int currentAmount;
    public bool isCompleted;
    
    // Sistema de objetivos múltiplos (NOVO)
    public List<QuestObjective> objectives = new List<QuestObjective>();
    
    // Quests relacionadas
    public string prerequisiteQuestId; // Quest que precisa ser completa antes
    public List<string> nextQuestIds = new List<string>(); // Quests que serão disponibilizadas após completar esta
    
    // Recompensas
    public List<QuestReward> rewards = new List<QuestReward>();
    
    // Informações de gameplay
    public float timeLimit = 0; // 0 = sem limite
    public Vector3 locationPosition; // Posição no mundo para a quest (se relevante)
    public string targetName; // Nome do alvo (NPC, inimigo, etc.)
    
    // Dados de UI
    public Sprite questIcon; // Ícone para uso na UI
    public Color questColor = Color.white; // Cor customizada (se desejado)
    
    // Informações de narrativa
    public string questGiverName;
    public string questGiverId;
    public string introDialogue; // Diálogo ao aceitar a quest
    public string progressDialogue; // Diálogo ao verificar progresso
    public string completionDialogue; // Diálogo ao completar a quest
    
    // Timestamps para estatísticas/tracking (NOVO)
    public DateTime acceptedTime;
    public DateTime completedTime;
    
    // Construtor básico
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
    
    // Construtor avançado com objetivos múltiplos
    public Quest(string id, string title, string description, QuestType mainType)
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.type = mainType;
        this.requiredAmount = 1; // Valor padrão
        this.currentAmount = 0;
        this.isCompleted = false;
    }
    
    // Adicionar objetivo
    public void AddObjective(string description, QuestType type, int requiredAmount, string targetId = "")
    {
        QuestObjective objective = new QuestObjective(description, type, requiredAmount, targetId);
        objectives.Add(objective);
    }
    
    // Atualizar progresso de objetivos múltiplos
    public void UpdateObjectiveProgress(QuestType type, int amount = 1, string targetId = "")
    {
        bool allCompleted = true;
        bool anyUpdated = false;
        
        foreach (QuestObjective objective in objectives)
        {
            // Atualizar objetivo correspondente
            if (objective.type == type && !objective.isCompleted)
            {
                // Se tiver targetId, verificar se corresponde
                if (!string.IsNullOrEmpty(targetId) && !string.IsNullOrEmpty(objective.targetId))
                {
                    if (objective.targetId != targetId)
                        continue; // Pular se o targetId não corresponder
                }
                
                objective.UpdateProgress(amount);
                anyUpdated = true;
                
                // Atualizar progresso geral da quest para compatibilidade
                if (type == this.type)
                {
                    this.currentAmount += amount;
                    if (this.currentAmount > this.requiredAmount)
                        this.currentAmount = this.requiredAmount;
                }
            }
            
            // Verificar se todos os objetivos estão completos
            if (!objective.isCompleted)
                allCompleted = false;
        }
        
        // Se todos os objetivos estiverem completos, a quest está completa
        if (allCompleted && objectives.Count > 0)
        {
            isCompleted = true;
        }
        
        // Se não há objetivos específicos, usar o método antigo
        if (objectives.Count == 0 || !anyUpdated)
        {
            UpdateProgress(amount);
        }
    }
    
    // Método antigo de atualização para compatibilidade
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
        completedTime = DateTime.Now;
        Debug.Log("Quest completa: " + title);
        
        // Dar recompensas ao jogador
        PlayerController player = UnityEngine.Object.FindObjectOfType<PlayerController>();
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
                
                // Dar habilidade
                if (reward.skillReward != null)
                {
                    player.AddSkill(reward.skillReward);
                }
            }
        }
    }
    
    // Registrar aceitação da quest
    public void OnAccept()
    {
        acceptedTime = DateTime.Now;
        
        // Se a quest tiver múltiplos objetivos mas não tiver sido configurada com o método AddObjective,
        // criar um objetivo padrão baseado nos dados principais da quest
        if (objectives.Count == 0)
        {
            AddObjective(description, type, requiredAmount);
        }
    }
    
    // Calcular duração da quest
    public TimeSpan GetCompletionTime()
    {
        if (!isCompleted || acceptedTime == DateTime.MinValue || completedTime == DateTime.MinValue)
            return TimeSpan.Zero;
            
        return completedTime - acceptedTime;
    }
    
    // Verificar se está prestes a expirar (se tiver limite de tempo)
    public bool IsAboutToExpire()
    {
        if (timeLimit <= 0 || acceptedTime == DateTime.MinValue)
            return false;
            
        float elapsedHours = (float)(DateTime.Now - acceptedTime).TotalHours;
        return (elapsedHours >= timeLimit * 0.8f && elapsedHours < timeLimit);
    }
    
    // Verificar se expirou
    public bool HasExpired()
    {
        if (timeLimit <= 0 || acceptedTime == DateTime.MinValue)
            return false;
            
        float elapsedHours = (float)(DateTime.Now - acceptedTime).TotalHours;
        return elapsedHours >= timeLimit;
    }
    
    // Método para obter string de progresso formatada
    public string GetProgressText()
    {
        if (objectives.Count > 0)
        {
            int completedObjectives = 0;
            foreach (QuestObjective objective in objectives)
            {
                if (objective.isCompleted)
                    completedObjectives++;
            }
            
            return $"{completedObjectives}/{objectives.Count} objetivos concluídos";
        }
        else
        {
            return $"{currentAmount}/{requiredAmount}";
        }
    }
    
    // Obter percentual total de progresso da quest
    public float GetTotalProgress()
    {
        if (objectives.Count > 0)
        {
            float totalProgress = 0f;
            foreach (QuestObjective objective in objectives)
            {
                totalProgress += objective.GetProgress();
            }
            
            return totalProgress / objectives.Count;
        }
        else
        {
            return Mathf.Clamp01((float)currentAmount / requiredAmount);
        }
    }
    
    // Obter texto formatado de recompensas
    public string GetRewardsText()
    {
        string rewardsText = "";
        
        foreach (QuestReward reward in rewards)
        {
            if (reward.gold > 0)
                rewardsText += $"{reward.gold} de ouro\n";
                
            if (reward.experience > 0)
                rewardsText += $"{reward.experience} de experiência\n";
                
            if (reward.item != null)
                rewardsText += $"Item: {reward.item.name}\n";
                
            if (reward.skillReward != null)
                rewardsText += $"Habilidade: {reward.skillReward.name}\n";
        }
        
        return rewardsText;
    }
    
    // Criar cópia da quest para modificações temporárias
    public Quest Clone()
    {
        Quest clone = new Quest(id, title, description, type, requiredAmount);
        clone.currentAmount = currentAmount;
        clone.isCompleted = isCompleted;
        
        // Clonar objetivos
        foreach (QuestObjective objective in objectives)
        {
            QuestObjective clonedObjective = new QuestObjective(objective.description, objective.type, objective.requiredAmount, objective.targetId);
            clonedObjective.currentAmount = objective.currentAmount;
            clonedObjective.isCompleted = objective.isCompleted;
            clone.objectives.Add(clonedObjective);
        }
        
        // Clonar outros dados importantes
        clone.priority = priority;
        clone.prerequisiteQuestId = prerequisiteQuestId;
        clone.nextQuestIds = new List<string>(nextQuestIds);
        clone.rewards = new List<QuestReward>(rewards);
        clone.timeLimit = timeLimit;
        clone.locationPosition = locationPosition;
        clone.targetName = targetName;
        clone.questIcon = questIcon;
        clone.questColor = questColor;
        
        return clone;
    }
}