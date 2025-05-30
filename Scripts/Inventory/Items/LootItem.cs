using UnityEngine;
using System.Collections;

/// <summary>
/// LootItem melhorado com animações, efeitos visuais e sistema de atração inteligente
/// </summary>
public class LootItem : MonoBehaviour
{
    [Header("Item Configuration")]
    public Item item;
    
    [Header("Visual Settings")]
    public MeshRenderer meshRenderer;
    public Light itemLight;
    public ParticleSystem rarityParticles;
    public TrailRenderer trailRenderer;
    
    [Header("Animation Settings")]
    public float rotationSpeed = 45f;
    public float bobHeight = 0.3f;
    public float bobSpeed = 2f;
    public float scaleVariation = 0.1f;
    public AnimationCurve bobCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Attraction Settings")]
    public float attractSpeed = 8f;
    public float pickupDistance = 2f;
    public float magnetDistance = 5f;            // NOVO: Distância para começar atração
    public float autoPickupDelay = 0.5f;        // NOVO: Delay antes de coletar automaticamente
    public bool useSmartAttraction = true;       // NOVO: Atração inteligente
    
    [Header("Physics Settings")]
    public float initialForce = 3f;
    public float initialTorque = 180f;
    public float airResistance = 0.98f;
    public float groundBounce = 0.3f;
    public LayerMask groundMask = -1; // Mudado para -1 para detectar todas as camadas
    
