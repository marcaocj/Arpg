using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

// Definindo o enum fora da classe para evitar problemas de escopo
public enum EnemyState { Patrol, Chase, Attack, Die }

public class Enemy : MonoBehaviour
{
    [Header("Estatísticas")]
    public string enemyName = "Monstro";
    public int level = 1;
    public int health = 50;
    public int maxHealth = 50;
    public int damage = 10;
    public int experienceReward = 20;
    
    [Header("Comportamento")]
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    private float attackTimer = 0f;
    
    [Header("Movimento")]
    public float patrolRadius = 10f;
    public float patrolWaitTime = 3f;
    private Vector3 startPosition;
    private Vector3 randomPatrolPoint;
    private float patrolTimer;
    
    [Header("Componentes")]
    private NavMeshAgent navMeshAgent;
    private Animator animator;
    
    [Header("Estados")]
    public EnemyState currentState = EnemyState.Patrol;
    
    private Transform playerTransform;
    private PlayerController playerController;
    
    [Header("Loot")]
    public LootTable lootTable;
    
    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        startPosition = transform.position;
        randomPatrolPoint = startPosition;
        patrolTimer = patrolWaitTime;
        
        // Ajustar estatísticas baseado no level
        maxHealth = 50 + (level * 10);
        health = maxHealth;
        damage = 10 + (level * 2);
        experienceReward = 20 + (level * 5);
    }
    
    private void Start()
    {
        // Encontrar jogador
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        if (players.Length > 0)
        {
            playerController = players[0];
            playerTransform = playerController.transform;
        }
        
        // Inicializar tabela de loot se necessário
        if (lootTable == null)
        {
            lootTable = ScriptableObject.CreateInstance<LootTable>();
            lootTable.InitializeDefault(level);
        }
    }
    
    private void Update()
    {
        if (health <= 0)
        {
            if (currentState != EnemyState.Die)
            {
                Die();
            }
            return;
        }
        
        // Atualizar timer de ataque
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }
        
        // Lógica de estados
        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                CheckForPlayer();
                break;
                
            case EnemyState.Chase:
                ChasePlayer();
                CheckAttackRange();
                break;
                
            case EnemyState.Attack:
                AttackPlayer();
                break;
        }
    }
    
    private void Patrol()
    {
        if (animator != null)
            animator.SetFloat("Speed", navMeshAgent.velocity.magnitude / navMeshAgent.speed);
        
        // Verificar se chegamos ao ponto de patrulha
        if (navMeshAgent.remainingDistance < 0.5f)
        {
            patrolTimer -= Time.deltaTime;
            
            // Esperar um pouco e então definir um novo ponto de patrulha
            if (patrolTimer <= 0)
            {
                randomPatrolPoint = GetRandomPointInRadius(startPosition, patrolRadius);
                navMeshAgent.SetDestination(randomPatrolPoint);
                patrolTimer = patrolWaitTime;
            }
        }
    }
    
    private Vector3 GetRandomPointInRadius(Vector3 center, float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += center;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        
        return center;
    }
    
    private void CheckForPlayer()
    {
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distanceToPlayer <= detectionRange)
            {
                // Verificar linha de visão
                RaycastHit hit;
                Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
                
                if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out hit, detectionRange))
                {
                    if (hit.transform == playerTransform)
                    {
                        // Jogador detectado!
                        currentState = EnemyState.Chase;
                    }
                }
            }
        }
    }
    
    private void ChasePlayer()
    {
        if (playerTransform != null)
        {
            navMeshAgent.SetDestination(playerTransform.position);
            if (animator != null)
                animator.SetFloat("Speed", navMeshAgent.velocity.magnitude / navMeshAgent.speed);
            
            // Verificar se perdemos o jogador de vista
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer > detectionRange * 1.5f)
            {
                // Voltar a patrulhar
                currentState = EnemyState.Patrol;
                navMeshAgent.SetDestination(randomPatrolPoint);
            }
        }
    }
    
    private void CheckAttackRange()
    {
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distanceToPlayer <= attackRange)
            {
                // Jogador está no alcance de ataque
                currentState = EnemyState.Attack;
                navMeshAgent.ResetPath();
            }
        }
    }
    
    private void AttackPlayer()
    {
        if (playerTransform != null)
        {
            // Olhar para o jogador
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            directionToPlayer.y = 0;
            transform.rotation = Quaternion.LookRotation(directionToPlayer);
            
            // Animar velocidade zero
            if (animator != null)
                animator.SetFloat("Speed", 0);
            
            // Atacar se o cooldown permitir
            if (attackTimer <= 0)
            {
                // Animar ataque
                if (animator != null)
                    animator.SetTrigger("Attack");
                
                // Verificar distância novamente para ter certeza
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distanceToPlayer <= attackRange)
                {
                    // Causar dano ao jogador
                    playerController.TakeDamage(damage);
                    Debug.Log(enemyName + " atacou o jogador por " + damage + " de dano!");
                }
                
                // Reiniciar cooldown
                attackTimer = attackCooldown;
            }
            
            // Verificar se o jogador saiu do alcance
            float currentDistanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (currentDistanceToPlayer > attackRange)
            {
                // Voltar a perseguir
                currentState = EnemyState.Chase;
            }
        }
    }
    // Adicione este método ao script Enemy.cs na classe Enemy
// Coloque logo após o método TakeDamage existente

