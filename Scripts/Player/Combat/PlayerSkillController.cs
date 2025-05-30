using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Controlador de habilidades do jogador - OTIMIZADO COMPLETO: Elimina FindObjectOfType, usa cache
/// </summary>
public class PlayerSkillController : MonoBehaviour
{
    [Header("Habilidades")]
    public List<Skill> skills = new List<Skill>();
    public int currentSkillIndex = 0;
    
    [Header("UI References")]
    public SkillBarUI skillBarUI;
    
    // Componentes cached
    private PlayerController playerController;
    private PlayerStatsManager statsManager;
    private PlayerHealthManager healthManager;
    private PlayerInventoryManager inventoryManager;
    private PlayerCombat playerCombat;
    private Animator animator;
    
    // Cache para SkillBarUI - OTIMIZAÇÃO CRÍTICA
    private SkillBarUI cachedSkillBarUI;
    private bool skillBarUICacheValid = false;
    private float uiCacheCheckTimer = 0f;
    private const float UI_CACHE_CHECK_INTERVAL = 1f;
    
    // Cooldown tracking
    private Dictionary<int, float> skillCooldowns = new Dictionary<int, float>();
    
    // Performance optimization
    private float lastCooldownUpdate = 0f;
    private const float COOLDOWN_UPDATE_INTERVAL = 0.05f;
    
    private void Awake()
    {
        InitializeComponents();
        InitializeDefaultSkills();
    }
    
    private void Start()
    {
        // Tentar encontrar SkillBarUI
        TryCacheSkillBarUI();
        
        // Subscrever a eventos
        SubscribeToEvents();
        
        // Notificar UI sobre skills carregadas
        RefreshUI();
    }
    
    private void SubscribeToEvents()
    {
        // Subscrever a eventos para invalidar cache
        EventManager.Subscribe<SceneTransitionEvent>(OnSceneTransition);
        EventManager.Subscribe<UIElementToggledEvent>(OnUIElementToggled);
    }
    
    private void OnDestroy()
    {
        EventManager.Unsubscribe<SceneTransitionEvent>(OnSceneTransition);
        EventManager.Unsubscribe<UIElementToggledEvent>(OnUIElementToggled);
    }
    
    #region Event Handlers
    
    private void OnSceneTransition(SceneTransitionEvent eventData)
    {
        // Invalidar cache da UI ao trocar de cena
        cachedSkillBarUI = null;
        skillBarUICacheValid = false;
        uiCacheCheckTimer = 0f;
    }
    
    private void OnUIElementToggled(UIElementToggledEvent eventData)
    {
        // Responder a mudanças de UI se necessário
        if (eventData.elementName == "SkillBar")
        {
            // Re-validar cache se skill bar foi toggled
            skillBarUICacheValid = false;
            TryCacheSkillBarUI();
        }
    }
    
    #endregion
    
    private void InitializeComponents()
    {
        playerController = GetComponent<PlayerController>();
        
        if (playerController != null)
        {
            statsManager = playerController.GetStatsManager();
            healthManager = playerController.GetHealthManager();
            inventoryManager = playerController.GetInventoryManager();
            animator = playerController.GetAnimator();
        }
        
        playerCombat = GetComponent<PlayerCombat>();
        
        if (statsManager == null)
        {
            Debug.LogError("PlayerSkillController: PlayerStatsManager não encontrado!");
        }
        
        if (healthManager == null)
        {
            Debug.LogError("PlayerSkillController: PlayerHealthManager não encontrado!");
        }
        
        if (playerCombat == null)
        {
            Debug.LogError("PlayerSkillController: PlayerCombat não encontrado!");
        }
    }
    
    private void TryCacheSkillBarUI()
    {
        if (skillBarUI != null)
        {
            cachedSkillBarUI = skillBarUI;
            skillBarUICacheValid = true;
            return;
        }
        
        // Buscar apenas se não temos referência e o cache não é válido
        if (!skillBarUICacheValid)
        {
            // Usar o singleton do EventManager para encontrar UIs ativas
            SkillBarUI foundUI = null;
            
            // Tentar encontrar através de GameObject.Find (mais eficiente que FindObjectOfType)
            GameObject skillBarObject = GameObject.Find("SkillBarUI");
            if (skillBarObject != null)
            {
                foundUI = skillBarObject.GetComponent<SkillBarUI>();
            }
            
            // Fallback para FindObjectOfType apenas se realmente necessário
            if (foundUI == null)
            {
                foundUI = Object.FindObjectOfType<SkillBarUI>(true); // incluir inativos
            }
            
            if (foundUI != null)
            {
                cachedSkillBarUI = foundUI;
                skillBarUI = foundUI; // Atualizar referência pública
                skillBarUICacheValid = true;
                Debug.Log("PlayerSkillController: SkillBarUI encontrada e cacheada.");
            }
        }
    }
    
