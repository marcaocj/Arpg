using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Adicionar using para o namespace RPG.UI.Quest
using RPG.UI.Quest;

public class QuestDialogOption : MonoBehaviour
{
    public TextMeshProUGUI optionText;
    public Button optionButton;
    
    private Quest quest;
    private NPCController questGiver;
    private QuestUI questUI;
    private bool isCloseButton = false;
    
    private void Awake()
    {
        if (optionButton == null)
            optionButton = GetComponent<Button>();
            
        if (optionButton != null)
            optionButton.onClick.AddListener(OnClick);
    }
    
    public void SetupOption(Quest quest, NPCController questGiver, QuestUI questUI)
    {
        this.quest = quest;
        this.questGiver = questGiver;
        this.questUI = questUI;
        
        if (optionText != null)
            optionText.text = "Aceitar: " + quest.title;
    }
    
    public void SetupCloseButton(QuestUI questUI)
    {
        this.questUI = questUI;
        isCloseButton = true;
        
        if (optionText != null)
            optionText.text = "Fechar";
    }
    
    private void OnClick()
    {
        if (isCloseButton)
        {
            if (questUI != null)
                questUI.CloseQuestDialog();
        }
        else
        {
            if (questGiver != null && quest != null)
            {
                QuestManager questManager = FindObjectOfType<QuestManager>();
                if (questManager != null)
                {
                    questManager.AcceptQuest(quest);
                    
                    // Atualizar UI
                    if (questUI != null)
                    {
                        questUI.CloseQuestDialog();
                        questUI.UpdateQuestList();
                    }
                }
            }
        }
    }
}