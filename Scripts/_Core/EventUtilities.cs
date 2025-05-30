using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Utilitários e helpers para o sistema de eventos
/// </summary>
public static class EventUtilities
{
    #region Color Utilities
    
    /// <summary>
    /// Obtém a cor associada à raridade de um item
    /// </summary>
    public static Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return Color.white;
            case ItemRarity.Uncommon:
                return Color.green;
            case ItemRarity.Rare:
                return Color.blue;
            case ItemRarity.Epic:
                return new Color(0.5f, 0f, 0.5f); // Roxo
            case ItemRarity.Legendary:
                return new Color(1f, 0.5f, 0f); // Laranja
            default:
                return Color.white;
        }
    }
    
    /// <summary>
    /// Obtém a cor baseada no tipo de notificação
    /// </summary>
    public static Color GetNotificationColor(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Info:
                return Color.white;
            case NotificationType.Success:
                return Color.green;
            case NotificationType.Warning:
                return Color.yellow;
            case NotificationType.Error:
                return Color.red;
            case NotificationType.ItemCollected:
                return Color.cyan;
            case NotificationType.LevelUp:
                return Color.yellow;
            case NotificationType.QuestComplete:
                return Color.green;
            default:
                return Color.white;
        }
    }
    
    /// <summary>
    /// Obtém a cor baseada no tipo de skill
    /// </summary>
    public static Color GetSkillTypeColor(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Physical:
                return new Color(0.9f, 0.9f, 0.9f);
            case SkillType.Fire:
                return new Color(1f, 0.4f, 0.2f);
            case SkillType.Ice:
                return new Color(0.4f, 0.8f, 1f);
            case SkillType.Lightning:
                return new Color(1f, 1f, 0.4f);
            case SkillType.Poison:
                return new Color(0.6f, 1f, 0.4f);
            default:
                return Color.white;
        }
    }
    
    #endregion
    
    #region Event Helper Methods
    
    /// <summary>
    /// Dispara um evento de notificação simples
    /// </summary>
    public static void ShowNotification(string message, NotificationType type = NotificationType.Info, float duration = 2f)
    {
        EventManager.TriggerEvent(new NotificationEvent
        {
            message = message,
            type = type,
            duration = duration,
            color = GetNotificationColor(type)
        });
    }
    
    /// <summary>
    /// Dispara um evento de coleta de item com notificação
    /// </summary>
    public static void TriggerItemCollection(Item item, Vector3 position, GameObject collector)
    {
        EventManager.TriggerEvent(new ItemCollectedEvent
        {
            item = item,
            collectionPosition = position,
            collector = collector
        });
        
        ShowNotification($"Item coletado: {item.name}", NotificationType.ItemCollected);
    }
    
    /// <summary>
    /// Dispara um evento de popup de dano
    /// </summary>
    public static void ShowDamagePopup(Vector3 position, int amount, bool isCritical = false, bool isHeal = false)
    {
        Color color = isHeal ? Color.green : (isCritical ? Color.red : Color.white);
        
        EventManager.TriggerEvent(new DamagePopupRequestEvent
        {
            worldPosition = position,
            amount = amount,
            isCritical = isCritical,
            isHeal = isHeal,
            customColor = color
        });
    }
    
    /// <summary>
    /// Dispara um evento de tooltip
    /// </summary>
    public static void ShowTooltip(Item item, Vector3 screenPosition)
    {
        EventManager.TriggerEvent(new TooltipRequestEvent
        {
            item = item,
            screenPosition = screenPosition,
            show = true
        });
    }
    
    /// <summary>
    /// Esconde o tooltip
    /// </summary>
    public static void HideTooltip()
    {
        EventManager.TriggerEvent(new TooltipRequestEvent
        {
            item = null,
            screenPosition = Vector3.zero,
            show = false
        });
    }
    
    /// <summary>
    /// Dispara um evento de dano causado
    /// </summary>
    public static void TriggerDamageDealt(GameObject attacker, GameObject target, int damage, bool isCritical, DamageType damageType, Vector3 hitPosition)
    {
        EventManager.TriggerEvent(new DamageDealtEvent
        {
            attacker = attacker,
            target = target,
            damage = damage,
            isCritical = isCritical,
            damageType = damageType,
            hitPosition = hitPosition
        });
    }
    
    /// <summary>
    /// Dispara um evento de skill usada
    /// </summary>
    public static void TriggerSkillUsed(Skill skill, GameObject caster, Vector3 targetPosition, int actualDamage, int manaCost, float cooldown)
    {
        EventManager.TriggerEvent(new SkillUsedEvent
        {
            skill = skill,
            caster = caster,
            targetPosition = targetPosition,
            actualDamage = actualDamage,
            manaCost = manaCost,
            cooldown = cooldown
        });
    }
    
    /// <summary>
    /// Dispara um evento de coleta de ouro
    /// </summary>
    public static void TriggerGoldCollected(int amount, int totalGold, Vector3 position)
    {
        EventManager.TriggerEvent(new GoldCollectedEvent
        {
            amount = amount,
            totalGold = totalGold,
            collectionPosition = position
        });
    }
    
    /// <summary>
    /// Dispara um evento de loot dropado
    /// </summary>
    public static void TriggerLootDropped(List<Item> items, int goldAmount, Vector3 dropPosition, GameObject source)
    {
        EventManager.TriggerEvent(new LootDroppedEvent
        {
            items = items,
            goldAmount = goldAmount,
            dropPosition = dropPosition,
            source = source
        });
    }
    
    /// <summary>
    /// Dispara um evento de quest aceita
    /// </summary>
    public static void TriggerQuestAccepted(Quest quest, GameObject questGiver = null)
    {
        EventManager.TriggerEvent(new QuestAcceptedEvent
        {
            quest = quest,
            questGiver = questGiver
        });
    }
    
    /// <summary>
    /// Dispara um evento de quest completada
    /// </summary>
    public static void TriggerQuestCompleted(Quest quest)
    {
        EventManager.TriggerEvent(new QuestCompletedEvent
        {
            quest = quest,
            rewards = quest.rewards
        });
    }
    
    /// <summary>
    /// Dispara um evento de game pausado/despausado
    /// </summary>
    public static void TriggerGamePaused(bool isPaused, string reason = "")
    {
        EventManager.TriggerEvent(new GamePausedEvent
        {
            isPaused = isPaused,
            reason = reason
        });
    }
    
    #endregion
    
    #region Validation Helpers
    
    /// <summary>
    /// Verifica se um item é válido
    /// </summary>
    public static bool IsValidItem(Item item)
    {
        return item != null && !string.IsNullOrEmpty(item.name);
    }
    
    /// <summary>
    /// Verifica se uma quest é válida
    /// </summary>
    public static bool IsValidQuest(Quest quest)
    {
        return quest != null && !string.IsNullOrEmpty(quest.id) && !string.IsNullOrEmpty(quest.title);
    }
    
    /// <summary>
    /// Verifica se uma skill é válida
    /// </summary>
    public static bool IsValidSkill(Skill skill)
    {
        return skill != null && !string.IsNullOrEmpty(skill.name);
    }
    
    #endregion
    
    #region Debug Helpers
    
    /// <summary>
    /// Imprime informações de debug sobre um evento
    /// </summary>
    public static void DebugLogEvent<T>(T eventData) where T : struct
    {
        Debug.Log($"[EventManager] Evento disparado: {typeof(T).Name} - {eventData.ToString()}");
    }
    
    /// <summary>
    /// Mostra uma notificação de debug
    /// </summary>
    public static void ShowDebugNotification(string message)
    {
        if (Debug.isDebugBuild)
        {
            ShowNotification($"[DEBUG] {message}", NotificationType.Info, 1f);
        }
    }
    
    #endregion
}