using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "RPG/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Player Settings")]
    [Tooltip("Velocidade base de movimento do jogador")]
    public float playerMoveSpeed = 6f;
    
    [Tooltip("Velocidade de rotação do jogador")]
    public float playerRotationSpeed = 20f;
    
    [Tooltip("Multiplicador de velocidade ao andar para trás")]
    [Range(0.1f, 1f)]
    public float backwardSpeedMultiplier = 0.7f;
    
    [Tooltip("Multiplicador de velocidade ao andar de lado")]
    [Range(0.1f, 1f)]
    public float strafeSpeedMultiplier = 0.8f;
    
    [Header("Combat Settings")]
    [Tooltip("Range padrão de ataque")]
    public float defaultAttackRange = 3f;
    
    [Tooltip("Cooldown padrão de ataque")]
    public float defaultAttackCooldown = 1f;
    
    [Tooltip("Duração da animação de ataque")]
    public float attackAnimationDuration = 0.5f;
    
    [Header("UI Settings")]
    [Tooltip("Intervalo de atualização da UI (em segundos)")]
    public float uiUpdateInterval = 0.1f;
    
    [Tooltip("Duração das notificações")]
    public float notificationDuration = 2f;
    
    [Tooltip("Intervalo de atualização da skill bar")]
    public float skillBarUpdateInterval = 0.5f;
    
    [Header("Damage Popup Settings")]
    [Tooltip("Velocidade de movimento dos popups de dano")]
    public float damagePopupMoveSpeed = 1.5f;
    
    [Tooltip("Tempo de vida dos popups")]
    public float damagePopupLifetime = 1f;
    
    [Tooltip("Velocidade de desaparecimento")]
    public float damagePopupDisappearSpeed = 3f;
    
    [Header("Experience & Leveling")]
    [Tooltip("Pontos de atributo ganhos por nível")]
    public int attributePointsPerLevel = 5;
    
    [Tooltip("Multiplicador de experiência base")]
    public float experienceMultiplier = 1f;
    
    [Header("Loot Settings")]
    [Tooltip("Distância de coleta automática de itens")]
    public float lootPickupDistance = 1.5f;
    
    [Tooltip("Velocidade de atração dos itens")]
    public float lootAttractSpeed = 5f;
    
    [Tooltip("Altura do bounce dos itens")]
    public float lootBobHeight = 0.2f;
    
    [Tooltip("Velocidade do bounce")]
    public float lootBobSpeed = 2f;
    
    [Header("Performance Settings")]
    [Tooltip("Usar object pooling para damage popups")]
    public bool useDamagePopupPooling = true;
    
    [Tooltip("Tamanho inicial do pool de damage popups")]
    public int damagePopupPoolSize = 20;
    
    [Tooltip("Distância máxima para renderizar efeitos")]
    public float maxEffectRenderDistance = 50f;
    
    [Header("Debug Settings")]
    [Tooltip("Mostrar gizmos de debug")]
    public bool showDebugGizmos = true;
    
    [Tooltip("Mostrar informações de performance")]
    public bool showPerformanceInfo = false;
    
    [Tooltip("Log detalhado de combate")]
    public bool verboseCombatLogging = false;
    
    // Singleton para acesso global
    private static GameConfig _instance;
    public static GameConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameConfig>("GameConfig");
                if (_instance == null)
                {
                    // CORRIGIDO: Usar UnityEngine.Debug diretamente para evitar loop
                    UnityEngine.Debug.LogWarning("GameConfig não encontrado em Resources! Criando configuração padrão.");
                    _instance = CreateInstance<GameConfig>();
                }
            }
            return _instance;
        }
    }
    
    [Header("Skill Scaling")]
    [Tooltip("Percentual de scaling de força por ponto")]
    [Range(0.01f, 0.1f)]
    public float strengthScalingPerPoint = 0.025f;
    
    [Tooltip("Percentual de scaling de inteligência por ponto")]
    [Range(0.01f, 0.1f)]
    public float intelligenceScalingPerPoint = 0.03f;
    
    [Tooltip("Percentual de scaling de destreza por ponto")]
    [Range(0.01f, 0.1f)]
    public float dexterityScalingPerPoint = 0.015f;
    
    [Tooltip("Percentual de scaling de vitalidade por ponto")]
    [Range(0.001f, 0.01f)]
    public float vitalityScalingPerPoint = 0.003f;
    
    [Header("Attribute Formulas")]
    [Tooltip("HP base + (level * valorPorLevel) + (vitality * valorPorVitality)")]
    public int baseHealth = 100;
    public int healthPerLevel = 5;
    public int healthPerVitality = 8;
    
    [Tooltip("MP base + (level * valorPorLevel) + (intelligence * valorPorIntelligence)")]
    public int baseMana = 50;
    public int manaPerLevel = 3;
    public int manaPerIntelligence = 6;
    
    [Header("Critical Hit Settings")]
    [Tooltip("Chance de crítico base")]
    [Range(0f, 0.2f)]
    public float baseCriticalChance = 0.05f;
    
    [Tooltip("Chance de crítico por ponto de destreza")]
    [Range(0.001f, 0.01f)]
    public float criticalChancePerDexterity = 0.002f;
    
    [Tooltip("Multiplicador de crítico base")]
    [Range(1f, 3f)]
    public float baseCriticalMultiplier = 1.5f;
    
    [Tooltip("Multiplicador de crítico por ponto de destreza")]
    [Range(0.001f, 0.05f)]
    public float criticalMultiplierPerDexterity = 0.01f;
    
    [Header("Speed Settings")]
    [Tooltip("Velocidade de ataque base")]
    public float baseAttackSpeed = 1f;
    
    [Tooltip("Velocidade de ataque por ponto de destreza")]
    [Range(0.001f, 0.02f)]
    public float attackSpeedPerDexterity = 0.005f;
    
    [Tooltip("Velocidade de cast base")]
    public float baseCastSpeed = 1f;
    
    [Tooltip("Velocidade de cast por ponto de inteligência")]
    [Range(0.001f, 0.02f)]
    public float castSpeedPerIntelligence = 0.005f;
    
    [Header("Resistance Settings")]
    [Tooltip("Resistência física máxima")]
    [Range(0.5f, 0.9f)]
    public float maxPhysicalResistance = 0.75f;
    
    [Tooltip("Resistência elemental máxima")]
    [Range(0.5f, 0.9f)]
    public float maxElementalResistance = 0.75f;
    
    [Header("Mana Cost Reduction")]
    [Tooltip("Redução máxima de custo de mana por inteligência")]
    [Range(0.1f, 0.5f)]
    public float maxManaReduction = 0.30f;
    
    [Tooltip("Redução de mana por ponto de inteligência")]
    [Range(0.0001f, 0.005f)]
    public float manaReductionPerIntelligence = 0.001f;
    
    // Métodos de validação
    private void OnValidate()
    {
        // Garantir valores mínimos
        playerMoveSpeed = Mathf.Max(1f, playerMoveSpeed);
        playerRotationSpeed = Mathf.Max(1f, playerRotationSpeed);
        uiUpdateInterval = Mathf.Max(0.01f, uiUpdateInterval);
        notificationDuration = Mathf.Max(0.5f, notificationDuration);
        
        // Garantir que os pools tenham tamanho mínimo
        damagePopupPoolSize = Mathf.Max(5, damagePopupPoolSize);
        
        // Validar fórmulas de atributos
        baseHealth = Mathf.Max(50, baseHealth);
        baseMana = Mathf.Max(20, baseMana);
        healthPerLevel = Mathf.Max(1, healthPerLevel);
        manaPerLevel = Mathf.Max(1, manaPerLevel);
        healthPerVitality = Mathf.Max(1, healthPerVitality);
        manaPerIntelligence = Mathf.Max(1, manaPerIntelligence);
    }
    
    // Métodos de conveniência para acesso rápido
    public static class Combat
    {
        public static float AttackRange => Instance.defaultAttackRange;
        public static float AttackCooldown => Instance.defaultAttackCooldown;
        public static bool VerboseLogging => Instance.verboseCombatLogging;
    }
    
    public static class UI
    {
        public static float UpdateInterval => Instance.uiUpdateInterval;
        public static float NotificationDuration => Instance.notificationDuration;
        public static float SkillBarUpdateInterval => Instance.skillBarUpdateInterval;
    }
    
    public static class Player
    {
        public static float MoveSpeed => Instance.playerMoveSpeed;
        public static float RotationSpeed => Instance.playerRotationSpeed;
        public static float BackwardMultiplier => Instance.backwardSpeedMultiplier;
        public static float StrafeMultiplier => Instance.strafeSpeedMultiplier;
    }
    
    public static class Loot
    {
        public static float PickupDistance => Instance.lootPickupDistance;
        public static float AttractSpeed => Instance.lootAttractSpeed;
        public static float BobHeight => Instance.lootBobHeight;
        public static float BobSpeed => Instance.lootBobSpeed;
    }
    
    public static class Performance
    {
        public static bool UseDamagePopupPooling => Instance.useDamagePopupPooling;
        public static int DamagePopupPoolSize => Instance.damagePopupPoolSize;
        public static float MaxEffectRenderDistance => Instance.maxEffectRenderDistance;
    }
    
    public static class Debug
    {
        public static bool ShowGizmos => Instance.showDebugGizmos;
        public static bool ShowPerformanceInfo => Instance.showPerformanceInfo;
        public static bool VerboseLogging => Instance.verboseCombatLogging;
        
        // Métodos de conveniência para logging condicional
        public static void Log(string message)
        {
            if (VerboseLogging)
                UnityEngine.Debug.Log($"[GameConfig] {message}");
        }
        
        public static void LogWarning(string message)
        {
            if (VerboseLogging)
                UnityEngine.Debug.LogWarning($"[GameConfig] {message}");
        }
        
        public static void LogError(string message)
        {
            UnityEngine.Debug.LogError($"[GameConfig] {message}");
        }
    }
}