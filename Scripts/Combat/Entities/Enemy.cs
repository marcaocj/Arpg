using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public enum EnemyState { Patrol, Chase, Attack, Die }

/// <summary>
/// Enemy otimizado - elimina FindObjectOfType, usa EventManager e cache
/// </summary>
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
    private EnemyState previousState;
    
    // Cache para evitar FindObjectOfType
    private Transform playerTransform;
    private PlayerController playerController;
    private bool playerCacheValid = false;
    
    [Header("Loot")]
    public LootTable lootTable;
    
    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        startPosition = transform.position;
        randomPatrolPoint = startPosition;
        patrolTimer = patrolWaitTime;
        previousState = currentState;
        
        // Ajustar estatísticas baseado no level
        maxHealth = 50 + (level * 10);
        health = maxHealth;
        damage = 10 + (level * 2);
        experienceReward = 20 + (level * 5);
    }
    
    private void Start()
    {
        // Subscrever a eventos para detectar player
        SubscribeToEvents();
        
        // Tentar encontrar player imediatamente
        TryCachePlayer();
        
        // Inicializar tabela de loot se necessário
        if (lootTable == null)
        {
            lootTable = ScriptableObject.CreateInstance<LootTable>();
            lootTable.InitializeDefault(level);
        }
    }
    
    private void SubscribeToEvents()
    {
        // Subscrever a eventos do player para cache
        EventManager.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        EventManager.Subscribe<PlayerDestroyedEvent>(OnPlayerDestroyed);
        EventManager.Subscribe<SceneTransitionEvent>(OnSceneTransition);
    }
    
    private void OnDestroy()
    {
        // Desinscrever de eventos
        EventManager.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        EventManager.Unsubscribe<PlayerDestroyedEvent>(OnPlayerDestroyed);
        EventManager.Unsubscribe<SceneTransitionEvent>(OnSceneTransition);
    }
    
    #region Event Handlers
    
    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        // Cachear player quando ele for criado
        playerController = eventData.player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerTransform = playerController.transform;
            playerCacheValid = true;
        }
    }
    
    private void OnPlayerDestroyed(PlayerDestroyedEvent eventData)
    {
        // Invalidar cache quando player for destruído
        playerController = null;
        playerTransform = null;
        playerCacheValid = false;
    }
    
    private void OnSceneTransition(SceneTransitionEvent eventData)
    {
        // Invalidar cache ao trocar de cena
        playerController = null;
        playerTransform = null;
        playerCacheValid = false;
    }
    
    #endregion
    
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
        
        // Verificar mudança de estado
        if (currentState != previousState)
        {
            OnStateChanged();
            previousState = currentState;
        }
        
        // Atualizar timer de ataque
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }
        
        // Tentar cachear player se ainda não temos
        if (!playerCacheValid)
        {
            TryCachePlayer();
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
    
    private void TryCachePlayer()
    {
        // Usar singleton em vez de FindObjectOfType
        if (PlayerController.Instance != null)
        {
            playerController = PlayerController.Instance;
            playerTransform = playerController.transform;
            playerCacheValid = true;
        }
    }
    
    private void OnStateChanged()
    {
        EventManager.TriggerEvent(new EnemyStateChangedEvent
        {
            enemy = gameObject,
            oldState = previousState,
            newState = currentState
        });
        
        Debug.Log($"{enemyName} mudou estado: {previousState} -> {currentState}");
    }
    
    private void Patrol()
    {
        if (animator != null)
            animator.SetFloat("Speed", navMeshAgent.velocity.magnitude / navMeshAgent.speed);
        
        if (navMeshAgent.remainingDistance < 0.5f)
        {
            patrolTimer -= Time.deltaTime;
            
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
        if (playerTransform != null && playerCacheValid)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distanceToPlayer <= detectionRange)
            {
                RaycastHit hit;
                Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
                
                if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out hit, detectionRange))
                {
                    if (hit.transform == playerTransform)
                    {
                        currentState = EnemyState.Chase;
                    }
                }
            }
        }
    }
    
    private void ChasePlayer()
    {
        if (playerTransform != null && playerCacheValid)
        {
            navMeshAgent.SetDestination(playerTransform.position);
            if (animator != null)
                animator.SetFloat("Speed", navMeshAgent.velocity.magnitude / navMeshAgent.speed);
            
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer > detectionRange * 1.5f)
            {
                currentState = EnemyState.Patrol;
                navMeshAgent.SetDestination(randomPatrolPoint);
            }
        }
    }
    
    private void CheckAttackRange()
    {
        if (playerTransform != null && playerCacheValid)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distanceToPlayer <= attackRange)
            {
                currentState = EnemyState.Attack;
                navMeshAgent.ResetPath();
            }
        }
    }
    
    private void AttackPlayer()
    {
        if (playerTransform != null && playerCacheValid)
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
                    // Disparar evento de dano causado
                    EventManager.TriggerEvent(new DamageDealtEvent
                    {
                        attacker = gameObject,
                        target = playerController.gameObject,
                        damage = damage,
                        isCritical = false,
                        damageType = DamageType.Physical,
                        hitPosition = playerTransform.position
                    });
                    
                    // Causar dano ao jogador usando cache
                    if (playerController != null)
                    {
                        playerController.TakeDamage(damage);
                        Debug.Log(enemyName + " atacou o jogador por " + damage + " de dano!");
                    }
                }
                
                // Reiniciar cooldown
                attackTimer = attackCooldown;
            }
            
            // Verificar se o jogador saiu do alcance
            float currentDistanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (currentDistanceToPlayer > attackRange)
            {
                currentState = EnemyState.Chase;
            }
        }
    }
    
    public void TakeDamage(int amount)
    {
        health -= amount;
        
        // Verificar se é crítico (aproximadamente 25% maior que o dano base)
        bool isCritical = amount > (damage * 1.25f);
        
        // Disparar evento de dano recebido
        EventManager.TriggerEvent(new EnemyTakeDamageEvent
        {
            enemy = gameObject,
            damage = amount,
            isCritical = isCritical,
            remainingHealth = health
        });
        
        // Solicitar popup de dano através do EventManager - NO INIMIGO, NÃO NO PLAYER!
        EventManager.TriggerEvent(new DamagePopupRequestEvent
        {
            worldPosition = transform.position, // Posição DO INIMIGO
            amount = amount,
            isCritical = isCritical,
            isHeal = false,
            customColor = isCritical ? Color.red : Color.white
        });
        
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
        
        // Disparar evento de morte
        bool isBoss = (name.Contains("Boss") || name.Contains("Chief") || name.Contains("Leader"));
        
        EventManager.TriggerEvent(new EnemyDefeatedEvent
        {
            enemy = gameObject,
            enemyName = enemyName,
            enemyLevel = level,
            deathPosition = transform.position,
            experienceReward = experienceReward,
            isBoss = isBoss
        });
        
        // Dar experiência ao jogador através do evento - usando cache
        if (playerController != null)
        {
            playerController.GainExperience(experienceReward);
        }
        
        // Gerar loot
        DropLoot();
        
        // Destruir após algum tempo
        Destroy(gameObject, 5f);
        
        Debug.Log(enemyName + " foi derrotado!");
    }
    
    private void DropLoot()
    {
        if (lootTable != null)
        {
            List<Item> droppedItems = lootTable.RollForLoot();
            
            // Dropar ouro primeiro
            int goldAmount = Mathf.RoundToInt(Random.Range(lootTable.goldMin, lootTable.goldMax));
            if (goldAmount > 0)
            {
                // Usar cache do GameManager em vez de FindObjectOfType
                GameManager gameManager = GameManager.instance;
                int totalGold = gameManager?.goldCollected ?? 0;
                
                EventManager.TriggerEvent(new GoldCollectedEvent
                {
                    amount = goldAmount,
                    totalGold = totalGold + goldAmount,
                    collectionPosition = transform.position
                });
                
                if (gameManager != null)
                {
                    gameManager.AddGold(goldAmount);
                }
            }
            
            // Disparar evento de loot dropado
            if (droppedItems.Count > 0)
            {
                EventManager.TriggerEvent(new LootDroppedEvent
                {
                    items = droppedItems,
                    goldAmount = goldAmount,
                    dropPosition = transform.position,
                    source = gameObject
                });
            }
            
            // Instanciar itens físicos
            foreach (Item item in droppedItems)
            {
                CreateLootItem(item);
            }
        }
    }
    
    private void CreateLootItem(Item item)
    {
        // Verificar se temos acesso ao prefab de loot usando cache
        GameManager gameManager = GameManager.instance;
        GameObject lootPrefab = gameManager?.lootItemPrefab;
        
        if (lootPrefab == null)
        {
            Debug.LogError("Prefab de loot não configurado no GameManager!");
            return;
        }
        
        // Calcular posição com um pequeno deslocamento aleatório
        Vector3 dropPosition = transform.position;
        dropPosition.y += 0.3f; // Reduzido de 1f para 0.3f - altura inicial menor
        
        // Adicionar um deslocamento horizontal aleatório para espalhar os itens
        float randomX = Random.Range(-0.3f, 0.3f); // Reduzido de 0.5f para 0.3f
        float randomZ = Random.Range(-0.3f, 0.3f); // Reduzido de 0.5f para 0.3f
        dropPosition += new Vector3(randomX, 0, randomZ);
        
        // Instanciar o prefab
        GameObject lootObject = Instantiate(lootPrefab, dropPosition, Quaternion.identity);
        
        // Configurar o item
        LootItem lootComponent = lootObject.GetComponent<LootItem>();
        if (lootComponent != null)
        {
            lootComponent.item = item;
            
            // Configurar cor baseada na raridade
            ConfigureLootVisual(lootObject, item);
        }
        
        // Configurar Rigidbody para evitar que atravesse o terreno
        Rigidbody rb = lootObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.mass = 1f; // Reduzido de 2f para 1f
            rb.drag = 0.5f; // Reduzido de 1f para 0.5f
            rb.angularDrag = 0.5f; // Reduzido de 1f para 0.5f
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.maxAngularVelocity = 0.3f; // Reduzido de 0.5f para 0.3f
            
            // Aplicar uma pequena força para fazer o item "pular"
            rb.AddForce(Vector3.up * 1.5f, ForceMode.Impulse); // Reduzido de 3f para 1.5f
        }
        
        // Garantir que o item tem um collider adequado
        Collider collider = lootObject.GetComponent<Collider>();
        if (collider == null)
        {
            // Adicionar um BoxCollider se não existir
            BoxCollider boxCollider = lootObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(0.5f, 0.5f, 0.5f);
            boxCollider.center = new Vector3(0, 0.15f, 0); // Reduzido de 0.25f para 0.15f
            boxCollider.material = new PhysicMaterial("LootMaterial") 
            { 
                dynamicFriction = 0.4f, // Reduzido de 0.6f para 0.4f
                staticFriction = 0.4f, // Reduzido de 0.6f para 0.4f
                bounciness = 0.05f, // Reduzido de 0.1f para 0.05f
                frictionCombine = PhysicMaterialCombine.Maximum,
                bounceCombine = PhysicMaterialCombine.Minimum
            };
        }
        
        // Adicionar um script para garantir que o item pare quando atingir o chão
        StartCoroutine(EnsureItemGrounded(lootObject));
        
        Debug.Log("Item dropado: " + item.name);
    }
    
    private IEnumerator EnsureItemGrounded(GameObject lootObject)
    {
        if (lootObject == null) yield break;
        
        Rigidbody rb = lootObject.GetComponent<Rigidbody>();
        if (rb == null) yield break;
        
        // Esperar um pouco para a física inicial se estabilizar
        yield return new WaitForSeconds(0.5f);
        
        // Verificar se o item está muito abaixo do terreno
        RaycastHit hit;
        if (Physics.Raycast(lootObject.transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 1f))
        {
            // Se estiver abaixo do terreno, reposicionar
            lootObject.transform.position = hit.point + Vector3.up * 0.25f;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Esperar mais um pouco e verificar novamente
        yield return new WaitForSeconds(1f);
        
        if (lootObject != null && rb != null)
        {
            // Se o item ainda estiver se movendo muito, forçar parada
            if (rb.velocity.magnitude > 0.1f)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
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
            Color itemColor = GetRarityColor(item.rarity);
            
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
    
    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return Color.white;
            case ItemRarity.Uncommon:
                return Color.green;
            case ItemRarity.Rare:
                return Color.blue;
            case ItemRarity.Epic:
                return new Color(0.5f, 0, 0.5f); // Roxo
            case ItemRarity.Legendary:
                return new Color(1.0f, 0.5f, 0); // Laranja
            default:
                return Color.white;
        }
    }
}