using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente para UI de objetivos individuais de quest
/// </summary>
public class QuestObjectiveUI : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI progressText;
    public Image checkmarkImage;
    public Slider progressSlider;
    public Image backgroundImage;
    
    [Header("Colors")]
    public Color completedColor = Color.green;
    public Color incompleteColor = Color.white;
    public Color optionalColor = Color.gray;
    
    private QuestObjective objective;
    
    public void SetupObjective(QuestObjective objective)
    {
        this.objective = objective;
        
        if (objective == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        UpdateDisplay();
    }
    
    public void UpdateDisplay()
    {
        if (objective == null) return;
        
        // Update objective text
        if (objectiveText != null)
        {
            string prefix = objective.isCompleted ? "✓" : "○";
            string optional = objective.isOptional ? " (Opcional)" : "";
            objectiveText.text = $"{prefix} {objective.description}{optional}";
            
            // Color based on completion status
            if (objective.isCompleted)
                objectiveText.color = completedColor;
            else if (objective.isOptional)
                objectiveText.color = optionalColor;
            else
                objectiveText.color = incompleteColor;
        }
        
        // Update progress text
        if (progressText != null)
        {
            if (objective.trackProgress && objective.requiredAmount > 1)
            {
                progressText.text = objective.GetProgressText();
                progressText.gameObject.SetActive(true);
            }
            else
            {
                progressText.gameObject.SetActive(false);
            }
        }
        
        // Update progress slider
        if (progressSlider != null)
        {
            if (objective.trackProgress && objective.requiredAmount > 1)
            {
                progressSlider.value = objective.GetProgressPercentage();
                progressSlider.gameObject.SetActive(true);
            }
            else
            {
                progressSlider.gameObject.SetActive(false);
            }
        }
        
        // Update checkmark
        if (checkmarkImage != null)
        {
            checkmarkImage.gameObject.SetActive(objective.isCompleted);
        }
        
        // Update background
        if (backgroundImage != null)
        {
            Color bgColor = objective.isCompleted ? 
                new Color(0.1f, 0.4f, 0.1f, 0.3f) : 
                new Color(0.2f, 0.2f, 0.2f, 0.3f);
            backgroundImage.color = bgColor;
        }
    }
    
    public QuestObjective GetObjective()
    {
        return objective;
    }
    
    public void ForceUpdate()
    {
        UpdateDisplay();
    }
}

