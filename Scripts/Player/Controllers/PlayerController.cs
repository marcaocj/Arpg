using UnityEngine;
using System.Collections;

/// <summary>
/// Controlador principal do jogador - coordena todos os outros componentes
/// OTIMIZADO: Elimina FindObjectOfType, usa EventManager para comunicação
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Componentes")]
    private CharacterController characterController;
    private Animator animator;
    private Camera mainCamera;
    
    [Header("Managers")]
    public PlayerStatsManager statsManager;
    public PlayerHealthManager healthManager;
    public PlayerInventoryManager inventoryManager;
    public PlayerMovement movement;
    public PlayerCombat combat;
    public PlayerSkillController skillController;
    
    [Header("Debug")]
    public bool showStatsOnGUI = false;
    
    // Propriedades para compatibilidade com código existente
    public int level => statsManager?.Level ?? 1;
    public int experiencePoints => statsManager?.ExperiencePoints ?? 0;
    public int experienceToNextLevel => statsManager?.ExperienceToNextLevel ?? 100;
    public int health => healthManager?.CurrentHealth ?? 100;
    public int maxHealth => healthManager?.MaxHealth ?? 100;
    public int mana => healthManager?.CurrentMana ?? 50;
    public int maxMana => healthManager?.MaxMana ?? 50;
    public int strength => statsManager?.Strength ?? 10;
    public int intelligence => statsManager?.Intelligence ?? 10;
    public int dexterity => statsManager?.Dexterity ?? 10;
    public int vitality => statsManager?.Vitality ?? 10;
    
    // Propriedade para compatibilidade do inventário
    public Inventory inventory => inventoryManager as Inventory;
    
    // Singleton para acesso fácil
    public static PlayerController Instance { get; private set; }
    
    private void Awake()
    {
        // Configurar singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Múltiplas instâncias de PlayerController detectadas!");
        }
        
        InitializeComponents();
        ValidateComponents();
    }
    
    private void Start()
    {
        SetupComponentReferences();
        SubscribeToEvents();
        
        // Notificar outros sistemas que o player foi criado
        EventManager.TriggerEvent(new PlayerSpawnedEvent
        {
            player = gameObject,
            spawnPosition = transform.position
        });
    }
    
    private void InitializeComponents()
    {
        // Componentes básicos do Unity
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        mainCamera = Camera.main;
        
        // Managers do jogador
        if (statsManager == null)
            statsManager = GetComponent<PlayerStatsManager>();
        
        if (healthManager == null)
            healthManager = GetComponent<PlayerHealthManager>();
        
        if (inventoryManager == null)
            inventoryManager = GetComponent<PlayerInventoryManager>();
        
        // Componentes de jogabilidade
        if (movement == null)
            movement = GetComponent<PlayerMovement>();
        
        if (combat == null)
            combat = GetComponent<PlayerCombat>();
        
        if (skillController == null)
            skillController = GetComponent<PlayerSkillController>();
    }
    
    private void ValidateComponents()
    {
        // Validar componentes críticos
        if (characterController == null)
            Debug.LogError("PlayerController: CharacterController não encontrado!");
        
        if (statsManager == null)
            Debug.LogError("PlayerController: PlayerStatsManager não encontrado!");
        
        if (healthManager == null)
            Debug.LogError("PlayerController: PlayerHealthManager não encontrado!");
        
        if (inventoryManager == null)
            Debug.LogError("PlayerController: PlayerInventoryManager não encontrado!");
        
        // Avisos para componentes opcionais
        if (animator == null)
            Debug.LogWarning("PlayerController: Animator não encontrado!");
        
        if (mainCamera == null)
            Debug.LogWarning("PlayerController: Camera principal não encontrada!");
    }
    
    private void SetupComponentReferences()
    {
        // Configurar referências entre componentes se necessário
        // (por exemplo, se algum componente precisar de referência direta a outro)
    }
    
    private void SubscribeToEvents()
    {
        // Subscrever a eventos importantes - SEM FindObjectOfType
        if (healthManager != null)
        {
            healthManager.OnPlayerDied += OnPlayerDied;
            healthManager.OnPlayerRespawned += OnPlayerRespawned;
        }
        
        if (statsManager != null)
        {
            statsManager.OnLevelUp += OnLevelUp;
        }
        
        // Subscrever a eventos globais através do EventManager
        EventManager.Subscribe<UIElementToggledEvent>(OnUIToggled);
    }
    
    private void Update()
    {
        // Verificar se os componentes essenciais estão ativos
        if (!IsPlayerActive()) return;
        
        // Delegar responsabilidades para componentes específicos
        movement?.HandleInput();
        combat?.HandleInput();
        skillController?.HandleInput();
        
        // Gerenciar input específico do PlayerController
        HandleControllerInput();
    }
    
    private void HandleControllerInput()
    {
        // Input do inventário
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
        
        // Input de debug
        if (Input.GetKeyDown(KeyCode.F1) && showStatsOnGUI)
        {
            statsManager?.DebugPrintStats();
        }
        
        if (Input.GetKeyDown(KeyCode.F2) && showStatsOnGUI)
        {
            inventoryManager?.DebugPrintInventory();
        }
    }
    
    private bool IsPlayerActive()
    {
        return enabled && healthManager != null && healthManager.IsAlive();
    }
    
    #region Public API - Delegates to Managers
    
    // Métodos de Stats
    public PlayerStats GetStats() => statsManager?.Stats;
    public void GainExperience(int amount) => statsManager?.GainExperience(amount);
    public bool SpendAttributePoint(string attribute) => statsManager?.SpendAttributePoint(attribute) ?? false;
    
    // Métodos de Saúde - OTIMIZADO: não cria popup aqui, deixa para o HealthManager
    public void TakeDamage(int amount, DamageType damageType = DamageType.Physical)
    {
        if (healthManager != null)
        {
            healthManager.TakeDamage(amount, damageType);
            
            // Animar hit
            if (animator != null)
                animator.SetTrigger("Hit");
            
            Debug.Log($"Player tomou {amount} de dano ({damageType})!");
        }
    }
    
    public void Heal(int amount) => healthManager?.Heal(amount);
    public bool UseMana(int amount) => healthManager?.UseMana(amount) ?? false;
    public void RestoreMana(int amount) => healthManager?.RestoreMana(amount);
    
    // Métodos de Inventário
    public bool AddItemToInventory(Item item) => inventoryManager?.AddItem(item) ?? false;
    public bool RemoveItemFromInventory(Item item) => inventoryManager?.RemoveItem(item) ?? false;
    public bool EquipItem(Item item) => inventoryManager?.EquipItem(item) ?? false;
    public Item GetEquippedWeapon() => inventoryManager?.equippedWeapon;
    
    #endregion
    
    #region Event Handlers
    
    private void OnPlayerDied()
    {
        Debug.Log("PlayerController: Jogador morreu!");
        
        // Desabilitar inputs
        if (movement != null) movement.enabled = false;
        if (combat != null) combat.enabled = false;
        if (skillController != null) skillController.enabled = false;
        
        // Animar morte
        if (animator != null)
            animator.SetTrigger("Die");
    }
    
    private void OnPlayerRespawned()
    {
        Debug.Log("PlayerController: Jogador respawnou!");
        
        // Reabilitar inputs
        if (movement != null) movement.enabled = true;
        if (combat != null) combat.enabled = true;
        if (skillController != null) skillController.enabled = true;
        
        // Resetar animações
        if (animator != null)
        {
            animator.ResetTrigger("Die");
            animator.SetTrigger("Respawn");
        }
    }
    
    private void OnLevelUp(int newLevel)
    {
        Debug.Log($"PlayerController: Level up! Novo nível: {newLevel}");
        
        // Animar level up
        if (animator != null)
            animator.SetTrigger("LevelUp");
    }
    
    private void OnUIToggled(UIElementToggledEvent eventData)
    {
        // Responder a mudanças de UI se necessário
        if (eventData.elementName == "Inventory")
        {
            // Gerenciar pausa/despausa baseado no estado do inventário
         //   Time.timeScale = eventData.isVisible ? 0.2f : 1f;
        }
    }
    
    #endregion
    
    #region UI Management
    
    public void ToggleInventory()
    {
        // Usar EventManager em vez de encontrar GameManager
        EventManager.TriggerEvent(new UIElementToggledEvent
        {
            elementName = "Inventory",
            isVisible = !IsInventoryVisible()
        });
    }
    
    private bool IsInventoryVisible()
    {
        // Verificar através de outros meios, não FindObjectOfType
        return GameManager.instance?.inventoryUI?.activeSelf ?? false;
    }
    
    #endregion
    
    #region Accessors (for compatibility)
    
    public CharacterController GetCharacterController() => characterController;
    public Animator GetAnimator() => animator;
    public Camera GetMainCamera() => mainCamera;
    
    // Accessor methods para componentes específicos
    public PlayerStatsManager GetStatsManager() => statsManager;
    public PlayerHealthManager GetHealthManager() => healthManager;
    public PlayerInventoryManager GetInventoryManager() => inventoryManager;
    public PlayerMovement GetMovement() => movement;
    public PlayerCombat GetCombat() => combat;
    public PlayerSkillController GetSkillController() => skillController;
    
    #endregion
    
    #region Debug GUI
    
    private void OnGUI()
    {
        if (!showStatsOnGUI || statsManager?.Stats == null) return;
        
        var stats = statsManager.Stats;
        
        GUI.Box(new Rect(10, 10, 350, 250), "Player Stats");
        
        int yPos = 35;
        int spacing = 20;
        
        GUI.Label(new Rect(20, yPos, 320, 20), $"Level: {stats.Level} | EXP: {stats.ExperiencePoints}/{stats.ExperienceToNextLevel}");
        yPos += spacing;
        
        GUI.Label(new Rect(20, yPos, 320, 20), $"Health: {healthManager.CurrentHealth}/{healthManager.MaxHealth} | Mana: {healthManager.CurrentMana}/{healthManager.MaxMana}");
        yPos += spacing;
        
        GUI.Label(new Rect(20, yPos, 320, 20), $"STR: {stats.Strength} | INT: {stats.Intelligence} | DEX: {stats.Dexterity} | VIT: {stats.Vitality}");
        yPos += spacing;
        
        GUI.Label(new Rect(20, yPos, 320, 20), $"Crit: {stats.CriticalChance:P1} | Multi: {stats.CriticalMultiplier:F2}x");
        yPos += spacing;
        
        GUI.Label(new Rect(20, yPos, 320, 20), $"Inventory: {inventoryManager.CurrentItemCount}/{inventoryManager.MaxItems}");
        yPos += spacing;
        
        if (stats.AvailableAttributePoints > 0)
        {
            GUI.Label(new Rect(20, yPos, 320, 20), $"<color=yellow>PONTOS: {stats.AvailableAttributePoints}</color>");
            yPos += spacing;
        }
        
        // Botões de teste
        if (GUI.Button(new Rect(20, yPos, 80, 25), "Gain EXP"))
        {
            GainExperience(500);
        }
        
        if (GUI.Button(new Rect(110, yPos, 80, 25), "Take Dmg"))
        {
            TakeDamage(25);
        }
        
        if (GUI.Button(new Rect(200, yPos, 80, 25), "Heal"))
        {
            Heal(50);
        }
        
        yPos += 30;
        
        if (GUI.Button(new Rect(20, yPos, 100, 25), "Debug Stats"))
        {
            statsManager.DebugPrintStats();
        }
        
        if (GUI.Button(new Rect(130, yPos, 100, 25), "Debug Inv"))
        {
            inventoryManager.DebugPrintInventory();
        }
    }
    
    #endregion
    
    #region Lifecycle
    
    private void OnDestroy()
    {
        // Limpar singleton
        if (Instance == this)
        {
            Instance = null;
        }
        
        // Desinscrever de eventos locais
        if (healthManager != null)
        {
            healthManager.OnPlayerDied -= OnPlayerDied;
            healthManager.OnPlayerRespawned -= OnPlayerRespawned;
        }
        
        if (statsManager != null)
        {
            statsManager.OnLevelUp -= OnLevelUp;
        }
        
        // Desinscrever de eventos do EventManager
        EventManager.Unsubscribe<UIElementToggledEvent>(OnUIToggled);
        
        // Notificar que o player foi destruído
        EventManager.TriggerEvent(new PlayerDestroyedEvent
        {
            player = gameObject
        });
    }
    
    #endregion
    
    #region Validation Methods
    
    /// <summary>
    /// Valida se todos os componentes críticos estão configurados corretamente
    /// </summary>
    public bool ValidateSetup()
    {
        bool isValid = true;
        
        if (statsManager == null)
        {
            Debug.LogError("PlayerController: PlayerStatsManager é obrigatório!");
            isValid = false;
        }
        
        if (healthManager == null)
        {
            Debug.LogError("PlayerController: PlayerHealthManager é obrigatório!");
            isValid = false;
        }
        
        if (inventoryManager == null)
        {
            Debug.LogError("PlayerController: PlayerInventoryManager é obrigatório!");
            isValid = false;
        }
        
        if (characterController == null)
        {
            Debug.LogError("PlayerController: CharacterController é obrigatório!");
            isValid = false;
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Força uma validação completa e tenta corrigir problemas
    /// </summary>
    [ContextMenu("Validate and Fix Setup")]
    public void ValidateAndFixSetup()
    {
        InitializeComponents();
        
        if (ValidateSetup())
        {
            Debug.Log("PlayerController: Setup validado com sucesso!");
        }
        else
        {
            Debug.LogError("PlayerController: Problemas encontrados no setup!");
        }
    }
    
    #endregion
}

// Novos eventos para eliminhar FindObjectOfType
[System.Serializable]
public struct PlayerSpawnedEvent
{
    public GameObject player;
    public Vector3 spawnPosition;
}

[System.Serializable]
public struct PlayerDestroyedEvent
{
    public GameObject player;
}