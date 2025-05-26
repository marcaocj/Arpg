using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
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
    
    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        timer = wanderTimer;
        
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
        QuestManager questManager = FindObjectOfType<QuestManager>();
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
}