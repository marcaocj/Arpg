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
    public float CriticalChance => Mathf.Min(0.05f + (Dexterity * 0.002f), 0.75f);
    public float CriticalMultiplier => 1.5f + (Dexterity * 0.01f);
    public float AttackSpeed => 1.0f + (Dexterity * 0.005f);
    public float CastSpeed => 1.0f + (Intelligence * 0.005f);
    
    // Resistências
    public float PhysicalResistance => Mathf.Min(Vitality * 0.003f, 0.75f);
    public float ElementalResistance { get; private set; } = 0f;
    
    // Construtor
    public PlayerStats(int level = 1, int strength = 10, int intelligence = 10, int dexterity = 10, int vitality = 10)
    {
        Level = level;
        _baseStrength = strength;
        _baseIntelligence = intelligence;
        _baseDexterity = dexterity;
        _baseVitality = vitality;
        
        RecalculateStats();
        
        Health = MaxHealth;
        Mana = MaxMana;
    }
    
    // ADICIONADO: Método para disparar eventos iniciais da UI
    public void TriggerInitialUIUpdate()
    {
        EventManager.TriggerEvent(new PlayerHealthChangedEvent
        {
            currentHealth = Health,
            maxHealth = MaxHealth,
            healthDelta = 0,
            damageType = DamageType.Physical
        });
        
        EventManager.TriggerEvent(new PlayerManaChangedEvent
        {
            currentMana = Mana,
            maxMana = MaxMana,
            manaDelta = 0
        });
        
        EventManager.TriggerEvent(new PlayerStatsRecalculatedEvent
        {
            strength = Strength,
            intelligence = Intelligence,
            dexterity = Dexterity,
            vitality = Vitality,
            maxHealth = MaxHealth,
            maxMana = MaxMana
        });
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
        
        int oldValue = 0;
        
        switch (attribute.ToLower())
        {
            case "strength":
                oldValue = _baseStrength;
                _baseStrength++;
                break;
            case "intelligence":
                oldValue = _baseIntelligence;
                _baseIntelligence++;
                break;
            case "dexterity":
                oldValue = _baseDexterity;
                _baseDexterity++;
                break;
            case "vitality":
                oldValue = _baseVitality;
                _baseVitality++;
                break;
            default:
                return false;
        }
        
        AvailableAttributePoints--;
        
        // Disparar evento de mudança de atributo
        EventManager.TriggerEvent(new PlayerAttributeChangedEvent
        {
            attributeName = attribute,
            oldValue = oldValue,
            newValue = oldValue + 1,
            isTemporary = false
        });
        
        RecalculateStats();
        
        Debug.Log($"Atributo {attribute} aumentado! Pontos restantes: {AvailableAttributePoints}");
        return true;
    }
    
    // CÁLCULOS DE DANO BALANCEADOS
    public int CalculatePhysicalDamage(int baseDamage, Item weapon = null)
    {
        float damage = baseDamage;
        
        if (weapon != null)
        {
            damage += weapon.physicalDamage;
        }
        
        float strengthMultiplier = 1.0f + (Strength * 0.02f);
        damage *= strengthMultiplier;
        
        damage *= UnityEngine.Random.Range(0.95f, 1.05f);
        
        return Mathf.RoundToInt(damage);
    }
    
    public int CalculateElementalDamage(int baseDamage, SkillType elementType, Item weapon = null)
    {
        float damage = baseDamage;
        
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
        
        float intelligenceMultiplier = 1.0f + (Intelligence * 0.025f);
        damage *= intelligenceMultiplier;
        
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
    
    private int CalculateExperienceRequired(int level)
    {
        return (level * level * 100) + (level * 50);
    }
    
    // Métodos para modificar saúde e mana usando EventManager
    public void SetHealth(int value)
    {
        int oldHealth = Health;
        Health = Mathf.Clamp(value, 0, MaxHealth);
        
        EventManager.TriggerEvent(new PlayerHealthChangedEvent
        {
            currentHealth = Health,
            maxHealth = MaxHealth,
            healthDelta = Health - oldHealth,
            damageType = DamageType.Physical
        });
    }
    
    public void SetMana(int value)
    {
        int oldMana = Mana;
        Mana = Mathf.Clamp(value, 0, MaxMana);
        
        EventManager.TriggerEvent(new PlayerManaChangedEvent
        {
            currentMana = Mana,
            maxMana = MaxMana,
            manaDelta = Mana - oldMana
        });
    }
    
    // Dano com resistência aplicada
    public void TakeDamage(int amount, DamageType damageType = DamageType.Physical)
    {
        if (amount < 0) return;
        
        float finalDamage = amount;
        
        switch (damageType)
        {
            case DamageType.Physical:
                finalDamage *= (1.0f - PhysicalResistance);
                break;
            case DamageType.Elemental:
                finalDamage *= (1.0f - ElementalResistance);
                break;
        }
        
        int actualDamage = Mathf.RoundToInt(finalDamage);
        int oldHealth = Health;
        Health = Mathf.Max(0, Health - actualDamage);
        
        EventManager.TriggerEvent(new PlayerHealthChangedEvent
        {
            currentHealth = Health,
            maxHealth = MaxHealth,
            healthDelta = Health - oldHealth,
            damageType = damageType
        });
        
        Debug.Log($"Tomou {actualDamage} de dano ({damageType}). Saúde restante: {Health}/{MaxHealth}");
    }
    
    public void Heal(int amount)
    {
        if (amount < 0) return;
        
        int oldHealth = Health;
        Health = Mathf.Min(MaxHealth, Health + amount);
        
        EventManager.TriggerEvent(new PlayerHealthChangedEvent
        {
            currentHealth = Health,
            maxHealth = MaxHealth,
            healthDelta = Health - oldHealth,
            damageType = DamageType.True
        });
    }
    
    public bool UseMana(int amount)
    {
        float manaReduction = Mathf.Min(Intelligence * 0.001f, 0.30f);
        int actualCost = Mathf.RoundToInt(amount * (1.0f - manaReduction));
        
        if (Mana < actualCost) return false;
        
        int oldMana = Mana;
        Mana -= actualCost;
        
        EventManager.TriggerEvent(new PlayerManaChangedEvent
        {
            currentMana = Mana,
            maxMana = MaxMana,
            manaDelta = Mana - oldMana
        });
        
        return true;
    }
    
    public void RestoreMana(int amount)
    {
        int oldMana = Mana;
        Mana = Mathf.Min(MaxMana, Mana + amount);
        
        EventManager.TriggerEvent(new PlayerManaChangedEvent
        {
            currentMana = Mana,
            maxMana = MaxMana,
            manaDelta = Mana - oldMana
        });
    }
    
    // Ganho de experiência usando EventManager
    public void GainExperience(int amount)
    {
        if (amount <= 0) return;
        
        ExperiencePoints += amount;
        
        EventManager.TriggerEvent(new PlayerExperienceGainedEvent
        {
            experienceGained = amount,
            currentExperience = ExperiencePoints,
            experienceToNextLevel = ExperienceToNextLevel
        });
        
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
        int oldLevel = Level;
        Level++;
        ExperiencePoints -= ExperienceToNextLevel;
        
        ExperienceToNextLevel = CalculateExperienceRequired(Level);
        
        AvailableAttributePoints += AttributePointsPerLevel;
        
        RecalculateStats();
        
        Health = MaxHealth;
        Mana = MaxMana;
        
        // Disparar evento de level up
        EventManager.TriggerEvent(new PlayerLevelUpEvent
        {
            newLevel = Level,
            oldLevel = oldLevel,
            attributePointsGained = AttributePointsPerLevel
        });
        
        Debug.Log($"Level up! Agora nível {Level}. Ganhou {AttributePointsPerLevel} pontos de atributo para distribuir!");
    }
    
    // Recalcular estatísticas baseadas nos atributos
    public void RecalculateStats()
    {
        int oldMaxHealth = MaxHealth;
        int oldMaxMana = MaxMana;
        
        MaxHealth = 100 + (Level * 5) + (Vitality * 8);
        MaxMana = 50 + (Level * 3) + (Intelligence * 6);
        
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
        
        Health = Mathf.Min(Health, MaxHealth);
        Mana = Mathf.Min(Mana, MaxMana);
        
        // Disparar evento de recálculo de stats
        EventManager.TriggerEvent(new PlayerStatsRecalculatedEvent
        {
            strength = Strength,
            intelligence = Intelligence,
            dexterity = Dexterity,
            vitality = Vitality,
            maxHealth = MaxHealth,
            maxMana = MaxMana
        });
    }
    
    // Método para equipamentos ajustarem atributos
    public void AdjustAttribute(string attribute, int amount)
    {
        int oldValue = 0;
        
        switch (attribute.ToLower())
        {
            case "strength":
                oldValue = _strengthModifier;
                _strengthModifier += amount;
                break;
            case "intelligence":
                oldValue = _intelligenceModifier;
                _intelligenceModifier += amount;
                break;
            case "dexterity":
                oldValue = _dexterityModifier;
                _dexterityModifier += amount;
                break;
            case "vitality":
                oldValue = _vitalityModifier;
                _vitalityModifier += amount;
                break;
            default:
                Debug.LogWarning($"Tentativa de ajustar atributo desconhecido: {attribute}");
                return;
        }
        
        EventManager.TriggerEvent(new PlayerAttributeChangedEvent
        {
            attributeName = attribute,
            oldValue = oldValue,
            newValue = oldValue + amount,
            isTemporary = true
        });
        
        RecalculateStats();
    }
    
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

public enum DamageType
{
    Physical,
    Elemental,
    True // Dano que ignora resistências
}