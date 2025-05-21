using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 6f;
    public float rotationSpeed = 20f;
    public float backwardSpeedMultiplier = 0.7f; // Velocidade reduzida ao andar para trás
    public float strafeSpeedMultiplier = 0.8f;   // Velocidade reduzida ao andar de lado

    [Header("Estatísticas")]
    [SerializeField] private int initialLevel = 1;
    [SerializeField] private int initialStrength = 10;
    [SerializeField] private int initialIntelligence = 10;
    [SerializeField] private int initialDexterity = 10;
    [SerializeField] private int initialVitality = 10;
    
    // Instância de PlayerStats - acessível através de GetStats()
    private PlayerStats _stats;
    
    // Propriedades para compatibilidade com código existente
    public int level { get { return _stats.Level; } }
    public int experiencePoints { get { return _stats.ExperiencePoints; } }
    public int experienceToNextLevel { get { return _stats.ExperienceToNextLevel; } }
    public int health { 
        get { return _stats.Health; } 
        set { _stats.SetHealth(value); } 
    }
    public int maxHealth { get { return _stats.MaxHealth; } }
    public int mana { 
        get { return _stats.Mana; } 
        set { _stats.SetMana(value); } 
    }
    public int maxMana { get { return _stats.MaxMana; } }
    
    public int strength { get { return _stats.Strength; } }
    public int intelligence { get { return _stats.Intelligence; } }
    public int dexterity { get { return _stats.Dexterity; } }
    public int vitality { get { return _stats.Vitality; } }

    [Header("Combate")]
    public float attackRange = 3f; // Mantido para compatibilidade, mas skills usam seu próprio range
    public float attackCooldown = 1f;
    private float attackTimer = 0f;
    public KeyCode attackButton = KeyCode.Mouse0;
    
    [Header("Componentes")]
    private CharacterController characterController;
    private Animator animator;
    private Camera mainCamera;
    
    [Header("Habilidades")]
    public List<Skill> skills = new List<Skill>();
    public int currentSkillIndex = 0;
    
    [Header("UI References")]
    public SkillBarUI skillBarUI; // Referência para a UI das skills
    
    // Inventário
    public Inventory inventory;
    
    // Para direcionamento do mouse
    private Plane groundPlane;
    private Ray ray;
    private float rayDistance;
    
    // Para movimento estilo PoE2
    private Vector3 inputDirection = Vector3.zero;
    private Vector3 worldMoveDirection = Vector3.zero;
    private Vector3 lastMouseWorldPosition;
    private bool wasPreviouslyMoving = false;
    
    // Variáveis para direções relativas ao personagem
    private bool isMovingForward = false;
    private bool isMovingBackward = false;
    private bool isMovingLeft = false;
    private bool isMovingRight = false;

    [Header("Debug")]
    public bool showStatsOnGUI = false;

    // Método para acessar as estatísticas completas
    public PlayerStats GetStats()
    {
        return _stats;
    }

    private void Awake()
    {
        // Inicializar estatísticas do jogador
        _stats = new PlayerStats(initialLevel, initialStrength, initialIntelligence, initialDexterity, initialVitality);
        
        // Configurar eventos para estatísticas
        _stats.OnHealthChanged += (health) => {
            if (health <= 0)
            {
                Die();
            }
        };
        
        _stats.OnLevelUp += (level) => {
            Debug.Log($"Personagem alcançou o nível {level}!");
        };
        
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        mainCamera = Camera.main;
        
        // Verificar componentes
        if (characterController == null)
            Debug.LogError("CharacterController não encontrado!");
        if (animator == null)
            Debug.LogWarning("Animator não encontrado!");
        if (mainCamera == null)
            Debug.LogError("Camera principal não encontrada!");
        
        // Inicializar inventário
        if (inventory == null)
            inventory = GetComponent<Inventory>();
        
        // Inicializar habilidades básicas com o novo sistema
        InitializeDefaultSkills();
        
        // Inicializar plano para raycasting do mouse
        groundPlane = new Plane(Vector3.up, new Vector3(0, 0, 0));
    }

    // NOVA INICIALIZAÇÃO DAS SKILLS
    private void InitializeDefaultSkills()
    {
        // Limpar skills existentes
        skills.Clear();
        
        // Ataque Básico - Físico, single target
        Skill basicAttack = new Skill(
            "Ataque Básico", 
            "Um ataque físico direto que escala com Força.",
            SkillType.Physical, 
            SkillTargetType.Single,
            0,      // Sem custo de mana
            0.8f,   // Cooldown base
            12,     // Dano base
            3.5f    // Range
        );
        basicAttack.scalesWithStrength = true;
        basicAttack.rangeScalesWithAttribute = true;
        basicAttack.rangeScalingAttribute = "strength";
        skills.Add(basicAttack);
        
        // Bola de Fogo - Elemental, projétil com AoE
        Skill fireball = new Skill(
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
        fireball.damageScaling = 1.2f; // 20% mais dano que o básico
        skills.Add(fireball);
        
        // Para compatibilidade com o sistema atual, adicionar as skills antigas também
        if (skills.Count == 2)
        {
            // Só adicionar se ainda não temos as skills antigas
            skills.Add(new Skill("Rajada de Gelo", 12, 1.2f, 18, SkillType.Ice));
            skills.Add(new Skill("Golpe Rápido", 5, 0.4f, 8, SkillType.Physical));
        }
    }

    private void Update()
    {
        // Prioridade: sempre olhar para o mouse
        LookAtMouse();
        
        // Outros controles
        HandleMovement();
        HandleAttack();
        HandleSkillSelection();
        
        // Atualiza timers
        UpdateAttackTimer();
    }
    
    private void UpdateAttackTimer()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }
    }
    
    private void LookAtMouse()
    {
        // Lançar raio da câmera para o mouse
        ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (groundPlane.Raycast(ray, out rayDistance))
        {
            // Ponto no mundo onde o mouse "está"
            Vector3 mouseWorldPosition = ray.GetPoint(rayDistance);
            lastMouseWorldPosition = mouseWorldPosition;
            
            // Ignorar altura Y - rotação apenas no eixo Y como em PoE
            Vector3 playerPosition = transform.position;
            mouseWorldPosition.y = playerPosition.y;
            
            // Direção do jogador para o mouse
            Vector3 lookDirection = mouseWorldPosition - playerPosition;
            
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                // Criar rotação olhando para o ponto do mouse (apenas no eixo Y)
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                
                // Aplicar rotação suave
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }

    private void HandleMovement()
    {
        // Input do jogador (WASD)
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical");     // W/S
        
        // Criar direção de input baseada no WASD
        inputDirection = new Vector3(horizontal, 0, vertical).normalized;
        
        // Verifica se há input de movimento
        bool hasMovementInput = inputDirection.magnitude > 0.1f;
        
        if (hasMovementInput)
        {
            // Converter input para direção mundial baseada na câmera
            ConvertInputToWorldDirection();
            
            // Calcular direção relativa ao personagem (para animações)
            CalculateRelativeMovementDirections();
            
            // Calcular velocidade baseada na direção
            float currentSpeed = CalculateMovementSpeed();
            
            // Aplicar movimento
            characterController.Move(worldMoveDirection * currentSpeed * Time.deltaTime);
            
            // Animar movimento
            UpdateMovementAnimation();
            
            wasPreviouslyMoving = true;
        }
        else
        {
            // Parado
            inputDirection = Vector3.zero;
            worldMoveDirection = Vector3.zero;
            
            // Resetar flags de direção
            isMovingForward = false;
            isMovingBackward = false;
            isMovingLeft = false;
            isMovingRight = false;
            
            if (animator != null)
            {
                animator.SetFloat("Speed", 0);
                animator.SetFloat("Forward", 0);
                animator.SetFloat("Right", 0);
                animator.SetBool("IsMoving", false);
            }
            
            wasPreviouslyMoving = false;
        }
        
        // Aplicar gravidade se não estiver no chão
        if (!characterController.isGrounded)
        {
            characterController.Move(Physics.gravity * Time.deltaTime);
        }
    }
    
    private void ConvertInputToWorldDirection()
    {
        // Obter direções da câmera (sem a rotação Y)
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        
        // Garantir que estamos movendo no plano X/Z apenas
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();
        
        // Calcular direção mundial final
        worldMoveDirection = (cameraForward * inputDirection.z + cameraRight * inputDirection.x).normalized;
    }
    
    private void CalculateRelativeMovementDirections()
    {
        // Calcular direção relativa ao forward do personagem
        Vector3 playerForward = transform.forward;
        Vector3 playerRight = transform.right;
        
        // Produto escalar para determinar direção relativa
        float forwardDot = Vector3.Dot(worldMoveDirection, playerForward);
        float rightDot = Vector3.Dot(worldMoveDirection, playerRight);
        
        // Determinar direções (com threshold para evitar oscilação)
        isMovingForward = forwardDot > 0.5f;
        isMovingBackward = forwardDot < -0.5f;
        isMovingRight = rightDot > 0.5f;
        isMovingLeft = rightDot < -0.5f;
    }
    
    private float CalculateMovementSpeed()
    {
        float speed = moveSpeed;
        
        // Aplicar modificadores de velocidade baseados na direção
        if (isMovingBackward)
        {
            speed *= backwardSpeedMultiplier;
        }
        else if (isMovingLeft || isMovingRight)
        {
            // Se está se movendo principalmente para os lados (strafe)
            if (!isMovingForward && !isMovingBackward)
            {
                speed *= strafeSpeedMultiplier;
            }
        }
        
        return speed;
    }
    
    private void UpdateMovementAnimation()
    {
        if (animator == null) return;
        
        // Configurar parâmetros do Animator baseados na direção relativa
        float animationSpeed = worldMoveDirection.magnitude;
        
        // Valores para blend tree de movimento
        float forwardValue = 0f;
        float rightValue = 0f;
        
        if (isMovingForward)
        {
            forwardValue = 1f;
        }
        else if (isMovingBackward)
        {
            forwardValue = -1f;
        }
        
        if (isMovingRight)
        {
            rightValue = 1f;
        }
        else if (isMovingLeft)
        {
            rightValue = -1f;
        }
        
        // Definir parâmetros do Animator
        animator.SetFloat("Speed", animationSpeed);
        animator.SetFloat("Forward", forwardValue);
        animator.SetFloat("Right", rightValue);
        animator.SetBool("IsMoving", animationSpeed > 0.1f);
        
        // Se começou a mover agora
        if (!wasPreviouslyMoving)
        {
            animator.SetBool("IsMoving", true);
        }
    }

private void HandleAttack()
{
    // NOVO: Verificar se está sobre UI primeiro
    if (IsPointerOverUI())
    {
        return; // Não ataca se o mouse estiver sobre UI
    }
    
    // Código original de ataque
    if (Input.GetKeyDown(attackButton))
    {
        if (attackTimer <= 0)
        {
            UseCurrentSkill();
        }
    }
}
// 3. Adicione este novo método
	private bool IsPointerOverUI()
	{
		return EventSystem.current != null && 
           EventSystem.current.IsPointerOverGameObject();
	}

    
    private void HandleSkillSelection()
    {
        // Seleção de habilidades com teclas numéricas
        if (Input.GetKeyDown(KeyCode.Alpha1) && skills.Count > 0)
        {
            currentSkillIndex = 0;
            Debug.Log("Habilidade selecionada: " + skills[currentSkillIndex].name);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && skills.Count > 1)
        {
            currentSkillIndex = 1;
            Debug.Log("Habilidade selecionada: " + skills[currentSkillIndex].name);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && skills.Count > 2)
        {
            currentSkillIndex = 2;
            Debug.Log("Habilidade selecionada: " + skills[currentSkillIndex].name);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) && skills.Count > 3)
        {
            currentSkillIndex = 3;
            Debug.Log("Habilidade selecionada: " + skills[currentSkillIndex].name);
        }
    }
    
    public void UseCurrentSkill()
    {
        if (currentSkillIndex >= 0 && currentSkillIndex < skills.Count)
        {
            Skill skill = skills[currentSkillIndex];
            
            // Verificar se tem o novo sistema ou usar o antigo para compatibilidade
            bool hasNewSystem = skill.GetType().GetMethod("CanUse") != null;
            
            if (hasNewSystem)
            {
                // NOVO SISTEMA - usar os métodos avançados
                if (!skill.CanUse(_stats))
                {
                    Debug.Log($"Mana insuficiente para {skill.name}! Necessário: {skill.GetActualManaCost(_stats)}, Atual: {_stats.Mana}");
                    return;
                }
                
                // Consumir mana usando o custo calculado
                int actualManaCost = skill.GetActualManaCost(_stats);
                _stats.UseMana(actualManaCost);
                
                // Aplicar cooldown calculado
                float actualCooldown = skill.GetActualCooldown(_stats);
                attackTimer = actualCooldown;
                
                // Notificar a UI sobre o uso da skill
                if (skillBarUI != null)
                {
                    skillBarUI.OnSkillUsed(currentSkillIndex);
                }
                
                // Executar skill avançada
                ExecuteAdvancedSkill(skill);
            }
            else
            {
                // SISTEMA ANTIGO - compatibilidade com skills básicas
                if (_stats.UseMana(skill.baseManaoCost))
                {
                    // Definir cooldown baseado na velocidade de ataque/cast
                    float speedMultiplier = skill.type == SkillType.Physical ? _stats.AttackSpeed : _stats.CastSpeed;
                    attackTimer = skill.baseCooldown / speedMultiplier;
                    
                    // Executar sistema antigo
                    ExecuteBasicSkill(skill);
                }
                else
                {
                    Debug.Log("Mana insuficiente para usar " + skill.name + "!");
                }
            }
            
            // Animar ataque baseado no tipo
            if (animator != null)
            {
                string animationTrigger = GetAnimationTrigger(skill.type);
                animator.SetTrigger(animationTrigger);
                
                // Ajustar velocidade da animação
                float speedMultiplier = skill.type == SkillType.Physical ? _stats.AttackSpeed : _stats.CastSpeed;
                animator.speed = speedMultiplier;
            }
            
            // Resetar velocidade do animator
            StartCoroutine(ResetAnimatorSpeed());
        }
    }

    // EXECUTAR SKILL AVANÇADA (NOVO SISTEMA)
    private void ExecuteAdvancedSkill(Skill skill)
    {
        switch (skill.targetType)
        {
            case SkillTargetType.Single:
                ExecuteSingleTargetSkill(skill);
                break;
            case SkillTargetType.Area:
                ExecuteAreaSkill(skill);
                break;
            case SkillTargetType.Projectile:
                ExecuteProjectileSkill(skill);
                break;
            case SkillTargetType.Self:
                ExecuteSelfTargetSkill(skill);
                break;
        }
        
        Debug.Log($"Usou {skill.name} | Dano: {skill.GetActualDamage(_stats, inventory?.equippedWeapon)} | Cooldown: {skill.GetActualCooldown(_stats):F1}s | Range: {skill.GetActualRange(_stats):F1}m");
    }

    // EXECUTAR SKILL BÁSICA (SISTEMA ANTIGO)
    private void ExecuteBasicSkill(Skill skill)
    {
        // Usar o range da skill se disponível, senão usar o global
        float useRange = skill.range > 0 ? skill.range : attackRange;
        
        // Detectar inimigos no alcance
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, useRange);
        
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.gameObject == gameObject) continue;
            
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Verificar ângulo de ataque
                Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToEnemy);
                
                if (angle <= 45f)
                {
                    // Calcular dano usando o sistema de stats
                    int damage = CalculateSkillDamage(skill);
                    
                    // Verificar crítico
                    bool isCritical = _stats.RollCriticalHit();
                    if (isCritical)
                    {
                        damage = _stats.ApplyCriticalDamage(damage);
                    }
                    
                    // Aplicar dano
                    enemy.TakeDamage(damage);
                    
                    // Mostrar popup de dano
                    if (DamagePopupManager.Instance != null)
                    {
                        DamagePopupManager.Instance.ShowDamage(enemy.transform.position, damage, isCritical);
                    }
                    
                    // Log para debug
                    string critText = isCritical ? " (CRÍTICO!)" : "";
                    Debug.Log($"Atacou {enemy.enemyName} com {skill.name} por {damage} de dano{critText}");
                }
            }
        }
        
        // Efeitos de ataque
        Vector3 attackPosition = transform.position + transform.forward * 2f;
        if (lastMouseWorldPosition != Vector3.zero)
        {
            attackPosition = lastMouseWorldPosition;
        }
        SpawnAttackEffect(skill, attackPosition);
    }

    // MÉTODOS DO SISTEMA AVANÇADO
    private void ExecuteSingleTargetSkill(Skill skill)
    {
        float actualRange = skill.GetActualRange(_stats);
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, actualRange);
        
        Enemy targetEnemy = null;
        float closestDistance = float.MaxValue;
        
        // Encontrar o inimigo mais próximo na direção do mouse/forward
        foreach (Collider hitCollider in hitColliders)
        {
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
        Vector3 effectPosition = targetEnemy != null ? targetEnemy.transform.position : transform.position + transform.forward * 2f;
        SpawnAttackEffect(skill, effectPosition);
    }

    private void ExecuteAreaSkill(Skill skill)
    {
        float actualRange = skill.GetActualRange(_stats);
        float actualRadius = skill.GetActualAreaRadius(_stats);
        
        // Posição do centro da AoE (na frente do player)
        Vector3 aoeCenter = transform.position + transform.forward * (actualRange * 0.7f);
        
        // Encontrar todos os inimigos na área
        Collider[] hitColliders = Physics.OverlapSphere(aoeCenter, actualRadius);
        int enemiesHit = 0;
        
        foreach (Collider hitCollider in hitColliders)
        {
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

    private void ExecuteProjectileSkill(Skill skill)
    {
        // Por enquanto, simular projétil como single target instantâneo
        Vector3 targetPosition = lastMouseWorldPosition != Vector3.zero ? lastMouseWorldPosition : transform.position + transform.forward * skill.GetActualRange(_stats);
        
        // Simular tempo de viagem
        float travelTime = Vector3.Distance(transform.position, targetPosition) / skill.projectileSpeed;
        
        StartCoroutine(ProjectileImpact(skill, targetPosition, travelTime));
        
        // Spawn effect inicial (lançamento)
        SpawnAttackEffect(skill, transform.position + transform.forward * 1f);
    }

    private void ExecuteSelfTargetSkill(Skill skill)
    {
        // Implementar buffs/cura conforme necessário
        Debug.Log($"Skill self-target: {skill.name}");
        SpawnAttackEffect(skill, transform.position);
    }

    private IEnumerator ProjectileImpact(Skill skill, Vector3 impactPosition, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Verificar inimigos na área de impacto
        float impactRadius = skill.areaRadius > 0 ? skill.GetActualAreaRadius(_stats) : 1f;
        Collider[] hitColliders = Physics.OverlapSphere(impactPosition, impactRadius);
        
        foreach (Collider hitCollider in hitColliders)
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                ApplyDamageToEnemy(enemy, skill);
            }
        }
        
        // Effect de impacto
        SpawnAttackEffect(skill, impactPosition);
    }

    private void ApplyDamageToEnemy(Enemy enemy, Skill skill)
    {
        int damage = skill.GetActualDamage(_stats, inventory?.equippedWeapon);
        
        // Verificar crítico
        bool isCritical = _stats.RollCriticalHit();
        if (isCritical)
        {
            damage = _stats.ApplyCriticalDamage(damage);
        }
        
        // Aplicar dano
        enemy.TakeDamage(damage);
        
        // Mostrar popup
        if (DamagePopupManager.Instance != null)
        {
            DamagePopupManager.Instance.ShowDamage(enemy.transform.position, damage, isCritical);
        }
    }

    private string GetAnimationTrigger(SkillType skillType)
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

    // MÉTODOS AUXILIARES
    public void AddSkill(Skill newSkill)
    {
        skills.Add(newSkill);
        
        if (skillBarUI != null)
        {
            skillBarUI.RefreshSkillBar();
        }
        
        Debug.Log($"Nova skill adicionada: {newSkill.name}");
    }
    
    public void RemoveSkill(int index)
    {
        if (index >= 0 && index < skills.Count)
        {
            string skillName = skills[index].name;
            skills.RemoveAt(index);
            
            if (currentSkillIndex >= skills.Count)
            {
                currentSkillIndex = skills.Count - 1;
            }
            if (currentSkillIndex < 0)
            {
                currentSkillIndex = 0;
            }
            
            if (skillBarUI != null)
            {
                skillBarUI.RefreshSkillBar();
            }
            
            Debug.Log($"Skill removida: {skillName}");
        }
    }

    // Corrotina para resetar a velocidade do animator
    private IEnumerator ResetAnimatorSpeed()
    {
        yield return new WaitForSeconds(0.5f);
        if (animator != null)
        {
            animator.speed = 1f;
        }
    }

    // MÉTODO ANTIGO PARA COMPATIBILIDADE: Calcular dano de habilidade com balanceamento
    private int CalculateSkillDamage(Skill skill)
    {
        int damage;
        
        // Obter arma equipada (se houver)
        Item equippedWeapon = inventory?.equippedWeapon;
        
        // Calcular dano baseado no tipo de habilidade
        if (skill.type == SkillType.Physical)
        {
            damage = _stats.CalculatePhysicalDamage(skill.baseDamage, equippedWeapon);
        }
        else
        {
            damage = _stats.CalculateElementalDamage(skill.baseDamage, skill.type, equippedWeapon);
        }
        
        return damage;
    }
    
    private void SpawnAttackEffect(Skill skill, Vector3 position)
    {
        Debug.Log($"Efeito de ataque para: {skill.name} em {position}");
    }
    
    public void GainExperience(int amount)
    {
        _stats.GainExperience(amount);
    }
    
    public void TakeDamage(int amount, DamageType damageType = DamageType.Physical)
    {
        _stats.TakeDamage(amount, damageType);
        
        if (DamagePopupManager.Instance != null)
        {
            DamagePopupManager.Instance.ShowDamage(transform.position, amount, false);
        }
        
        if (animator != null)
            animator.SetTrigger("Hit");
    }
    
    private void Die()
    {
        Debug.Log("Jogador morreu!");
        
        if (animator != null)
            animator.SetTrigger("Die");
        
        enabled = false;
        Invoke("Respawn", 3f);
    }
    
    private void Respawn()
    {
        _stats.SetHealth(maxHealth / 2);
        _stats.SetMana(maxMana / 2);
        
        enabled = false;
        
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            transform.position = gameManager.GetRespawnPoint();
        }
        
        enabled = true;
    }

    // DEBUG UI
    private void OnGUI()
    {
        if (!showStatsOnGUI || _stats == null) return;
        
        GUI.Box(new Rect(10, 10, 350, 250), "Player Stats");
        
        int yPos = 35;
        int spacing = 20;
        
        GUI.Label(new Rect(20, yPos, 320, 20), $"Level: {_stats.Level} | EXP: {_stats.ExperiencePoints}/{_stats.ExperienceToNextLevel}");
        yPos += spacing;
        
        GUI.Label(new Rect(20, yPos, 320, 20), $"Health: {_stats.Health}/{_stats.MaxHealth} | Mana: {_stats.Mana}/{_stats.MaxMana}");
        yPos += spacing;
        
        GUI.Label(new Rect(20, yPos, 320, 20), $"STR: {_stats.Strength} | INT: {_stats.Intelligence} | DEX: {_stats.Dexterity} | VIT: {_stats.Vitality}");
        yPos += spacing;
        
        GUI.Label(new Rect(20, yPos, 320, 20), $"Crit Chance: {_stats.CriticalChance:P1} | Crit Multi: {_stats.CriticalMultiplier:F2}x");
        yPos += spacing;
        
        GUI.Label(new Rect(20, yPos, 320, 20), $"Attack Speed: {_stats.AttackSpeed:F2} | Cast Speed: {_stats.CastSpeed:F2}");
        yPos += spacing;
        
        GUI.Label(new Rect(20, yPos, 320, 20), $"Phys Res: {_stats.PhysicalResistance:P1} | Elem Res: {_stats.ElementalResistance:P1}");
        yPos += spacing;
        
        if (_stats.AvailableAttributePoints > 0)
        {
            GUI.Label(new Rect(20, yPos, 320, 20), $"<color=yellow>PONTOS DISPONÍVEIS: {_stats.AvailableAttributePoints}</color>");
            yPos += spacing;
        }
        
        // Botões de teste
        if (GUI.Button(new Rect(20, yPos, 100, 30), "Gain 500 EXP"))
        {
            _stats.GainExperience(500);
        }
        
        if (GUI.Button(new Rect(130, yPos, 100, 30), "Debug Stats"))
        {
            _stats.DebugPrintStats();
        }

        if (GUI.Button(new Rect(240, yPos, 100, 30), "Take 50 Dmg"))
        {
            TakeDamage(50, DamageType.Physical);
        }
        
        // INFO DAS SKILLS
        if (skills.Count > 0 && currentSkillIndex < skills.Count)
        {
            Skill currentSkill = skills[currentSkillIndex];
            
            GUI.Box(new Rect(370, 10, 300, 160), $"Current Skill: {currentSkill.name}");
            
            int skillYPos = 35;
            int skillSpacing = 20;
            
            // Verificar se tem métodos avançados
            bool hasAdvancedMethods = currentSkill.GetType().GetMethod("GetActualDamage") != null;
            
            if (hasAdvancedMethods)
            {
                GUI.Label(new Rect(380, skillYPos, 280, 20), $"Dano: {currentSkill.GetActualDamage(_stats, inventory?.equippedWeapon)}");
                skillYPos += skillSpacing;
                
                GUI.Label(new Rect(380, skillYPos, 280, 20), $"Mana: {currentSkill.GetActualManaCost(_stats)}");
                skillYPos += skillSpacing;
                
                GUI.Label(new Rect(380, skillYPos, 280, 20), $"Cooldown: {currentSkill.GetActualCooldown(_stats):F1}s");
                skillYPos += skillSpacing;
                
                GUI.Label(new Rect(380, skillYPos, 280, 20), $"Range: {currentSkill.GetActualRange(_stats):F1}m");
                skillYPos += skillSpacing;
                
                if (currentSkill.areaRadius > 0)
                {
                    GUI.Label(new Rect(380, skillYPos, 280, 20), $"AoE: {currentSkill.GetActualAreaRadius(_stats):F1}m");
                    skillYPos += skillSpacing;
                }
                
                GUI.Label(new Rect(380, skillYPos, 280, 20), $"Tipo: {currentSkill.targetType}");
            }
            else
            {
                // Sistema antigo
                GUI.Label(new Rect(380, skillYPos, 280, 20), $"Dano Base: {currentSkill.baseDamage}");
                skillYPos += skillSpacing;
                
                GUI.Label(new Rect(380, skillYPos, 280, 20), $"Mana: {currentSkill.baseManaoCost}");
                skillYPos += skillSpacing;
                
                GUI.Label(new Rect(380, skillYPos, 280, 20), $"Cooldown: {currentSkill.baseCooldown:F1}s");
                skillYPos += skillSpacing;
                
                float useRange = currentSkill.range > 0 ? currentSkill.range : attackRange;
                GUI.Label(new Rect(380, skillYPos, 280, 20), $"Range: {useRange:F1}m");
                skillYPos += skillSpacing;
                
                GUI.Label(new Rect(380, skillYPos, 280, 20), $"Tipo: {currentSkill.type}");
            }
        }
    }
    
    // GIZMOS VISUAIS
    private void OnDrawGizmos()
    {
        if (skills.Count > 0 && currentSkillIndex < skills.Count && Application.isPlaying && _stats != null)
        {
            Skill currentSkill = skills[currentSkillIndex];
            
            // Verificar se tem métodos avançados
            bool hasAdvancedMethods = currentSkill.GetType().GetMethod("GetActualRange") != null;
            
            if (hasAdvancedMethods)
            {
                float actualRange = currentSkill.GetActualRange(_stats);
                
                // Range da skill atual (verde)
                Gizmos.color = new Color(0, 1, 0, 0.2f);
                Gizmos.DrawSphere(transform.position, actualRange);
                
                // AoE radius se aplicável (vermelho)
                if (currentSkill.areaRadius > 0)
                {
                    Gizmos.color = new Color(1, 0, 0, 0.3f);
                    Vector3 aoeCenter = transform.position + transform.forward * (actualRange * 0.7f);
                    Gizmos.DrawSphere(aoeCenter, currentSkill.GetActualAreaRadius(_stats));
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
            else
            {
                // Sistema antigo - usar range da skill ou range global
                float useRange = currentSkill.range > 0 ? currentSkill.range : attackRange;
                Gizmos.color = new Color(1, 1, 0, 0.2f);
                Gizmos.DrawSphere(transform.position, useRange);
            }
        }
        else if (!Application.isPlaying)
        {
            // No editor, mostrar range padrão
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawSphere(transform.position, attackRange);
        }
        
        // Gizmos de movimento
        if (Application.isPlaying && worldMoveDirection.sqrMagnitude > 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, worldMoveDirection * 2);
        }
        
        if (Application.isPlaying && inputDirection.sqrMagnitude > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, inputDirection * 2);
        }
    }
}