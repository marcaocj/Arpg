using UnityEngine;
using System.Collections.Generic;
using System;

public class QuestManager : MonoBehaviour
{
    public List<Quest> availableQuests = new List<Quest>();
    public List<Quest> activeQuests = new List<Quest>();
    public List<Quest> completedQuests = new List<Quest>();
    
    // Novos eventos para sistemas de UI e gameplay
    public delegate void QuestEventHandler(Quest quest);
    public event QuestEventHandler OnQuestAccepted;
    public event QuestEventHandler OnQuestCompleted;
    public event QuestEventHandler OnQuestProgress;
    public event QuestEventHandler OnQuestAbandoned;
    
    [Header("Settings")]
    public int maxActiveQuests = 10; // Número máximo de quests ativas simultaneamente
    
    [Header("Debug")]
    public bool logQuestEvents = true;
    
    // Singleton pattern (opcional)
    public static QuestManager Instance { get; private set; }
    
    private void Awake()
    {
        // Implementação do Singleton (opcional)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Opcional - se quiser persistir entre cenas
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Inicializar algumas quests básicas
        CreateDefaultQuests();
    }
    
    private void CreateDefaultQuests()
    {
        // Quest para matar inimigos
        Quest killQuest = new Quest(
            "kill_enemies_01", // ID único
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
            "collect_potions_01", // ID único
            "Colecionador",
            "Colete 5 poções de cura.",
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
        
        // Quest para derrotar um boss (nova)
        Quest bossQuest = new Quest(
            "defeat_boss_01", // ID único
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
        
        // NOVO: Quest encadeada (só fica disponível após completar outra)
        Quest chainedQuest = new Quest(
            "chained_quest_01",
            "Desafio do Exterminador",
            "Agora que você derrotou o Chefe Goblin, prove seu valor derrotando 20 inimigos com sua nova espada.",
            QuestType.KillEnemies,
            20
        );
        chainedQuest.prerequisiteQuestId = "defeat_boss_01"; // Só fica disponível após completar a quest do boss
        
        QuestReward chainedReward = new QuestReward();
        chainedReward.gold = 300;
        chainedReward.experience = 800;
        chainedQuest.rewards.Add(chainedReward);
        
        // Adicionar quests à lista
        availableQuests.Add(killQuest);
        availableQuests.Add(collectQuest);
        availableQuests.Add(bossQuest);
        
        // Adicionar quest encadeada apenas quando a pré-requisito for concluída
        if (!IsQuestAvailable(chainedQuest))
        {
            // Armazenar em uma lista separada até que os pré-requisitos sejam atendidos
            AddQuestToHiddenPool(chainedQuest);
        }
        else
        {
            availableQuests.Add(chainedQuest);
        }
    }
    
    // Lista para armazenar quests que ainda não estão disponíveis
    private List<Quest> hiddenQuestPool = new List<Quest>();
    
    private void AddQuestToHiddenPool(Quest quest)
    {
        hiddenQuestPool.Add(quest);
        
        if (logQuestEvents)
            Debug.Log($"Quest '{quest.title}' adicionada ao pool oculto. Será disponibilizada após completar: {quest.prerequisiteQuestId}");
    }
    
    private bool IsQuestAvailable(Quest quest)
    {
        // Se não tem pré-requisito, está sempre disponível
        if (string.IsNullOrEmpty(quest.prerequisiteQuestId))
            return true;
            
        // Verificar se o pré-requisito foi concluído
        foreach (Quest completedQuest in completedQuests)
        {
            if (completedQuest.id == quest.prerequisiteQuestId)
                return true;
        }
        
        return false;
    }
    
    private void CheckForNewlyAvailableQuests()
    {
        List<Quest> newlyAvailable = new List<Quest>();
        
        foreach (Quest hiddenQuest in hiddenQuestPool)
        {
            if (IsQuestAvailable(hiddenQuest))
            {
                newlyAvailable.Add(hiddenQuest);
                availableQuests.Add(hiddenQuest);
                
                if (logQuestEvents)
                    Debug.Log($"Nova quest disponível: {hiddenQuest.title}");
            }
        }
        
        // Remover as quests que se tornaram disponíveis do pool oculto
        foreach (Quest quest in newlyAvailable)
        {
            hiddenQuestPool.Remove(quest);
        }
    }
    
    public void AcceptQuest(Quest quest)
    {
        // Verificar se já atingiu o número máximo de quests ativas
        if (activeQuests.Count >= maxActiveQuests)
        {
            Debug.LogWarning($"Número máximo de quests ativas atingido ({maxActiveQuests})!");
            // Aqui você pode mostrar uma mensagem para o jogador
            return;
        }
        
        if (availableQuests.Contains(quest))
        {
            availableQuests.Remove(quest);
            activeQuests.Add(quest);
            
            // Disparar evento
            OnQuestAccepted?.Invoke(quest);
            
            if (logQuestEvents)
                Debug.Log("Quest aceita: " + quest.title);
        }
    }
    
    public void UpdateQuestProgress(QuestType type, int amount = 1)
    {
        List<Quest> completedQuestsCopy = new List<Quest>();
        
        foreach (Quest quest in activeQuests)
        {
            if (quest.type == type && !quest.isCompleted)
            {
                int previousAmount = quest.currentAmount;
                quest.UpdateProgress(amount);
                
                // Disparar evento de progresso
                OnQuestProgress?.Invoke(quest);
                
                // Verificar conclusão
                if (quest.isCompleted)
                {
                    completedQuestsCopy.Add(quest);
                }
            }
        }
        
        // Processar quests completadas
        foreach (Quest completedQuest in completedQuestsCopy)
        {
            activeQuests.Remove(completedQuest);
            completedQuests.Add(completedQuest);
            
            // Disparar evento
            OnQuestCompleted?.Invoke(completedQuest);
            
            if (logQuestEvents)
                Debug.Log("Quest completada: " + completedQuest.title);
        }
        
        // Verificar se novas quests ficaram disponíveis
        if (completedQuestsCopy.Count > 0)
        {
            CheckForNewlyAvailableQuests();
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
            
            // Disparar evento
            OnQuestAbandoned?.Invoke(quest);
            
            if (logQuestEvents)
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
        
        // Verificar também no pool oculto
        foreach (Quest quest in hiddenQuestPool)
        {
            if (quest.id == questID)
                return quest;
        }
        
        return null; // Não encontrado
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
        
        Debug.Log("Quests Ocultas (com pré-requisitos pendentes):");
        foreach (Quest quest in hiddenQuestPool)
        {
            Debug.Log($"- {quest.id}: {quest.title} - Precisa completar: {quest.prerequisiteQuestId}");
        }
    }
    
    // Método para limpar todas as quests (útil para testes ou reset de jogo)
    public void ResetAllQuests()
    {
        // Copiar todas as quests para uma lista temporária
        List<Quest> allQuests = new List<Quest>();
        allQuests.AddRange(availableQuests);
        allQuests.AddRange(activeQuests);
        allQuests.AddRange(completedQuests);
        allQuests.AddRange(hiddenQuestPool);
        
        // Limpar as listas
        availableQuests.Clear();
        activeQuests.Clear();
        completedQuests.Clear();
        hiddenQuestPool.Clear();
        
        // Resetar cada quest
        foreach (Quest quest in allQuests)
        {
            quest.currentAmount = 0;
            quest.isCompleted = false;
            
            // Determinar onde a quest deve ir inicialmente
            if (string.IsNullOrEmpty(quest.prerequisiteQuestId))
            {
                // Se não tem pré-requisito, vai para disponíveis
                availableQuests.Add(quest);
            }
            else
            {
                // Se tem pré-requisito, vai para o pool oculto
                hiddenQuestPool.Add(quest);
            }
        }
        
        if (logQuestEvents)
            Debug.Log("Todas as quests foram resetadas.");
    }
}