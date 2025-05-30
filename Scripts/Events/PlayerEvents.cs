using UnityEngine;

/// <summary>
/// Eventos relacionados ao jogador
/// </summary>
public static class PlayerEvents
{
    /// <summary>
    /// Evento disparado quando o jogador é criado
    /// </summary>
    public struct PlayerSpawnedEvent : IEvent
    {
        public GameObject player;
    }

    /// <summary>
    /// Evento disparado quando o jogador é destruído
    /// </summary>
    public struct PlayerDestroyedEvent : IEvent
    {
        public GameObject player;
    }

    /// <summary>
    /// Evento disparado quando o jogador sofre dano
    /// </summary>
    public struct PlayerDamagedEvent : IEvent
    {
        public float damage;
        public float currentHealth;
        public float maxHealth;
        public GameObject source;
    }

    /// <summary>
    /// Evento disparado quando o jogador é curado
    /// </summary>
    public struct PlayerHealedEvent : IEvent
    {
        public float amount;
        public float currentHealth;
        public float maxHealth;
    }

    /// <summary>
    /// Evento disparado quando o jogador morre
    /// </summary>
    public struct PlayerDiedEvent : IEvent
    {
        public GameObject killer;
    }

    /// <summary>
    /// Evento disparado quando o jogador respawna
    /// </summary>
    public struct PlayerRespawnedEvent : IEvent
    {
        public Vector3 position;
    }

    /// <summary>
    /// Evento disparado quando o jogador sobe de nível
    /// </summary>
    public struct PlayerLevelUpEvent : IEvent
    {
        public int newLevel;
        public int availablePoints;
    }

    /// <summary>
    /// Evento disparado quando as estatísticas do jogador mudam
    /// </summary>
    public struct PlayerStatsChangedEvent : IEvent
    {
        public PlayerStats oldStats;
        public PlayerStats newStats;
    }

    /// <summary>
    /// Evento disparado quando o mana do jogador muda
    /// </summary>
    public struct PlayerManaChangedEvent : IEvent
    {
        public float currentMana;
        public float maxMana;
    }

    /// <summary>
    /// Evento disparado quando o jogador ganha experiência
    /// </summary>
    public struct PlayerExperienceGainedEvent : IEvent
    {
        public float amount;
        public float currentExperience;
        public float experienceToNextLevel;
    }

    /// <summary>
    /// Evento disparado quando o jogador ganha ouro
    /// </summary>
    public struct PlayerGoldGainedEvent : IEvent
    {
        public int amount;
        public int totalGold;
    }
} 