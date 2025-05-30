using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Sistema centralizado de eventos para desacoplar componentes do jogo
/// </summary>
public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }
    
    // Dictionary para armazenar todos os eventos
    private Dictionary<Type, Delegate> eventDictionary = new Dictionary<Type, Delegate>();
    
    #region Singleton Setup
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[EventManager] Sistema de eventos inicializado");
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            eventDictionary.Clear();
            Instance = null;
        }
    }
    #endregion

    #region Generic Event System
    public static void Subscribe<T>(Action<T> listener) where T : struct
    {
        if (Instance == null) return;

        Type eventType = typeof(T);
        
        if (Instance.eventDictionary.ContainsKey(eventType))
        {
            Instance.eventDictionary[eventType] = (Action<T>)Instance.eventDictionary[eventType] + listener;
        }
        else
        {
            Instance.eventDictionary[eventType] = listener;
        }
    }
    
    public static void Unsubscribe<T>(Action<T> listener) where T : struct
    {
        if (Instance == null) return;

        Type eventType = typeof(T);
        
        if (Instance.eventDictionary.ContainsKey(eventType))
        {
            Instance.eventDictionary[eventType] = (Action<T>)Instance.eventDictionary[eventType] - listener;
            
            if (Instance.eventDictionary[eventType] == null)
            {
                Instance.eventDictionary.Remove(eventType);
            }
        }
    }
    
    public static void TriggerEvent<T>(T eventData) where T : struct
    {
        if (Instance == null) return;

        Type eventType = typeof(T);
        
        if (Instance.eventDictionary.ContainsKey(eventType))
        {
            ((Action<T>)Instance.eventDictionary[eventType])?.Invoke(eventData);
        }
    }
    #endregion
}

#region Event Data Structures

// === PLAYER EVENTS ===
public struct PlayerHealthChangedEvent
{
    public int currentHealth;
    public int maxHealth;
    public int healthDelta;
    public DamageType damageType;
}

public struct PlayerManaChangedEvent
{
    public int currentMana;
    public int maxMana;
    public int manaDelta;
}

public struct PlayerLevelUpEvent
{
    public int newLevel;
    public int oldLevel;
    public int attributePointsGained;
}

public struct PlayerExperienceGainedEvent
{
    public int experienceGained;
    public int currentExperience;
    public int experienceToNextLevel;
}

public struct PlayerAttributeChangedEvent
{
    public string attributeName;
    public int oldValue;
    public int newValue;
    public bool isTemporary;
}

public struct PlayerStatsRecalculatedEvent
{
    public int strength;
    public int intelligence;
    public int dexterity;
    public int vitality;
    public int maxHealth;
    public int maxMana;
}

public struct PlayerDeathEvent
{
    public Vector3 deathPosition;
    public string causeOfDeath;
}

public struct PlayerRespawnEvent
{
    public Vector3 respawnPosition;
}

// === COMBAT EVENTS ===
public struct DamageDealtEvent
{
    public GameObject attacker;
    public GameObject target;
    public int damage;
    public bool isCritical;
    public DamageType damageType;
    public Vector3 hitPosition;
}

public struct SkillUsedEvent
{
    public Skill skill;
    public GameObject caster;
    public Vector3 targetPosition;
    public int actualDamage;
    public int manaCost;
    public float cooldown;
}

public struct SkillCooldownChangedEvent
{
    public int skillIndex;
    public float remainingCooldown;
    public float totalCooldown;
}

public struct CriticalHitEvent
{
    public GameObject attacker;
    public GameObject target;
    public int baseDamage;
    public int criticalDamage;
    public float criticalMultiplier;
}

// === ENEMY EVENTS ===
public struct EnemyDefeatedEvent
{
    public GameObject enemy;
    public string enemyName;
    public int enemyLevel;
    public Vector3 deathPosition;
    public int experienceReward;
    public bool isBoss;
}

public struct EnemyTakeDamageEvent
{
    public GameObject enemy;
    public int damage;
    public bool isCritical;
    public int remainingHealth;
}

public struct EnemyStateChangedEvent
{
    public GameObject enemy;
    public EnemyState oldState;
    public EnemyState newState;
}

// === INVENTORY EVENTS ===
public struct ItemAddedEvent
{
    public Item item;
    public bool wasSuccessful;
    public string failureReason;
}

public struct ItemRemovedEvent
{
    public Item item;
    public bool wasEquipped;
}

public struct ItemEquippedEvent
{
    public Item item;
    public Item previousItem;
    public ItemType slotType;
}

public struct ItemUnequippedEvent
{
    public Item item;
    public ItemType slotType;
}

public struct ItemCollectedEvent
{
    public Item item;
    public Vector3 collectionPosition;
    public GameObject collector;
}

public struct InventoryFullEvent
{
    public Item attemptedItem;
    public int maxCapacity;
}

// === LOOT EVENTS ===
public struct LootDroppedEvent
{
    public List<Item> items;
    public int goldAmount;
    public Vector3 dropPosition;
    public GameObject source;
}

public struct GoldCollectedEvent
{
    public int amount;
    public int totalGold;
    public Vector3 collectionPosition;
}

// === QUEST EVENTS ===
public struct QuestAcceptedEvent
{
    public Quest quest;
    public GameObject questGiver;
}

public struct QuestCompletedEvent
{
    public Quest quest;
    public List<QuestReward> rewards;
}

public struct QuestProgressUpdatedEvent
{
    public Quest quest;
    public int oldProgress;
    public int newProgress;
    public int requiredAmount;
}

public struct QuestAbandonedEvent
{
    public Quest quest;
}

// === UI EVENTS ===
public struct UIElementToggledEvent
{
    public string elementName;
    public bool isVisible;
}

public struct TooltipRequestEvent
{
    public Item item;
    public Vector3 screenPosition;
    public bool show;
}

public struct NotificationEvent
{
    public string message;
    public NotificationType type;
    public float duration;
    public Color color;
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    ItemCollected,
    LevelUp,
    QuestComplete
}

// === GAME STATE EVENTS ===
public struct GamePausedEvent
{
    public bool isPaused;
    public string reason;
}

public struct SceneTransitionEvent
{
    public string fromScene;
    public string toScene;
    public bool isLoading;
}

// === DAMAGE POPUP EVENTS ===
public struct DamagePopupRequestEvent
{
    public Vector3 worldPosition;
    public int amount;
    public bool isCritical;
    public bool isHeal;
    public Color customColor;
}

// === NPC EVENTS ===
public struct NPCInteractionEvent
{
    public NPCController npc;
    public GameObject player;
    public NPCType interactionType;
}

#endregion