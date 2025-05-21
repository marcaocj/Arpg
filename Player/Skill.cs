using UnityEngine;
using System.Collections.Generic;
using System;

public enum SkillType
{
    Physical,
    Fire,
    Ice,
    Lightning,
    Poison
}

public enum SkillTargetType
{
    Single,     // Ataque único
    Area,       // Área de efeito
    Projectile, // Projétil
    Self        // Buff/cura própria
}

[System.Serializable]
public class Skill
{
    [Header("Informações Básicas")]
    public string name;
    public string description;
    public SkillType type;
    public SkillTargetType targetType;
    
    [Header("Custos e Cooldowns")]
    public int baseManaoCost;
    public float baseCooldown;
    
    [Header("Dano")]
    public int baseDamage;
    public float damageScaling = 1.0f; // Multiplicador para scaling com atributos
    
    [Header("Range e Área")]
    public float range = 3f;           // Alcance da habilidade
    public float areaRadius = 0f;      // Raio para AoE (0 = single target)
    public float projectileSpeed = 10f; // Velocidade do projétil (se aplicável)
    
    [Header("Efeitos")]
    public GameObject effectPrefab;    // Efeito visual
    public GameObject projectilePrefab; // Prefab do projétil
    public AudioClip soundEffect;     // Som da habilidade
    
    [Header("Modificadores Dinâmicos")]
    public bool scalesWithStrength = false;
    public bool scalesWithIntelligence = false;
    public bool scalesWithDexterity = false;
    public bool rangeScalesWithAttribute = false;
    public string rangeScalingAttribute = ""; // "strength", "intelligence", etc.
    
    // Construtor básico (mantido para compatibilidade)
    public Skill(string name, int manaCost, float cooldown, int baseDamage, SkillType type)
    {
        this.name = name;
        this.baseManaoCost = manaCost;
        this.baseCooldown = cooldown;
        this.baseDamage = baseDamage;
        this.type = type;
        this.range = 3f; // Range padrão
        this.targetType = SkillTargetType.Single;
        
        // Configurar scaling baseado no tipo
        if (type == SkillType.Physical)
        {
            scalesWithStrength = true;
        }
        else
        {
            scalesWithIntelligence = true;
        }
    }
    
    // Construtor completo
    public Skill(string name, string description, SkillType type, SkillTargetType targetType,
                 int manaCost, float cooldown, int baseDamage, float range, float areaRadius = 0f)
    {
        this.name = name;
        this.description = description;
        this.type = type;
        this.targetType = targetType;
        this.baseManaoCost = manaCost;
        this.baseCooldown = cooldown;
        this.baseDamage = baseDamage;
        this.range = range;
        this.areaRadius = areaRadius;
        
        // Configurar scaling automático baseado no tipo
        ConfigureAutomaticScaling();
    }
    
    private void ConfigureAutomaticScaling()
    {
        switch (type)
        {
            case SkillType.Physical:
                scalesWithStrength = true;
                rangeScalesWithAttribute = true;
                rangeScalingAttribute = "strength";
                break;
                
            case SkillType.Fire:
            case SkillType.Ice:
            case SkillType.Lightning:
            case SkillType.Poison:
                scalesWithIntelligence = true;
                rangeScalesWithAttribute = true;
                rangeScalingAttribute = "intelligence";
                break;
        }
        
        // Skills baseadas em destreza sempre se beneficiam
        scalesWithDexterity = true; // Para cooldown/crit
    }
    
    // MÉTODOS PARA CÁLCULOS DINÂMICOS
    
    public int GetActualManaCost(PlayerStats stats)
    {
        float cost = baseManaoCost;
        
        // Redução baseada em inteligência (já implementada no PlayerStats)
        // Aqui podemos adicionar reduções específicas da skill
        
        return Mathf.RoundToInt(cost);
    }
    
