using UnityEngine;
using System;

[Serializable]
public class PlayerStats
{
    // Estatísticas básicas
    public int Level { get; private set; } = 1;
    public int ExperiencePoints { get; private set; } = 0;
    public int ExperienceToNextLevel { get; private set; } = 100;
    
    // Saúde e mana
    public int Health { get; private set; } = 100;
    public int MaxHealth { get; private set; } = 100;
    public int Mana { get; private set; } = 50;
    public int MaxMana { get; private set; } = 50;
    
    // Atributos base (sem modificadores de equipamentos)
    [SerializeField] private int _baseStrength = 10;
    [SerializeField] private int _baseIntelligence = 10;
    [SerializeField] private int _baseDexterity = 10;
    [SerializeField] private int _baseVitality = 10;
    
    // Modificadores de equipamentos e buffs
    [SerializeField] private int _strengthModifier = 0;
    [SerializeField] private int _intelligenceModifier = 0;
    [SerializeField] private int _dexterityModifier = 0;
    [SerializeField] private int _vitalityModifier = 0;
    
    // Pontos de atributo disponíveis para distribuir
    public int AvailableAttributePoints { get; private set; } = 0;
    public int AttributePointsPerLevel = 5;
    
    // Propriedades calculadas (base + modificadores)
    public int Strength => _baseStrength + _strengthModifier;
    public int Intelligence => _baseIntelligence + _intelligenceModifier;
    public int Dexterity => _baseDexterity + _dexterityModifier;
    public int Vitality => _baseVitality + _vitalityModifier;
    
    // Estatísticas derivadas para combate
    public float CriticalChance => Mathf.Min(0.05f + (Dexterity * 0.002f), 0.75f); // 5% base + 0.2% por DEX, máximo 75%
    public float CriticalMultiplier => 1.5f + (Dexterity * 0.01f); // 150% base + 1% por DEX
    public float AttackSpeed => 1.0f + (Dexterity * 0.005f); // Velocidade base + 0.5% por DEX
    public float CastSpeed => 1.0f + (Intelligence * 0.005f); // Velocidade de cast + 0.5% por INT
    
    // Resistências
    public float PhysicalResistance => Mathf.Min(Vitality * 0.003f, 0.75f); // 0.3% por VIT, máximo 75%
    public float ElementalResistance { get; private set; } = 0f; // Vem de equipamentos
    
    // Eventos para notificar mudanças
    public event Action<int> OnHealthChanged;
    public event Action<int> OnManaChanged;
    public event Action<int> OnLevelUp;
    public event Action<int> OnExperienceGained;
    public event Action<int> OnAttributePointsGained;
    public event Action OnAttributeChanged;
    
    // Construtor
    public PlayerStats(int level = 1, int strength = 10, int intelligence = 10, int dexterity = 10, int vitality = 10)
    {
        Level = level;
        _baseStrength = strength;
        _baseIntelligence = intelligence;
        _baseDexterity = dexterity;
        _baseVitality = vitality;
        
        // Calcular valores derivados
        RecalculateStats();
        
        // Inicializar com saúde e mana cheias
        Health = MaxHealth;
        Mana = MaxMana;
    }
    
    // SISTEMA DE DISTRIBUIÇÃO DE ATRIBUTOS
    public bool CanSpendAttributePoint(string attribute)
    {
        return AvailableAttributePoints > 0;
    }
    
    public bool SpendAttributePoint(string attribute)
    {
        if (!CanSpendAttributePoint(attribute))
            return false;
        
        switch (attribute.ToLower())
        {
            case "strength":
                _baseStrength++;
                break;
            case "intelligence":
                _baseIntelligence++;
                break;
            case "dexterity":
                _baseDexterity++;
                break;
            case "vitality":
                _baseVitality++;
                break;
            default:
                return false;
        }
        
        AvailableAttributePoints--;
        RecalculateStats();
        OnAttributeChanged?.Invoke();
        
        Debug.Log($"Atributo {attribute} aumentado! Pontos restantes: {AvailableAttributePoints}");
        return true;
    }
    
    // CÁLCULOS DE DANO BALANCEADOS
    public int CalculatePhysicalDamage(int baseDamage, Item weapon = null)
    {
        float damage = baseDamage;
        
        // Dano base da arma
        if (weapon != null)
        {
            damage += weapon.physicalDamage;
        }
        
        // Modificador de força (cada ponto de força adiciona 2% ao dano físico)
        float strengthMultiplier = 1.0f + (Strength * 0.02f);
        damage *= strengthMultiplier;
        
        // Variação aleatória (-5% a +5%)
        damage *= UnityEngine.Random.Range(0.95f, 1.05f);
        
        return Mathf.RoundToInt(damage);
    }
    