    public void HandleInput()
    {
        if (!ValidateComponents()) return;
        
        HandleSkillSelection();
    }
    
    private void Update()
    {
        // Update cooldowns com throttling para performance
        if (Time.time - lastCooldownUpdate >= COOLDOWN_UPDATE_INTERVAL)
        {
            UpdateSkillCooldowns();
            lastCooldownUpdate = Time.time;
        }
        
        // Verificar cache da UI periodicamente
        uiCacheCheckTimer -= Time.deltaTime;
        if (uiCacheCheckTimer <= 0f)
        {
            if (!skillBarUICacheValid)
            {
                TryCacheSkillBarUI();
            }
            uiCacheCheckTimer = UI_CACHE_CHECK_INTERVAL;
        }
    }
    
    private bool ValidateComponents()
    {
        return statsManager != null && 
               healthManager != null && 
               healthManager.IsAlive() &&
               playerCombat != null;
    }
    
    private void InitializeDefaultSkills()
    {
        skills.Clear();
        
        // Ataque Básico - Físico, single target
        var basicAttack = new Skill(
            "Ataque Básico", 
            "Um ataque físico direto que escala com Força.",
            SkillType.Physical, 
            SkillTargetType.Single,
            0,      // Sem custo de mana
            0.8f,   // Cooldown base
            12,     // Dano base
            3.5f    // Range
        );
        skills.Add(basicAttack);
        
        // Bola de Fogo - Elemental, projétil com AoE
        var fireball = new Skill(
            "Bola de Fogo",
            "Lança uma bola de fogo que explode ao atingir o alvo, causando dano em área.",
            SkillType.Fire,
            SkillTargetType.Projectile,
            15,     // Custo de mana
            1.5f,   // Cooldown base  
            25,     // Dano base
            6f,     // Range
            2f      // Raio da explosão
        );
        fireball.projectileSpeed = 12f;
        fireball.damageScaling = 1.2f;
        skills.Add(fireball);
        
        // Rajada de Gelo - Elemental, cone
        var iceBlast = new Skill(
            "Rajada de Gelo",
            "Dispara projéteis de gelo em cone que congelam inimigos.",
            SkillType.Ice,
            SkillTargetType.Area,
            12,     // Custo de mana
            1.2f,   // Cooldown
            18,     // Dano base
            4f,     // Range
            3f      // Raio do cone
        );
        skills.Add(iceBlast);
        
        // Golpe Rápido - Físico, single target, low cooldown
        var quickStrike = new Skill(
            "Golpe Rápido",
            "Um ataque rápido que escala com Destreza.",
            SkillType.Physical,
            SkillTargetType.Single,
            5,      // Custo baixo de mana
            0.4f,   // Cooldown muito baixo
            8,      // Dano menor
            2.5f    // Range menor
        );
        quickStrike.scalesWithDexterity = true;
        quickStrike.scalesWithStrength = true;
        skills.Add(quickStrike);
        
        // Inicializar cooldowns
        for (int i = 0; i < skills.Count; i++)
        {
            skillCooldowns[i] = 0f;
        }
    }
    
