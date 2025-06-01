using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Adicionar using para o namespace RPG.UI.Quest
using RPG.UI.Quest;

/// <summary>
/// CORRIGIDO: Conflito de namespace com Quest resolvido usando referência global explícita
/// </summary>
public class QuestDialogOption : MonoBehaviour
{
    public TextMeshProUGUI optionText;
    public Button optionButton;
    
    private global::Quest quest; // EXPLÍCITO: usar Quest do namespace global
    private NPCController questGiver;
    private QuestUI questUI;
    private bool isCloseButton = false;
    private bool isTurnIn = false;
    private bool enabled = true;
    
    private void Awake()
    {
        if (optionButton == null)
            optionButton = GetComponent<Button>();
            
        if (optionButton != null)
            optionButton.onClick.AddListener(OnClick);
    }
    
    public void SetupOption(global::Quest quest, NPCController questGiver, QuestUI questUI) // EXPLÍCITO
    {
        SetupOption(quest, questGiver, questUI, false);
    }
    
    public void SetupOption(global::Quest quest, NPCController questGiver, QuestUI questUI, bool isTurnIn) // EXPLÍCITO
    {
        this.quest = quest;
        this.questGiver = questGiver;
        this.questUI = questUI;
        this.isTurnIn = isTurnIn;
        this.isCloseButton = false;
        
        if (optionText != null)
        {
            if (isTurnIn)
            {
                optionText.text = "Entregar: " + quest.title;
            }
            else
            {
                optionText.text = "Aceitar: " + quest.title;
            }
        }
    }
    
    public void SetupCloseButton(QuestUI questUI)
    {
        this.questUI = questUI;
        this.isCloseButton = true;
        this.quest = null;
        this.questGiver = null;
        this.isTurnIn = false;
        
        if (optionText != null)
            optionText.text = "Fechar";
    }
    
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        
        if (optionButton != null)
        {
            optionButton.interactable = enabled;
        }
        
        if (optionText != null)
        {
            optionText.color = enabled ? Color.white : Color.gray;
        }
    }
    
    private void OnClick()
    {
        if (!enabled) return;
        
        if (isCloseButton)
        {
            if (questUI != null)
                questUI.CloseQuestDialog();
        }
        else if (quest != null && questGiver != null)
        {
            QuestManager questManager = QuestManager.Instance;
            if (questManager == null)
                questManager = FindObjectOfType<QuestManager>();
            
            if (questManager != null)
            {
                bool success = false;
                
                if (isTurnIn)
                {
                    success = questManager.TurnInQuest(quest);
                    if (success)
                    {
                        Debug.Log($"Quest entregue: {quest.title}");
                    }
                }
                else
                {
                    success = questManager.AcceptQuest(quest);
                    if (success)
                    {
                        Debug.Log($"Quest aceita: {quest.title}");
                    }
                }
                
                if (success)
                {
                    // Atualizar UI
                    if (questUI != null)
                    {
                        questUI.CloseQuestDialog();
                        questUI.UpdateQuestList();
                    }
                    
                    // Notificar NPC se necessário
                    if (isTurnIn)
                    {
                        questGiver?.TurnInQuestFromDialog(quest);
                    }
                    else
                    {
                        questGiver?.AcceptQuestFromDialog(quest);
                    }
                }
            }
            else
            {
                Debug.LogError("QuestManager não encontrado!");
            }
        }
    }
    
    public global::Quest GetQuest() // EXPLÍCITO
    {
        return quest;
    }
    
    public bool IsCloseButton()
    {
        return isCloseButton;
    }
    
    public bool IsTurnInOption()
    {
        return isTurnIn;
    }
    
    public bool IsEnabled()
    {
        return enabled;
    }
}