    public int CalculateElementalDamage(int baseDamage, SkillType elementType, Item weapon = null)
    {
        float damage = baseDamage;
        
        // Dano elemental da arma
        if (weapon != null)
        {
            switch (elementType)
            {
                case SkillType.Fire:
                    damage += weapon.fireDamage;
                    break;
                case SkillType.Ice:
                    damage += weapon.iceDamage;
                    break;
                case SkillType.Lightning:
                    damage += weapon.lightningDamage;
                    break;
                case SkillType.Poison:
                    damage += weapon.poisonDamage;
                    break;
            }
        }
        
        // Modificador de inteligência (cada ponto de inteligência adiciona 2.5% ao dano elemental)
        float intelligenceMultiplier = 1.0f + (Intelligence * 0.025f);
        damage *= intelligenceMultiplier;
        
        // Variação aleatória
        damage *= UnityEngine.Random.Range(0.95f, 1.05f);
        
        return Mathf.RoundToInt(damage);
    }
    
    public bool RollCriticalHit()
    {
        return UnityEngine.Random.value < CriticalChance;
    }
    
    public int ApplyCriticalDamage(int damage)
    {
        return Mathf.RoundToInt(damage * CriticalMultiplier);
    }
    
    // FÓRMULA DE EXPERIÊNCIA MELHORADA
    private int CalculateExperienceRequired(int level)
    {
        // Fórmula inspirada em Diablo: EXP = level^2 * 100 + level * 50
        return (level * level * 100) + (level * 50);
    }
    
    // Métodos para modificar saúde e mana
    public void SetHealth(int value)
    {
        Health = Mathf.Clamp(value, 0, MaxHealth);
        OnHealthChanged?.Invoke(Health);
    }
    
    public void SetMana(int value)
    {
        Mana = Mathf.Clamp(value, 0, MaxMana);
        OnManaChanged?.Invoke(Mana);
    }
    
    // Dano com resistência aplicada
    public void TakeDamage(int amount, DamageType damageType = DamageType.Physical)
    {
        if (amount < 0) return;
        
        float finalDamage = amount;
        
        // Aplicar resistências
        switch (damageType)
        {
            case DamageType.Physical:
                finalDamage *= (1.0f - PhysicalResistance);
                break;
            case DamageType.Elemental:
                finalDamage *= (1.0f - ElementalResistance);
                break;
        }
        
        // Chance de bloquear (baseada em equipamentos - a ser implementado)
        // if (RollBlock()) finalDamage *= 0.5f;
        
        int actualDamage = Mathf.RoundToInt(finalDamage);
        Health = Mathf.Max(0, Health - actualDamage);
        OnHealthChanged?.Invoke(Health);
        
        Debug.Log($"Tomou {actualDamage} de dano ({damageType}). Saúde restante: {Health}/{MaxHealth}");
    }
    
    public void Heal(int amount)
    {
        if (amount < 0) return;
        
        Health = Mathf.Min(MaxHealth, Health + amount);
        OnHealthChanged?.Invoke(Health);
    }
    
    // Consumo de mana com desconto baseado em inteligência
    public bool UseMana(int amount)
    {
        // Redução de custo de mana baseada em inteligência (máximo 30% de redução)
        float manaReduction = Mathf.Min(Intelligence * 0.001f, 0.30f);
        int actualCost = Mathf.RoundToInt(amount * (1.0f - manaReduction));
        
        if (Mana < actualCost) return false;
        
        Mana -= actualCost;
        OnManaChanged?.Invoke(Mana);
        return true;
    }
    
    public void RestoreMana(int amount)
    {
        Mana = Mathf.Min(MaxMana, Mana + amount);
        OnManaChanged?.Invoke(Mana);
    }
    
    // Ganho de experiência balanceado
    public void GainExperience(int amount)
    {
        if (amount <= 0) return;
        
        ExperiencePoints += amount;
        OnExperienceGained?.Invoke(amount);
        
        Debug.Log($"Ganhou {amount} pontos de experiência! Total: {ExperiencePoints}");
        
        CheckLevelUp();
    }
    
