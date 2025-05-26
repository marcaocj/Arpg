using UnityEngine;
using System.Collections;

/// <summary>
/// LootItem otimizado - elimina FindObjectOfType, usa cache e EventManager
/// </summary>
public class LootItem : MonoBehaviour
{
    [Header("Item")]
    public Item item;
    
    [Header("Animação")]
    public float rotationSpeed = 100f;
    public float bobHeight = 0.2f;
    public float bobSpeed = 2f;
    public float attractSpeed = 5f;
    public float pickupDistance = 1.5f;
    
    [Header("Efeitos")]
    public GameObject pickupEffectPrefab; // Opcional - um efeito para quando o item for coletado
    
    // Variáveis internas
    private Vector3 startPosition;
    private Rigidbody rb;
    private bool isAttracting = false;
    
    // Cache para evitar FindObjectOfType
    private Transform playerTransform;
    private PlayerController playerController;
    private bool playerCacheValid = false;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        
        // Subscrever a eventos para cache do player
        SubscribeToEvents();
        
        // Tentar cachear player imediatamente
        TryCachePlayer();
        
        // Desativar física após o "pulo" inicial
        StartCoroutine(DisablePhysicsAfterDelay(0.5f));
    }
    
    private void SubscribeToEvents()
    {
        EventManager.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        EventManager.Subscribe<PlayerDestroyedEvent>(OnPlayerDestroyed);
        EventManager.Subscribe<SceneTransitionEvent>(OnSceneTransition);
    }
    
    private void OnDestroy()
    {
        EventManager.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        EventManager.Unsubscribe<PlayerDestroyedEvent>(OnPlayerDestroyed);
        EventManager.Unsubscribe<SceneTransitionEvent>(OnSceneTransition);
    }
    
    #region Event Handlers
    
    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        playerController = eventData.player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerTransform = playerController.transform;
            playerCacheValid = true;
        }
    }
    
    private void OnPlayerDestroyed(PlayerDestroyedEvent eventData)
    {
        playerController = null;
        playerTransform = null;
        playerCacheValid = false;
        isAttracting = false;
    }
    
    private void OnSceneTransition(SceneTransitionEvent eventData)
    {
        playerController = null;
        playerTransform = null;
        playerCacheValid = false;
        isAttracting = false;
    }
    
    #endregion
    
    private IEnumerator DisablePhysicsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
    
    private void Update()
    {
        // Rotação constante
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        // Movimento de flutuação (bob)
        if (!isAttracting && rb != null && rb.isKinematic)
        {
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = startPosition + new Vector3(0, yOffset, 0);
        }
        
        // Tentar cachear player se não temos
        if (!playerCacheValid)
        {
            TryCachePlayer();
        }
        
        // Verificar se o jogador está próximo para atração automática
        if (!isAttracting)
        {
            CheckPlayerProximity();
        }
        else
        {
            // Mover em direção ao jogador
            MoveTowardsPlayer();
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
    
    private void CheckPlayerProximity()
    {
        if (playerTransform != null && playerCacheValid)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distance < pickupDistance)
            {
                isAttracting = true;
                
                // Desabilitar o collider para evitar coleta prematura
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = false;
                }
            }
        }
    }
    
    private void MoveTowardsPlayer()
    {
        if (playerTransform != null && playerCacheValid)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            transform.position += direction * attractSpeed * Time.deltaTime;
            
            // Verificar se chegou próximo o suficiente para coleta
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distance < 0.5f)
            {
                CollectItem();
            }
        }
    }
    
    private void CollectItem()
    {
        if (playerController != null && playerCacheValid)
        {
            bool added = playerController.inventory.AddItem(item);
            if (added)
            {
                // Disparar evento de coleta através do EventManager
                EventManager.TriggerEvent(new ItemCollectedEvent
                {
                    item = item,
                    collectionPosition = transform.position,
                    collector = playerController.gameObject
                });
                
                // Mostrar efeito de coleta
                if (pickupEffectPrefab != null)
                {
                    Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
                }
                
                // Tocar um som de coleta através do EventManager (futuro)
                // EventManager.TriggerEvent(new AudioRequestEvent
                // {
                //     audioClipName = "ItemPickup",
                //     position = transform.position,
                //     volume = 1f,
                //     is3D = true
                // });
                
                // Destruir o objeto
                Destroy(gameObject);
            }
            else
            {
                // Inventário cheio - restaurar a colisão e parar de atrair
                isAttracting = false;
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = true;
                }
                
                // O evento de inventário cheio já é disparado pelo Inventory.cs
                Debug.Log("Inventário cheio!");
            }
        }
    }
    
    // Isso ainda é útil se o jogador coletar o item antes da atração
    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && !isAttracting)
        {
            bool added = player.inventory.AddItem(item);
            if (added)
            {
                // Disparar evento de coleta através do EventManager
                EventManager.TriggerEvent(new ItemCollectedEvent
                {
                    item = item,
                    collectionPosition = transform.position,
                    collector = player.gameObject
                });
                
                // Mostrar efeito de coleta
                if (pickupEffectPrefab != null)
                {
                    Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
                }
                
                // Destruir o objeto
                Destroy(gameObject);
            }
            else
            {
                // O evento de inventário cheio já é disparado pelo Inventory.cs
                Debug.Log("Inventário cheio!");
            }
        }
    }
    
    // Método para configurar o item externamente
    public void SetItem(Item newItem)
    {
        item = newItem;
        
        // Configurar visual baseado na raridade
        ConfigureVisualBasedOnRarity();
    }
    
    private void ConfigureVisualBasedOnRarity()
    {
        if (item == null) return;
        
        // Configurar cor baseada na raridade
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = GetRarityColor(item.rarity);
        }
        
        // Configurar luz baseada na raridade
        Light light = GetComponentInChildren<Light>();
        if (light != null)
        {
            light.color = GetRarityColor(item.rarity);
            light.intensity = GetRarityLightIntensity(item.rarity);
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
                return new Color(0.5f, 0f, 0.5f); // Roxo
            case ItemRarity.Legendary:
                return new Color(1f, 0.5f, 0f); // Laranja
            default:
                return Color.white;
        }
    }
    
    private float GetRarityLightIntensity(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return 0.5f;
            case ItemRarity.Uncommon:
                return 0.8f;
            case ItemRarity.Rare:
                return 1.2f;
            case ItemRarity.Epic:
                return 1.5f;
            case ItemRarity.Legendary:
                return 2.0f;
            default:
                return 0.5f;
        }
    }
}