    private void HandleSkillSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && skills.Count > 0)
        {
            SelectSkill(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && skills.Count > 1)
        {
            SelectSkill(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && skills.Count > 2)
        {
            SelectSkill(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) && skills.Count > 3)
        {
            SelectSkill(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5) && skills.Count > 4)
        {
            SelectSkill(4);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6) && skills.Count > 5)
        {
            SelectSkill(5);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7) && skills.Count > 6)
        {
            SelectSkill(6);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8) && skills.Count > 7)
        {
            SelectSkill(7);
        }
    }
    
    public void SelectSkill(int skillIndex)
    {
        if (skillIndex >= 0 && skillIndex < skills.Count)
        {
            currentSkillIndex = skillIndex;
            Debug.Log($"Skill selecionada: {skills[currentSkillIndex].name}");
            
            // Notificar UI através do cache com fallback
            NotifyUIUpdate("UpdateSelection");
        }
    }
    
    public void UseCurrentSkill()
    {
        if (!ValidateComponents()) return;
        
        if (currentSkillIndex < 0 || currentSkillIndex >= skills.Count)
            return;
            
        Skill skill = skills[currentSkillIndex];
        
        // Verificar cooldown
        if (IsSkillOnCooldown(currentSkillIndex))
        {
            Debug.Log($"{skill.name} ainda está em cooldown!");
            return;
        }
        
        var stats = statsManager.Stats;
        
        // Verificar mana
        if (!skill.CanUse(stats))
        {
            Debug.Log($"Mana insuficiente para {skill.name}! Necessário: {skill.GetActualManaCost(stats)}, Atual: {healthManager.CurrentMana}");
            return;
        }
        
        // Consumir mana
        int actualManaCost = skill.GetActualManaCost(stats);
        if (!healthManager.UseMana(actualManaCost))
        {
            Debug.Log($"Falha ao consumir mana para {skill.name}!");
            return;
        }
        
        // Aplicar cooldown
        float actualCooldown = skill.GetActualCooldown(stats);
        skillCooldowns[currentSkillIndex] = actualCooldown;
        
        // Executar skill
        ExecuteSkill(skill);
        
        // Animar
        AnimateSkillUse(skill);
        
        // Disparar evento de skill usada através do EventManager
        EventManager.TriggerEvent(new SkillUsedEvent
        {
            skill = skill,
            caster = gameObject,
            targetPosition = transform.position + transform.forward * skill.GetActualRange(stats),
            actualDamage = skill.GetActualDamage(stats, inventoryManager?.equippedWeapon),
            manaCost = actualManaCost,
            cooldown = actualCooldown
        });
        
        Debug.Log($"Usou {skill.name} | Dano: {skill.GetActualDamage(stats, inventoryManager?.equippedWeapon)} | Cooldown: {actualCooldown:F1}s");
    }
    
    private void ExecuteSkill(Skill skill)
    {
        if (playerCombat == null) return;
        
        switch (skill.targetType)
        {
            case SkillTargetType.Single:
                playerCombat.ExecuteSingleTargetSkill(skill);
                break;
            case SkillTargetType.Area:
                playerCombat.ExecuteAreaSkill(skill);
                break;
            case SkillTargetType.Projectile:
                playerCombat.ExecuteProjectileSkill(skill);
                break;
            case SkillTargetType.Self:
                playerCombat.ExecuteSelfTargetSkill(skill);
                break;
        }
    }
    
    private void AnimateSkillUse(Skill skill)
    {
        if (animator == null) return;
        
        string animationTrigger = playerCombat.GetAnimationTrigger(skill.type);
        animator.SetTrigger(animationTrigger);
        
        // Ajustar velocidade da animação
        var stats = statsManager.Stats;
        float speedMultiplier = skill.type == SkillType.Physical ? 
            stats.AttackSpeed : stats.CastSpeed;
        animator.speed = speedMultiplier;
        
        // Resetar velocidade após animação
        StartCoroutine(ResetAnimatorSpeed());
    }
    
    private IEnumerator ResetAnimatorSpeed()
    {
        yield return new WaitForSeconds(0.5f);
        if (animator != null)
        {
            animator.speed = 1f;
        }
    }
    
