using UnityEngine;
using System.Collections.Generic;
using System;

public enum QuestType
{
    KillEnemies,
    CollectItems,
    ExploreArea,
    DefeatBoss,
    TalkToNPC,
    DeliverItem,
    ReachLevel,
    UseSkill,
    Custom
}

public enum QuestStatus
{
    NotStarted,
    Active,
    Completed,
    TurnedIn,
    Failed,
    Abandoned
}

public enum QuestPriority
{
    Low,
    Normal,
    High,
    Critical
}

[System.Serializable]
public class QuestObjective
{
    [Header("Objective Info")]
    public string id = "";
    public string description = "";
    public QuestType type;
    public int requiredAmount = 1;
    public int currentAmount = 0;
    public bool isCompleted = false;
    public bool isOptional = false;
    
    [Header("Specific Parameters")]
    public string targetId = "";              // ID específico (item, NPC, área, etc.)
    public string targetName = "";            // Nome amigável do alvo
    public List<string> validTargets = new List<string>(); // Múltiplos alvos válidos
    public Vector3 targetLocation = Vector3.zero; // Para objetivos de localização
    public float targetRadius = 5f;           // Raio para objetivos de área
    
    [Header("Progress Tracking")]
    public bool trackProgress = true;
    public string progressFormat = "{0}/{1}"; // Formato de exibição do progresso
    public Color progressColor = Color.white;
    
    public QuestObjective()
    {
        id = System.Guid.NewGuid().ToString();
    }
    
    public QuestObjective(string description, QuestType type, int requiredAmount, string targetId = "")
    {
        this.id = System.Guid.NewGuid().ToString();
        this.description = description;
        this.type = type;
        this.requiredAmount = requiredAmount;
        this.targetId = targetId;
        this.currentAmount = 0;
        this.isCompleted = false;
    }
    
    public bool UpdateProgress(int amount = 1)
    {
        if (isCompleted) return false;
        
        currentAmount = Mathf.Min(currentAmount + amount, requiredAmount);
        
        if (currentAmount >= requiredAmount && !isCompleted)
        {
            isCompleted = true;
            return true; // Objetivo completado agora
        }
        
        return false;
    }
    
    public void ResetProgress()
    {
        currentAmount = 0;
        isCompleted = false;
    }
    
    public float GetProgressPercentage()
    {
        if (requiredAmount <= 0) return 1f;
        return (float)currentAmount / requiredAmount;
    }
    
    public string GetProgressText()
    {
        return string.Format(progressFormat, currentAmount, requiredAmount);
    }
    
    public bool IsTargetValid(string checkTarget)
    {
        if (!string.IsNullOrEmpty(targetId) && targetId == checkTarget)
            return true;
        
        return validTargets.Contains(checkTarget);
    }
    
    public QuestObjective Clone()
    {
        var clone = new QuestObjective
        {
            id = System.Guid.NewGuid().ToString(),
            description = this.description,
            type = this.type,
            requiredAmount = this.requiredAmount,
            currentAmount = 0,
            isCompleted = false,
            isOptional = this.isOptional,
            targetId = this.targetId,
            targetName = this.targetName,
            validTargets = new List<string>(this.validTargets),
            targetLocation = this.targetLocation,
            targetRadius = this.targetRadius,
            trackProgress = this.trackProgress,
            progressFormat = this.progressFormat,
            progressColor = this.progressColor
        };
        
        return clone;
    }
}

[System.Serializable]
public class QuestReward
{
    [Header("Rewards")]
    public int gold = 0;
    public int experience = 0;
    public Item item = null;
    public List<Item> items = new List<Item>();
    
    [Header("Advanced Rewards")]
    public int attributePoints = 0;
    public float reputationGain = 0f;
    public string reputationFaction = "";
    public List<string> unlockedFeatures = new List<string>();
    public Quest unlockedQuest = null;
    
    [Header("Temporary Bonuses")]
    public float healthBonus = 0f;
    public float manaBonus = 0f;
    public float experienceMultiplier = 0f;
    public float bonusDuration = 0f;
    
    public bool HasRewards()
    {
        return gold > 0 || experience > 0 || item != null || items.Count > 0 || 
               attributePoints > 0 || reputationGain != 0f || unlockedFeatures.Count > 0 ||
               unlockedQuest != null;
    }
    
