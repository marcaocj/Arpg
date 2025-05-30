using UnityEngine;
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
    public float damageScaling = 1.0f;
    
    [Header("Range e Área")]
    public float range = 3f;
    public float areaRadius = 0f;
    public float projectileSpeed = 10f;
    
    [Header("Efeitos")]
    public GameObject effectPrefab;
    public GameObject projectilePrefab;
    public AudioClip soundEffect;
    
    [Header("Scaling Automático")]
    public bool scalesWithStrength = false;
    public bool scalesWithIntelligence = false;
    public bool scalesWithDexterity = true; // Sempre verdadeiro para cooldown
    public bool rangeScalesWithAttribute = false;
    public string rangeScalingAttribute = "";
    
    // CONSTRUTOR COMPLETO (APENAS ESTE)
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
        
        scalesWithDexterity = true; // Sempre para cooldown/crit
    }
    
    // CÁLCULOS DINÂMICOS
    public int GetActualManaCost(PlayerStats stats)
    {
        // CORREÇÃO: Verificação de null para stats
        if (stats == null)
        {
            Debug.LogWarning($"[Skill.GetActualManaCost] PlayerStats é null para skill {name}. Retornando custo base.");
            return baseManaoCost;
        }
        
        float cost = baseManaoCost;
        
        // Redução baseada em inteligência (já implementada no PlayerStats.UseMana)
        float manaReduction = Mathf.Min(stats.Intelligence * 0.001f, 0.30f);
        cost *= (1.0f - manaReduction);
        
        return Mathf.RoundToInt(cost);
    }
    
    public float GetActualCooldown(PlayerStats stats)
    {
        // CORREÇÃO: Verificação de null para stats
        if (stats == null)
        {
            Debug.LogWarning($"[Skill.GetActualCooldown] PlayerStats é null para skill {name}. Retornando cooldown base.");
            return baseCooldown;
        }
        
        float cooldown = baseCooldown;
        
        // Redução baseada no tipo de skill
        if (type == SkillType.Physical)
        {
            cooldown /= stats.AttackSpeed;
        }
        else
        {
            cooldown /= stats.CastSpeed;
        }
        
        // Redução adicional baseada em destreza
        if (scalesWithDexterity)
        {
            float dexterityReduction = 1.0f + (stats.Dexterity * 0.002f);
            cooldown /= dexterityReduction;
        }
        
        return cooldown;
    }
    
    public int GetActualDamage(PlayerStats stats, Item weapon = null)
    {
        // CORREÇÃO: Verificação de null para stats
        if (stats == null)
        {
            Debug.LogWarning($"[Skill.GetActualDamage] PlayerStats é null para skill {name}. Retornando dano base.");
            return baseDamage;
        }
        
        float damage = baseDamage;
        
        // Scaling baseado nos atributos
        if (scalesWithStrength)
        {
            damage *= (1.0f + (stats.Strength * 0.025f));
        }
        
        if (scalesWithIntelligence)
        {
            damage *= (1.0f + (stats.Intelligence * 0.03f));
        }
        
        if (scalesWithDexterity)
        {
            damage *= (1.0f + (stats.Dexterity * 0.015f));
        }
        
        // Aplicar scaling geral
        damage *= damageScaling;
        
        // Adicionar dano da arma
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
                    case SkillType.Fire: damage += weapon.fireDamage; break;
                    case SkillType.Ice: damage += weapon.iceDamage; break;
                    case SkillType.Lightning: damage += weapon.lightningDamage; break;
                    case SkillType.Poison: damage += weapon.poisonDamage; break;
                }
            }
        }
        
        return Mathf.RoundToInt(damage);
    }
    
    public float GetActualRange(PlayerStats stats)
    {
        // CORREÇÃO: Verificação de null para stats
        if (stats == null)
        {
            Debug.LogWarning($"[Skill.GetActualRange] PlayerStats é null para skill {name}. Retornando range base.");
            return range;
        }
        
        float actualRange = range;
        
        if (rangeScalesWithAttribute && !string.IsNullOrEmpty(rangeScalingAttribute))
        {
            switch (rangeScalingAttribute.ToLower())
            {
                case "strength":
                    actualRange *= (1.0f + (stats.Strength * 0.01f));
                    break;
                case "intelligence":
                    actualRange *= (1.0f + (stats.Intelligence * 0.015f));
                    break;
                case "dexterity":
                    actualRange *= (1.0f + (stats.Dexterity * 0.01f));
                    break;
                case "vitality":
                    actualRange *= (1.0f + (stats.Vitality * 0.005f));
                    break;
            }
        }
        
        return actualRange;
    }
    
    public float GetActualAreaRadius(PlayerStats stats)
    {
        if (areaRadius <= 0) return 0f;
        
        // CORREÇÃO: Verificação de null para stats
        if (stats == null)
        {
            Debug.LogWarning($"[Skill.GetActualAreaRadius] PlayerStats é null para skill {name}. Retornando area radius base.");
            return areaRadius;
        }
        
        float actualRadius = areaRadius;
        
        if (scalesWithIntelligence)
        {
            actualRadius *= (1.0f + (stats.Intelligence * 0.01f));
        }
        
        return actualRadius;
    }
    
    // CORREÇÃO PRINCIPAL: Método CanUse com verificação robusta de null
    public bool CanUse(PlayerStats stats)
    {
        // VERIFICAÇÃO CRÍTICA: Se stats for null, retornar false
        if (stats == null)
        {
            Debug.LogWarning($"[Skill.CanUse] PlayerStats é null para skill {name}. Skill não pode ser usada.");
            return false;
        }
        
        // Verificar se há mana suficiente
        int manaCost = GetActualManaCost(stats);
        return stats.Mana >= manaCost;
    }
    
    public string GetDetailedDescription(PlayerStats stats)
    {
        // CORREÇÃO: Verificação de null para stats
        if (stats == null)
        {
            return $"{description}\n\n<color=red>Estatísticas indisponíveis (PlayerStats é null)</color>";
        }
        
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