    public float GetActualCooldown(PlayerStats stats)
    {
        float cooldown = baseCooldown;
        
        // Redução de cooldown baseada nos atributos
        if (type == SkillType.Physical)
        {
            // Skills físicas se beneficiam da velocidade de ataque
            cooldown /= stats.AttackSpeed;
        }
        else
        {
            // Skills mágicas se beneficiam da velocidade de cast
            cooldown /= stats.CastSpeed;
        }
        
        // Redução adicional baseada em destreza (todas as skills)
        if (scalesWithDexterity)
        {
            float dexterityReduction = 1.0f + (stats.Dexterity * 0.002f); // 0.2% por DEX
            cooldown /= dexterityReduction;
        }
        
        return cooldown;
    }
    
    public int GetActualDamage(PlayerStats stats, Item weapon = null)
    {
        float damage = baseDamage;
        
        // Scaling baseado nos atributos configurados
        if (scalesWithStrength)
        {
            damage *= (1.0f + (stats.Strength * 0.025f)); // 2.5% por STR
        }
        
        if (scalesWithIntelligence)
        {
            damage *= (1.0f + (stats.Intelligence * 0.03f)); // 3% por INT
        }
        
        if (scalesWithDexterity)
        {
            damage *= (1.0f + (stats.Dexterity * 0.015f)); // 1.5% por DEX
        }
        
        // Aplicar scaling geral da skill
        damage *= damageScaling;
        
        // Adicionar dano da arma se aplicável
        if (weapon != null)
        {
            if (type == SkillType.Physical)
            {
                damage += weapon.physicalDamage;
            }
            else
            {
                switch (type)
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
        }
        
        return Mathf.RoundToInt(damage);
    }
    
    public float GetActualRange(PlayerStats stats)
    {
        float actualRange = range;
        
        // Scaling de range baseado em atributos
        if (rangeScalesWithAttribute && !string.IsNullOrEmpty(rangeScalingAttribute))
        {
            switch (rangeScalingAttribute.ToLower())
            {
                case "strength":
                    actualRange *= (1.0f + (stats.Strength * 0.01f)); // 1% por STR
                    break;
                case "intelligence":
                    actualRange *= (1.0f + (stats.Intelligence * 0.015f)); // 1.5% por INT
                    break;
                case "dexterity":
                    actualRange *= (1.0f + (stats.Dexterity * 0.01f)); // 1% por DEX
                    break;
                case "vitality":
                    actualRange *= (1.0f + (stats.Vitality * 0.005f)); // 0.5% por VIT
                    break;
            }
        }
        
        return actualRange;
    }
    
    public float GetActualAreaRadius(PlayerStats stats)
    {
        if (areaRadius <= 0) return 0f;
        
        float actualRadius = areaRadius;
        
        // AoE pode escalar com inteligência
        if (scalesWithIntelligence)
        {
            actualRadius *= (1.0f + (stats.Intelligence * 0.01f)); // 1% por INT
        }
        
        return actualRadius;
    }
    
    // Verificar se pode ser usada
    public bool CanUse(PlayerStats stats)
    {
        return stats.Mana >= GetActualManaCost(stats);
    }
    
    // Método para debug/tooltip
    public string GetDetailedDescription(PlayerStats stats)
    {
        string desc = description + "\n\n";
        
        desc += $"<color=yellow>Estatísticas:</color>\n";
        desc += $"Dano: {GetActualDamage(stats, null)}\n";
        desc += $"Custo de Mana: {GetActualManaCost(stats)}\n";
        desc += $"Cooldown: {GetActualCooldown(stats):F1}s\n";
        desc += $"Alcance: {GetActualRange(stats):F1}m\n";
        
        if (areaRadius > 0)
        {
            desc += $"Raio AoE: {GetActualAreaRadius(stats):F1}m\n";
        }
        
        desc += $"\n<color=cyan>Scaling:</color>\n";
        if (scalesWithStrength) desc += "• Força aumenta o dano\n";
        if (scalesWithIntelligence) desc += "• Inteligência aumenta o dano\n";
        if (scalesWithDexterity) desc += "• Destreza reduz cooldown\n";
        if (rangeScalesWithAttribute) desc += $"• {rangeScalingAttribute} aumenta o alcance\n";
        
        return desc;
    }
}