public void TakeDamage(int amount)
{
    health -= amount;
    
    // Mostrar popup de dano (verifica se o crítico é aproximadamente 25% maior que o dano base)
    bool isCritical = amount > (damage * 1.25f);
    if (DamagePopupManager.Instance != null)
    {
        DamagePopupManager.Instance.ShowDamage(transform.position, amount, isCritical);
    }
    
    // Animar dano
    if (animator != null)
        animator.SetTrigger("Hit");
    
    Debug.Log(enemyName + " tomou " + amount + " de dano! Saúde restante: " + health);
    
    if (health <= 0)
    {
        Die();
    }
    else if (currentState == EnemyState.Patrol)
    {
        // Se estiver patrulhando e tomar dano, começar a perseguir
        currentState = EnemyState.Chase;
    }
}
    
    private void Die()
    {
        // Definir estado
        currentState = EnemyState.Die;
        
        // Animar morte
        if (animator != null)
            animator.SetTrigger("Die");
        
        // Desativar componentes
        navMeshAgent.enabled = false;
        GetComponent<Collider>().enabled = false;
        
        // Dar experiência ao jogador
        if (playerController != null)
        {
            playerController.GainExperience(experienceReward);
        }
        
        // Gerar loot
        DropLoot();
        
        // Destruir após algum tempo
        Destroy(gameObject, 5f);
        
        Debug.Log(enemyName + " foi derrotado!");
		   // Notificar o QuestManager que este inimigo foi morto
    QuestManager questManager = FindObjectOfType<QuestManager>();
    if (questManager != null)
    {
        // Atualizar quests do tipo KillEnemies
        questManager.UpdateQuestProgress(QuestType.KillEnemies, 1);
        
        // Se for um boss, atualizar também quests do tipo DefeatBoss
        bool isBoss = (name.Contains("Boss") || name.Contains("Chief") || name.Contains("Leader"));
        if (isBoss)
        {
            questManager.UpdateQuestProgress(QuestType.DefeatBoss, 1);
        }
    }
    }
    
private void DropLoot()
{
    if (lootTable != null)
    {
        List<Item> droppedItems = lootTable.RollForLoot();
        
        foreach (Item item in droppedItems)
        {
            // Verificar se temos acesso ao prefab de loot
            GameObject lootPrefab = GameManager.instance.lootItemPrefab;
            
            if (lootPrefab == null)
            {
                Debug.LogError("Prefab de loot não configurado no GameManager!");
                return;
            }
            
            // Calcular posição com um pequeno deslocamento aleatório
            Vector3 dropPosition = transform.position;
            dropPosition.y += 0.5f; // Elevar um pouco do chão
            
            // Adicionar um deslocamento horizontal aleatório para espalhar os itens
            float randomX = Random.Range(-0.5f, 0.5f);
            float randomZ = Random.Range(-0.5f, 0.5f);
            dropPosition += new Vector3(randomX, 0, randomZ);
            
            // Instanciar o prefab
            GameObject lootObject = Instantiate(lootPrefab, dropPosition, Quaternion.identity);
            
            // Configurar o item
            LootItem lootComponent = lootObject.GetComponent<LootItem>();
            if (lootComponent != null)
            {
                lootComponent.item = item;
                
                // Configurar cor baseada na raridade (opcional)
                ConfigureLootVisual(lootObject, item);
            }
            
            // Aplicar uma pequena força para fazer o item "pular"
            Rigidbody rb = lootObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(Vector3.up * 2f, ForceMode.Impulse);
            }
            
            Debug.Log("Item dropado: " + item.name);
        }
        
        // Dropar ouro (se implementado)
        DropGold();
    }
}

// Método auxiliar para configurar o visual do loot baseado na raridade
private void ConfigureLootVisual(GameObject lootObject, Item item)
{
    // Encontrar o renderizador
    Renderer renderer = lootObject.GetComponentInChildren<Renderer>();
    if (renderer != null)
    {
        // Definir cor baseada na raridade
        Color itemColor = Color.white; // Comum (padrão)
        
        switch (item.rarity)
        {
            case ItemRarity.Common:
                itemColor = Color.white;
                break;
            case ItemRarity.Uncommon:
                itemColor = Color.green;
                break;
            case ItemRarity.Rare:
                itemColor = Color.blue;
                break;
            case ItemRarity.Epic:
                itemColor = new Color(0.5f, 0, 0.5f); // Roxo
                break;
            case ItemRarity.Legendary:
                itemColor = new Color(1.0f, 0.5f, 0); // Laranja
                break;
        }
        
        // Aplicar a cor
        renderer.material.color = itemColor;
    }
    
    // Configurar luz baseada na raridade (opcional)
    Light light = lootObject.GetComponentInChildren<Light>();
    if (light != null)
    {
        // Mesma cor do material
        light.color = renderer.material.color;
        
        // Intensidade baseada na raridade
        switch (item.rarity)
        {
            case ItemRarity.Common:
                light.intensity = 0.5f;
                break;
            case ItemRarity.Uncommon:
                light.intensity = 0.8f;
                break;
            case ItemRarity.Rare:
                light.intensity = 1.2f;
                break;
            case ItemRarity.Epic:
                light.intensity = 1.5f;
                break;
            case ItemRarity.Legendary:
                light.intensity = 2.0f;
                break;
        }
    }
}

// Método para dropar ouro
private void DropGold()
{
    if (lootTable != null)
    {
        int goldAmount = Mathf.RoundToInt(Random.Range(lootTable.goldMin, lootTable.goldMax));
        
        if (goldAmount > 0 && GameManager.instance != null)
        {
            GameManager.instance.AddGold(goldAmount);
            Debug.Log("Ouro adicionado: " + goldAmount);
            
            // Aqui você pode adicionar um efeito visual ou sonoro
            // Para mostrar que o ouro foi coletado automaticamente
        }
    }
}
}