/// <summary>
/// Extensões para a classe Item para compatibilidade com o sistema de quest
/// </summary>
public static class ItemExtensions
{
    /// <summary>
    /// Retorna a cor baseada na raridade do item
    /// </summary>
    public static Color GetRarityColor(this Item item)
    {
        switch (item.rarity)
        {
            case ItemRarity.Common: return Color.white;
            case ItemRarity.Uncommon: return Color.green;
            case ItemRarity.Rare: return Color.blue;
            case ItemRarity.Epic: return new Color(0.5f, 0, 0.5f); // Roxo
            case ItemRarity.Legendary: return new Color(1f, 0.5f, 0); // Laranja
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// Verifica se um item é melhor que outro baseado em raridade e nível
    /// </summary>
    public static bool IsBetterThan(this Item item, Item other)
    {
        if (other == null) return true;
        if (item == null) return false;
        
        // Comparar raridade primeiro
        if (item.rarity != other.rarity)
        {
            return item.rarity > other.rarity;
        }
        
        // Se mesma raridade, comparar nível
        if (item.level != other.level)
        {
            return item.level > other.level;
        }
        
        // Se mesmo nível, comparar valor
        return item.GetCurrentValue() > other.GetCurrentValue();
    }
    
    /// <summary>
    /// Retorna uma descrição detalhada do item
    /// </summary>
    public static string GetDetailedDescription(this Item item, PlayerStats playerStats = null)
    {
        if (item == null) return "Item inválido";
        
        var description = new System.Text.StringBuilder();
        
        // Nome com cor da raridade
        string rarityColor = ColorUtility.ToHtmlStringRGB(item.GetRarityColor());
        description.AppendLine($"<color=#{rarityColor}><b>{item.name}</b></color>");
        
        // Tipo e raridade
        description.AppendLine($"{item.type} - {item.rarity}");
        
        // Nível
        if (item.level > 1)
        {
            description.AppendLine($"Nível: {item.level}");
        }
        
        // Descrição
        if (!string.IsNullOrEmpty(item.description))
        {
            description.AppendLine();
            description.AppendLine(item.description);
        }
        
        // Estatísticas de combate
        if (item.IsEquipment)
        {
            description.AppendLine();
            description.AppendLine("<b>Estatísticas:</b>");
            
            if (item.physicalDamage > 0)
                description.AppendLine($"Dano Físico: +{item.physicalDamage}");
            
            if (item.fireDamage > 0)
                description.AppendLine($"Dano de Fogo: +{item.fireDamage}");
            
            if (item.iceDamage > 0)
                description.AppendLine($"Dano de Gelo: +{item.iceDamage}");
            
            if (item.lightningDamage > 0)
                description.AppendLine($"Dano Elétrico: +{item.lightningDamage}");
            
            if (item.poisonDamage > 0)
                description.AppendLine($"Dano de Veneno: +{item.poisonDamage}");
            
            if (item.strengthModifier > 0)
                description.AppendLine($"Força: +{item.strengthModifier}");
            
            if (item.intelligenceModifier > 0)
                description.AppendLine($"Inteligência: +{item.intelligenceModifier}");
            
            if (item.dexterityModifier > 0)
                description.AppendLine($"Destreza: +{item.dexterityModifier}");
            
            if (item.vitalityModifier > 0)
                description.AppendLine($"Vitalidade: +{item.vitalityModifier}");
        }
        
        // Efeitos de consumível
        if (item.IsConsumable)
        {
            description.AppendLine();
            description.AppendLine("<b>Efeitos:</b>");
            
            if (item.healthRestore > 0)
                description.AppendLine($"Restaura {item.healthRestore} de vida");
            
            if (item.manaRestore > 0)
                description.AppendLine($"Restaura {item.manaRestore} de mana");
        }
        
        // Requisitos
        if (item.levelRequirement > 1)
        {
            description.AppendLine();
            bool canUse = playerStats == null || playerStats.Level >= item.levelRequirement;
            string colorTag = canUse ? "white" : "red";
            description.AppendLine($"<color={colorTag}>Requisito: Nível {item.levelRequirement}</color>");
        }
        
        // Valor
        if (item.GetCurrentValue() > 0)
        {
            description.AppendLine();
            description.AppendLine($"Valor: {item.GetCurrentValue()} moedas");
        }
        
        // Stack info
        if (item.IsStackable && item.currentStack > 1)
        {
            description.AppendLine($"Quantidade: {item.currentStack}");
        }
        
        return description.ToString();
    }
    
    /// <summary>
    /// Verifica se o player pode usar este item
    /// </summary>
    public static bool CanPlayerUse(this Item item, PlayerStats playerStats)
    {
        if (item == null || playerStats == null) return false;
        
        // Verificar nível
        if (playerStats.Level < item.levelRequirement)
            return false;
        
        // Adicionar outras verificações conforme necessário
        // Como classe, facção, etc.
        
        return true;
    }
    
    /// <summary>
    /// Retorna lista de requisitos não atendidos
    /// </summary>
    public static List<string> GetMissingRequirements(this Item item, PlayerStats playerStats)
    {
        var missing = new List<string>();
        
        if (item == null) 
        {
            missing.Add("Item inválido");
            return missing;
        }
        
        if (playerStats == null)
        {
            missing.Add("Stats do player indisponíveis");
            return missing;
        }
        
        if (playerStats.Level < item.levelRequirement)
        {
            missing.Add($"Nível {item.levelRequirement} (atual: {playerStats.Level})");
        }
        
        return missing;
    }
    
    /// <summary>
    /// Retorna o valor atual do item (pode ser diferente do valor base)
    /// </summary>
    public static int GetCurrentValue(this Item item)
    {
        if (item == null) return 0;
        
        // Valor base multiplicado por raridade e qualidade
        float value = item.value;
        
        // Multiplicador por raridade
        switch (item.rarity)
        {
            case ItemRarity.Common: value *= 1f; break;
            case ItemRarity.Uncommon: value *= 1.5f; break;
            case ItemRarity.Rare: value *= 3f; break;
            case ItemRarity.Epic: value *= 6f; break;
            case ItemRarity.Legendary: value *= 12f; break;
        }
        
        // Multiplicador por qualidade (se implementado)
        value *= (item.quality / 100f);
        
        // Multiplicar por stack
        if (item.IsStackable)
        {
            value *= item.currentStack;
        }
        
        return Mathf.RoundToInt(value);
    }
    
    /// <summary>
    /// Verifica se é um item de equipamento
    /// </summary>
    public static bool IsEquipment(this Item item)
    {
        return item.type == ItemType.Weapon ||
               item.type == ItemType.Helmet ||
               item.type == ItemType.Chest ||
               item.type == ItemType.Gloves ||
               item.type == ItemType.Boots ||
               item.type == ItemType.Jewelry;
    }
    
    /// <summary>
    /// Verifica se é um item consumível
    /// </summary>
    public static bool IsConsumable(this Item item)
    {
        return item.type == ItemType.Consumable;
    }
    
    /// <summary>
    /// Verifica se o item é empilhável
    /// </summary>
    public static bool IsStackable(this Item item)
    {
        return item.IsConsumable || item.type == ItemType.Material;
    }
    
    /// <summary>
    /// Verifica se pode fazer stack com outro item
    /// </summary>
    public static bool CanStack(this Item item, Item other)
    {
        if (item == null || other == null) return false;
        if (!item.IsStackable || !other.IsStackable) return false;
        
        return item.id == other.id && 
               item.name == other.name && 
               item.level == other.level &&
               item.currentStack < item.maxStackSize;
    }
    
    /// <summary>
    /// Tenta fazer stack com outro item
    /// </summary>
    public static bool TryStack(this Item item, Item other, out int leftOver)
    {
        leftOver = 0;
        
        if (!item.CanStack(other)) return false;
        
        int totalAmount = item.currentStack + other.currentStack;
        
        if (totalAmount <= item.maxStackSize)
        {
            // Cabe tudo
            item.currentStack = totalAmount;
            leftOver = 0;
            return true;
        }
        else
        {
            // Não cabe tudo
            item.currentStack = item.maxStackSize;
            leftOver = totalAmount - item.maxStackSize;
            return true;
        }
    }
    
    /// <summary>
    /// Verifica se o item é válido
    /// </summary>
    public static bool IsValid(this Item item)
    {
        return item != null && 
               !string.IsNullOrEmpty(item.name) && 
               !string.IsNullOrEmpty(item.id);
    }
}

/// <summary>
/// Componente para tracking de quest ativa na HUD
/// </summary>
public class QuestTrackingItem : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI currentObjectiveText;
    public TextMeshProUGUI timeRemainingText;
    public Slider progressSlider;
    public TextMeshProUGUI progressText;
    public Image priorityIndicator;
    public Button questButton;
    
    [Header("Colors")]
    public Color criticalColor = Color.red;
    public Color highColor = Color.orange;
    public Color normalColor = Color.white;
    public Color lowColor = Color.gray;
    
    private global::Quest trackedQuest; // Explícito para evitar conflito de namespace
    
    public void SetupTrackedQuest(global::Quest quest)
    {
        this.trackedQuest = quest;
        
        if (quest == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        UpdateDisplay();
        
        // Setup button click
        if (questButton != null)
        {
            questButton.onClick.RemoveAllListeners();
            questButton.onClick.AddListener(() => OpenQuestDetails());
        }
    }
    
    public void UpdateDisplay()
    {
        if (trackedQuest == null) return;
        
        // Update title
        if (questTitleText != null)
        {
            questTitleText.text = trackedQuest.title;
            questTitleText.color = GetPriorityColor(trackedQuest.priority);
        }
        
        // Update current objective
        if (currentObjectiveText != null)
        {
            var currentObjective = GetCurrentObjective();
            if (currentObjective != null)
            {
                if (currentObjective.trackProgress && currentObjective.requiredAmount > 1)
                {
                    currentObjectiveText.text = $"{currentObjective.description} {currentObjective.GetProgressText()}";
                }
                else
                {
                    currentObjectiveText.text = currentObjective.description;
                }
            }
            else
            {
                currentObjectiveText.text = "Objetivos completos";
            }
        }
        
        // Update time remaining
        if (timeRemainingText != null)
        {
            if (trackedQuest.hasTimeLimit && trackedQuest.status == QuestStatus.Active)
            {
                timeRemainingText.text = $"Tempo: {trackedQuest.GetTimeRemainingText()}";
                timeRemainingText.gameObject.SetActive(true);
                
                // Color based on urgency
                float timePercentage = trackedQuest.GetTimePercentage();
                if (timePercentage < 0.25f)
                    timeRemainingText.color = Color.red;
                else if (timePercentage < 0.5f)
                    timeRemainingText.color = Color.yellow;
                else
                    timeRemainingText.color = Color.white;
            }
            else
            {
                timeRemainingText.gameObject.SetActive(false);
            }
        }
        
        // Update progress
        if (progressSlider != null)
        {
            float progress = trackedQuest.GetOverallProgress();
            progressSlider.value = progress;
            
            if (progressText != null)
            {
                progressText.text = $"{progress:P0}";
            }
        }
        
        // Update priority indicator
        if (priorityIndicator != null)
        {
            priorityIndicator.color = GetPriorityColor(trackedQuest.priority);
            priorityIndicator.gameObject.SetActive(trackedQuest.priority > QuestPriority.Normal);
        }
    }
    
    private QuestObjective GetCurrentObjective()
    {
        // Retorna o primeiro objetivo não completado e não opcional
        foreach (var objective in trackedQuest.objectives)
        {
            if (!objective.isCompleted && !objective.isOptional)
            {
                return objective;
            }
        }
        
        // Se todos os obrigatórios estão completos, retorna o primeiro opcional não completo
        foreach (var objective in trackedQuest.objectives)
        {
            if (!objective.isCompleted && objective.isOptional)
            {
                return objective;
            }
        }
        
        return null;
    }
    
    private Color GetPriorityColor(QuestPriority priority)
    {
        switch (priority)
        {
            case QuestPriority.Critical: return criticalColor;
            case QuestPriority.High: return highColor;
            case QuestPriority.Normal: return normalColor;
            case QuestPriority.Low: return lowColor;
            default: return normalColor;
        }
    }
    
    private void OpenQuestDetails()
    {
        if (trackedQuest == null) return;
        
        // Encontrar QuestUI e mostrar detalhes
        var questUI = FindObjectOfType<RPG.UI.Quest.QuestUI>();
        if (questUI != null)
        {
            questUI.ShowQuestDetails(trackedQuest);
            questUI.ToggleQuestLog(); // Abrir quest log se não estiver aberto
        }
    }
    
    public global::Quest GetTrackedQuest()
    {
        return trackedQuest;
    }
    
    public void ForceUpdate()
    {
        UpdateDisplay();
    }
}