    [Header("Lifetime Settings")]
    public float maxLifetime = 300f;            // 5 minutos
    public float fadeStartTime = 270f;          // Começar fade aos 4.5 minutos
    public bool despawnWhenFar = true;
    public float despawnDistance = 50f;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] dropSounds;
    public AudioClip pickupSound;
    public AudioClip raritySound;               // NOVO: Som baseado na raridade
    
    [Header("Effects")]
    public GameObject pickupEffectPrefab;
    public GameObject rarityEffectPrefab;
    
    // State
    private Vector3 startPosition;
    private Rigidbody rb;
    private Collider itemCollider;
    private bool isAttracting = false;
    private bool isPickedUp = false;
    private bool hasLanded = false;
    private float spawnTime;
    private float lastPlayerDistance = float.MaxValue;
    
    // Cache
    private PlayerController cachedPlayer;
    private Transform playerTransform;
    private PlayerInventoryManager cachedInventory;
    private bool playerCacheValid = false;
    private float cacheUpdateTimer = 0f;
    private const float CACHE_UPDATE_INTERVAL = 1f;
    
    // Animation
    private Vector3 originalScale;
    private float bobTimer = 0f;
    private Coroutine attractionCoroutine;
    private Coroutine lifetimeCoroutine;
    
    // Material caching
    private Material originalMaterial;
    private Material[] cachedMaterials;
    
    #region Initialization
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();
        
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<MeshRenderer>();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        originalScale = transform.localScale;
        spawnTime = Time.time;
        
        // Cache materials
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
            cachedMaterials = meshRenderer.materials;
        }
        
        SubscribeToEvents();
    }
    
    private void Start()
    {
        TryCachePlayer();
        ApplyInitialPhysics();
        ConfigureVisualEffects();
        StartLifetimeCoroutine();
        
        // Random bob offset para evitar sincronização
        bobTimer = Random.Range(0f, Mathf.PI * 2f);
    }
    
    private void SubscribeToEvents()
    {
        EventManager.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        EventManager.Subscribe<PlayerDestroyedEvent>(OnPlayerDestroyed);
        EventManager.Subscribe<SceneTransitionEvent>(OnSceneTransition);
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        cachedPlayer = eventData.player.GetComponent<PlayerController>();
        if (cachedPlayer != null)
        {
            playerTransform = cachedPlayer.transform;
            cachedInventory = cachedPlayer.GetInventoryManager();
            playerCacheValid = true;
        }
    }
    
    private void OnPlayerDestroyed(PlayerDestroyedEvent eventData)
    {
        cachedPlayer = null;
        playerTransform = null;
        cachedInventory = null;
        playerCacheValid = false;
        isAttracting = false;
    }
    
    private void OnSceneTransition(SceneTransitionEvent eventData)
    {
        cachedPlayer = null;
        playerTransform = null;
        cachedInventory = null;
        playerCacheValid = false;
        isAttracting = false;
    }
    
    #endregion
    
    #region Setup & Configuration
    
    public void SetItem(Item newItem)
    {
        item = newItem;
        
        if (item != null)
        {
            ConfigureVisualEffects();
            PlayDropEffects();
        }
    }
    
    private void ConfigureVisualEffects()
    {
        if (item == null) return;
        
        ConfigureBasicVisuals();
        ConfigureRarityEffects();
        ConfigureLight();
        ConfigureParticles();
        ConfigureAudio();
    }
    
    private void ConfigureBasicVisuals()
    {
        if (meshRenderer == null) return;
        
        // Aplicar cor do item
        Color itemColor = item.itemColor != Color.white ? item.itemColor : item.GetRarityColor();
        
        if (originalMaterial != null)
        {
            Material instanceMaterial = new Material(originalMaterial);
            instanceMaterial.color = itemColor;
            
            // Efeito especial para itens de alta raridade
            if (item.rarity >= ItemRarity.Epic)
            {
                instanceMaterial.EnableKeyword("_EMISSION");
                instanceMaterial.SetColor("_EmissionColor", itemColor * 0.3f);
            }
            
            meshRenderer.material = instanceMaterial;
        }
        
        // Scaling baseado na raridade
        float rarityScale = GetRarityScale(item.rarity);
        transform.localScale = originalScale * rarityScale;
    }
    
    private void ConfigureRarityEffects()
    {
        // Efeitos especiais para itens raros
        if (item.rarity >= ItemRarity.Legendary)
        {
            if (rarityEffectPrefab != null)
            {
                GameObject effect = Instantiate(rarityEffectPrefab, transform);
                Destroy(effect, 10f); // Auto-destruir após 10 segundos
            }
        }
    }
    
    private void ConfigureLight()
    {
        if (itemLight == null) return;
        
        itemLight.color = item.GetRarityColor();
        
        // Intensidade baseada na raridade
        switch (item.rarity)
        {
            case ItemRarity.Common:
                itemLight.intensity = 0.5f;
                itemLight.range = 2f;
                break;
            case ItemRarity.Uncommon:
                itemLight.intensity = 0.8f;
                itemLight.range = 3f;
                break;
            case ItemRarity.Rare:
                itemLight.intensity = 1.2f;
                itemLight.range = 4f;
                break;
            case ItemRarity.Epic:
                itemLight.intensity = 1.8f;
                itemLight.range = 5f;
                StartCoroutine(PulseLight());
                break;
            case ItemRarity.Legendary:
                itemLight.intensity = 2.5f;
                itemLight.range = 6f;
                StartCoroutine(PulseLight());
                break;
            case ItemRarity.Artifact:
                itemLight.intensity = 3f;
                itemLight.range = 8f;
                StartCoroutine(RainbowLight());
                break;
        }
    }
    
    private void ConfigureParticles()
    {
        if (rarityParticles == null) return;
        
        var main = rarityParticles.main;
        main.startColor = item.GetRarityColor();
        
        // Configurar partículas baseado na raridade
        if (item.rarity >= ItemRarity.Rare)
        {
            var emission = rarityParticles.emission;
            emission.rateOverTime = (int)item.rarity * 2;
            
            rarityParticles.Play();
        }
        else
        {
            rarityParticles.Stop();
        }
    }
    
    private void ConfigureAudio()
    {
        if (audioSource == null) return;
        
        // Volume baseado na raridade
        audioSource.volume = Mathf.Clamp(0.3f + ((int)item.rarity * 0.1f), 0.3f, 1f);
    }
    
    #endregion
    
    #region Physics & Animation
    
    private void ApplyInitialPhysics()
    {
        if (rb == null) return;
        
        // Configurar Rigidbody para melhor detecção de colisão
        rb.useGravity = true;
        rb.mass = 2f;
        rb.drag = 1f;
        rb.angularDrag = 2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        // Força inicial aleatória reduzida
        Vector3 randomDirection = new Vector3(
            Random.Range(-0.5f, 0.5f),  // Reduzido de -1,1 para -0.5,0.5
            Random.Range(0.2f, 0.4f),   // Reduzido de 0.5,1 para 0.2,0.4
            Random.Range(-0.5f, 0.5f)   // Reduzido de -1,1 para -0.5,0.5
        ).normalized;
        
        rb.AddForce(randomDirection * (initialForce * 0.5f), ForceMode.Impulse); // Reduzido para 50% da força original
        rb.AddTorque(Random.insideUnitSphere * (initialTorque * 0.5f)); // Reduzido para 50% do torque original
    }
    
    private void Update()
    {
        if (isPickedUp) return;
        
        // Update cache periodicamente
        cacheUpdateTimer -= Time.deltaTime;
        if (cacheUpdateTimer <= 0f)
        {
            TryCachePlayer();
            cacheUpdateTimer = CACHE_UPDATE_INTERVAL;
        }
        
        // Animações básicas
        UpdateRotation();
        UpdateBobbing();
        UpdateScaling();
        
        // Sistema de atração
        if (!isAttracting)
        {
            CheckPlayerProximity();
        }
        
        // Verificar se está muito longe do player
        CheckDespawnConditions();
        
        // Aplicar resistência do ar se ainda estiver se movendo
        if (!hasLanded)
        {
            ApplyAirResistance();
            
            // Verificar se o item está caindo muito rápido
            if (rb != null && rb.velocity.y < -10f)
            {
                rb.velocity = new Vector3(rb.velocity.x, -10f, rb.velocity.z);
            }
        }
    }
    
    private void UpdateRotation()
    {
        if (hasLanded && !isAttracting)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
    
    private void UpdateBobbing()
    {
        if (!hasLanded || isAttracting) return;
        
        bobTimer += Time.deltaTime * bobSpeed;
        float bobOffset = bobCurve.Evaluate(Mathf.Sin(bobTimer)) * bobHeight;
        
        Vector3 targetPosition = startPosition + Vector3.up * bobOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 2f);
    }
    
    private void UpdateScaling()
    {
        if (isAttracting) return;
        
        float scaleOffset = Mathf.Sin(Time.time * 3f) * scaleVariation;
        float rarityScale = GetRarityScale(item?.rarity ?? ItemRarity.Common);
        transform.localScale = originalScale * (rarityScale + scaleOffset);
    }
    
    private void ApplyAirResistance()
    {
        if (rb != null && !hasLanded)
        {
            if (!rb.isKinematic)
            {
                rb.velocity *= airResistance;
                rb.angularVelocity *= airResistance;
            }
            else
            {
                // Se for kinematic, aplicar resistência do ar manualmente
                transform.position += rb.velocity * Time.deltaTime;
                transform.Rotate(rb.angularVelocity * Time.deltaTime);
                
                // Atualizar velocidades
                rb.velocity *= airResistance;
                rb.angularVelocity *= airResistance;
            }
        }
    }
    
    #endregion
    
    #region Player Detection & Attraction
    
    private void TryCachePlayer()
    {
        if (!playerCacheValid && PlayerController.Instance != null)
        {
            cachedPlayer = PlayerController.Instance;
            playerTransform = cachedPlayer.transform;
            cachedInventory = cachedPlayer.GetInventoryManager();
            playerCacheValid = true;
        }
    }
    
    private void CheckPlayerProximity()
    {
        if (!playerCacheValid || playerTransform == null) return;
        
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        lastPlayerDistance = distance;
        
        // Verificar se deve começar atração
        if (distance <= magnetDistance && !isAttracting)
        {
            StartAttraction();
        }
        
        // Pickup imediato se muito próximo
        if (distance <= pickupDistance && hasLanded)
        {
            StartCoroutine(DelayedPickup());
        }
    }
    
    private void StartAttraction()
    {
        if (isAttracting || isPickedUp) return;
        
        isAttracting = true;
        
        // Parar física
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // Desabilitar collider para evitar interferência
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }
        
        // Iniciar movimento de atração
        if (attractionCoroutine != null)
        {
            StopCoroutine(attractionCoroutine);
        }
        attractionCoroutine = StartCoroutine(AttractionMovement());
        
        // Efeitos visuais
        PlayAttractionEffects();
    }
    
    private IEnumerator AttractionMovement()
    {
        Vector3 startPos = transform.position;
        
        while (isAttracting && !isPickedUp && playerTransform != null)
        {
            // Verificar se player ainda está próximo
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distance > magnetDistance * 1.5f)
            {
                // Player se afastou muito, parar atração
                StopAttraction();
                yield break;
            }
            
            // Movimento suave em direção ao player
            Vector3 targetPosition = playerTransform.position + Vector3.up * 0.5f;
            Vector3 direction = (targetPosition - transform.position).normalized;
            
            // Velocidade variável baseada na distância
            float speedMultiplier = useSmartAttraction ? 
                Mathf.Clamp(distance / magnetDistance, 0.5f, 2f) : 1f;
            
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                attractSpeed * speedMultiplier * Time.deltaTime
            );
            
            // Rotação durante atração
            transform.Rotate(Vector3.up, rotationSpeed * 2f * Time.deltaTime);
            
            // Verificar se chegou ao player
            if (distance <= 0.5f)
            {
                CollectItem();
                yield break;
            }
            
            yield return null;
        }
    }
    
    private void StopAttraction()
    {
        isAttracting = false;
        
        if (attractionCoroutine != null)
        {
            StopCoroutine(attractionCoroutine);
            attractionCoroutine = null;
        }
        
        // Reativar física
        if (rb != null && hasLanded)
        {
            rb.isKinematic = true; // Manter kinematic se já pousou
        }
        
        // Reativar collider
        if (itemCollider != null)
        {
            itemCollider.enabled = true;
        }
    }
    
    private IEnumerator DelayedPickup()
    {
        yield return new WaitForSeconds(autoPickupDelay);
        
        if (!isPickedUp && Vector3.Distance(transform.position, playerTransform.position) <= pickupDistance)
        {
            CollectItem();
        }
    }
    
    #endregion
    
    #region Item Collection
    
    private void CollectItem()
    {
        if (isPickedUp || cachedInventory == null) return;
        
        // Verificar se pode ser coletado
        if (!CanBeCollected())
        {
            ShowInventoryFullMessage();
            return;
        }
        
        isPickedUp = true;
        
        // Adicionar ao inventário
        bool success = cachedInventory.AddItem(item);
        
        if (success)
        {
            // Disparar eventos
            EventManager.TriggerEvent(new ItemCollectedEvent
            {
                item = item,
                collectionPosition = transform.position,
                collector = cachedPlayer.gameObject
            });
            
            // Efeitos de coleta
            PlayPickupEffects();
            
            // Destruir objeto
            StartCoroutine(DestroyAfterEffects());
        }
        else
        {
            isPickedUp = false;
            ShowInventoryFullMessage();
            StopAttraction();
        }
    }
    
    private bool CanBeCollected()
    {
        if (cachedInventory == null || item == null) return false;
        
        // Verificar se tem espaço no inventário
        if (!cachedInventory.HasSpace && !item.IsStackable) return false;
        
        // Se for stackable, verificar se pode fazer stack
        if (item.IsStackable)
        {
            var existingItem = cachedInventory.FindItemById(item.id);
            if (existingItem != null && existingItem.CanStack(item))
            {
                return true;
            }
        }
        
        return cachedInventory.HasSpace;
    }
    
    private void ShowInventoryFullMessage()
    {
        EventManager.TriggerEvent(new InventoryFullEvent
        {
            attemptedItem = item,
            maxCapacity = cachedInventory?.MaxItems ?? 0
        });
        
        EventManager.TriggerEvent(new NotificationEvent
        {
            message = "Inventário cheio!",
            type = NotificationType.Warning,
            duration = 2f,
            color = Color.yellow
        });
    }
    
    #endregion
    
    #region Collision & Physics
    
    private void OnCollisionEnter(Collision collision)
    {
        if (hasLanded) return;
        
        // Verificar se colidiu com o chão ou qualquer objeto estático
        if (collision.gameObject.isStatic || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            hasLanded = true;
            startPosition = transform.position;
            
            // Configurar física para item pousado
            if (rb != null)
            {
                // Não tornar kinematic imediatamente
                StartCoroutine(StabilizeItem());
            }
            
            // Efeito de pouso
            PlayLandingEffects();
            
            // Som de queda
            PlayDropSound();
        }
        else
        {
            // Bounce em outras superfícies
            if (rb != null)
            {
                Vector3 bounceDirection = Vector3.Reflect(rb.velocity.normalized, collision.contacts[0].normal);
                rb.velocity = bounceDirection * (rb.velocity.magnitude * groundBounce);
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Coleta por trigger (backup)
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && !isAttracting && !isPickedUp)
        {
            CollectItem();
        }
    }
    
    #endregion
    
    #region Lifetime Management
    
    private void StartLifetimeCoroutine()
    {
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
        }
        lifetimeCoroutine = StartCoroutine(LifetimeCountdown());
    }
    
    private IEnumerator LifetimeCountdown()
    {
        yield return new WaitForSeconds(fadeStartTime);
        
        // Começar fade
        StartCoroutine(FadeOut());
        
        yield return new WaitForSeconds(maxLifetime - fadeStartTime);
        
        // Destruir se ainda existir
        if (!isPickedUp)
        {
            Destroy(gameObject);
        }
    }
    
    private IEnumerator FadeOut()
    {
        float fadeTime = maxLifetime - fadeStartTime;
        float elapsed = 0f;
        
        Color originalColor = meshRenderer?.material?.color ?? Color.white;
        float originalLightIntensity = itemLight?.intensity ?? 0f;
        
        while (elapsed < fadeTime && !isPickedUp)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeTime);
            
            // Fade material
            if (meshRenderer?.material != null)
            {
                Color newColor = originalColor;
                newColor.a = alpha;
                meshRenderer.material.color = newColor;
            }
            
            // Fade light
            if (itemLight != null)
            {
                itemLight.intensity = originalLightIntensity * alpha;
            }
            
            yield return null;
        }
    }
    
    private void CheckDespawnConditions()
    {
        if (isPickedUp || !despawnWhenFar) return;
        
        if (lastPlayerDistance > despawnDistance)
        {
            // Player muito longe, considerar despawn
            StartCoroutine(DespawnAfterDelay());
        }
    }
    
    private IEnumerator DespawnAfterDelay()
    {
        yield return new WaitForSeconds(10f); // 10 segundos de delay
        
        // Verificar novamente se player ainda está longe
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance > despawnDistance && !isPickedUp)
            {
                Destroy(gameObject);
            }
        }
    }
    
    #endregion
    
    #region Effects & Audio
    
    private void PlayDropEffects()
    {
        PlayDropSound();
        
        // Efeito visual de spawn
        if (rarityEffectPrefab != null && item.rarity >= ItemRarity.Rare)
        {
            GameObject effect = Instantiate(rarityEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
    }
    
    private void PlayLandingEffects()
    {
        // Pequeno efeito de impacto
        if (item.rarity >= ItemRarity.Epic)
        {
            // Shake da câmera para itens muito raros
            // CameraShake.Instance?.Shake(0.1f, 0.3f);
        }
        
        // Partículas de poeira
        // Implementar se necessário
    }
    
    private void PlayAttractionEffects()
    {
        if (trailRenderer != null)
        {
            trailRenderer.enabled = true;
            trailRenderer.startColor = item.GetRarityColor();
            trailRenderer.endColor = Color.clear;
        }
        
        // Som de atração
        if (audioSource != null && raritySound != null)
        {
            audioSource.PlayOneShot(raritySound, 0.5f);
        }
    }
    
    private void PlayPickupEffects()
    {
        // Efeito visual
        if (pickupEffectPrefab != null)
        {
            GameObject effect = Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Som de coleta
        if (audioSource != null && pickupSound != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }
    }
    
    private void PlayDropSound()
    {
        if (audioSource != null && dropSounds != null && dropSounds.Length > 0)
        {
            AudioClip randomSound = dropSounds[Random.Range(0, dropSounds.Length)];
            audioSource.PlayOneShot(randomSound, 0.7f);
        }
    }
    
    private IEnumerator PulseLight()
    {
        if (itemLight == null) yield break;
        
        float baseIntensity = itemLight.intensity;
        
        while (gameObject.activeInHierarchy && !isPickedUp)
        {
            float pulse = Mathf.Sin(Time.time * 3f) * 0.5f + 1f;
            itemLight.intensity = baseIntensity * pulse;
            yield return null;
        }
    }
    
    private IEnumerator RainbowLight()
    {
        if (itemLight == null) yield break;
        
        while (gameObject.activeInHierarchy && !isPickedUp)
        {
            float hue = (Time.time * 0.5f) % 1f;
            itemLight.color = Color.HSVToRGB(hue, 1f, 1f);
            yield return null;
        }
    }
    
    private IEnumerator DestroyAfterEffects()
    {
        // Esperar efeitos terminarem
        yield return new WaitForSeconds(0.5f);
        
        Destroy(gameObject);
    }
    
    #endregion
    
    #region Utility Methods
    
    private float GetRarityScale(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return 1f;
            case ItemRarity.Uncommon: return 1.1f;
            case ItemRarity.Rare: return 1.2f;
            case ItemRarity.Epic: return 1.3f;
            case ItemRarity.Legendary: return 1.5f;
            case ItemRarity.Artifact: return 1.7f;
            default: return 1f;
        }
    }
    
    public void ForceCollection()
    {
        if (!isPickedUp)
        {
            CollectItem();
        }
    }
    
    public void SetCustomLifetime(float lifetime)
    {
        maxLifetime = lifetime;
        fadeStartTime = lifetime * 0.9f;
        
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
        }
        StartLifetimeCoroutine();
    }
    
    public float GetDistanceToPlayer()
    {
        return lastPlayerDistance;
    }
    
    public bool IsBeingAttracted()
    {
        return isAttracting;
    }
    
    #endregion
    
    #region Debug
    
    private void OnDrawGizmosSelected()
    {
        // Visualizar distâncias
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupDistance);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, magnetDistance);
        
        if (despawnWhenFar)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, despawnDistance);
        }
        
        // Mostrar direção do player se estiver atraindo
        if (isAttracting && playerTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
    
    [ContextMenu("Debug Item Info")]
    private void DebugItemInfo()
    {
        if (item == null)
        {
            Debug.Log("Item is null");
            return;
        }
        
        Debug.Log($"=== {item.name} ===");
        Debug.Log($"Rarity: {item.rarity}");
        Debug.Log($"Value: {item.GetCurrentValue()}");
        Debug.Log($"Is Attracting: {isAttracting}");
        Debug.Log($"Has Landed: {hasLanded}");
        Debug.Log($"Distance to Player: {lastPlayerDistance:F2}");
        Debug.Log($"Lifetime: {Time.time - spawnTime:F1}s / {maxLifetime:F1}s");
    }
    
    [ContextMenu("Force Attract to Player")]
    private void DebugForceAttract()
    {
        if (playerTransform != null)
        {
            StartAttraction();
        }
    }
    
    [ContextMenu("Force Collect")]
    private void DebugForceCollect()
    {
        ForceCollection();
    }
    
    #endregion
    
    #region Cleanup
    
    private void OnDestroy()
    {
        EventManager.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        EventManager.Unsubscribe<PlayerDestroyedEvent>(OnPlayerDestroyed);
        EventManager.Unsubscribe<SceneTransitionEvent>(OnSceneTransition);
        
        // Limpar coroutines
        if (attractionCoroutine != null)
        {
            StopCoroutine(attractionCoroutine);
        }
        
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
        }
        
        // Limpar materiais instanciados
        if (meshRenderer?.material != null && meshRenderer.material != originalMaterial)
        {
            DestroyImmediate(meshRenderer.material);
        }
    }
    
    #endregion

    private IEnumerator StabilizeItem()
    {
        // Esperar um pouco para a física se estabilizar
        yield return new WaitForSeconds(0.5f);
        
        if (rb != null)
        {
            // Verificar se o item está muito abaixo do terreno
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out hit, 0.5f))
            {
                // Se estiver abaixo do terreno, reposicionar
                transform.position = hit.point + Vector3.up * 0.1f;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // Esperar mais um pouco
            yield return new WaitForSeconds(0.5f);
            
            // Só então tornar kinematic
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
}