private void UpdateSkillCooldowns()
{
    if (!ValidateComponents()) return;
    
    var keys = new List<int>(skillCooldowns.Keys);
    
    foreach (int skillIndex in keys)
    {
        if (skillCooldowns[skillIndex] > 0)
        {
            float oldCooldown = skillCooldowns[skillIndex];
            skillCooldowns[skillIndex] -= Time.deltaTime;
            skillCooldowns[skillIndex] = Mathf.Max(0, skillCooldowns[skillIndex]);
            
            // Disparar evento de mudança de cooldown se houve mudança significativa
            if (Mathf.Abs(oldCooldown - skillCooldowns[skillIndex]) > 0.01f)
            {
                var stats = statsManager.Stats;
                float totalCooldown = skillIndex < skills.Count ? 
                    skills[skillIndex].GetActualCooldown(stats) : 1f;
                
                EventManager.TriggerEvent(new SkillCooldownChangedEvent
                {
                    skillIndex = skillIndex,
                    remainingCooldown = skillCooldowns[skillIndex],
                    totalCooldown = totalCooldown
                });
            }
        }
    }
        
        // A UI será atualizada automaticamente via eventos
    }
    
    public bool IsSkillOnCooldown(int skillIndex)
    {
        return skillCooldowns.ContainsKey(skillIndex) && skillCooldowns[skillIndex] > 0;
    }
    
    public float GetSkillCooldownRemaining(int skillIndex)
    {
        if (skillCooldowns.ContainsKey(skillIndex))
            return skillCooldowns[skillIndex];
        return 0f;
    }
    
    public void AddSkill(Skill newSkill)
    {
        if (newSkill == null) return;
        
        skills.Add(newSkill);
        skillCooldowns[skills.Count - 1] = 0f;
        
        NotifyUIUpdate("RefreshSkillBar");
        
        Debug.Log($"Nova skill adicionada: {newSkill.name}");
    }
    
    public void RemoveSkill(int index)
    {
        if (index >= 0 && index < skills.Count)
        {
            string skillName = skills[index].name;
            skills.RemoveAt(index);
            
            // Reorganizar cooldowns
            var newCooldowns = new Dictionary<int, float>();
            for (int i = 0; i < skills.Count; i++)
            {
                if (skillCooldowns.ContainsKey(i))
                    newCooldowns[i] = skillCooldowns[i];
            }
            skillCooldowns = newCooldowns;
            
            // Ajustar seleção atual
            if (currentSkillIndex >= skills.Count)
            {
                currentSkillIndex = Mathf.Max(0, skills.Count - 1);
            }
            
            NotifyUIUpdate("RefreshSkillBar");
            
            Debug.Log($"Skill removida: {skillName}");
        }
    }
    
    #region Getters
    
    public Skill GetCurrentSkill()
    {
        if (currentSkillIndex >= 0 && currentSkillIndex < skills.Count)
            return skills[currentSkillIndex];
        return null;
    }
    
    public List<Skill> GetAllSkills() => skills;
    
    public Skill GetSkill(int index)
    {
        if (index >= 0 && index < skills.Count)
            return skills[index];
        return null;
    }
    
    public bool CanUseCurrentSkill()
    {
        if (!ValidateComponents()) return false;
        
        var currentSkill = GetCurrentSkill();
        if (currentSkill == null) return false;
        
        var stats = statsManager.Stats;
        return currentSkill.CanUse(stats) && 
               !IsSkillOnCooldown(currentSkillIndex) &&
               healthManager.HasEnoughMana(currentSkill.GetActualManaCost(stats));
    }
    
    #endregion
    
    #region Utility
    
    /// <summary>
    /// Método otimizado para notificar UI com fallback
    /// </summary>
    private void NotifyUIUpdate(string methodName)
    {
        if (skillBarUICacheValid && cachedSkillBarUI != null)
        {
            try
            {
                switch (methodName)
                {
                    case "RefreshSkillBar":
                        cachedSkillBarUI.RefreshSkillBar();
                        break;
                    case "UpdateSelection":
                        cachedSkillBarUI.UpdateSelection();
                        break;
                    case "ForceRefresh":
                        cachedSkillBarUI.ForceRefresh();
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"PlayerSkillController: Erro ao atualizar UI - {e.Message}");
                // Invalidar cache se houve erro
                skillBarUICacheValid = false;
                cachedSkillBarUI = null;
            }
        }
        else
        {
            // Tentar re-cachear UI se não temos
            TryCacheSkillBarUI();
            
            // Tentar novamente se conseguiu cachear
            if (skillBarUICacheValid && cachedSkillBarUI != null)
            {
                NotifyUIUpdate(methodName);
            }
        }
    }
    
    public void RefreshUI()
    {
        NotifyUIUpdate("RefreshSkillBar");
    }
    
    public void ResetAllCooldowns()
    {
        var keys = new List<int>(skillCooldowns.Keys);
        foreach (var key in keys)
        {
            skillCooldowns[key] = 0f;
        }
        
        // Disparar eventos para UI
        foreach (int skillIndex in keys)
        {
            var stats = statsManager?.Stats;
            if (stats != null && skillIndex < skills.Count)
            {
                EventManager.TriggerEvent(new SkillCooldownChangedEvent
                {
                    skillIndex = skillIndex,
                    remainingCooldown = 0f,
                    totalCooldown = skills[skillIndex].GetActualCooldown(stats)
                });
            }
        }
        
        Debug.Log("Todos os cooldowns foram resetados!");
    }
    
    public float GetTotalCooldownReduction()
    {
        if (!ValidateComponents()) return 0f;
        
        var stats = statsManager.Stats;
        return stats.CastSpeed - 1f; // Retorna o bônus de velocidade de cast
    }
    
    public void LevelUpSkill(int skillIndex)
    {
        if (skillIndex >= 0 && skillIndex < skills.Count)
        {
            var skill = skills[skillIndex];
            
            // Implementar sistema de level up de skills
            // Por exemplo: aumentar dano base, reduzir cooldown, etc.
            skill.baseDamage = Mathf.RoundToInt(skill.baseDamage * 1.1f);
            skill.baseCooldown = Mathf.Max(0.1f, skill.baseCooldown * 0.95f);
            
            Debug.Log($"Skill {skill.name} evoluiu!");
            
            NotifyUIUpdate("RefreshSkillBar");
            
            // Disparar evento de skill evoluída
            EventManager.TriggerEvent(new NotificationEvent
            {
                message = $"Skill {skill.name} evoluiu!",
                type = NotificationType.Success,
                duration = 2f,
                color = Color.yellow
            });
        }
    }
    
    /// <summary>
    /// Força refresh completo da UI
    /// </summary>
    public void ForceUIRefresh()
    {
        skillBarUICacheValid = false;
        cachedSkillBarUI = null;
        TryCacheSkillBarUI();
        NotifyUIUpdate("ForceRefresh");
    }
    
    #endregion
    
    #region Debug
    
    public void DebugPrintSkills()
    {
        Debug.Log("=== SKILLS DO JOGADOR ===");
        
        for (int i = 0; i < skills.Count; i++)
        {
            var skill = skills[i];
            var stats = statsManager?.Stats;
            
            if (stats != null)
            {
                Debug.Log($"{i + 1}. {skill.name} - Dano: {skill.GetActualDamage(stats, inventoryManager?.equippedWeapon)} | " +
                         $"Mana: {skill.GetActualManaCost(stats)} | Cooldown: {skill.GetActualCooldown(stats):F1}s | " +
                         $"Restante: {GetSkillCooldownRemaining(i):F1}s");
            }
            else
            {
                Debug.Log($"{i + 1}. {skill.name} - Stats não disponíveis");
            }
        }
        
        Debug.Log($"Skill atual: {(currentSkillIndex >= 0 && currentSkillIndex < skills.Count ? skills[currentSkillIndex].name : "Nenhuma")}");
        Debug.Log($"UI Cache válido: {skillBarUICacheValid}");
    }
    
    /// <summary>
    /// Debug para verificar estado do cache
    /// </summary>
    [ContextMenu("Debug Cache Status")]
    public void DebugCacheStatus()
    {
        Debug.Log($"=== SKILL CONTROLLER CACHE STATUS ===");
        Debug.Log($"SkillBarUI Cache Válido: {skillBarUICacheValid}");
        Debug.Log($"SkillBarUI Cacheada: {(cachedSkillBarUI != null ? cachedSkillBarUI.name : "null")}");
        Debug.Log($"SkillBarUI Referência: {(skillBarUI != null ? skillBarUI.name : "null")}");
        Debug.Log($"Próxima verificação de cache em: {uiCacheCheckTimer:F1}s");
        Debug.Log($"Skills carregadas: {skills.Count}");
        Debug.Log($"Skill atual: {currentSkillIndex}");
    }
    
    #endregion
    
    #region Gizmos para Debug
    
    private void OnDrawGizmos()
    {
        if (skills.Count > 0 && 
            currentSkillIndex < skills.Count && 
            Application.isPlaying && 
            statsManager?.Stats != null)
        {
            Skill currentSkill = skills[currentSkillIndex];
            var stats = statsManager.Stats;
            float actualRange = currentSkill.GetActualRange(stats);
            
            // Range da skill atual (verde)
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawSphere(transform.position, actualRange);
            
            // AoE radius se aplicável (vermelho)
            if (currentSkill.areaRadius > 0)
            {
                Gizmos.color = new Color(1, 0, 0, 0.3f);
                Vector3 aoeCenter = transform.position + transform.forward * (actualRange * 0.7f);
                Gizmos.DrawSphere(aoeCenter, currentSkill.GetActualAreaRadius(stats));
            }
            
            // Cone de ataque para single target (azul)
            if (currentSkill.targetType == SkillTargetType.Single)
            {
                Gizmos.color = new Color(0, 0, 1, 0.3f);
                Gizmos.DrawRay(transform.position, Quaternion.Euler(0, -30, 0) * transform.forward * actualRange);
                Gizmos.DrawRay(transform.position, Quaternion.Euler(0, 30, 0) * transform.forward * actualRange);
                Gizmos.DrawRay(transform.position, transform.forward * actualRange);
            }
        }
    }
    
    #endregion
}