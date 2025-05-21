using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using RPG.UI.Quest;

public class QuestListItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI questTypeText;
    public Slider progressSlider;
    public TextMeshProUGUI progressText;
    public Button itemButton;
    public Image backgroundImage;
    
    [Header("Visual Feedback")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color hoverColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
    public Color selectedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public Sprite normalBackground;
    public Sprite trackedBackground;
    
    private Quest quest;
    private QuestUI questUI;
    private bool isSelected = false;
    private bool isHovered = false;
    private bool isTracked = false;
    
    private void Awake()
    {
        if (itemButton == null)
            itemButton = GetComponent<Button>();
            
        if (itemButton != null)
            itemButton.onClick.AddListener(OnClick);
            
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
    }
    
    public void SetupQuest(Quest quest, QuestUI questUI)
    {
        this.quest = quest;
        this.questUI = questUI;
        
        UpdateVisuals();
    }
    
    public void UpdateProgress(Quest quest)
    {
        // Atualizar referência da quest
        this.quest = quest;
        
        // Atualizar progressbar
        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = quest.requiredAmount;
            progressSlider.value = quest.currentAmount;
        }
        
        // Atualizar texto de progresso
        if (progressText != null)
        {
            progressText.text = $"{quest.currentAmount}/{quest.requiredAmount}";
            
            // Mudar cor baseado no progresso
            float progress = (float)quest.currentAmount / quest.requiredAmount;
            if (progress < 0.3f)
                progressText.color = Color.red;
            else if (progress < 0.7f)
                progressText.color = Color.yellow;
            else
                progressText.color = Color.green;
        }
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisuals();
    }
    
    public void SetTracked(bool tracked, Color trackedColor)
    {
        isTracked = tracked;
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        // Atualizar título
        if (questTitleText != null && quest != null)
        {
            string titleText = quest.title;
            
            // Adicionar ícone para quest rastreada
            if (isTracked)
                titleText = "★ " + titleText;
                
            questTitleText.text = titleText;
            
            // Aplicar cor de rastreamento se necessário
            if (isTracked)
                questTitleText.color = questUI.trackedQuestColor;
            else
                questTitleText.color = Color.white;
        }
        
        // Atualizar tipo da quest
        if (questTypeText != null && quest != null)
        {
            string typeText = "";
            
            switch (quest.type)
            {
                case QuestType.KillEnemies:
                    typeText = "Caça";
                    break;
                case QuestType.CollectItems:
                    typeText = "Coleção";
                    break;
                case QuestType.ExploreArea:
                    typeText = "Exploração";
                    break;
                case QuestType.DefeatBoss:
                    typeText = "Chefe";
                    break;
            }
            
            questTypeText.text = typeText;
        }
        
        // Atualizar progresso
        if (quest != null)
        {
            UpdateProgress(quest);
        }
        
        // Atualizar cor de fundo
        if (backgroundImage != null)
        {
            if (isSelected)
                backgroundImage.color = selectedColor;
            else if (isHovered)
                backgroundImage.color = hoverColor;
            else
                backgroundImage.color = normalColor;
                
            // Alterar sprite se for rastreada
            if (isTracked && trackedBackground != null)
                backgroundImage.sprite = trackedBackground;
            else if (normalBackground != null)
                backgroundImage.sprite = normalBackground;
        }
    }
    
    private void OnClick()
    {
        if (questUI != null && quest != null)
        {
            questUI.ShowQuestDetails(quest);
            SetSelected(true);
            
            // Desmarcar outros itens (através do QuestUI)
            // Esta funcionalidade requer uma modificação no QuestUI para gerenciar a seleção
        }
    }
    
    // Implementação das interfaces de pointer
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateVisuals();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateVisuals();
    }
    
    // Método público para atualizar o estado "ativo" (selecionado)
    public void SetActive(bool active)
    {
        if (active)
        {
            // Adicionar efeito visual para o item selecionado
            StartCoroutine(PulseAnimation());
        }
    }
    
    private System.Collections.IEnumerator PulseAnimation()
    {
        Vector3 originalScale = transform.localScale;
        float duration = 0.3f;
        float halfDuration = duration / 2f;
        
        // Crescer
        for (float t = 0; t < halfDuration; t += Time.deltaTime)
        {
            float normalizedTime = t / halfDuration;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.05f, normalizedTime);
            yield return null;
        }
        
        // Diminuir
        for (float t = 0; t < halfDuration; t += Time.deltaTime)
        {
            float normalizedTime = t / halfDuration;
            transform.localScale = Vector3.Lerp(originalScale * 1.05f, originalScale, normalizedTime);
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
}