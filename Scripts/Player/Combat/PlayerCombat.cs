using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// Gerencia o sistema de combate do jogador - SEM duplicação de popups
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Combate")]
    public KeyCode attackButton = KeyCode.Mouse0;
    
    // Componentes cached
    private PlayerController playerController;
    private PlayerStatsManager statsManager;
    private PlayerHealthManager healthManager;
    private PlayerInventoryManager inventoryManager;
    private Animator animator;
    private PlayerMovement playerMovement;
    private PlayerSkillController skillController;
    
    private void Awake()
    {
        InitializeComponents();
    }
    
    private void InitializeComponents()
    {
        playerController = GetComponent<PlayerController>();
        
        if (playerController != null)
        {
            statsManager = playerController.GetStatsManager();
            healthManager = playerController.GetHealthManager();
            inventoryManager = playerController.GetInventoryManager();
            animator = playerController.GetAnimator();
            playerMovement = GetComponent<PlayerMovement>();
            skillController = GetComponent<PlayerSkillController>();
        }
        else
        {
            Debug.LogError("PlayerCombat: PlayerController não encontrado!");
        }
    }

    public void HandleInput()
    {
        if (!ValidateComponents()) return;
        
        HandleAttack();
    }
    
    private bool ValidateComponents()
    {
        return playerController != null && 
               statsManager != null && 
               healthManager != null && 
               healthManager.IsAlive();
    }

    private void HandleAttack()
    {
        if (IsPointerOverUI()) return;
        
        if (Input.GetKeyDown(attackButton))
        {
            if (skillController != null)
            {
                skillController.UseCurrentSkill();
            }
        }
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && 
               EventSystem.current.IsPointerOverGameObject();
    }
    
    #region Skill Execution
    
    public void ExecuteSingleTargetSkill(Skill skill)
    {
        if (!ValidateComponents() || skill == null) return;
        
        var stats = statsManager.Stats;
        float actualRange = skill.GetActualRange(stats);
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, actualRange);
        
        Enemy targetEnemy = null;
        float closestDistance = float.MaxValue;
        
        // Encontrar o inimigo mais próximo na direção do forward
        foreach (Collider hitCollider in hitColliders)
        {
            // IMPORTANTE: Pular o próprio player!
            if (hitCollider.gameObject == gameObject) continue;
            
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Verificar ângulo de ataque (cone de 60 graus)
                Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToEnemy);
                
                if (angle <= 30f) // 60 graus total (30 para cada lado)
                {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        targetEnemy = enemy;
                    }
                }
            }
        }
        
        // Atacar o inimigo encontrado
        if (targetEnemy != null)
        {
            ApplyDamageToEnemy(targetEnemy, skill);
        }
        
        // Spawn effect na posição do alvo ou na frente do player
        Vector3 effectPosition = targetEnemy != null ? 
            targetEnemy.transform.position : 
            transform.position + transform.forward * 2f;
        SpawnAttackEffect(skill, effectPosition);
    }

    public void ExecuteAreaSkill(Skill skill)
    {
        if (!ValidateComponents() || skill == null) return;
        
        var stats = statsManager.Stats;
        float actualRange = skill.GetActualRange(stats);
        float actualRadius = skill.GetActualAreaRadius(stats);
        
        // Posição do centro da AoE (na frente do player)
        Vector3 aoeCenter = transform.position + transform.forward * (actualRange * 0.7f);
        
        // Encontrar todos os inimigos na área
        Collider[] hitColliders = Physics.OverlapSphere(aoeCenter, actualRadius);
        int enemiesHit = 0;
        
        foreach (Collider hitCollider in hitColliders)
        {
            // IMPORTANTE: Pular o próprio player!
            if (hitCollider.gameObject == gameObject) continue;
            
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                ApplyDamageToEnemy(enemy, skill);
                enemiesHit++;
            }
        }
        
        // Spawn effect no centro da AoE
        SpawnAttackEffect(skill, aoeCenter);
        
        Debug.Log($"AoE atingiu {enemiesHit} inimigos");
    }

    public void ExecuteProjectileSkill(Skill skill)
    {
        if (!ValidateComponents() || skill == null) return;
        
        var stats = statsManager.Stats;
        
        // Determinar posição alvo
        Vector3 targetPosition = playerMovement?.GetLastMouseWorldPosition() != Vector3.zero ? 
            playerMovement.GetLastMouseWorldPosition() : 
            transform.position + transform.forward * skill.GetActualRange(stats);
        
        // Simular tempo de viagem
        float travelTime = Vector3.Distance(transform.position, targetPosition) / skill.projectileSpeed;
        
        StartCoroutine(ProjectileImpact(skill, targetPosition, travelTime));
        
        // Spawn effect inicial (lançamento)
        SpawnAttackEffect(skill, transform.position + transform.forward * 1f);
    }

    public void ExecuteSelfTargetSkill(Skill skill)
    {
        if (!ValidateComponents() || skill == null) return;
        
        Debug.Log($"Skill self-target: {skill.name}");
        SpawnAttackEffect(skill, transform.position);
        
        // Exemplo de cura (se for um heal)
        if (skill.name.Contains("Cura") || skill.name.Contains("Heal"))
        {
            var stats = statsManager.Stats;
            Item weapon = inventoryManager?.equippedWeapon;
            int healAmount = skill.GetActualDamage(stats, weapon);
            healthManager.Heal(healAmount);
        }
    }

    private IEnumerator ProjectileImpact(Skill skill, Vector3 impactPosition, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (!ValidateComponents()) yield break;
        
        var stats = statsManager.Stats;
        
        // Verificar inimigos na área de impacto
        float impactRadius = skill.areaRadius > 0 ? skill.GetActualAreaRadius(stats) : 1f;
        Collider[] hitColliders = Physics.OverlapSphere(impactPosition, impactRadius);
        
        foreach (Collider hitCollider in hitColliders)
        {
            // IMPORTANTE: Pular o próprio player!
            if (hitCollider.gameObject == gameObject) continue;
            
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                ApplyDamageToEnemy(enemy, skill);
            }
        }
        
        // Effect de impacto
        SpawnAttackEffect(skill, impactPosition);
    }

    public void ApplyDamageToEnemy(Enemy enemy, Skill skill)
    {
        if (!ValidateComponents() || enemy == null || skill == null) return;
        
        // VERIFICAÇÃO DE SEGURANÇA: Não atacar o próprio player
        if (enemy.gameObject == gameObject)
        {
            Debug.LogWarning("PlayerCombat: Tentativa de atacar o próprio player foi bloqueada!");
            return;
        }
        
        var stats = statsManager.Stats;
        Item weapon = inventoryManager?.equippedWeapon;
        int damage = skill.GetActualDamage(stats, weapon);
        
        // Verificar crítico
        bool isCritical = stats.RollCriticalHit();
        if (isCritical)
        {
            damage = stats.ApplyCriticalDamage(damage);
            
            EventManager.TriggerEvent(new CriticalHitEvent
            {
                attacker = gameObject,
                target = enemy.gameObject,
                baseDamage = skill.GetActualDamage(stats, weapon),
                criticalDamage = damage,
                criticalMultiplier = stats.CriticalMultiplier
            });
        }
        
        // REMOVIDO: DamageDealtEvent para evitar duplicação de popups
        // O popup será criado pelo Enemy.TakeDamage()
        
        // Aplicar dano no INIMIGO - ELE criará o popup
        enemy.TakeDamage(damage);
        
        Debug.Log($"Player causou {damage} de dano em {enemy.enemyName}");
    }
    
    #endregion
    
    #region Animation and Effects

    public string GetAnimationTrigger(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Physical:
                return "Attack";
            case SkillType.Fire:
                return "CastFire";
            case SkillType.Ice:
                return "CastIce";
            case SkillType.Lightning:
                return "CastLightning";
            case SkillType.Poison:
                return "CastPoison";
            default:
                return "Attack";
        }
    }
    
    private void SpawnAttackEffect(Skill skill, Vector3 position)
    {
        if (skill == null) return;
        
        // Instanciar efeito visual se disponível
        if (skill.effectPrefab != null)
        {
            GameObject effect = Instantiate(skill.effectPrefab, position, Quaternion.identity);
            Destroy(effect, 3f); // Auto-destruir após 3 segundos
        }
        
        // Tocar som se disponível
        if (skill.soundEffect != null)
        {
            AudioSource.PlayClipAtPoint(skill.soundEffect, position);
        }
        
        Debug.Log($"Efeito de ataque para: {skill.name} em {position}");
    }
    
    #endregion
    
    #region Damage Taking
    
    /// <summary>
    /// Método para o player receber dano (chamado por inimigos)
    /// CORRIGIDO: Não cria popup duplicado, apenas aplica o dano
    /// </summary>
    public void TakeDamage(int amount, DamageType damageType = DamageType.Physical)
    {
        if (!ValidateComponents()) return;
        
        // Aplicar dano através do healthManager
        healthManager.TakeDamage(amount, damageType);
        
        // POPUP SERÁ CRIADO PELO PlayerHealthManager automaticamente via eventos
        // Não criamos popup aqui para evitar duplicação
        
        // Animar hit
        if (animator != null)
            animator.SetTrigger("Hit");
        
        Debug.Log($"Player tomou {amount} de dano ({damageType})!");
    }
    
    /// <summary>
    /// Método para o player se curar
    /// </summary>
    public void Heal(int amount)
    {
        if (!ValidateComponents()) return;
        
        healthManager.Heal(amount);
    }
    
    #endregion
    
    #region Utility
    
    /// <summary>
    /// Verifica se o player pode atacar
    /// </summary>
    public bool CanAttack()
    {
        return ValidateComponents() && 
               healthManager.IsAlive() && 
               enabled;
    }
    
    /// <summary>
    /// Obtém informações de combate do player
    /// </summary>
    public CombatInfo GetCombatInfo()
    {
        if (!ValidateComponents())
            return new CombatInfo();
        
        var stats = statsManager.Stats;
        return new CombatInfo
        {
            attackSpeed = stats.AttackSpeed,
            castSpeed = stats.CastSpeed,
            criticalChance = stats.CriticalChance,
            criticalMultiplier = stats.CriticalMultiplier,
            physicalResistance = stats.PhysicalResistance,
            elementalResistance = stats.ElementalResistance
        };
    }
    
    #endregion
}

/// <summary>
/// Estrutura para informações de combate
/// </summary>
[System.Serializable]
public struct CombatInfo
{
    public float attackSpeed;
    public float castSpeed;
    public float criticalChance;
    public float criticalMultiplier;
    public float physicalResistance;
    public float elementalResistance;
}