    public string GetRewardSummary()
    {
        var summary = new List<string>();
        
        if (gold > 0) summary.Add($"{gold} ouro");
        if (experience > 0) summary.Add($"{experience} XP");
        if (item != null) summary.Add(item.name);
        if (items.Count > 0) summary.Add($"{items.Count} itens");
        if (attributePoints > 0) summary.Add($"{attributePoints} pontos de atributo");
        if (reputationGain != 0f) summary.Add($"Reputação: {reputationGain:+0.0}");
        
        return string.Join(", ", summary);
    }
}

[System.Serializable]
public class QuestRequirement
{
    [Header("Level Requirements")]
    public int minLevel = 1;
    public int maxLevel = 999;
    
    [Header("Quest Requirements")]
    public List<string> requiredCompletedQuests = new List<string>();
    public List<string> requiredActiveQuests = new List<string>();
    public List<string> blockedByQuests = new List<string>();
    
    [Header("Item Requirements")]
    public List<Item> requiredItems = new List<Item>();
    public bool consumeItems = false;
    
    [Header("Faction & Reputation")]
    public string requiredFaction = "";
    public float minReputation = 0f;
    
    [Header("Time Requirements")]
    public bool hasTimeLimit = false;
    public float timeLimit = 0f; // Em segundos
    public bool isRepeatable = false;
    public float repeatCooldown = 86400f; // 24 horas padrão
    
    public bool CanPlayerAccept(PlayerController player, QuestManager questManager)
    {
        if (player == null) return false;
        
        var playerStats = player.GetStats();
        if (playerStats == null) return false;
        
        // Verificar nível
        if (playerStats.Level < minLevel || playerStats.Level > maxLevel)
            return false;
        
        // Verificar quests obrigatórias completadas
        foreach (string questId in requiredCompletedQuests)
        {
            if (!questManager.IsQuestCompleted(questId))
                return false;
        }
        
        // Verificar quests que devem estar ativas
        foreach (string questId in requiredActiveQuests)
        {
            if (!questManager.IsQuestActive(questId))
                return false;
        }
        
        // Verificar quests que bloqueiam esta
        foreach (string questId in blockedByQuests)
        {
            if (questManager.IsQuestActive(questId) || questManager.IsQuestCompleted(questId))
                return false;
        }
        
        // Verificar itens obrigatórios
        var inventory = player.GetInventoryManager();
        if (inventory != null)
        {
            foreach (var requiredItem in requiredItems)
            {
                if (!inventory.HasItem(requiredItem.id))
                    return false;
            }
        }
        
        return true;
    }
    
    public List<string> GetUnmetRequirements(PlayerController player, QuestManager questManager)
    {
        var unmet = new List<string>();
        
        if (player == null) 
        {
            unmet.Add("Player não encontrado");
            return unmet;
        }
        
        var playerStats = player.GetStats();
        if (playerStats == null)
        {
            unmet.Add("Stats do player indisponíveis");
            return unmet;
        }
        
        if (playerStats.Level < minLevel)
            unmet.Add($"Nível mínimo: {minLevel} (atual: {playerStats.Level})");
        
        if (playerStats.Level > maxLevel)
            unmet.Add($"Nível máximo: {maxLevel} (atual: {playerStats.Level})");
        
        foreach (string questId in requiredCompletedQuests)
        {
            if (!questManager.IsQuestCompleted(questId))
            {
                var quest = questManager.GetQuestByID(questId);
                string questName = quest?.title ?? questId;
                unmet.Add($"Quest obrigatória: {questName}");
            }
        }
        
        return unmet;
    }
}

[CreateAssetMenu(fileName = "New Quest", menuName = "RPG/Quest System/Quest")]
public class Quest : ScriptableObject
{
    [Header("Basic Info")]
    public string id = "";
    public string title = "";
    [TextArea(3, 5)]
    public string description = "";
    [TextArea(2, 4)]
    public string summary = ""; // Resumo curto para UI
    
    [Header("Quest Properties")]
    public QuestStatus status = QuestStatus.NotStarted;
    public QuestPriority priority = QuestPriority.Normal;
    public bool isMainQuest = false;
    public bool isSideQuest = true;
    public bool isRepeatable = false;
    public bool autoComplete = true; // Completa automaticamente quando objetivos são atingidos
    public bool autoTurnIn = false;  // Entrega automaticamente quando completa
    