    private void CheckLevelUp()
    {
        while (ExperiencePoints >= ExperienceToNextLevel)
        {
            LevelUp();
        }
    }
    
    private void LevelUp()
    {
        Level++;
        ExperiencePoints -= ExperienceToNextLevel;
        
        // Recalcular experiência necessária para o próximo nível
        ExperienceToNextLevel = CalculateExperienceRequired(Level);
        
        // Ganhar pontos de atributo
        AvailableAttributePoints += AttributePointsPerLevel;
        OnAttributePointsGained?.Invoke(AttributePointsPerLevel);
        
        // Atualizar saúde e mana máxima
        RecalculateStats();
        
        // Restaurar saúde e mana (opcional - remover se quiser mais desafio)
        Health = MaxHealth;
        Mana = MaxMana;
        
        OnLevelUp?.Invoke(Level);
        
        Debug.Log($"Level up! Agora nível {Level}. Ganhou {AttributePointsPerLevel} pontos de atributo para distribuir!");
    }
    
    // Recalcular estatísticas baseadas nos atributos
    public void RecalculateStats()
    {
        int oldMaxHealth = MaxHealth;
        int oldMaxMana = MaxMana;
        
        // Fórmulas balanceadas para saúde e mana
        MaxHealth = 100 + (Level * 5) + (Vitality * 8);  // +5 HP por nível + 8 HP por VIT
        MaxMana = 50 + (Level * 3) + (Intelligence * 6);  // +3 MP por nível + 6 MP por INT
        
        // Ajustar saúde e mana atuais proporcionalmente
        if (oldMaxHealth > 0)
        {
            float healthRatio = (float)Health / oldMaxHealth;
            Health = Mathf.RoundToInt(MaxHealth * healthRatio);
        }
        
        if (oldMaxMana > 0)
        {
            float manaRatio = (float)Mana / oldMaxMana;
            Mana = Mathf.RoundToInt(MaxMana * manaRatio);
        }
        
        // Garantir que não exceda os máximos
        Health = Mathf.Min(Health, MaxHealth);
        Mana = Mathf.Min(Mana, MaxMana);
    }
    
    // Método para equipamentos ajustarem atributos
    public void AdjustAttribute(string attribute, int amount)
    {
        switch (attribute.ToLower())
        {
            case "strength":
                _strengthModifier += amount;
                break;
            case "intelligence":
                _intelligenceModifier += amount;
                break;
            case "dexterity":
                _dexterityModifier += amount;
                break;
            case "vitality":
                _vitalityModifier += amount;
                break;
            default:
                Debug.LogWarning($"Tentativa de ajustar atributo desconhecido: {attribute}");
                return;
        }
        
        RecalculateStats();
        OnAttributeChanged?.Invoke();
    }
    
    // Método para debug - mostrar todas as estatísticas
    public void DebugPrintStats()
    {
        Debug.Log("=== ESTATÍSTICAS DO JOGADOR ===");
        Debug.Log($"Nível: {Level} | EXP: {ExperiencePoints}/{ExperienceToNextLevel}");
        Debug.Log($"Saúde: {Health}/{MaxHealth} | Mana: {Mana}/{MaxMana}");
        Debug.Log($"FOR: {Strength} ({_baseStrength}+{_strengthModifier}) | INT: {Intelligence} ({_baseIntelligence}+{_intelligenceModifier})");
        Debug.Log($"DES: {Dexterity} ({_baseDexterity}+{_dexterityModifier}) | VIT: {Vitality} ({_baseVitality}+{_vitalityModifier})");
        Debug.Log($"Crítico: {CriticalChance:P1} | Mult. Crítico: {CriticalMultiplier:F2}x");
        Debug.Log($"Velocidade Ataque: {AttackSpeed:F2} | Velocidade Cast: {CastSpeed:F2}");
        Debug.Log($"Resistência Física: {PhysicalResistance:P1} | Resistência Elemental: {ElementalResistance:P1}");
        Debug.Log($"Pontos de Atributo Disponíveis: {AvailableAttributePoints}");
    }
    
    // Serialização
    public string Serialize()
    {
        return JsonUtility.ToJson(this);
    }
    
    public static PlayerStats Deserialize(string json)
    {
        try
        {
            return JsonUtility.FromJson<PlayerStats>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao carregar estatísticas do jogador: {e.Message}");
            return new PlayerStats();
        }
    }
}

// Enum para tipos de dano
public enum DamageType
{
    Physical,
    Elemental,
    True // Dano que ignora resistências
}