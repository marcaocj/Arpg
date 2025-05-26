using UnityEngine;
using System;

/// <summary>
/// Gerencia todas as estatísticas do jogador de forma centralizada
/// </summary>
public class PlayerStatsManager : MonoBehaviour
{
    [Header("Configurações Iniciais")]
    [SerializeField] private int initialLevel = 1;
    [SerializeField] private int initialStrength = 10;
    [SerializeField] private int initialIntelligence = 10;
    [SerializeField] private int initialDexterity = 10;
    [SerializeField] private int initialVitality = 10;
    
    // Instância de PlayerStats
    private PlayerStats _stats;
    
    // Propriedades públicas para acesso
    public PlayerStats Stats => _stats;
    public int Level => _stats.Level;
    public int ExperiencePoints => _stats.ExperiencePoints;
    public int ExperienceToNextLevel => _stats.ExperienceToNextLevel;
    public int Strength => _stats.Strength;
    public int Intelligence => _stats.Intelligence;
    public int Dexterity => _stats.Dexterity;
    public int Vitality => _stats.Vitality;
    public int AvailableAttributePoints => _stats.AvailableAttributePoints;
    
    // Eventos para mudanças de stats
    public event Action<int> OnLevelUp;
    public event Action<int> OnExperienceGained;
    public event Action<string, int> OnAttributeChanged;
    
    private void Awake()
    {
        InitializeStats();
    }
    
    private void Start()
    {
        // Disparar eventos iniciais após um frame
        StartCoroutine(TriggerInitialEvents());
    }
    
    private void InitializeStats()
    {
        _stats = new PlayerStats(initialLevel, initialStrength, initialIntelligence, initialDexterity, initialVitality);
        
        // Subscrever aos eventos internos das stats
        SubscribeToInternalEvents();
    }
    
    private void SubscribeToInternalEvents()
    {
        // Aqui podemos adicionar listeners internos se necessário
    }
    
    private System.Collections.IEnumerator TriggerInitialEvents()
    {
        yield return new WaitForEndOfFrame();
        
        if (_stats != null)
        {
            _stats.TriggerInitialUIUpdate();
        }
    }
    
    #region Experience and Leveling
    
    public void GainExperience(int amount)
    {
        if (amount <= 0) return;
        
        int oldLevel = _stats.Level;
        _stats.GainExperience(amount);
        
        OnExperienceGained?.Invoke(amount);
        
        if (_stats.Level > oldLevel)
        {
            OnLevelUp?.Invoke(_stats.Level);
        }
    }
    
    #endregion
    
    #region Attribute Management
    
    public bool CanSpendAttributePoint(string attribute)
    {
        return _stats.CanSpendAttributePoint(attribute);
    }
    
    public bool SpendAttributePoint(string attribute)
    {
        bool success = _stats.SpendAttributePoint(attribute);
        
        if (success)
        {
            OnAttributeChanged?.Invoke(attribute, GetAttributeValue(attribute));
        }
        
        return success;
    }
    
    public int GetAttributeValue(string attribute)
    {
        switch (attribute.ToLower())
        {
            case "strength": return _stats.Strength;
            case "intelligence": return _stats.Intelligence;
            case "dexterity": return _stats.Dexterity;
            case "vitality": return _stats.Vitality;
            default: return 0;
        }
    }
    
    public void AdjustAttribute(string attribute, int amount)
    {
        _stats.AdjustAttribute(attribute, amount);
        OnAttributeChanged?.Invoke(attribute, GetAttributeValue(attribute));
    }
    
    #endregion
    
    #region Combat Stats
    
    public float GetCriticalChance() => _stats.CriticalChance;
    public float GetCriticalMultiplier() => _stats.CriticalMultiplier;
    public float GetAttackSpeed() => _stats.AttackSpeed;
    public float GetCastSpeed() => _stats.CastSpeed;
    public float GetPhysicalResistance() => _stats.PhysicalResistance;
    public float GetElementalResistance() => _stats.ElementalResistance;
    
    public bool RollCriticalHit() => _stats.RollCriticalHit();
    public int ApplyCriticalDamage(int damage) => _stats.ApplyCriticalDamage(damage);
    
    public int CalculatePhysicalDamage(int baseDamage, Item weapon = null)
    {
        return _stats.CalculatePhysicalDamage(baseDamage, weapon);
    }
    
    public int CalculateElementalDamage(int baseDamage, SkillType elementType, Item weapon = null)
    {
        return _stats.CalculateElementalDamage(baseDamage, elementType, weapon);
    }
    
    #endregion
    
    #region Utility
    
    public void DebugPrintStats()
    {
        _stats.DebugPrintStats();
    }
    
    public string SerializeStats()
    {
        return _stats.Serialize();
    }
    
    public void LoadStats(string json)
    {
        try
        {
            _stats = PlayerStats.Deserialize(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao carregar stats: {e.Message}");
            InitializeStats();
        }
    }
    
    #endregion
}