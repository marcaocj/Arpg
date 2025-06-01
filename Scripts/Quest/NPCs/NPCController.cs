using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;
using RPG.UI.Quest;

public enum NPCType
{
    Friendly,
    Merchant,
    QuestGiver,
    Enemy
}

public class NPCController : MonoBehaviour
{
    public string npcName = "NPC";
    public string npcId = ""; // ADICIONADO: ID único do NPC
    public NPCType type = NPCType.Friendly;
    
    [Header("Diálogo")]
    public string greeting = "Olá aventureiro!";
    public List<string> dialogues = new List<string>();
    
    [Header("Mercador")]
    public List<Item> itemsForSale = new List<Item>();
    
    [Header("Quest")]
    public List<Quest> availableQuests = new List<Quest>();
    
    [Header("Quest IDs")]
    public List<string> questIDs = new List<string>(); // IDs das quests para oferecer
    
    [Header("Movimento")]
    public bool canWander = true;
    public float wanderRadius = 5f;
    public float wanderTimer = 10f;
    private float timer;
    private NavMeshAgent navMeshAgent;
    
    [Header("Interação")]
    public float interactionRadius = 3f;
    private bool playerInRange = false;
    private PlayerController player;
    
    // Cache para QuestManager
    private QuestManager questManager;
    
    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        timer = wanderTimer;
        
        // Gerar ID único se não existir
        if (string.IsNullOrEmpty(npcId))
        {
            npcId = $"{npcName}_{gameObject.GetInstanceID()}";
        }
        
        // Cache do QuestManager
        questManager = QuestManager.Instance;
        if (questManager == null)
        {
            questManager = FindObjectOfType<QuestManager>();
        }
        
        // Inicializar alguns itens para venda se for mercador
        if (type == NPCType.Merchant)
        {
            CreateDefaultInventory();
        }
        
