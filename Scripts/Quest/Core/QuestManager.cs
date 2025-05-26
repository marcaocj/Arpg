using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// QuestManager otimizado - elimina FindObjectOfType, usa EventManager
/// </summary>
public class QuestManager : MonoBehaviour
{
    public List<Quest> availableQuests = new List<Quest>();
    public List<Quest> activeQuests = new List<Quest>();
    public List<Quest> completedQuests = new List<Quest>();
    
    // Cache para evitar FindObjectOfType
    private PlayerController cachedPlayer;
    private GameManager cachedGameManager;
    
    private void Start()
    {
        // Registrar para eventos relacionados a quests
        SubscribeToEvents();
        
        // Inicializar algumas quests básicas
        CreateDefaultQuests();
    }
    
    private void SubscribeToEvents()
    {
        // Registrar para eventos que afetam o progresso das quests
        EventManager.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
        EventManager.Subscribe<ItemCollectedEvent>(OnItemCollected);
        EventManager.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        EventManager.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        EventManager.Subscribe<SceneTransitionEvent>(OnSceneTransition);
    }
    
    private void OnDestroy()
    {
        // Desregistrar eventos
        EventManager.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
        EventManager.Unsubscribe<ItemCollectedEvent>(OnItemCollected);
        EventManager.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        EventManager.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        EventManager.Unsubscribe<SceneTransitionEvent>(OnSceneTransition);
    }
    
    #region Event Handlers
    
    private void OnEnemyDefeated(EnemyDefeatedEvent eventData)
    {
        // Atualizar quests do tipo KillEnemies
        UpdateQuestProgress(QuestType.KillEnemies, 1);
        
        // Se for um boss, atualizar também quests do tipo DefeatBoss
        if (eventData.isBoss)
        {
            UpdateQuestProgress(QuestType.DefeatBoss, 1);
        }
        
        Debug.Log($"Quest progress updated: Enemy {eventData.enemyName} defeated");
    }
    
    private void OnItemCollected(ItemCollectedEvent eventData)
    {
        // Atualizar quests do tipo CollectItems
        UpdateQuestProgress(QuestType.CollectItems, 1);
        
        Debug.Log($"Quest progress updated: Item {eventData.item.name} collected");
    }
    