    [Header("Timing")]
    public bool hasTimeLimit = false;
    public float timeLimit = 3600f; // 1 hora padrão
    public float remainingTime = 0f;
    
    [Header("Organization")]
    public string category = "General";
    public List<string> tags = new List<string>();
    public int sortOrder = 0;
    
    [Header("Objectives")]
    public List<QuestObjective> objectives = new List<QuestObjective>();
    
    [Header("Requirements")]
    public QuestRequirement requirements = new QuestRequirement();
    
    [Header("Rewards")]
    public QuestReward rewards = new QuestReward();
    
    [Header("NPCs")]
    public string questGiverNPC = "";
    public string turnInNPC = "";
    public List<string> relatedNPCs = new List<string>();
    
    [Header("Locations")]
    public string startLocation = "";
    public Vector3 startPosition = Vector3.zero;
    public List<Vector3> waypoints = new List<Vector3>();
    
    [Header("Dialogue")]
    [TextArea(2, 4)]
    public string acceptDialogue = "";
    [TextArea(2, 4)]
    public string progressDialogue = "";
    [TextArea(2, 4)]
    public string completeDialogue = "";
    [TextArea(2, 4)]
    public string turnInDialogue = "";
    
    [Header("Visual & Audio")]
    public Sprite questIcon;
    public Color questColor = Color.white;
    public AudioClip acceptSound;
    public AudioClip completeSound;
    public AudioClip turnInSound;
    
    // Estado interno
    [System.NonSerialized]
    private float acceptedTime = 0f;
    [System.NonSerialized]
    private bool hasBeenStarted = false;
    
    // Eventos
    public event Action<Quest> OnQuestStarted;
    public event Action<Quest> OnQuestCompleted;
    public event Action<Quest> OnQuestTurnedIn;
    public event Action<Quest> OnQuestFailed;
    public event Action<Quest, QuestObjective> OnObjectiveCompleted;
    public event Action<Quest> OnQuestAbandoned;
    
    #region Initialization
    