        // Inicializar quests do NPC se for um QuestGiver
        if (type == NPCType.QuestGiver)
        {
            LoadQuestsFromIDs();
        }
    }
    
    private void LoadQuestsFromIDs()
    {
        // Limpar lista atual
        availableQuests.Clear();
        
        // Obter quests do QuestManager pelos IDs
        if (questManager != null)
        {
            foreach (string questID in questIDs)
            {
                Quest quest = questManager.GetQuestByID(questID);
                if (quest != null)
                {
                    availableQuests.Add(quest);
                }
            }
        }
    }
    
    private void Update()
    {
        // Gerenciar movimento
        if (canWander && navMeshAgent != null)
        {
            timer -= Time.deltaTime;
            
            if (timer <= 0)
            {
                Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
                navMeshAgent.SetDestination(newPos);
                timer = wanderTimer;
            }
        }
        
        // Verificar interação do jogador
        if (playerInRange && Input.GetKeyDown(KeyCode.F))
        {
            Interact();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        PlayerController playerComponent = other.GetComponent<PlayerController>();
        if (playerComponent != null)
        {
            playerInRange = true;
            player = playerComponent;
            
            // Mostrar dica de interação
            Debug.Log("Pressione F para interagir com " + npcName);
            
            // Disparar evento de interação
            EventManager.TriggerEvent(new NPCInteractionEvent
            {
                npc = this,
                player = other.gameObject,
                interactionType = type
            });
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        PlayerController playerComponent = other.GetComponent<PlayerController>();
        if (playerComponent != null)
        {
            playerInRange = false;
            player = null;
        }
    }
    
    private void Interact()
    {
        switch (type)
        {
            case NPCType.Friendly:
                SayRandomDialogue();
                break;
                
            case NPCType.Merchant:
                OpenShop();
                break;
                
            case NPCType.QuestGiver:
                // Verificar se temos UI de quests
                QuestUI questUI = FindObjectOfType<QuestUI>();
                if (questUI != null)
                {
                    questUI.ShowQuestDialog(this);
                }
                else
                {
                    ShowQuests(); // Fallback para o método antigo
                }
                break;
        }
    }
    
    private void SayRandomDialogue()
    {
        if (dialogues.Count > 0)
        {
            int index = Random.Range(0, dialogues.Count);
            Debug.Log(npcName + ": " + dialogues[index]);
        }
        else
        {
            Debug.Log(npcName + ": " + greeting);
        }
    }
    
    private void OpenShop()
    {
        Debug.Log(npcName + ": " + "Bem-vindo à minha loja, aventureiro!");
        
        // Abrir UI da loja (a ser implementado)
        // ShopUI.instance.OpenShop(this);
    }
    
    private void ShowQuests()
    {
        Debug.Log(npcName + ": " + "Tenho algumas tarefas para você, aventureiro!");
        
        // Atualizar a lista de quests disponíveis
        LoadQuestsFromIDs();
        
        // Mostrar quests disponíveis via console (para debug)
        Debug.Log("Quests disponíveis:");
        foreach (Quest quest in availableQuests)
        {
            Debug.Log("- " + quest.title + ": " + quest.description);
        }
        
        // Abrir UI de quests (a ser implementado)
        // QuestUI.instance.ShowQuests(this);
    }
    
    // NOVOS MÉTODOS PARA INTEGRAÇÃO COM QUEST SYSTEM
    
    /// <summary>
    /// Retorna quests disponíveis que o player pode aceitar
    /// </summary>
    public List<Quest> GetAvailableQuestsForPlayer()
    {
        if (questManager == null || player == null) 
            return new List<Quest>();
        
        var playerAvailableQuests = new List<Quest>();
        
        // Verificar quests disponíveis no QuestManager
        foreach (var quest in questManager.availableQuests)
        {
            // Verificar se este NPC pode dar esta quest
            if (quest.questGiverNPC == npcId || questIDs.Contains(quest.id))
            {
                // Verificar se o player pode aceitar esta quest
                if (quest.CanPlayerAccept(player))
                {
                    playerAvailableQuests.Add(quest);
                }
            }
        }
        
        return playerAvailableQuests;
    }
    
    /// <summary>
    /// Retorna quests que podem ser entregues para este NPC
    /// </summary>
    public List<Quest> GetQuestsToTurnInForPlayer()
    {
        if (questManager == null) 
            return new List<Quest>();
        
        var questsToTurnIn = new List<Quest>();
        
        // Verificar quests completadas que podem ser entregues para este NPC
        foreach (var quest in questManager.completedQuests)
        {
            if (quest.turnInNPC == npcId || quest.questGiverNPC == npcId)
            {
                questsToTurnIn.Add(quest);
            }
        }
        
        return questsToTurnIn;
    }
    
    /// <summary>
    /// Callback quando uma quest é aceita através do diálogo
    /// </summary>
    public void AcceptQuestFromDialog(Quest quest)
    {
        if (quest == null) return;
        
        Debug.Log($"{npcName}: Excelente! Boa sorte com '{quest.title}'!");
        
        // Remover da lista de quests disponíveis do NPC
        availableQuests.Remove(quest);
        
        // Pode adicionar lógica específica do NPC aqui
        // Como mudar expressão, tocar som, etc.
    }
    
    /// <summary>
    /// Callback quando uma quest é entregue através do diálogo
    /// </summary>
    public void TurnInQuestFromDialog(Quest quest)
    {
        if (quest == null) return;
        
        Debug.Log($"{npcName}: Muito bem! Você completou '{quest.title}'!");
        
        // Pode adicionar lógica específica do NPC aqui
        // Como dar recompensas extras, mudar diálogo, etc.
        
        // Verificar se há quests subsequentes para desbloquear
        CheckForFollowUpQuests(quest);
    }
    
    /// <summary>
    /// Verifica se há quests subsequentes para desbloquear após completar uma quest
    /// </summary>
    private void CheckForFollowUpQuests(Quest completedQuest)
    {
        if (questManager == null) return;
        
        // Verificar se há quests no banco de dados que requerem esta quest
        foreach (var quest in questManager.questDatabase)
        {
            if (quest.requirements.requiredCompletedQuests.Contains(completedQuest.id))
            {
                // Se este NPC pode dar esta quest, adicionar às disponíveis
                if (quest.questGiverNPC == npcId && !questManager.availableQuests.Contains(quest))
                {
                    questManager.availableQuests.Add(quest);
                    availableQuests.Add(quest);
                    
                    Debug.Log($"{npcName}: Ah, agora tenho uma nova tarefa para você: '{quest.title}'!");
                }
            }
        }
    }
    
    /// <summary>
    /// Verifica se o NPC tem quests disponíveis ou para entregar
    /// </summary>
    public bool HasQuestsForPlayer()
    {
        return GetAvailableQuestsForPlayer().Count > 0 || GetQuestsToTurnInForPlayer().Count > 0;
    }
    
    /// <summary>
    /// Retorna o número total de quests que o player pode interagir com este NPC
    /// </summary>
    public int GetTotalQuestCount()
    {
        return GetAvailableQuestsForPlayer().Count + GetQuestsToTurnInForPlayer().Count;
    }
    
    /// <summary>
    /// Adiciona uma nova quest à lista de quests que este NPC pode oferecer
    /// </summary>
    public void AddQuest(Quest quest)
    {
        if (quest != null && !availableQuests.Contains(quest))
        {
            availableQuests.Add(quest);
            if (!questIDs.Contains(quest.id))
            {
                questIDs.Add(quest.id);
            }
        }
    }
    
    /// <summary>
    /// Remove uma quest da lista de quests do NPC
    /// </summary>
    public void RemoveQuest(Quest quest)
    {
        if (quest != null)
        {
            availableQuests.Remove(quest);
            questIDs.Remove(quest.id);
        }
    }
    
    /// <summary>
    /// Atualiza a lista de quests baseada nos IDs configurados
    /// </summary>
    public void RefreshQuests()
    {
        LoadQuestsFromIDs();
    }
    
    private void CreateDefaultInventory()
    {
        // Criar alguns itens básicos para vender
        Item healthPotion = new Item("Poção de Vida", "Recupera 50 pontos de vida", ItemType.Consumable, ItemRarity.Common, 1);
        healthPotion.healthRestore = 50;
        itemsForSale.Add(healthPotion);
        
        Item manaPotion = new Item("Poção de Mana", "Recupera 30 pontos de mana", ItemType.Consumable, ItemRarity.Common, 1);
        manaPotion.manaRestore = 30;
        itemsForSale.Add(manaPotion);
        
        Item sword = new Item("Espada Longa", "Uma espada bem balanceada", ItemType.Weapon, ItemRarity.Uncommon, 5);
        sword.physicalDamage = 15;
        sword.strengthModifier = 2;
        itemsForSale.Add(sword);
        
        Item helmet = new Item("Elmo de Aço", "Proteção robusta para a cabeça", ItemType.Helmet, ItemRarity.Uncommon, 5);
        helmet.vitalityModifier = 3;
        itemsForSale.Add(helmet);
    }
    
    public static Vector3 RandomNavSphere(Vector3 origin, float distance, int layerMask)
    {
        Vector3 randomDirection = Random.insideUnitSphere * distance;
        randomDirection += origin;
        
        NavMeshHit navHit;
        NavMesh.SamplePosition(randomDirection, out navHit, distance, layerMask);
        
        return navHit.position;
    }
    
    #region Debug
    
    [ContextMenu("Debug NPC Quests")]
    public void DebugNPCQuests()
    {
        Debug.Log($"=== {npcName} ({npcId}) ===");
        Debug.Log($"Tipo: {type}");
        
        var availableForPlayer = GetAvailableQuestsForPlayer();
        Debug.Log($"Quests disponíveis para o player ({availableForPlayer.Count}):");
        foreach (var quest in availableForPlayer)
        {
            Debug.Log($"  - {quest.title} (ID: {quest.id})");
        }
        
        var questsToTurnIn = GetQuestsToTurnInForPlayer();
        Debug.Log($"Quests para entregar ({questsToTurnIn.Count}):");
        foreach (var quest in questsToTurnIn)
        {
            Debug.Log($"  - {quest.title} (ID: {quest.id})");
        }
        
        Debug.Log($"Quest IDs configurados ({questIDs.Count}):");
        foreach (var questId in questIDs)
        {
            Debug.Log($"  - {questId}");
        }
    }
    
    #endregion
}
