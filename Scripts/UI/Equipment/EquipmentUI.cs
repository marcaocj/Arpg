using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI para exibição de equipamentos no estilo Diablo 3
/// </summary>
public class EquipmentUI : MonoBehaviour
{
    [Header("Equipment Slots")]
    public EquipmentSlotUI weaponSlot;
    public EquipmentSlotUI helmetSlot;
    public EquipmentSlotUI chestSlot;
    public EquipmentSlotUI glovesSlot;
    public EquipmentSlotUI bootsSlot;
    public EquipmentSlotUI ring1Slot;
    public EquipmentSlotUI ring2Slot;
    public EquipmentSlotUI necklaceSlot;

    [Header("Character Preview")]
    public Image characterPreview;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI characterLevel;

    [Header("Stats Display")]
    public TextMeshProUGUI strengthText;
    public TextMeshProUGUI intelligenceText;
    public TextMeshProUGUI dexterityText;
    public TextMeshProUGUI vitalityText;
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI defenseText;

    [Header("Visual Effects")]
    public GameObject glowEffect;
    public ParticleSystem rarityParticles;
    public AudioSource audioSource;

    [Header("Animation Settings")]
    public float hoverScale = 1.1f;
    public float animationDuration = 0.2f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Cache
    private PlayerInventoryManager inventoryManager;
    private PlayerStatsManager statsManager;
    private Dictionary<ItemType, EquipmentSlotUI> slotMap;

    private void Awake()
    {
        InitializeSlotMap();
        SubscribeToEvents();
    }

    private void InitializeSlotMap()
    {
        slotMap = new Dictionary<ItemType, EquipmentSlotUI>
        {
            { ItemType.Weapon, weaponSlot },
            { ItemType.Helmet, helmetSlot },
            { ItemType.Chest, chestSlot },
            { ItemType.Gloves, glovesSlot },
            { ItemType.Boots, bootsSlot }
        };

        // Configurar slots de joias
        if (ring1Slot != null) ring1Slot.Initialize(ItemType.Jewelry, "Anel 1");
        if (ring2Slot != null) ring2Slot.Initialize(ItemType.Jewelry, "Anel 2");
        if (necklaceSlot != null) necklaceSlot.Initialize(ItemType.Jewelry, "Colar");
    }

    private void SubscribeToEvents()
    {
        EventManager.Subscribe<ItemEquippedEvent>(OnItemEquipped);
        EventManager.Subscribe<ItemUnequippedEvent>(OnItemUnequipped);
        EventManager.Subscribe<PlayerEvents.PlayerStatsChangedEvent>(OnPlayerStatsChanged);
    }

    private void OnEnable()
    {
        UpdateAllSlots();
        UpdateStats();
    }

    private void UpdateAllSlots()
    {
        if (inventoryManager == null)
        {
            inventoryManager = PlayerController.Instance?.GetInventoryManager();
            if (inventoryManager == null) return;
        }

        // Atualizar slots principais
        foreach (var slot in slotMap)
        {
            var item = inventoryManager.GetEquippedItem(slot.Key);
            slot.Value.UpdateSlot(item);
        }

        // Atualizar slots de joias
        if (ring1Slot != null) ring1Slot.UpdateSlot(inventoryManager.equippedRing1);
        if (ring2Slot != null) ring2Slot.UpdateSlot(inventoryManager.equippedRing2);
        if (necklaceSlot != null) necklaceSlot.UpdateSlot(inventoryManager.equippedNecklace);
    }

    private void UpdateStats()
    {
        if (statsManager == null)
        {
            statsManager = PlayerController.Instance?.GetStatsManager();
            if (statsManager == null) return;
        }

        var stats = statsManager.Stats;
        if (stats == null) return;

        UpdateStats(stats);
    }

    private void UpdateStats(PlayerStats stats)
    {
        strengthText.text = $"Força: {stats.Strength}";
        intelligenceText.text = $"Inteligência: {stats.Intelligence}";
        dexterityText.text = $"Destreza: {stats.Dexterity}";
        vitalityText.text = $"Vitalidade: {stats.Vitality}";
        damageText.text = $"Dano: {stats.CalculatePhysicalDamage(0)}";
        defenseText.text = $"Defesa: {stats.PhysicalResistance:P0}";
    }

    private void OnItemEquipped(ItemEquippedEvent eventData)
    {
        UpdateAllSlots();
        UpdateStats();
        PlayEquipEffect(eventData.item);
    }

    private void OnItemUnequipped(ItemUnequippedEvent eventData)
    {
        UpdateAllSlots();
        UpdateStats();
    }

    private void OnPlayerStatsChanged(PlayerEvents.PlayerStatsChangedEvent eventData)
    {
        UpdateStats();
    }

    private void PlayEquipEffect(Item item)
    {
        if (item == null) return;

        // Efeito de brilho baseado na raridade
        if (glowEffect != null)
        {
            var glow = Instantiate(glowEffect, transform);
            var glowImage = glow.GetComponent<Image>();
            if (glowImage != null)
            {
                glowImage.color = GetRarityColor(item.rarity);
            }
            Destroy(glow, 1f);
        }

        // Partículas de raridade
        if (rarityParticles != null)
        {
            var particles = Instantiate(rarityParticles, transform);
            var main = particles.main;
            main.startColor = GetRarityColor(item.rarity);
            Destroy(particles.gameObject, 2f);
        }

        // Som de equipar
        if (audioSource != null)
        {
            audioSource.PlayOneShot(Resources.Load<AudioClip>("Audio/UI/Item_Equip"));
        }
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return Color.white;
            case ItemRarity.Uncommon: return Color.green;
            case ItemRarity.Rare: return Color.blue;
            case ItemRarity.Epic: return new Color(0.5f, 0, 0.5f); // Roxo
            case ItemRarity.Legendary: return new Color(1f, 0.5f, 0); // Laranja
            default: return Color.white;
        }
    }

    private void OnDestroy()
    {
        EventManager.Unsubscribe<ItemEquippedEvent>(OnItemEquipped);
        EventManager.Unsubscribe<ItemUnequippedEvent>(OnItemUnequipped);
        EventManager.Unsubscribe<PlayerEvents.PlayerStatsChangedEvent>(OnPlayerStatsChanged);
    }
} 