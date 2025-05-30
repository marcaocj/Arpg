using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Gerenciador avançado de quests com sistema completo de eventos e progressão
/// </summary>
public class QuestManager : MonoBehaviour
{
    [Header("Quest Management")]
    public List<Quest> availableQuests = new List<Quest>();
    public List<Quest> activeQuests = new List<Quest>();
    public List<Quest> completedQuests = new List<Quest>();
    public List<Quest> turnedInQuests = new List<Quest>();
    public List<Quest> failedQuests = new List<Quest>();
    
    [Header("Configuration")]
    public int maxActiveQuests = 10;
    public bool autoLoadQuestDatabase = true;
    public bool enableQuestTracking = true;
    public bool enableQuestNotifications = true;
    public bool enableQuestSounds = true;
    
    [Header("Quest Database")]
    public List<Quest> questDatabase = new List<Quest>();
    public string questDatabasePath = "Quests/";
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip questAcceptedSound;
    public AudioClip questCompletedSound;
    public AudioClip questFailedSound;
    public AudioClip objectiveCompletedSound;
    
    // Cache para evitar FindObjectOfType
    private PlayerController cachedPlayer;
    private GameManager cachedGameManager;
    private bool playerCacheValid = false;
    private bool gameManagerCacheValid = false;
    
    // Tracking de quest repetíveis
    private Dictionary<string, float> questCooldowns = new Dictionary<string, float>();
    private Dictionary<string, int> questCompletionCounts = new Dictionary<string, int>();
    
    // Eventos
    public event Action<Quest> OnQuestAccepted;
    public event Action<Quest> OnQuestCompleted;
    public event Action<Quest> OnQuestTurnedIn;
    public event Action<Quest> OnQuestFailed;
    public event Action<Quest> OnQuestAbandoned;
    public event Action<Quest, QuestObjective> OnObjectiveCompleted;
    
    // Singleton para acesso fácil
    public static QuestManager Instance { get; private set; }
    
    #region Initialization
    