    private void OnPlayerLevelUp(PlayerLevelUpEvent eventData)
    {
        // Verificar se alguma quest foi desbloqueada pelo nível
        CheckLevelBasedQuestUnlocks(eventData.newLevel);
    }
    
    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        // Cachear referência do player quando ele for criado
        cachedPlayer = eventData.player.GetComponent<PlayerController>();
    }
    
    private void OnSceneTransition(SceneTransitionEvent eventData)
    {
        // Invalidar caches ao trocar de cena
        cachedPlayer = null;
        cachedGameManager = null;
    }
    
    #endregion
    
    private void CreateDefaultQuests()
    {
        // Quest para matar inimigos
        Quest killQuest = new Quest(
            "kill_enemies_01",
            "Limpar o Caminho",
            "Derrote 10 inimigos na zona inicial.",
            QuestType.KillEnemies,
            10
        );
        
        QuestReward killReward = new QuestReward();
        killReward.gold = 50;
        killReward.experience = 100;
        killQuest.rewards.Add(killReward);
        
        // Quest para coletar itens
        Quest collectQuest = new Quest(
            "collect_potions_01",
            "Colecionador",
            "Colete 5 itens quaisquer.",
            QuestType.CollectItems,
            5
        );
        
        QuestReward collectReward = new QuestReward();
        collectReward.gold = 30;
        collectReward.experience = 50;
        Item rewardItem = new Item("Anel de Proteção", "Aumenta a defesa do usuário", ItemType.Gloves, ItemRarity.Uncommon, 1);
        rewardItem.vitalityModifier = 3;
        collectReward.item = rewardItem;
        collectQuest.rewards.Add(collectReward);
        
        // Quest para derrotar um boss
        Quest bossQuest = new Quest(
            "defeat_boss_01",
            "A Ameaça Final",
            "Derrote o Chefe Goblin que está aterrorizando o vilarejo.",
            QuestType.DefeatBoss,
            1
        );
        
        QuestReward bossReward = new QuestReward();
        bossReward.gold = 200;
        bossReward.experience = 500;
        Item bossItem = new Item("Espada do Exterminador", "Uma arma poderosa forjada em fogo antigo", ItemType.Weapon, ItemRarity.Epic, 10);
        bossItem.physicalDamage = 25;
        bossItem.strengthModifier = 5;
        bossReward.item = bossItem;
        bossQuest.rewards.Add(bossReward);
        
        // Adicionar quests à lista
        availableQuests.Add(killQuest);
        availableQuests.Add(collectQuest);
        availableQuests.Add(bossQuest);
    }
    
    public void AcceptQuest(Quest quest)
    {
        if (availableQuests.Contains(quest))
        {
            availableQuests.Remove(quest);
            activeQuests.Add(quest);
            
            // Disparar evento de quest aceita
            EventManager.TriggerEvent(new QuestAcceptedEvent
            {
                quest = quest,
                questGiver = null // ou referência ao NPC se disponível
            });
            
            // Mostrar notificação
            EventManager.TriggerEvent(new NotificationEvent
            {
                message = $"Quest aceita: {quest.title}",
                type = NotificationType.Success,
                duration = 3f,
                color = Color.green
            });
            
            Debug.Log("Quest aceita: " + quest.title);
        }
    }
    
    public void UpdateQuestProgress(QuestType type, int amount = 1)
    {
        List<Quest> questsToComplete = new List<Quest>();
        
        foreach (Quest quest in activeQuests)
        {
            if (quest.type == type && !quest.isCompleted)
            {
                int oldProgress = quest.currentAmount;
                quest.UpdateProgress(amount);
                
                // Disparar evento de progresso atualizado
                EventManager.TriggerEvent(new QuestProgressUpdatedEvent
                {
                    quest = quest,
                    oldProgress = oldProgress,
                    newProgress = quest.currentAmount,
                    requiredAmount = quest.requiredAmount
                });
                
                // Verificar conclusão
                if (quest.isCompleted)
                {
                    questsToComplete.Add(quest);
                }
            }
        }
        
        // Processar quests completadas
        foreach (Quest completedQuest in questsToComplete)
        {
            CompleteQuest(completedQuest);
        }
    }
    
    private void CompleteQuest(Quest quest)
    {
        if (activeQuests.Contains(quest))
        {
            activeQuests.Remove(quest);
            completedQuests.Add(quest);
            
            // Disparar evento de quest completada
            EventManager.TriggerEvent(new QuestCompletedEvent
            {
                quest = quest,
                rewards = quest.rewards
            });
            
            // Dar recompensas
            GiveQuestRewards(quest);
            
            // Mostrar notificação de conclusão
            EventManager.TriggerEvent(new NotificationEvent
            {
                message = $"Quest completada: {quest.title}!",
                type = NotificationType.QuestComplete,
                duration = 4f,
                color = new Color(1f, 0.84f, 0f) // Cor dourada
            });
            
            Debug.Log($"Quest completada: {quest.title}");
        }
    }
    
    private void GiveQuestRewards(Quest quest)
    {
        // Usar cache do player em vez de FindObjectOfType
        PlayerController player = GetCachedPlayer();
        if (player == null) return;
        
        foreach (QuestReward reward in quest.rewards)
        {
            // Dar ouro
            if (reward.gold > 0)
            {
                GameManager gameManager = GetCachedGameManager();
                if (gameManager != null)
                {
                    gameManager.AddGold(reward.gold);
                    
                    EventManager.TriggerEvent(new GoldCollectedEvent
                    {
                        amount = reward.gold,
                        totalGold = gameManager.goldCollected,
                        collectionPosition = player.transform.position
                    });
                }
            }
            
            // Dar experiência
            if (reward.experience > 0)
            {
                player.GainExperience(reward.experience);
            }
            
            // Dar item
            if (reward.item != null)
            {
                bool added = player.inventory.AddItem(reward.item);
                if (added)
                {
                    EventManager.TriggerEvent(new ItemCollectedEvent
                    {
                        item = reward.item,
                        collectionPosition = player.transform.position,
                        collector = player.gameObject
                    });
                }
            }
        }
    }
    
    public void AbandonQuest(Quest quest)
    {
        if (activeQuests.Contains(quest))
        {
            activeQuests.Remove(quest);
            availableQuests.Add(quest);
            
            // Resetar progresso
            quest.currentAmount = 0;
            quest.isCompleted = false;
            
            // Disparar evento de quest abandonada
            EventManager.TriggerEvent(new QuestAbandonedEvent
            {
                quest = quest
            });
            
            // Mostrar notificação
            EventManager.TriggerEvent(new NotificationEvent
            {
                message = $"Quest abandonada: {quest.title}",
                type = NotificationType.Warning,
                duration = 2f,
                color = new Color(1f, 0.5f, 0f) // Cor laranja
            });
            
            Debug.Log("Quest abandonada: " + quest.title);
        }
    }
    
    // Método para buscar quest pelo ID
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
        
        return null;
    }
    
    private void CheckLevelBasedQuestUnlocks(int playerLevel)
    {
        // Verificar se há quests que devem ser desbloqueadas baseadas no nível
        // Exemplo: quests que só aparecem em nível 5+, 10+, etc.
        
        if (playerLevel == 5)
        {
            // Desbloquear quest especial de nível 5
            Quest specialQuest = new Quest(
                "level_5_special",
                "Provação do Aventureiro",
                "Agora que alcançou o nível 5, prove seu valor derrotando 20 inimigos.",
                QuestType.KillEnemies,
                20
            );
            
            QuestReward specialReward = new QuestReward();
            specialReward.experience = 200;
            specialReward.gold = 100;
            specialQuest.rewards.Add(specialReward);
            
            availableQuests.Add(specialQuest);
            
            EventManager.TriggerEvent(new NotificationEvent
            {
                message = "Nova quest desbloqueada!",
                type = NotificationType.Success,
                duration = 3f,
                color = Color.cyan
            });
        }
    }
    
    // Método para verificar se uma quest específica está ativa
    public bool IsQuestActive(string questID)
    {
        foreach (Quest quest in activeQuests)
        {
            if (quest.id == questID)
                return true;
        }
        return false;
    }
    
    // Método para verificar se uma quest específica foi completada
    public bool IsQuestCompleted(string questID)
    {
        foreach (Quest quest in completedQuests)
        {
            if (quest.id == questID)
                return true;
        }
        return false;
    }
    
    // Método para obter progresso de uma quest ativa
    public float GetQuestProgress(string questID)
    {
        foreach (Quest quest in activeQuests)
        {
            if (quest.id == questID)
            {
                return (float)quest.currentAmount / quest.requiredAmount;
            }
        }
        return 0f;
    }
    
    // Método para depuração
    public void DebugPrintAllQuests()
    {
        Debug.Log("===== TODAS AS QUESTS =====");
        
        Debug.Log("Quests Disponíveis:");
        foreach (Quest quest in availableQuests)
        {
            Debug.Log($"- {quest.id}: {quest.title} ({quest.type})");
        }
        
        Debug.Log("Quests Ativas:");
        foreach (Quest quest in activeQuests)
        {
            Debug.Log($"- {quest.id}: {quest.title} - Progresso: {quest.currentAmount}/{quest.requiredAmount}");
        }
        
        Debug.Log("Quests Completadas:");
        foreach (Quest quest in completedQuests)
        {
            Debug.Log($"- {quest.id}: {quest.title}");
        }
    }
    
    #region Cache Management - Elimina FindObjectOfType
    
    private PlayerController GetCachedPlayer()
    {
        if (cachedPlayer == null)
        {
            cachedPlayer = PlayerController.Instance; // Usa singleton em vez de FindObjectOfType
        }
        return cachedPlayer;
    }
    
    private GameManager GetCachedGameManager()
    {
        if (cachedGameManager == null)
        {
            cachedGameManager = GameManager.instance; // Usa singleton em vez de FindObjectOfType
        }
        return cachedGameManager;
    }
    
    #endregion
}