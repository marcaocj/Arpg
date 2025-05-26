using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Adicionar using para o namespace RPG.UI.Quest
using RPG.UI.Quest;

public class QuestListItem : MonoBehaviour
{
    public TextMeshProUGUI questTitleText;
    public Slider progressSlider;
    public Button itemButton;
    
    private Quest quest;
    private QuestUI questUI;
    
    private void Awake()
    {
        if (itemButton == null)
            itemButton = GetComponent<Button>();
            
        if (itemButton != null)
            itemButton.onClick.AddListener(OnClick);
    }
    
    public void SetupQuest(Quest quest, QuestUI questUI)
    {
        this.quest = quest;
        this.questUI = questUI;
        
        if (questTitleText != null)
            questTitleText.text = quest.title;
            
        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = quest.requiredAmount;
            progressSlider.value = quest.currentAmount;
        }
    }
    
    private void OnClick()
    {
        if (questUI != null && quest != null)
        {
            questUI.ShowQuestDetails(quest);
        }
    }
}