    private void Awake()
    {
        // Configurar singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Configurar audio source
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void Start()
    {
        SubscribeToEvents();
        
        if (autoLoadQuestDatabase)
        {
            LoadQuestDatabase();
        }
        
        InitializeDefaultQuests();
        TryCacheComponents();
    }
    
    private void SubscribeToEvents()
    {
        EventManager.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
        EventManager.Subscribe<ItemCollectedEvent>(OnItemCollected);
        EventManager.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        EventManager.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        EventManager.Subscribe<PlayerDestroyedEvent>(OnPlayerDestroyed);
        EventManager.Subscribe<SceneTransitionEvent>(OnSceneTransition);
        EventManager.Subscribe<NPCInteractionEvent>(OnNPCInteraction);
    }
    
    private void LoadQuestDatabase()
    {
        try
        {
            Quest[] loadedQuests = Resources.LoadAll<Quest>(questDatabasePath);
            questDatabase.Clear();
            questDatabase.AddRange(loadedQuests);
            
            Debug.Log($"QuestManager: Carregadas {questDatabase.Count} quests do banco de dados");
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao carregar banco de dados de quests: {e.Message}");
        }
    }
    
    private void InitializeDefaultQuests()
    {
        if (questDatabase.Count > 0)
        {
            // Adicionar quests do banco de dados às disponíveis
            foreach (var questTemplate in questDatabase)
            {
                if (questTemplate != null && !availableQuests.Any(q => q.id == questTemplate.id))
                {
                    availableQuests.Add(questTemplate);
                }
            }
        }
        else
        {
            // Criar quests padrão se não houver banco de dados
            CreateDefaultQuests();
        }
    }
    
    private void CreateDefaultQuests()
    {
        // Quest para matar inimigos
        var killQuest = CreateQuestInstance("kill_enemies_01", "Limpar o Caminho", 
            "Derrote 10 inimigos na zona inicial para garantir a segurança dos viajantes.");
        
        killQuest.objectives.Add(new QuestObjective("Derrotar inimigos", QuestType.KillEnemies, 10));
        killQuest.rewards.gold = 50;
        killQuest.rewards.experience = 100;
        killQuest.category = "Combat";
        killQuest.priority = QuestPriority.Normal;
        
        availableQuests.Add(killQuest);
        
        // Quest para coletar itens
        var collectQuest = CreateQuestInstance("collect_potions_01", "Colecionador Iniciante", 
            "Colete 5 itens quaisquer para demonstrar suas habilidades de exploração.");
        
        collectQuest.objectives.Add(new QuestObjective("Coletar itens", QuestType.CollectItems, 5));
        collectQuest.rewards.gold = 30;
        collectQuest.rewards.experience = 50;
        
        var rewardItem = new Item("Anel de Proteção", "Aumenta a defesa do usuário", ItemType.Jewelry, ItemRarity.Uncommon, 1);
        rewardItem.vitalityModifier = 3;
        collectQuest.rewards.item = rewardItem;
        collectQuest.category = "Exploration";
        
        availableQuests.Add(collectQuest);
        
        // Quest para derrotar boss
        var bossQuest = CreateQuestInstance("defeat_boss_01", "A Ameaça Final", 
            "Derrote o Chefe Goblin que está aterrorizando o vilarejo e traga paz para a região.");
        
        bossQuest.objectives.Add(new QuestObjective("Derrotar o Chefe Goblin", QuestType.DefeatBoss, 1, "goblin_chief"));
        bossQuest.rewards.gold = 200;
        bossQuest.rewards.experience = 500;
        bossQuest.rewards.attributePoints = 2;
        
        var bossItem = new Item("Espada do Exterminador", "Uma arma poderosa forjada em fogo antigo", ItemType.Weapon, ItemRarity.Epic, 10);
        bossItem.physicalDamage = 25;
        bossItem.strengthModifier = 5;
        bossQuest.rewards.item = bossItem;
        bossQuest.isMainQuest = true;
        bossQuest.priority = QuestPriority.High;
        bossQuest.category = "Main Story";
        
        // Requirements para boss quest
        bossQuest.requirements.minLevel = 5;
        bossQuest.requirements.requiredCompletedQuests.Add("kill_enemies_01");
        
        availableQuests.Add(bossQuest);
        
        // Quest de delivery
        var deliveryQuest = CreateQuestInstance("delivery_01", "Mensageiro Confiável", 
            "Entregue esta carta importante para o comerciante na cidade vizinha.");
        
        deliveryQuest.objectives.Add(new QuestObjective("Entregar carta para o comerciante", QuestType.DeliverItem, 1, "merchant_01"));
        deliveryQuest.rewards.gold = 75;
        deliveryQuest.rewards.experience = 80;
        deliveryQuest.questGiverNPC = "guard_captain";
        deliveryQuest.turnInNPC = "merchant_01";
        deliveryQuest.category = "Delivery";
        
        availableQuests.Add(deliveryQuest);
        
        Debug.Log($"QuestManager: Criadas {availableQuests.Count} quests padrão");
    }
    
    private Quest CreateQuestInstance(string id, string title, string description)
    {
        var quest = ScriptableObject.CreateInstance<Quest>();
        quest.id = id;
        quest.title = title;
        quest.description = description;
        quest.summary = description.Length > 50 ? description.Substring(0, 47) + "..." : description;
        
        return quest;
    }
    
    private void TryCacheComponents()
    {
        if (!playerCacheValid && PlayerController.Instance != null)
        {
            cachedPlayer = PlayerController.Instance;
            playerCacheValid = true;
        }
        
        if (!gameManagerCacheValid && GameManager.instance != null)
        {
            cachedGameManager = GameManager.instance;
            gameManagerCacheValid = true;
        }
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnEnemyDefeated(EnemyDefeatedEvent eventData)
    {
        string enemyId = eventData.enemyName.ToLower().Replace(" ", "_");
        
        // Atualizar quests do tipo KillEnemies
        UpdateQuestProgress(QuestType.KillEnemies, 1);
        
        // Se for um boss, atualizar também quests do tipo DefeatBoss
        if (eventData.isBoss)
        {
            UpdateQuestProgress(QuestType.DefeatBoss, 1, enemyId);
        }
        
        // Atualizar quests específicas para este inimigo
        UpdateQuestProgress(QuestType.KillEnemies, 1, enemyId);
        
        if (enableQuestNotifications)
        {
            Debug.Log($"Quest progress updated: Enemy {eventData.enemyName} defeated");
        }
    }
    
    private void OnItemCollected(ItemCollectedEvent eventData)
    {
        string itemId = eventData.item.id;
        
        // Atualizar quests do tipo CollectItems
        UpdateQuestProgress(QuestType.CollectItems, 1);
        UpdateQuestProgress(QuestType.CollectItems, 1, itemId);
        
        if (enableQuestNotifications)
        {
            Debug.Log($"Quest progress updated: Item {eventData.item.name} collected");
        }
    }
    
    private void OnPlayerLevelUp(PlayerLevelUpEvent eventData)
    {
        // Verificar se alguma quest foi desbloqueada pelo nível
        CheckLevelBasedQuestUnlocks(eventData.newLevel);
        
        // Atualizar quests do tipo ReachLevel
        UpdateQuestProgress(QuestType.ReachLevel, 1);
    }
    
    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        cachedPlayer = eventData.player.GetComponent<PlayerController>();
        playerCacheValid = true;
    }
    
    private void OnPlayerDestroyed(PlayerDestroyedEvent eventData)
    {
        cachedPlayer = null;
        playerCacheValid = false;
    }
    
    private void OnSceneTransition(SceneTransitionEvent eventData)
    {
        cachedPlayer = null;
        cachedGameManager = null;
        playerCacheValid = false;
        gameManagerCacheValid = false;
        
        // Atualizar quests de exploração
        UpdateQuestProgress(QuestType.ExploreArea, 1, eventData.toScene);
    }
    
    private void OnNPCInteraction(NPCInteractionEvent eventData)
    {
        string npcId = eventData.npc.npcId;
        
        // Atualizar quests do tipo TalkToNPC
        UpdateQuestProgress(QuestType.TalkToNPC, 1, npcId);
        
        // Verificar quests de delivery
        UpdateQuestProgress(QuestType.DeliverItem, 1, npcId);
    }
    
    #endregion
    
    #region Quest Management - Core
    
    public bool AcceptQuest(Quest quest)
    {
        if (quest == null)
        {
            Debug.LogWarning("Tentativa de aceitar quest nula");
            return false;
        }
        
        if (activeQuests.Contains(quest))
        {
            Debug.LogWarning($"Quest {quest.title} já está ativa");
            return false;
        }
        
        if (activeQuests.Count >= maxActiveQuests)
        {
            ShowNotification("Limite de quests ativas atingido!", NotificationType.Warning);
            return false;
        }
        
        // Verificar requirements
        if (!quest.CanPlayerAccept(cachedPlayer))
        {
            var unmetRequirements = quest.GetUnmetRequirements(cachedPlayer);
            string requirementText = string.Join(", ", unmetRequirements);
            ShowNotification($"Requisitos não atendidos: {requirementText}", NotificationType.Warning);
            return false;
        }
        
        // Verificar cooldown para quests repetíveis
        if (quest.isRepeatable && IsQuestOnCooldown(quest.id))
        {
            float remainingCooldown = GetQuestCooldownRemaining(quest.id);
            ShowNotification($"Quest em cooldown. Tempo restante: {FormatTime(remainingCooldown)}", NotificationType.Warning);
            return false;
        }
        
        // Remover das disponíveis se estiver lá
        availableQuests.Remove(quest);
        
        // Criar uma instância da quest para o player
        var questInstance = quest.Clone();
        
        // Iniciar a quest
        if (questInstance.StartQuest())
        {
            activeQuests.Add(questInstance);
            
            // Consumir itens obrigatórios se necessário
            ConsumeRequiredItems(questInstance);
            
            // Subscrever aos eventos da quest
            SubscribeToQuestEvents(questInstance);
            
            // Tocar som e mostrar notificação
            PlayQuestSound(questAcceptedSound);
            ShowNotification($"Quest aceita: {questInstance.title}", NotificationType.Success);
            
            // Disparar eventos
            OnQuestAccepted?.Invoke(questInstance);
            EventManager.TriggerEvent(new QuestAcceptedEvent
            {
                quest = questInstance,
                questGiver = null
            });
            
            Debug.Log($"Quest aceita: {questInstance.title}");
            return true;
        }
        
        return false;
    }
    
    public bool CompleteQuest(Quest quest)
    {
        if (quest == null || !activeQuests.Contains(quest))
        {
            return false;
        }
        
        if (!quest.AreAllObjectivesComplete())
        {
            Debug.LogWarning($"Tentativa de completar quest {quest.title} com objetivos incompletos");
            return false;
        }
        
        // Mover para completadas
        activeQuests.Remove(quest);
        completedQuests.Add(quest);
        
        // Completar a quest
        quest.CompleteQuest();
        
        // Dar recompensas
        GiveQuestRewards(quest);
        
        // Tocar som e mostrar notificação
        PlayQuestSound(questCompletedSound);
        ShowNotification($"Quest completada: {quest.title}!", NotificationType.Success);
        
        // Desbloquear quests dependentes
        UnlockDependentQuests(quest);
        
        // Disparar eventos
        OnQuestCompleted?.Invoke(quest);
        
        Debug.Log($"Quest completada: {quest.title}");
        return true;
    }
    
    public bool TurnInQuest(Quest quest)
    {
        if (quest == null || !completedQuests.Contains(quest))
        {
            return false;
        }
        
        // Mover para entregues
        completedQuests.Remove(quest);
        turnedInQuests.Add(quest);
        
        // Marcar como entregue
        quest.TurnInQuest();
        
        // Tocar som
        PlayQuestSound(quest.turnInSound);
        ShowNotification($"Quest entregue: {quest.title}", NotificationType.Info);
        
        // Configurar cooldown se for repetível
        if (quest.isRepeatable)
        {
            SetQuestCooldown(quest.id, quest.requirements.repeatCooldown);
            IncrementQuestCompletionCount(quest.id);
            
            // Recolocar na lista de disponíveis após cooldown
            if (quest.requirements.repeatCooldown <= 0f)
            {
                availableQuests.Add(quest);
            }
        }
        
        // Disparar eventos
        OnQuestTurnedIn?.Invoke(quest);
        
        Debug.Log($"Quest entregue: {quest.title}");
        return true;
    }
    
    public bool AbandonQuest(Quest quest)
    {
        if (quest == null || !activeQuests.Contains(quest))
        {
            return false;
        }
        
        // Abandonar a quest
        quest.AbandonQuest();
        
        // Remover das ativas
        activeQuests.Remove(quest);
        
        // Recolocar nas disponíveis se não for única
        if (!failedQuests.Contains(quest))
        {
            availableQuests.Add(quest);
        }
        
        // Desinscrever dos eventos
        UnsubscribeFromQuestEvents(quest);
        
        // Mostrar notificação
        ShowNotification($"Quest abandonada: {quest.title}", NotificationType.Warning);
        
        // Disparar eventos
        OnQuestAbandoned?.Invoke(quest);
        
        Debug.Log($"Quest abandonada: {quest.title}");
        return true;
    }
    
    public bool FailQuest(Quest quest, string reason = "")
    {
        if (quest == null || !activeQuests.Contains(quest))
        {
            return false;
        }
        
        // Falhar a quest
        quest.FailQuest();
        
        // Mover para falhadas
        activeQuests.Remove(quest);
        failedQuests.Add(quest);
        
        // Tocar som e mostrar notificação
        PlayQuestSound(questFailedSound);
        string message = string.IsNullOrEmpty(reason) ? 
            $"Quest falhada: {quest.title}" : 
            $"Quest falhada: {quest.title} - {reason}";
        ShowNotification(message, NotificationType.Error);
        
        // Desinscrever dos eventos
        UnsubscribeFromQuestEvents(quest);
        
        // Disparar eventos
        OnQuestFailed?.Invoke(quest);
        
        Debug.Log($"Quest falhada: {quest.title}");
        return true;
    }
    
    #endregion
    
    #region Progress Update
    
    public void UpdateQuestProgress(QuestType type, int amount = 1, string targetId = "")
    {
        var questsToComplete = new List<Quest>();
        
        foreach (Quest quest in activeQuests.ToList()) // ToList para evitar modificação durante iteração
        {
            if (quest.status != QuestStatus.Active) continue;
            
            bool wasUpdated = quest.UpdateObjectiveByType(type, amount, targetId);
            
            if (wasUpdated && quest.autoComplete && quest.AreAllObjectivesComplete())
            {
                questsToComplete.Add(quest);
            }
        }
        
        // Processar quests completadas
        foreach (Quest completedQuest in questsToComplete)
        {
            CompleteQuest(completedQuest);
        }
    }
    
    public void UpdateSpecificQuestObjective(string questId, string objectiveId, int amount = 1)
    {
        var quest = GetActiveQuestById(questId);
        if (quest == null)
        {
            Debug.LogWarning($"Quest ativa não encontrada: {questId}");
            return;
        }
        
        bool wasUpdated = quest.UpdateObjective(objectiveId, amount);
        
        if (wasUpdated && quest.autoComplete && quest.AreAllObjectivesComplete())
        {
            CompleteQuest(quest);
        }
    }
    
    public void ForceCompleteQuest(string questId)
    {
        var quest = GetActiveQuestById(questId);
        if (quest == null)
        {
            Debug.LogWarning($"Quest ativa não encontrada: {questId}");
            return;
        }
        
        // Completar todos os objetivos
        foreach (var objective in quest.objectives)
        {
            objective.currentAmount = objective.requiredAmount;
            objective.isCompleted = true;
        }
        
        CompleteQuest(quest);
    }
    
    #endregion
    
    #region Quest Rewards
    
    private void GiveQuestRewards(Quest quest)
    {
        if (quest?.rewards == null || !playerCacheValid) return;
        
        var rewards = quest.rewards;
        
        // Dar ouro
        if (rewards.gold > 0)
        {
            if (gameManagerCacheValid && cachedGameManager != null)
            {
                cachedGameManager.AddGold(rewards.gold);
                
                EventManager.TriggerEvent(new GoldCollectedEvent
                {
                    amount = rewards.gold,
                    totalGold = cachedGameManager.goldCollected,
                    collectionPosition = cachedPlayer.transform.position
                });
            }
        }
        
        // Dar experiência
        if (rewards.experience > 0)
        {
            cachedPlayer.GainExperience(rewards.experience);
        }
        
        // Dar pontos de atributo
        if (rewards.attributePoints > 0)
        {
            var statsManager = cachedPlayer.GetStatsManager();
            if (statsManager?.Stats != null)
            {
                // Implementar sistema de pontos de atributo extra
                // Por enquanto, mostrar notificação
                ShowNotification($"Ganhou {rewards.attributePoints} pontos de atributo extra!", NotificationType.Success);
            }
        }
        
        // Dar item único
        if (rewards.item != null)
        {
            var inventory = cachedPlayer.GetInventoryManager();
            if (inventory != null)
            {
                bool added = inventory.AddItem(rewards.item);
                if (added)
                {
                    EventManager.TriggerEvent(new ItemCollectedEvent
                    {
                        item = rewards.item,
                        collectionPosition = cachedPlayer.transform.position,
                        collector = cachedPlayer.gameObject
                    });
                }
                else
                {
                    ShowNotification("Inventário cheio! Item de recompensa perdido.", NotificationType.Warning);
                }
            }
        }
        
        // Dar itens múltiplos
        if (rewards.items?.Count > 0)
        {
            var inventory = cachedPlayer.GetInventoryManager();
            if (inventory != null)
            {
                foreach (var item in rewards.items)
                {
                    bool added = inventory.AddItem(item);
                    if (added)
                    {
                        EventManager.TriggerEvent(new ItemCollectedEvent
                        {
                            item = item,
                            collectionPosition = cachedPlayer.transform.position,
                            collector = cachedPlayer.gameObject
                        });
                    }
                }
            }
        }
        
        // Desbloquear quest
        if (rewards.unlockedQuest != null)
        {
            if (!availableQuests.Contains(rewards.unlockedQuest))
            {
                availableQuests.Add(rewards.unlockedQuest);
                ShowNotification($"Nova quest desbloqueada: {rewards.unlockedQuest.title}!", NotificationType.Info);
            }
        }
        
        // Aplicar buffs temporários
        ApplyTemporaryBuffs(rewards);
        
        Debug.Log($"Recompensas da quest '{quest.title}' entregues: {rewards.GetRewardSummary()}");
    }
    
    private void ApplyTemporaryBuffs(QuestReward rewards)
    {
        if (rewards.bonusDuration <= 0f) return;
        
        // Implementar sistema de buffs temporários
        if (rewards.healthBonus > 0f)
        {
            // Aplicar buff de saúde
            Debug.Log($"Buff de saúde aplicado: +{rewards.healthBonus} por {rewards.bonusDuration}s");
        }
        
        if (rewards.manaBonus > 0f)
        {
            // Aplicar buff de mana
            Debug.Log($"Buff de mana aplicado: +{rewards.manaBonus} por {rewards.bonusDuration}s");
        }
        
        if (rewards.experienceMultiplier > 0f)
        {
            // Aplicar multiplicador de experiência
            Debug.Log($"Multiplicador de XP aplicado: +{rewards.experienceMultiplier:P} por {rewards.bonusDuration}s");
        }
    }
    
    private void ConsumeRequiredItems(Quest quest)
    {
        if (!quest.requirements.consumeItems || quest.requirements.requiredItems.Count == 0)
            return;
        
        var inventory = cachedPlayer?.GetInventoryManager();
        if (inventory == null) return;
        
        foreach (var requiredItem in quest.requirements.requiredItems)
        {
            inventory.RemoveItem(requiredItem.id, 1);
        }
    }
    
    #endregion
    
    #region Quest Discovery & Unlocking
    
    private void UnlockDependentQuests(Quest completedQuest)
    {
        var questsToUnlock = new List<Quest>();
        
        // Verificar quests no banco de dados
        foreach (var questTemplate in questDatabase)
        {
            if (questTemplate.requirements.requiredCompletedQuests.Contains(completedQuest.id))
            {
                if (!availableQuests.Any(q => q.id == questTemplate.id) &&
                    !activeQuests.Any(q => q.id == questTemplate.id) &&
                    !completedQuests.Any(q => q.id == questTemplate.id))
                {
                    questsToUnlock.Add(questTemplate);
                }
            }
        }
        
        // Adicionar quests desbloqueadas
        foreach (var quest in questsToUnlock)
        {
            availableQuests.Add(quest);
            ShowNotification($"Nova quest disponível: {quest.title}!", NotificationType.Info);
        }
        
        if (questsToUnlock.Count > 0)
        {
            Debug.Log($"{questsToUnlock.Count} quests desbloqueadas pela conclusão de '{completedQuest.title}'");
        }
    }
    
    private void CheckLevelBasedQuestUnlocks(int playerLevel)
    {
        var questsToUnlock = new List<Quest>();
        
        foreach (var questTemplate in questDatabase)
        {
            // Verificar se a quest pode ser desbloqueada por nível
            if (questTemplate.requirements.minLevel <= playerLevel &&
                questTemplate.requirements.maxLevel >= playerLevel)
            {
                // Verificar se não está já disponível/ativa/completa
                if (!availableQuests.Any(q => q.id == questTemplate.id) &&
                    !activeQuests.Any(q => q.id == questTemplate.id) &&
                    !completedQuests.Any(q => q.id == questTemplate.id))
                {
                    // Verificar outros requirements
                    if (questTemplate.CanPlayerAccept(cachedPlayer))
                    {
                        questsToUnlock.Add(questTemplate);
                    }
                }
            }
        }
        
        foreach (var quest in questsToUnlock)
        {
            availableQuests.Add(quest);
            ShowNotification($"Nova quest desbloqueada pelo nível {playerLevel}: {quest.title}!", NotificationType.Success);
        }
    }
    
    #endregion
    
    #region Cooldown Management
    
    private bool IsQuestOnCooldown(string questId)
    {
        return questCooldowns.ContainsKey(questId) && questCooldowns[questId] > Time.time;
    }
    
    private float GetQuestCooldownRemaining(string questId)
    {
        if (!questCooldowns.ContainsKey(questId)) return 0f;
        return Mathf.Max(0f, questCooldowns[questId] - Time.time);
    }
    
    private void SetQuestCooldown(string questId, float cooldownDuration)
    {
        questCooldowns[questId] = Time.time + cooldownDuration;
    }
    
    private void IncrementQuestCompletionCount(string questId)
    {
        if (questCompletionCounts.ContainsKey(questId))
        {
            questCompletionCounts[questId]++;
        }
        else
        {
            questCompletionCounts[questId] = 1;
        }
    }
    
    public int GetQuestCompletionCount(string questId)
    {
        return questCompletionCounts.ContainsKey(questId) ? questCompletionCounts[questId] : 0;
    }
    
    #endregion
    
    #region Quest Events
    
    private void SubscribeToQuestEvents(Quest quest)
    {
        quest.OnQuestCompleted += HandleQuestCompleted;
        quest.OnQuestFailed += HandleQuestFailed;
        quest.OnObjectiveCompleted += HandleObjectiveCompleted;
    }
    
    private void UnsubscribeFromQuestEvents(Quest quest)
    {
        quest.OnQuestCompleted -= HandleQuestCompleted;
        quest.OnQuestFailed -= HandleQuestFailed;
        quest.OnObjectiveCompleted -= HandleObjectiveCompleted;
    }
    
    private void HandleQuestCompleted(Quest quest)
    {
        // Lógica adicional quando quest é completada
        if (enableQuestNotifications)
        {
            PlayQuestSound(questCompletedSound);
        }
    }
    
    private void HandleQuestFailed(Quest quest)
    {
        // Lógica adicional quando quest falha
        if (enableQuestNotifications)
        {
            PlayQuestSound(questFailedSound);
        }
    }
    
    private void HandleObjectiveCompleted(Quest quest, QuestObjective objective)
    {
        // Lógica adicional quando objetivo é completado
        if (enableQuestNotifications)
        {
            PlayQuestSound(objectiveCompletedSound);
            ShowNotification($"Objetivo completado: {objective.description}", NotificationType.Success);
        }
        
        OnObjectiveCompleted?.Invoke(quest, objective);
    }
    
    #endregion
    
    #region Timer Updates
    
    private void Update()
    {
        UpdateQuestTimers();
        UpdateCooldowns();
        
        // Tentar cachear componentes se ainda não temos
        if (!playerCacheValid || !gameManagerCacheValid)
        {
            TryCacheComponents();
        }
    }
    
    private void UpdateQuestTimers()
    {
        var questsToFail = new List<Quest>();
        
        foreach (var quest in activeQuests)
        {
            if (quest.hasTimeLimit)
            {
                quest.UpdateTimer(Time.deltaTime);
                
                if (quest.status == QuestStatus.Failed)
                {
                    questsToFail.Add(quest);
                }
            }
        }
        
        // Processar quests que falharam por tempo
        foreach (var quest in questsToFail)
        {
            FailQuest(quest, "Tempo esgotado");
        }
    }
    
    private void UpdateCooldowns()
    {
        var questsToReactivate = new List<string>();
        
        foreach (var kvp in questCooldowns.ToList())
        {
            if (kvp.Value <= Time.time)
            {
                questsToReactivate.Add(kvp.Key);
                questCooldowns.Remove(kvp.Key);
            }
        }
        
        // Reativar quests repetíveis que saíram do cooldown
        foreach (var questId in questsToReactivate)
        {
            var questTemplate = questDatabase.FirstOrDefault(q => q.id == questId);
            if (questTemplate != null && questTemplate.isRepeatable)
            {
                if (!availableQuests.Any(q => q.id == questId))
                {
                    availableQuests.Add(questTemplate);
                    ShowNotification($"Quest repetível disponível novamente: {questTemplate.title}", NotificationType.Info);
                }
            }
        }
    }
    
    #endregion
    
    #region Query Methods
    
    public Quest GetQuestByID(string questID)
    {
        // Verificar todas as listas de quests
        foreach (Quest quest in availableQuests)
        {
            if (quest.id == questID)
                return quest;
        }
        
        foreach (Quest quest in activeQuests)
        {
            if (quest.id == questID)
                return quest;
        }
        
        foreach (Quest quest in completedQuests)
        {
            if (quest.id == questID)
                return quest;
        }
        
        foreach (Quest quest in turnedInQuests)
        {
            if (quest.id == questID)
                return quest;
        }
        
        foreach (Quest quest in failedQuests)
        {
            if (quest.id == questID)
                return quest;
        }
        
        return null;
    }
    
    public Quest GetActiveQuestById(string questId)
    {
        return activeQuests.FirstOrDefault(q => q.id == questId);
    }
    
    public List<Quest> GetQuestsByCategory(string category)
    {
        var categoryQuests = new List<Quest>();
        categoryQuests.AddRange(availableQuests.Where(q => q.category == category));
        categoryQuests.AddRange(activeQuests.Where(q => q.category == category));
        return categoryQuests;
    }
    
    public List<Quest> GetQuestsByType(bool mainQuests)
    {
        var typeQuests = new List<Quest>();
        typeQuests.AddRange(availableQuests.Where(q => q.isMainQuest == mainQuests));
        typeQuests.AddRange(activeQuests.Where(q => q.isMainQuest == mainQuests));
        return typeQuests;
    }
    
    public List<Quest> GetQuestsByPriority(QuestPriority priority)
    {
        var priorityQuests = new List<Quest>();
        priorityQuests.AddRange(availableQuests.Where(q => q.priority == priority));
        priorityQuests.AddRange(activeQuests.Where(q => q.priority == priority));
        return priorityQuests;
    }
    
    public List<Quest> GetQuestsForNPC(string npcId)
    {
        var npcQuests = new List<Quest>();
        
        // Quests que este NPC pode dar
        npcQuests.AddRange(availableQuests.Where(q => q.questGiverNPC == npcId));
        
        // Quests que podem ser entregues para este NPC
        npcQuests.AddRange(completedQuests.Where(q => q.turnInNPC == npcId));
        
        // Quests relacionadas a este NPC
        npcQuests.AddRange(activeQuests.Where(q => q.relatedNPCs.Contains(npcId)));
        
        return npcQuests.Distinct().ToList();
    }
    
    public bool IsQuestActive(string questID)
    {
        return activeQuests.Any(q => q.id == questID);
    }
    
    public bool IsQuestCompleted(string questID)
    {
        return completedQuests.Any(q => q.id == questID) || turnedInQuests.Any(q => q.id == questID);
    }
    
    public bool IsQuestAvailable(string questID)
    {
        return availableQuests.Any(q => q.id == questID);
    }
    
    public bool IsQuestTurnedIn(string questID)
    {
        return turnedInQuests.Any(q => q.id == questID);
    }
    
    public float GetQuestProgress(string questID)
    {
        var quest = GetActiveQuestById(questID);
        return quest?.GetOverallProgress() ?? 0f;
    }
    
    #endregion
    
    #region Statistics & Analytics
    
    public QuestStatistics GetQuestStatistics()
    {
        return new QuestStatistics
        {
            totalQuestsAvailable = availableQuests.Count,
            totalQuestsActive = activeQuests.Count,
            totalQuestsCompleted = completedQuests.Count + turnedInQuests.Count,
            totalQuestsFailed = failedQuests.Count,
            mainQuestsCompleted = turnedInQuests.Count(q => q.isMainQuest),
            sideQuestsCompleted = turnedInQuests.Count(q => q.isSideQuest),
            averageQuestProgress = activeQuests.Count > 0 ? activeQuests.Average(q => q.GetOverallProgress()) : 0f,
            questCompletionRate = GetQuestCompletionRate(),
            favoriteCategory = GetMostCompletedCategory(),
            totalRewardsEarned = GetTotalRewardsEarned()
        };
    }
    
    private float GetQuestCompletionRate()
    {
        int totalStarted = activeQuests.Count + completedQuests.Count + turnedInQuests.Count + failedQuests.Count;
        if (totalStarted == 0) return 0f;
        
        int totalCompleted = completedQuests.Count + turnedInQuests.Count;
        return (float)totalCompleted / totalStarted;
    }
    
    private string GetMostCompletedCategory()
    {
        var categoryCount = new Dictionary<string, int>();
        
        foreach (var quest in turnedInQuests)
        {
            if (categoryCount.ContainsKey(quest.category))
                categoryCount[quest.category]++;
            else
                categoryCount[quest.category] = 1;
        }
        
        return categoryCount.Count > 0 ? categoryCount.OrderByDescending(kvp => kvp.Value).First().Key : "None";
    }
    
    private QuestRewards GetTotalRewardsEarned()
    {
        var totalRewards = new QuestRewards();
        
        foreach (var quest in turnedInQuests)
        {
            totalRewards.totalGold += quest.rewards.gold;
            totalRewards.totalExperience += quest.rewards.experience;
            totalRewards.totalAttributePoints += quest.rewards.attributePoints;
            if (quest.rewards.item != null) totalRewards.totalItems++;
            totalRewards.totalItems += quest.rewards.items.Count;
        }
        
        return totalRewards;
    }
    
    [System.Serializable]
    public class QuestStatistics
    {
        public int totalQuestsAvailable;
        public int totalQuestsActive;
        public int totalQuestsCompleted;
        public int totalQuestsFailed;
        public int mainQuestsCompleted;
        public int sideQuestsCompleted;
        public float averageQuestProgress;
        public float questCompletionRate;
        public string favoriteCategory;
        public QuestRewards totalRewardsEarned;
    }
    
    [System.Serializable]
    public class QuestRewards
    {
        public int totalGold;
        public int totalExperience;
        public int totalAttributePoints;
        public int totalItems;
    }
    
    #endregion
    
    #region Utility Methods
    
    private void PlayQuestSound(AudioClip clip)
    {
        if (!enableQuestSounds || audioSource == null || clip == null) return;
        
        audioSource.PlayOneShot(clip);
    }
    
    private void ShowNotification(string message, NotificationType type)
    {
        if (!enableQuestNotifications) return;
        
        EventManager.TriggerEvent(new NotificationEvent
        {
            message = message,
            type = type,
            duration = 3f,
            color = GetNotificationColor(type)
        });
    }
    
    private Color GetNotificationColor(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Success: return Color.green;
            case NotificationType.Warning: return Color.yellow;
            case NotificationType.Error: return Color.red;
            case NotificationType.Info: return Color.cyan;
            default: return Color.white;
        }
    }
    
