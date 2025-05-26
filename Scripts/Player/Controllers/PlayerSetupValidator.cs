using UnityEngine;

/// <summary>
/// Validador e configurador automático para o sistema do jogador
/// </summary>
[System.Serializable]
public class PlayerSetupValidator : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupOnAwake = true;
    [SerializeField] private bool validateOnStart = true;
    [SerializeField] private bool showDetailedLogs = true;
    
    [Header("Component Status")]
    [SerializeField] private bool playerControllerValid;
    [SerializeField] private bool statsManagerValid;
    [SerializeField] private bool healthManagerValid;
    [SerializeField] private bool inventoryManagerValid;
    [SerializeField] private bool movementValid;
    [SerializeField] private bool combatValid;
    [SerializeField] private bool skillControllerValid;
    
    private PlayerController playerController;
    
    private void Awake()
    {
        if (autoSetupOnAwake)
        {
            AutoSetup();
        }
    }
    
    private void Start()
    {
        if (validateOnStart)
        {
            ValidateSetup();
        }
    }
    
    [ContextMenu("Auto Setup Player")]
    public void AutoSetup()
    {
        playerController = GetComponent<PlayerController>();
        
        if (playerController == null)
        {
            LogError("PlayerController não encontrado! Adicionando componente...");
            playerController = gameObject.AddComponent<PlayerController>();
        }
        
        // Verificar e adicionar PlayerStatsManager
        var statsManager = GetComponent<PlayerStatsManager>();
        if (statsManager == null)
        {
            LogWarning("PlayerStatsManager não encontrado! Adicionando componente...");
            statsManager = gameObject.AddComponent<PlayerStatsManager>();
        }
        
        // Verificar e adicionar PlayerHealthManager
        var healthManager = GetComponent<PlayerHealthManager>();
        if (healthManager == null)
        {
            LogWarning("PlayerHealthManager não encontrado! Adicionando componente...");
            healthManager = gameObject.AddComponent<PlayerHealthManager>();
        }
        
        // Verificar e adicionar PlayerInventoryManager
        var inventoryManager = GetComponent<PlayerInventoryManager>();
        if (inventoryManager == null)
        {
            LogWarning("PlayerInventoryManager não encontrado! Adicionando componente...");
            inventoryManager = gameObject.AddComponent<PlayerInventoryManager>();
        }
        
        // Verificar e adicionar Inventory (compatibilidade)
        var inventory = GetComponent<Inventory>();
        if (inventory == null)
        {
            LogWarning("Inventory (compatibilidade) não encontrado! Adicionando componente...");
            inventory = gameObject.AddComponent<Inventory>();
        }
        
        // Verificar e adicionar PlayerMovement
        var movement = GetComponent<PlayerMovement>();
        if (movement == null)
        {
            LogWarning("PlayerMovement não encontrado! Adicionando componente...");
            movement = gameObject.AddComponent<PlayerMovement>();
        }
        
        // Verificar e adicionar PlayerCombat
        var combat = GetComponent<PlayerCombat>();
        if (combat == null)
        {
            LogWarning("PlayerCombat não encontrado! Adicionando componente...");
            combat = gameObject.AddComponent<PlayerCombat>();
        }
        
        // Verificar e adicionar PlayerSkillController
        var skillController = GetComponent<PlayerSkillController>();
        if (skillController == null)
        {
            LogWarning("PlayerSkillController não encontrado! Adicionando componente...");
            skillController = gameObject.AddComponent<PlayerSkillController>();
        }
        
        // Verificar CharacterController
        var charController = GetComponent<CharacterController>();
        if (charController == null)
        {
            LogWarning("CharacterController não encontrado! Adicionando componente...");
            charController = gameObject.AddComponent<CharacterController>();
            
            // Configurar valores padrão
            charController.height = 1.8f;
            charController.radius = 0.5f;
            charController.center = new Vector3(0, 0.9f, 0);
        }
        
        // Configurar referências no PlayerController
        if (playerController != null)
        {
            playerController.statsManager = statsManager;
            playerController.healthManager = healthManager;
            playerController.inventoryManager = inventoryManager;
            playerController.movement = movement;
            playerController.combat = combat;
            playerController.skillController = skillController;
        }
        
        LogSuccess("Auto setup concluído!");
    }
    
    [ContextMenu("Validate Setup")]
    public void ValidateSetup()
    {
        LogInfo("=== VALIDANDO SETUP DO JOGADOR ===");
        
        bool allValid = true;
        
        // Validar PlayerController
        playerController = GetComponent<PlayerController>();
        playerControllerValid = playerController != null;
        if (!playerControllerValid)
        {
            LogError("❌ PlayerController não encontrado!");
            allValid = false;
        }
        else
        {
            LogSuccess("✅ PlayerController OK");
        }
        
        // Validar PlayerStatsManager
        var statsManager = GetComponent<PlayerStatsManager>();
        statsManagerValid = statsManager != null;
        if (!statsManagerValid)
        {
            LogError("❌ PlayerStatsManager não encontrado!");
            allValid = false;
        }
        else
        {
            LogSuccess("✅ PlayerStatsManager OK");
        }
        
        // Validar PlayerHealthManager
        var healthManager = GetComponent<PlayerHealthManager>();
        healthManagerValid = healthManager != null;
        if (!healthManagerValid)
        {
            LogError("❌ PlayerHealthManager não encontrado!");
            allValid = false;
        }
        else
        {
            LogSuccess("✅ PlayerHealthManager OK");
        }
        
        // Validar PlayerInventoryManager
        var inventoryManager = GetComponent<PlayerInventoryManager>();
        inventoryManagerValid = inventoryManager != null;
        if (!inventoryManagerValid)
        {
            LogError("❌ PlayerInventoryManager não encontrado!");
            allValid = false;
        }
        else
        {
            LogSuccess("✅ PlayerInventoryManager OK");
        }
        
        // Validar PlayerMovement
        var movement = GetComponent<PlayerMovement>();
        movementValid = movement != null;
        if (!movementValid)
        {
            LogWarning("⚠️ PlayerMovement não encontrado!");
        }
        else
        {
            LogSuccess("✅ PlayerMovement OK");
        }
        
        // Validar PlayerCombat
        var combat = GetComponent<PlayerCombat>();
        combatValid = combat != null;
        if (!combatValid)
        {
            LogWarning("⚠️ PlayerCombat não encontrado!");
        }
        else
        {
            LogSuccess("✅ PlayerCombat OK");
        }
        
        // Validar PlayerSkillController
        var skillController = GetComponent<PlayerSkillController>();
        skillControllerValid = skillController != null;
        if (!skillControllerValid)
        {
            LogWarning("⚠️ PlayerSkillController não encontrado!");
        }
        else
        {
            LogSuccess("✅ PlayerSkillController OK");
        }
        
        // Validar componentes do Unity
        var charController = GetComponent<CharacterController>();
        if (charController == null)
        {
            LogWarning("⚠️ CharacterController não encontrado!");
        }
        else
        {
            LogSuccess("✅ CharacterController OK");
        }
        
        var animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            LogWarning("⚠️ Animator não encontrado!");
        }
        else
        {
            LogSuccess("✅ Animator OK");
        }
        
        // Validar referências no PlayerController
        if (playerController != null)
        {
            ValidatePlayerControllerReferences();
        }
        
        // Resultado final
        if (allValid && statsManagerValid && healthManagerValid && inventoryManagerValid)
        {
            LogSuccess("🎉 SETUP VÁLIDO! Todos os componentes essenciais estão presentes.");
        }
        else
        {
            LogError("❌ SETUP INVÁLIDO! Execute 'Auto Setup Player' para corrigir.");
        }
    }
    
    private void ValidatePlayerControllerReferences()
    {
        LogInfo("Validando referências do PlayerController...");
        
        if (playerController.statsManager == null)
        {
            LogWarning("PlayerController.statsManager não está configurado!");
        }
        
        if (playerController.healthManager == null)
        {
            LogWarning("PlayerController.healthManager não está configurado!");
        }
        
        if (playerController.inventoryManager == null)
        {
            LogWarning("PlayerController.inventoryManager não está configurado!");
        }
        
        if (playerController.movement == null)
        {
            LogWarning("PlayerController.movement não está configurado!");
        }
        
        if (playerController.combat == null)
        {
            LogWarning("PlayerController.combat não está configurado!");
        }
        
        if (playerController.skillController == null)
        {
            LogWarning("PlayerController.skillController não está configurado!");
        }
    }
    
    [ContextMenu("Fix References")]
    public void FixReferences()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }
        
        if (playerController != null)
        {
            if (playerController.statsManager == null)
                playerController.statsManager = GetComponent<PlayerStatsManager>();
            
            if (playerController.healthManager == null)
                playerController.healthManager = GetComponent<PlayerHealthManager>();
            
            if (playerController.inventoryManager == null)
                playerController.inventoryManager = GetComponent<PlayerInventoryManager>();
            
            if (playerController.movement == null)
                playerController.movement = GetComponent<PlayerMovement>();
            
            if (playerController.combat == null)
                playerController.combat = GetComponent<PlayerCombat>();
            
            if (playerController.skillController == null)
                playerController.skillController = GetComponent<PlayerSkillController>();
            
            LogSuccess("Referências do PlayerController corrigidas!");
        }
    }
    
    [ContextMenu("Remove Old Components")]
    public void RemoveOldComponents()
    {
        // Esta função pode ser usada para remover componentes antigos se necessário
        LogInfo("Verificando componentes antigos para remoção...");
        
        // Exemplo: remover scripts antigos que foram substituídos
        var oldScripts = GetComponents<MonoBehaviour>();
        foreach (var script in oldScripts)
        {
            if (script.GetType().Name.Contains("Old") || 
                script.GetType().Name.Contains("Legacy"))
            {
                LogWarning($"Componente antigo encontrado: {script.GetType().Name}");
                // DestroyImmediate(script); // Descomente se quiser remover automaticamente
            }
        }
    }
    
    #region Logging Methods
    
    private void LogSuccess(string message)
    {
        if (showDetailedLogs)
            Debug.Log($"<color=green>[PlayerSetup]</color> {message}");
    }
    
    private void LogInfo(string message)
    {
        if (showDetailedLogs)
            Debug.Log($"<color=cyan>[PlayerSetup]</color> {message}");
    }
    
    private void LogWarning(string message)
    {
        if (showDetailedLogs)
            Debug.LogWarning($"<color=yellow>[PlayerSetup]</color> {message}");
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"<color=red>[PlayerSetup]</color> {message}");
    }
    
    #endregion
    
    #region GUI Debug
    
    private void OnGUI()
    {
        if (!Application.isPlaying || !showDetailedLogs) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 220, 10, 200, 300));
        GUILayout.Box("Player Setup Status");
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        
        // Status dos componentes
        style.normal.textColor = playerControllerValid ? Color.green : Color.red;
        GUILayout.Label($"PlayerController: {(playerControllerValid ? "✅" : "❌")}", style);
        
        style.normal.textColor = statsManagerValid ? Color.green : Color.red;
        GUILayout.Label($"StatsManager: {(statsManagerValid ? "✅" : "❌")}", style);
        
        style.normal.textColor = healthManagerValid ? Color.green : Color.red;
        GUILayout.Label($"HealthManager: {(healthManagerValid ? "✅" : "❌")}", style);
        
        style.normal.textColor = inventoryManagerValid ? Color.green : Color.red;
        GUILayout.Label($"InventoryManager: {(inventoryManagerValid ? "✅" : "❌")}", style);
        
        style.normal.textColor = movementValid ? Color.green : Color.yellow;
        GUILayout.Label($"Movement: {(movementValid ? "✅" : "⚠️")}", style);
        
        style.normal.textColor = combatValid ? Color.green : Color.yellow;
        GUILayout.Label($"Combat: {(combatValid ? "✅" : "⚠️")}", style);
        
        style.normal.textColor = skillControllerValid ? Color.green : Color.yellow;
        GUILayout.Label($"SkillController: {(skillControllerValid ? "✅" : "⚠️")}", style);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Validate Setup"))
        {
            ValidateSetup();
        }
        
        if (GUILayout.Button("Auto Setup"))
        {
            AutoSetup();
        }
        
        if (GUILayout.Button("Fix References"))
        {
            FixReferences();
        }
        
        GUILayout.EndArea();
    }
    
    #endregion
}