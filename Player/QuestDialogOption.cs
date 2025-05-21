using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using RPG.UI.Quest;

public class QuestDialogOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements")]
    public TextMeshProUGUI optionText;
    public Image buttonBackground;
    public Button optionButton;
    public Image iconImage;
    
    [Header("Visual Styles")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color hoverColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
    public Color acceptColor = new Color(0.2f, 0.5f, 0.2f, 0.8f);
    public Color completeColor = new Color(0.6f, 0.6f, 0.1f, 0.8f);
    public Color closeColor = new Color(0.5f, 0.2f, 0.2f, 0.8f);
    
    [Header("Icons")]
    public Sprite acceptIcon;
    public Sprite completeIcon;
    public Sprite progressIcon;
    public Sprite closeIcon;
    
    private Quest quest;
    private NPCController questGiver;
    private QuestUI questUI;
    private QuestManager questManager;
    private bool isHovered = false;
    private OptionType optionType = OptionType.Accept;
    
    // Tipos de opções de diálogo
    private enum OptionType
    {
        Accept,
        Complete,
        Progress,
        Close
    }
    
    private void Awake()
    {
        if (optionButton == null)
            optionButton = GetComponent<Button>();
            
        if (optionButton != null)
            optionButton.onClick.AddListener(OnClick);
            
        if (buttonBackground == null)
            buttonBackground = GetComponent<Image>();
            
        // Encontrar o QuestManager na cena se não for atribuído
        if (questManager == null)
            questManager = FindObjectOfType<QuestManager>();
    }
    
    // Configurar opção para aceitar quest
    public void SetupOption(Quest quest, NPCController questGiver, QuestUI questUI)
    {
        this.quest = quest;
        this.questGiver = questGiver;
        this.questUI = questUI;
        this.optionType = OptionType.Accept;
        
        if (optionText != null)
            optionText.text = "Aceitar: " + quest.title;
            
        // Configurar visual
        if (buttonBackground != null)
            buttonBackground.color = normalColor;
            
        if (iconImage != null && acceptIcon != null)
        {
            iconImage.sprite = acceptIcon;
            iconImage.gameObject.SetActive(true);
        }
    }
    
    // Configurar opção para completar quest
    public void SetupCompleteButton(Quest quest, QuestManager questManager, QuestUI questUI)
    {
        this.quest = quest;
        this.questManager = questManager;
        this.questUI = questUI;
        this.optionType = OptionType.Complete;
        
        if (optionText != null)
            optionText.text = "Completar: " + quest.title;
            
        // Configurar visual
        if (buttonBackground != null)
            buttonBackground.color = completeColor;
            
        if (iconImage != null && completeIcon != null)
        {
            iconImage.sprite = completeIcon;
            iconImage.gameObject.SetActive(true);
        }
    }
    
    // Configurar opção para mostrar progresso da quest
    public void SetupProgressButton(Quest quest, QuestUI questUI)
    {
        this.quest = quest;
        this.questUI = questUI;
        this.optionType = OptionType.Progress;
        
        if (optionText != null)
            optionText.text = $"Em andamento: {quest.title} ({quest.currentAmount}/{quest.requiredAmount})";
            
        // Configurar visual
        if (buttonBackground != null)
            buttonBackground.color = normalColor;
            
        if (iconImage != null && progressIcon != null)
        {
            iconImage.sprite = progressIcon;
            iconImage.gameObject.SetActive(true);
        }
        
        // Desabilitar botão se for apenas mostrar progresso
        if (optionButton != null)
            optionButton.interactable = false;
    }
    
    // Configurar botão para fechar diálogo
    public void SetupCloseButton(QuestUI questUI)
    {
        this.questUI = questUI;
        this.optionType = OptionType.Close;
        
        if (optionText != null)
            optionText.text = "Fechar";
            
        // Configurar visual
        if (buttonBackground != null)
            buttonBackground.color = closeColor;
            
        if (iconImage != null && closeIcon != null)
        {
            iconImage.sprite = closeIcon;
            iconImage.gameObject.SetActive(true);
        }
    }
    
    private void OnClick()
    {
        switch (optionType)
        {
            case OptionType.Accept:
                AcceptQuest();
                break;
                
            case OptionType.Complete:
                CompleteQuest();
                break;
                
            case OptionType.Progress:
                // Nada acontece - botão está desabilitado
                break;
                
            case OptionType.Close:
                CloseDialog();
                break;
        }
        
        // Animar click
        StartCoroutine(AnimateClick());
    }
    
    private void AcceptQuest()
    {
        if (questGiver != null && quest != null && questManager != null)
        {
            // Aceitar a quest
            questManager.AcceptQuest(quest);
            
            // Fechar o diálogo de quest
            if (questUI != null)
            {
                questUI.CloseQuestDialog();
                questUI.UpdateQuestList();
                
                // Se for a primeira quest aceita, abrir painel de quests automaticamente
                if (questManager.activeQuests.Count == 1)
                {
                    questUI.ToggleQuestPanel();
                }
            }
            
            // Reproduzir som ou efeito
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null && questUI.questAcceptedSound != null)
            {
                audioSource.PlayOneShot(questUI.questAcceptedSound);
            }
            
            Debug.Log("Quest aceita: " + quest.title);
        }
    }
    
    private void CompleteQuest()
    {
        if (quest != null && questManager != null)
        {
            // Completar a quest
            if (quest.currentAmount >= quest.requiredAmount)
            {
                quest.CompleteQuest();
                
                // Atualizar listas de quests
                questManager.activeQuests.Remove(quest);
                questManager.completedQuests.Add(quest);
                
                // Mostrar efeitos de conclusão
                if (questUI != null)
                {
                    // Atualizar UI
                    questUI.CloseQuestDialog();
                    questUI.UpdateQuestList();
                    
                    // Mostrar notificação
                    // Esta chamada depende de você implementar o método na classe QuestUI
                    // questUI.ShowQuestCompletedNotification(quest);
                }
                
                Debug.Log("Quest completada: " + quest.title);
            }
        }
    }
    
    private void CloseDialog()
    {
        if (questUI != null)
            questUI.CloseQuestDialog();
    }
    
    // Animação ao clicar no botão
    private IEnumerator AnimateClick()
    {
        Vector3 originalScale = transform.localScale;
        float clickDuration = 0.1f;
        
        // Diminuir ao clicar
        transform.localScale = originalScale * 0.95f;
        
        yield return new WaitForSeconds(clickDuration);
        
        // Restaurar escala original
        transform.localScale = originalScale;
    }
    
    // Implementação das interfaces de pointer
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        
        // Mudar cor ao passar o mouse
        if (buttonBackground != null)
        {
            Color targetColor = hoverColor;
            
            // Manter a cor base para cada tipo, apenas mais clara
            switch (optionType)
            {
                case OptionType.Accept:
                    targetColor = Color.Lerp(acceptColor, Color.white, 0.3f);
                    break;
                case OptionType.Complete:
                    targetColor = Color.Lerp(completeColor, Color.white, 0.3f);
                    break;
                case OptionType.Close:
                    targetColor = Color.Lerp(closeColor, Color.white, 0.3f);
                    break;
                case OptionType.Progress:
                    targetColor = Color.Lerp(normalColor, Color.white, 0.1f);
                    break;
            }
            
            buttonBackground.color = targetColor;
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        
        // Restaurar cor original
        if (buttonBackground != null)
        {
            Color originalColor = normalColor;
            
            switch (optionType)
            {
                case OptionType.Accept:
                    originalColor = acceptColor;
                    break;
                case OptionType.Complete:
                    originalColor = completeColor;
                    break;
                case OptionType.Close:
                    originalColor = closeColor;
                    break;
                case OptionType.Progress:
                    originalColor = normalColor;
                    break;
            }
            
            buttonBackground.color = originalColor;
        }
    }
}