    private string FormatTime(float seconds)
    {
        if (seconds <= 0f) return "0s";
        
        int hours = Mathf.FloorToInt(seconds / 3600f);
        int minutes = Mathf.FloorToInt((seconds % 3600f) / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        
        if (hours > 0)
            return $"{hours}h {minutes}m {secs}s";
        else if (minutes > 0)
            return $"{minutes}m {secs}s";
        else
            return $"{secs}s";
    }
    
    public void RefreshAvailableQuests()
    {
        // Verificar quests que podem ser desbloqueadas agora
        if (playerCacheValid && cachedPlayer != null)
        {
            CheckLevelBasedQuestUnlocks(cachedPlayer.GetStats()?.Level ?? 1);
        }
    }
    
    #endregion
    
    #region Debug Methods
    
    public void DebugPrintAllQuests()
    {
        Debug.Log("===== TODAS AS QUESTS =====");
        
        Debug.Log($"Quests Disponíveis ({availableQuests.Count}):");
        foreach (Quest quest in availableQuests)
        {
            Debug.Log($"- {quest.id}: {quest.title} ({quest.category}) - {quest.priority}");
        }
        
        Debug.Log($"\nQuests Ativas ({activeQuests.Count}):");
        foreach (Quest quest in activeQuests)
        {
            Debug.Log($"- {quest.id}: {quest.title} - Progresso: {quest.GetOverallProgress():P1}");
            if (quest.hasTimeLimit)
            {
                Debug.Log($"  Tempo restante: {quest.GetTimeRemainingText()}");
            }
        }
        
        Debug.Log($"\nQuests Completadas ({completedQuests.Count}):");
        foreach (Quest quest in completedQuests)
        {
            Debug.Log($"- {quest.id}: {quest.title}");
        }
        
        Debug.Log($"\nQuests Entregues ({turnedInQuests.Count}):");
        foreach (Quest quest in turnedInQuests)
        {
            Debug.Log($"- {quest.id}: {quest.title}");
        }
        
        Debug.Log($"\nQuests Falhadas ({failedQuests.Count}):");
        foreach (Quest quest in failedQuests)
        {
            Debug.Log($"- {quest.id}: {quest.title}");
        }
    }
    
    [ContextMenu("Debug Quest Statistics")]
    public void DebugPrintStatistics()
    {
        var stats = GetQuestStatistics();
        
        Debug.Log("=== ESTATÍSTICAS DE QUEST ===");
        Debug.Log($"Disponíveis: {stats.totalQuestsAvailable}");
        Debug.Log($"Ativas: {stats.totalQuestsActive}");
        Debug.Log($"Completadas: {stats.totalQuestsCompleted}");
        Debug.Log($"Falhadas: {stats.totalQuestsFailed}");
        Debug.Log($"Principais completadas: {stats.mainQuestsCompleted}");
        Debug.Log($"Secundárias completadas: {stats.sideQuestsCompleted}");
        Debug.Log($"Progresso médio: {stats.averageQuestProgress:P1}");
        Debug.Log($"Taxa de conclusão: {stats.questCompletionRate:P1}");
        Debug.Log($"Categoria favorita: {stats.favoriteCategory}");
        Debug.Log($"Recompensas totais:");
        Debug.Log($"  Ouro: {stats.totalRewardsEarned.totalGold}");
        Debug.Log($"  Experiência: {stats.totalRewardsEarned.totalExperience}");
        Debug.Log($"  Pontos de atributo: {stats.totalRewardsEarned.totalAttributePoints}");
        Debug.Log($"  Itens: {stats.totalRewardsEarned.totalItems}");
    }
    
    [ContextMenu("Complete All Active Quests")]
    public void DebugCompleteAllActiveQuests()
    {
        foreach (var quest in activeQuests.ToList())
        {
            ForceCompleteQuest(quest.id);
        }
        Debug.Log("Todas as quests ativas foram completadas");
    }
    
    [ContextMenu("Reset All Quests")]
    public void DebugResetAllQuests()
    {
        // Mover todas as quests de volta para disponíveis
        var allQuests = new List<Quest>();
        allQuests.AddRange(activeQuests);
        allQuests.AddRange(completedQuests);
        allQuests.AddRange(turnedInQuests);
        allQuests.AddRange(failedQuests);
        
        activeQuests.Clear();
        completedQuests.Clear();
        turnedInQuests.Clear();
        failedQuests.Clear();
        
        foreach (var quest in allQuests)
        {
            quest.ResetQuest();
            if (!availableQuests.Contains(quest))
            {
                availableQuests.Add(quest);
            }
        }
        
        questCooldowns.Clear();
        questCompletionCounts.Clear();
        
        Debug.Log("Todas as quests foram resetadas");
    }
    
    #endregion
    
    #region Cleanup
    
    private void OnDestroy()
    {
        // Desinscrever de todos os eventos
        EventManager.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
        EventManager.Unsubscribe<ItemCollectedEvent>(OnItemCollected);
        EventManager.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        EventManager.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        EventManager.Unsubscribe<PlayerDestroyedEvent>(OnPlayerDestroyed);
        EventManager.Unsubscribe<SceneTransitionEvent>(OnSceneTransition);
        EventManager.Unsubscribe<NPCInteractionEvent>(OnNPCInteraction);
        
        // Desinscrever de eventos das quests
        foreach (var quest in activeQuests)
        {
            UnsubscribeFromQuestEvents(quest);
        }
        
        // Limpar singleton
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    #endregion
}