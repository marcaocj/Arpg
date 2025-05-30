using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Gerencia saúde, mana e efeitos relacionados do jogador
/// OTIMIZADO: Elimina FindObjectOfType, usa EventManager
/// </summary>
public class PlayerHealthManager : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private bool autoRegeneration = true;
    [SerializeField] private float healthRegenRate = 1f;
    [SerializeField] private float manaRegenRate = 2f;
    [SerializeField] private float regenDelay = 5f; // Delay após tomar dano
    
    // Referências
    private PlayerStatsManager statsManager;
    private PlayerStats stats;
    
    // Estado atual
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; }
    public int CurrentMana { get; private set; }
    public int MaxMana { get; private set; }
    
    // Regeneração
    private float lastDamageTime;
    private Coroutine regenCoroutine;
    
    // Eventos
    public event Action<int, int, int> OnHealthChanged; // current, max, delta
    public event Action<int, int, int> OnManaChanged;   // current, max, delta
    public event Action OnPlayerDied;
    public event Action OnPlayerRespawned;
    
    // Cache para evitar buscar GameManager toda vez
    private Vector3? cachedRespawnPoint;
    
    private void Awake()
    {
        statsManager = GetComponent<PlayerStatsManager>();
        
        if (statsManager == null)
        {
            Debug.LogError("PlayerHealthManager: PlayerStatsManager não encontrado!");
            return;
        }
        
        stats = statsManager.Stats;
    }
    
    private void Start()
    {
        InitializeHealth();
        
        if (autoRegeneration)
        {
            StartRegeneration();
        }
        
        // Subscrever a eventos para invalidar cache quando necessário
        SubscribeToEvents();
    }
    
    private void SubscribeToEvents()
    {
        // Eventos para invalidar cache de respawn
        EventManager.Subscribe<SceneTransitionEvent>(OnSceneTransition);
    }
    
    private void InitializeHealth()
    {
        MaxHealth = stats.MaxHealth;
        MaxMana = stats.MaxMana;
        CurrentHealth = MaxHealth;
        CurrentMana = MaxMana;
        
        // Subscrever a mudanças de stats
        if (statsManager != null)
        {
            statsManager.OnLevelUp += OnLevelUp;
            statsManager.OnAttributeChanged += OnAttributeChanged;
        }
        
        TriggerInitialEvents();
    }
    
    private void TriggerInitialEvents()
    {
        StartCoroutine(TriggerInitialEventsCoroutine());
    }
    
    private System.Collections.IEnumerator TriggerInitialEventsCoroutine()
    {
        yield return new WaitForEndOfFrame();
        
        EventManager.TriggerEvent(new PlayerHealthChangedEvent
        {
            currentHealth = CurrentHealth,
            maxHealth = MaxHealth,
            healthDelta = 0,
            damageType = DamageType.Physical
        });
        
        EventManager.TriggerEvent(new PlayerManaChangedEvent
        {
            currentMana = CurrentMana,
            maxMana = MaxMana,
            manaDelta = 0
        });
    }
    
    #region Health Management
    
    public void TakeDamage(int amount, DamageType damageType = DamageType.Physical)
    {
        if (amount <= 0 || CurrentHealth <= 0) return;
        
        // Aplicar resistências
        float finalDamage = amount;
        
        switch (damageType)
        {
            case DamageType.Physical:
                finalDamage *= (1.0f - stats.PhysicalResistance);
                break;
            case DamageType.Elemental:
                finalDamage *= (1.0f - stats.ElementalResistance);
                break;
        }
        
        int actualDamage = Mathf.RoundToInt(finalDamage);
        int oldHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - actualDamage);
        
        lastDamageTime = Time.time;
        
        // Disparar popup de dano NO PLAYER através do EventManager
        EventManager.TriggerEvent(new DamagePopupRequestEvent
        {
            worldPosition = transform.position,
            amount = actualDamage,
            isCritical = false,
            isHeal = false,
            customColor = Color.red
        });
        
        // Disparar eventos
        EventManager.TriggerEvent(new PlayerHealthChangedEvent
        {
            currentHealth = CurrentHealth,
            maxHealth = MaxHealth,
            healthDelta = CurrentHealth - oldHealth,
            damageType = damageType
        });
        
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, CurrentHealth - oldHealth);
        
        Debug.Log($"Player tomou {actualDamage} de dano ({damageType}). Saúde: {CurrentHealth}/{MaxHealth}");
        
        // Verificar morte
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(int amount)
    {
        if (amount <= 0 || CurrentHealth >= MaxHealth) return;
        
        int oldHealth = CurrentHealth;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        int actualHealing = CurrentHealth - oldHealth;
        
        // Criar popup de cura através do EventManager
        if (actualHealing > 0)
        {
            EventManager.TriggerEvent(new DamagePopupRequestEvent
            {
                worldPosition = transform.position,
                amount = actualHealing,
                isCritical = false,
                isHeal = true,
                customColor = Color.green
            });
        }
        
        EventManager.TriggerEvent(new PlayerHealthChangedEvent
        {
            currentHealth = CurrentHealth,
            maxHealth = MaxHealth,
            healthDelta = CurrentHealth - oldHealth,
            damageType = DamageType.True // Cura ignora tipo de dano
        });
        
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, CurrentHealth - oldHealth);
        
        Debug.Log($"Player curou {CurrentHealth - oldHealth} pontos de vida. Saúde: {CurrentHealth}/{MaxHealth}");
    }
    
    public void SetHealth(int value)
    {
        int oldHealth = CurrentHealth;
        CurrentHealth = Mathf.Clamp(value, 0, MaxHealth);
        
        EventManager.TriggerEvent(new PlayerHealthChangedEvent
        {
            currentHealth = CurrentHealth,
            maxHealth = MaxHealth,
            healthDelta = CurrentHealth - oldHealth,
            damageType = DamageType.True
        });
        
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, CurrentHealth - oldHealth);
    }
    
    #endregion
    
    #region Mana Management
    
    public bool UseMana(int amount)
    {
        // Calcular redução de custo por inteligência
        float manaReduction = Mathf.Min(stats.Intelligence * 0.001f, 0.30f);
        int actualCost = Mathf.RoundToInt(amount * (1.0f - manaReduction));
        
        if (CurrentMana < actualCost) return false;
        
        int oldMana = CurrentMana;
        CurrentMana -= actualCost;
        
        EventManager.TriggerEvent(new PlayerManaChangedEvent
        {
            currentMana = CurrentMana,
            maxMana = MaxMana,
            manaDelta = CurrentMana - oldMana
        });
        
        OnManaChanged?.Invoke(CurrentMana, MaxMana, CurrentMana - oldMana);
        
        return true;
    }
    
    public void RestoreMana(int amount)
    {
        if (amount <= 0 || CurrentMana >= MaxMana) return;
        
        int oldMana = CurrentMana;
        CurrentMana = Mathf.Min(MaxMana, CurrentMana + amount);
        int actualRestore = CurrentMana - oldMana;
        
        // Criar popup de restauração de mana através do EventManager
        if (actualRestore > 0)
        {
            EventManager.TriggerEvent(new DamagePopupRequestEvent
            {
                worldPosition = transform.position + Vector3.up * 0.5f,
                amount = actualRestore,
                isCritical = false,
                isHeal = false,
                customColor = Color.cyan
            });
        }
        
        EventManager.TriggerEvent(new PlayerManaChangedEvent
        {
            currentMana = CurrentMana,
            maxMana = MaxMana,
            manaDelta = CurrentMana - oldMana
        });
        
        OnManaChanged?.Invoke(CurrentMana, MaxMana, CurrentMana - oldMana);
        
        Debug.Log($"Player restaurou {CurrentMana - oldMana} pontos de mana. Mana: {CurrentMana}/{MaxMana}");
    }
    
    public void SetMana(int value)
    {
        int oldMana = CurrentMana;
        CurrentMana = Mathf.Clamp(value, 0, MaxMana);
        
        EventManager.TriggerEvent(new PlayerManaChangedEvent
        {
            currentMana = CurrentMana,
            maxMana = MaxMana,
            manaDelta = CurrentMana - oldMana
        });
        
        OnManaChanged?.Invoke(CurrentMana, MaxMana, CurrentMana - oldMana);
    }
    
    #endregion
    
    #region Life Cycle
    
    private void Die()
    {
        Debug.Log("Jogador morreu!");
        
        EventManager.TriggerEvent(new PlayerDeathEvent
        {
            deathPosition = transform.position,
            causeOfDeath = "Derrotado em combate"
        });
        
        OnPlayerDied?.Invoke();
        
        // Desabilitar componentes relevantes
        GetComponent<PlayerController>().enabled = false;
        
        // Respawn após delay
        Invoke(nameof(Respawn), 3f);
    }
    
    private void Respawn()
    {
        // Restaurar saúde e mana parcialmente
        CurrentHealth = MaxHealth / 2;
        CurrentMana = MaxMana / 2;
        
        // Encontrar ponto de respawn usando cache ou evento
        Vector3 respawnPosition = GetRespawnPoint();
        transform.position = respawnPosition;
        
        // Disparar eventos
        EventManager.TriggerEvent(new PlayerRespawnEvent
        {
            respawnPosition = respawnPosition
        });
        
        EventManager.TriggerEvent(new PlayerHealthChangedEvent
        {
            currentHealth = CurrentHealth,
            maxHealth = MaxHealth,
            healthDelta = CurrentHealth,
            damageType = DamageType.True
        });
        
        EventManager.TriggerEvent(new PlayerManaChangedEvent
        {
            currentMana = CurrentMana,
            maxMana = MaxMana,
            manaDelta = CurrentMana
        });
        
        OnPlayerRespawned?.Invoke();
        
        // Reabilitar controle
        GetComponent<PlayerController>().enabled = true;
        
        Debug.Log($"Player respawnou em {respawnPosition}");
    }
    
    private Vector3 GetRespawnPoint()
    {
        // Usar cache se disponível
        if (cachedRespawnPoint.HasValue)
        {
            return cachedRespawnPoint.Value;
        }
        
        // Tentar usar GameManager se disponível
        if (GameManager.instance != null)
        {
            Vector3 point = GameManager.instance.GetRespawnPoint();
            cachedRespawnPoint = point;
            return point;
        }
        
        // Fallback para posição atual ou origem
        return transform.position != Vector3.zero ? transform.position : Vector3.zero;
    }
    
    #endregion
    
    #region Regeneration
    
    private void StartRegeneration()
    {
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
        }
        
        regenCoroutine = StartCoroutine(RegenerationLoop());
    }
    
    private IEnumerator RegenerationLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            
            // Só regenerar se não tomou dano recentemente
            if (Time.time - lastDamageTime >= regenDelay)
            {
                // Regenerar saúde
                if (CurrentHealth < MaxHealth && healthRegenRate > 0)
                {
                    int healthToRegen = Mathf.RoundToInt(healthRegenRate);
                    Heal(healthToRegen);
                }
                
                // Regenerar mana
                if (CurrentMana < MaxMana && manaRegenRate > 0)
                {
                    int manaToRegen = Mathf.RoundToInt(manaRegenRate);
                    RestoreMana(manaToRegen);
                }
            }
        }
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnLevelUp(int newLevel)
    {
        // Atualizar valores máximos
        int oldMaxHealth = MaxHealth;
        int oldMaxMana = MaxMana;
        
        MaxHealth = stats.MaxHealth;
        MaxMana = stats.MaxMana;
        
        // Manter proporção atual
        if (oldMaxHealth > 0)
        {
            float healthRatio = (float)CurrentHealth / oldMaxHealth;
            CurrentHealth = Mathf.RoundToInt(MaxHealth * healthRatio);
        }
        
        if (oldMaxMana > 0)
        {
            float manaRatio = (float)CurrentMana / oldMaxMana;
            CurrentMana = Mathf.RoundToInt(MaxMana * manaRatio);
        }
        
        // Disparar eventos de atualização
        EventManager.TriggerEvent(new PlayerStatsRecalculatedEvent
        {
            strength = stats.Strength,
            intelligence = stats.Intelligence,
            dexterity = stats.Dexterity,
            vitality = stats.Vitality,
            maxHealth = MaxHealth,
            maxMana = MaxMana
        });
        
        Debug.Log($"Level up! Nova saúde máxima: {MaxHealth}, Nova mana máxima: {MaxMana}");
    }
    
    private void OnAttributeChanged(string attribute, int newValue)
    {
        // Recalcular stats quando atributos mudarem
        MaxHealth = stats.MaxHealth;
        MaxMana = stats.MaxMana;
        
        // Garantir que valores atuais não excedam os máximos
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
        CurrentMana = Mathf.Min(CurrentMana, MaxMana);
        
        // Disparar evento de recálculo
        EventManager.TriggerEvent(new PlayerStatsRecalculatedEvent
        {
            strength = stats.Strength,
            intelligence = stats.Intelligence,
            dexterity = stats.Dexterity,
            vitality = stats.Vitality,
            maxHealth = MaxHealth,
            maxMana = MaxMana
        });
    }
    
    private void OnSceneTransition(SceneTransitionEvent eventData)
    {
        // Invalidar cache de respawn ao trocar de cena
        cachedRespawnPoint = null;
    }
    
    #endregion
    
    #region Utility
    
    public bool HasEnoughMana(int amount)
    {
        float manaReduction = Mathf.Min(stats.Intelligence * 0.001f, 0.30f);
        int actualCost = Mathf.RoundToInt(amount * (1.0f - manaReduction));
        return CurrentMana >= actualCost;
    }
    
    public float GetHealthPercentage() => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;
    public float GetManaPercentage() => MaxMana > 0 ? (float)CurrentMana / MaxMana : 0f;
    
    public bool IsAlive() => CurrentHealth > 0;
    public bool IsAtFullHealth() => CurrentHealth == MaxHealth;
    public bool IsAtFullMana() => CurrentMana == MaxMana;
    
    #endregion
    
    private void OnDestroy()
    {
        if (statsManager != null)
        {
            statsManager.OnLevelUp -= OnLevelUp;
            statsManager.OnAttributeChanged -= OnAttributeChanged;
        }
        
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
        }
        
        // Desinscrever de eventos do EventManager
        EventManager.Unsubscribe<SceneTransitionEvent>(OnSceneTransition);
    }
}