    private void OnEnable()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
        }
        
        ValidateQuest();
    }
    
    private void ValidateQuest()
    {
        // Validar objetivos
        for (int i = objectives.Count - 1; i >= 0; i--)
        {
            if (objectives[i] == null)
            {
                objectives.RemoveAt(i);
                continue;
            }
            
            if (string.IsNullOrEmpty(objectives[i].id))
            {
                objectives[i].id = System.Guid.NewGuid().ToString();
            }
        }
        
        // Garantir que temos pelo menos um objetivo
        if (objectives.Count == 0)
        {
            objectives.Add(new QuestObjective("Complete the quest", QuestType.Custom, 1));
        }
        
        // Validar IDs únicos dos objetivos
        var usedIds = new HashSet<string>();
        foreach (var objective in objectives)
        {
            if (usedIds.Contains(objective.id))
            {
                objective.id = System.Guid.NewGuid().ToString();
            }
            usedIds.Add(objective.id);
        }
    }
    
    #endregion
    
    #region Quest State Management
    
    public bool StartQuest()
    {
        if (status != QuestStatus.NotStarted)
        {
            Debug.LogWarning($"Tentativa de iniciar quest que já foi iniciada: {title}");
            return false;
        }
        
        status = QuestStatus.Active;
        acceptedTime = Time.time;
        hasBeenStarted = true;
        
        if (hasTimeLimit)
        {
            remainingTime = timeLimit;
        }
        
        // Resetar objetivos
        foreach (var objective in objectives)
        {
            objective.ResetProgress();
        }
        
        OnQuestStarted?.Invoke(this);
        
        // Disparar evento global
        EventManager.TriggerEvent(new QuestAcceptedEvent
        {
            quest = this,
            questGiver = null
        });
        
        Debug.Log($"Quest iniciada: {title}");
        return true;
    }
    
    public bool CompleteQuest()
    {
        if (status != QuestStatus.Active)
        {
            return false;
        }
        
        status = QuestStatus.Completed;
        OnQuestCompleted?.Invoke(this);
        
        // Disparar evento global
        EventManager.TriggerEvent(new QuestCompletedEvent
        {
            quest = this,
            rewards = new List<QuestReward> { rewards }
        });
        
        // Auto turn-in se configurado
        if (autoTurnIn)
        {
            TurnInQuest();
        }
        
        Debug.Log($"Quest completada: {title}");
        return true;
    }
    
    public bool TurnInQuest()
    {
        if (status != QuestStatus.Completed)
        {
            return false;
        }
        
        status = QuestStatus.TurnedIn;
        OnQuestTurnedIn?.Invoke(this);
        
        Debug.Log($"Quest entregue: {title}");
        return true;
    }
    
    public bool FailQuest()
    {
        if (status != QuestStatus.Active)
        {
            return false;
        }
        
        status = QuestStatus.Failed;
        OnQuestFailed?.Invoke(this);
        
        Debug.Log($"Quest falhada: {title}");
        return true;
    }
    
    public bool AbandonQuest()
    {
        if (status != QuestStatus.Active)
        {
            return false;
        }
        
        status = QuestStatus.Abandoned;
        OnQuestAbandoned?.Invoke(this);
        
        // Disparar evento global
        EventManager.TriggerEvent(new QuestAbandonedEvent
        {
            quest = this
        });
        
        Debug.Log($"Quest abandonada: {title}");
        return true;
    }
    
    public void ResetQuest()
    {
        status = QuestStatus.NotStarted;
        remainingTime = hasTimeLimit ? timeLimit : 0f;
        acceptedTime = 0f;
        hasBeenStarted = false;
        
        foreach (var objective in objectives)
        {
            objective.ResetProgress();
        }
        
        Debug.Log($"Quest resetada: {title}");
    }
    
    #endregion
    
    #region Objective Management
    
    public bool UpdateObjective(string objectiveId, int amount = 1)
    {
        var objective = GetObjective(objectiveId);
        if (objective == null)
        {
            Debug.LogWarning($"Objetivo não encontrado: {objectiveId} na quest {title}");
            return false;
        }
        
        return UpdateObjective(objective, amount);
    }
    
    public bool UpdateObjective(QuestObjective objective, int amount = 1)
    {
        if (objective == null || status != QuestStatus.Active)
            return false;
        
        bool wasCompleted = objective.UpdateProgress(amount);
        
        if (wasCompleted)
        {
            OnObjectiveCompleted?.Invoke(this, objective);
            
            Debug.Log($"Objetivo completado: {objective.description} em {title}");
            
            // Verificar se a quest foi completada
            if (autoComplete && AreAllObjectivesComplete())
            {
                CompleteQuest();
            }
        }
        
        // Disparar evento de progresso
        EventManager.TriggerEvent(new QuestProgressUpdatedEvent
        {
            quest = this,
            oldProgress = objective.currentAmount - amount,
            newProgress = objective.currentAmount,
            requiredAmount = objective.requiredAmount
        });
        
        return wasCompleted;
    }
    
    public bool UpdateObjectiveByType(QuestType type, int amount = 1, string targetId = "")
    {
        bool anyUpdated = false;
        
        foreach (var objective in objectives)
        {
            if (objective.type == type && !objective.isCompleted)
            {
                if (string.IsNullOrEmpty(targetId) || objective.IsTargetValid(targetId))
                {
                    if (UpdateObjective(objective, amount))
                    {
                        anyUpdated = true;
                    }
                }
            }
        }
        
        return anyUpdated;
    }
    
    public QuestObjective GetObjective(string objectiveId)
    {
        return objectives.Find(obj => obj.id == objectiveId);
    }
    
    public List<QuestObjective> GetObjectivesByType(QuestType type)
    {
        return objectives.FindAll(obj => obj.type == type);
    }
    
    public bool AreAllObjectivesComplete()
    {
        foreach (var objective in objectives)
        {
            if (!objective.isOptional && !objective.isCompleted)
            {
                return false;
            }
        }
        return true;
    }
    
    public bool AreRequiredObjectivesComplete()
    {
        foreach (var objective in objectives)
        {
            if (!objective.isOptional && !objective.isCompleted)
            {
                return false;
            }
        }
        return true;
    }
    
    public float GetOverallProgress()
    {
        if (objectives.Count == 0) return 1f;
        
        var requiredObjectives = objectives.FindAll(obj => !obj.isOptional);
        if (requiredObjectives.Count == 0) return 1f;
        
        float totalProgress = 0f;
        foreach (var objective in requiredObjectives)
        {
            totalProgress += objective.GetProgressPercentage();
        }
        
        return totalProgress / requiredObjectives.Count;
    }
    
    #endregion
    
    #region Time Management
    
    public void UpdateTimer(float deltaTime)
    {
        if (!hasTimeLimit || status != QuestStatus.Active)
            return;
        
        remainingTime -= deltaTime;
        
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            FailQuest();
        }
    }
    
    public float GetRemainingTime()
    {
        return hasTimeLimit ? remainingTime : 0f;
    }
    
    public float GetTimePercentage()
    {
        if (!hasTimeLimit || timeLimit <= 0f) return 1f;
        return remainingTime / timeLimit;
    }
    
    public string GetTimeRemainingText()
    {
        if (!hasTimeLimit) return "";
        
        if (remainingTime <= 0f) return "Tempo esgotado";
        
        int hours = Mathf.FloorToInt(remainingTime / 3600f);
        int minutes = Mathf.FloorToInt((remainingTime % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        
        if (hours > 0)
            return $"{hours}h {minutes}m {seconds}s";
        else if (minutes > 0)
            return $"{minutes}m {seconds}s";
        else
            return $"{seconds}s";
    }
    
    #endregion
    
    #region Requirements
    
    public bool CanPlayerAccept(PlayerController player)
    {
        var questManager = FindObjectOfType<QuestManager>();
        if (questManager == null) return false;
        
        return requirements.CanPlayerAccept(player, questManager);
    }
    
    public List<string> GetUnmetRequirements(PlayerController player)
    {
        var questManager = FindObjectOfType<QuestManager>();
        if (questManager == null) 
            return new List<string> { "Sistema de quest indisponível" };
        
        return requirements.GetUnmetRequirements(player, questManager);
    }
    
    #endregion
    
    #region Utility
    
    public Quest Clone()
    {
        var clone = CreateInstance<Quest>();
        
        // Copiar propriedades básicas
        clone.id = System.Guid.NewGuid().ToString();
        clone.title = this.title;
        clone.description = this.description;
        clone.summary = this.summary;
        clone.status = QuestStatus.NotStarted;
        clone.priority = this.priority;
        clone.isMainQuest = this.isMainQuest;
        clone.isSideQuest = this.isSideQuest;
        clone.isRepeatable = this.isRepeatable;
        clone.autoComplete = this.autoComplete;
        clone.autoTurnIn = this.autoTurnIn;
        clone.hasTimeLimit = this.hasTimeLimit;
        clone.timeLimit = this.timeLimit;
        clone.remainingTime = this.timeLimit;
        clone.category = this.category;
        clone.tags = new List<string>(this.tags);
        clone.sortOrder = this.sortOrder;
        
        // Clonar objetivos
        clone.objectives = new List<QuestObjective>();
        foreach (var objective in this.objectives)
        {
            clone.objectives.Add(objective.Clone());
        }
        
        // Copiar requirements (referência é ok para a maioria dos casos)
        clone.requirements = this.requirements;
        
        // Copiar rewards (referência é ok)
        clone.rewards = this.rewards;
        
        // Copiar outras propriedades
        clone.questGiverNPC = this.questGiverNPC;
        clone.turnInNPC = this.turnInNPC;
        clone.relatedNPCs = new List<string>(this.relatedNPCs);
        clone.startLocation = this.startLocation;
        clone.startPosition = this.startPosition;
        clone.waypoints = new List<Vector3>(this.waypoints);
        clone.acceptDialogue = this.acceptDialogue;
        clone.progressDialogue = this.progressDialogue;
        clone.completeDialogue = this.completeDialogue;
        clone.turnInDialogue = this.turnInDialogue;
        clone.questIcon = this.questIcon;
        clone.questColor = this.questColor;
        clone.acceptSound = this.acceptSound;
        clone.completeSound = this.completeSound;
        clone.turnInSound = this.turnInSound;
        
        return clone;
    }
    
    public string GetStatusText()
    {
        switch (status)
        {
            case QuestStatus.NotStarted: return "Não iniciada";
            case QuestStatus.Active: return "Ativa";
            case QuestStatus.Completed: return "Completada";
            case QuestStatus.TurnedIn: return "Entregue";
            case QuestStatus.Failed: return "Falhada";
            case QuestStatus.Abandoned: return "Abandonada";
            default: return "Desconhecida";
        }
    }
    
    public Color GetStatusColor()
    {
        switch (status)
        {
            case QuestStatus.NotStarted: return Color.gray;
            case QuestStatus.Active: return Color.yellow;
            case QuestStatus.Completed: return Color.green;
            case QuestStatus.TurnedIn: return Color.blue;
            case QuestStatus.Failed: return Color.red;
            case QuestStatus.Abandoned: return Color.magenta;
            default: return Color.white;
        }
    }
    
    public string GetDetailedDescription(bool includeObjectives = true, bool includeRewards = true)
    {
        var desc = new System.Text.StringBuilder();
        
        desc.AppendLine($"<b><size=18>{title}</size></b>");
        desc.AppendLine($"<color={ColorUtility.ToHtmlStringRGB(GetStatusColor())}>{GetStatusText()}</color>");
        
        if (isMainQuest)
            desc.AppendLine("<color=gold>[QUEST PRINCIPAL]</color>");
        
        if (hasTimeLimit && status == QuestStatus.Active)
            desc.AppendLine($"<color=red>Tempo restante: {GetTimeRemainingText()}</color>");
        
        desc.AppendLine();
        desc.AppendLine(description);
        
        if (includeObjectives && objectives.Count > 0)
        {
            desc.AppendLine();
            desc.AppendLine("<b>Objetivos:</b>");
            
            foreach (var objective in objectives)
            {
                string prefix = objective.isCompleted ? "✓" : "○";
                string optional = objective.isOptional ? " (Opcional)" : "";
                Color objColor = objective.isCompleted ? Color.green : Color.white;
                string colorHex = ColorUtility.ToHtmlStringRGB(objColor);
                
                if (objective.trackProgress && objective.requiredAmount > 1)
                {
                    desc.AppendLine($"<color=#{colorHex}>{prefix} {objective.description} {objective.GetProgressText()}{optional}</color>");
                }
                else
                {
                    desc.AppendLine($"<color=#{colorHex}>{prefix} {objective.description}{optional}</color>");
                }
            }
        }
        
        if (includeRewards && rewards.HasRewards())
        {
            desc.AppendLine();
            desc.AppendLine("<b>Recompensas:</b>");
            desc.AppendLine($"<color=yellow>{rewards.GetRewardSummary()}</color>");
        }
        
        return desc.ToString();
    }
    
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(id) && 
               !string.IsNullOrEmpty(title) && 
               objectives.Count > 0;
    }
    
    #endregion
    
    #region Debug
    
    [ContextMenu("Debug Quest Info")]
    public void DebugQuestInfo()
    {
        Debug.Log($"=== QUEST: {title} ===");
        Debug.Log($"ID: {id}");
        Debug.Log($"Status: {status}");
        Debug.Log($"Progresso geral: {GetOverallProgress():P1}");
        
        if (hasTimeLimit)
        {
            Debug.Log($"Tempo restante: {GetTimeRemainingText()}");
        }
        
        Debug.Log("Objetivos:");
        for (int i = 0; i < objectives.Count; i++)
        {
            var obj = objectives[i];
            string completedText = obj.isCompleted ? "✓" : "○";
            string optionalText = obj.isOptional ? " (Opcional)" : "";
            Debug.Log($"  {i + 1}. {completedText} {obj.description} {obj.GetProgressText()}{optionalText}");
        }
        
        if (rewards.HasRewards())
        {
            Debug.Log($"Recompensas: {rewards.GetRewardSummary()}");
        }
    }
    
    [ContextMenu("Complete All Objectives")]
    public void DebugCompleteAllObjectives()
    {
        foreach (var objective in objectives)
        {
            if (!objective.isCompleted)
            {
                objective.currentAmount = objective.requiredAmount;
                objective.isCompleted = true;
            }
        }
        
        if (autoComplete)
        {
            CompleteQuest();
        }
        
        Debug.Log($"Todos os objetivos da quest '{title}' foram completados");
    }
    
    [ContextMenu("Reset Quest")]
    public void DebugResetQuest()
    {
        ResetQuest();
        Debug.Log($"Quest '{title}' foi resetada");
    }
    
    #endregion
}