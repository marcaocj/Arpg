using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Namespace para UI de Quest 
namespace RPG.UI.Quest
{
    public class QuestUI : MonoBehaviour
    {
		[Header("Auto Update")]
		public bool autoUpdateUI = true;
		public float updateInterval = 0.5f; // Atualiza a cada 0.5 segundos
		private float updateTimer = 0f;

        [Header("Quests Ativas")]
        public GameObject activeQuestsPanel;
        public Transform questListContainer;
        public GameObject questItemPrefab;
        
        [Header("Detalhes da Quest")]
        public GameObject questDetailsPanel;
        public TextMeshProUGUI questTitleText;
        public TextMeshProUGUI questDescriptionText;
        public Slider questProgressSlider;
        public TextMeshProUGUI questProgressText;
        public Button abandonButton;
        
        [Header("Diálogo de Quest")]
        public GameObject questDialogPanel;
        public TextMeshProUGUI npcNameText;
        public TextMeshProUGUI questDialogText;
        public Transform questOptionsContainer;
        public GameObject questOptionPrefab;
        
        private QuestManager questManager;
        private global::Quest selectedQuest; // Use global::Quest para especificar a classe Quest do escopo global
        
        private void Start()
        {
            questManager = FindObjectOfType<QuestManager>();
            
            // Inicialmente esconder painéis
            if (activeQuestsPanel != null)
                activeQuestsPanel.SetActive(false);
                
            if (questDetailsPanel != null)
                questDetailsPanel.SetActive(false);
                
            if (questDialogPanel != null)
                questDialogPanel.SetActive(false);
                
            // Adicionar listener ao botão de abandonar
            if (abandonButton != null)
                abandonButton.onClick.AddListener(AbandonSelectedQuest);
        }
        
        private void Update()
        {
    // Tecla de atalho para abrir/fechar o painel de quests
    if (Input.GetKeyDown(KeyCode.J))
    {
        ToggleQuestPanel();
    }
    
    // Atualização automática da UI (NOVO)
    if (autoUpdateUI && activeQuestsPanel != null && activeQuestsPanel.activeSelf)
    {
        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0f)
        {
            UpdateQuestList();
            
            // Se há uma quest selecionada, atualizar os detalhes também
            if (selectedQuest != null && questDetailsPanel != null && questDetailsPanel.activeSelf)
            {
                ShowQuestDetails(selectedQuest);
            }
            
            updateTimer = updateInterval;
        }
    }
}
        
        public void ToggleQuestPanel()
        {
            if (activeQuestsPanel != null)
            {
                bool isActive = activeQuestsPanel.activeSelf;
                activeQuestsPanel.SetActive(!isActive);
                
                if (!isActive)
                {
                    UpdateQuestList();
                }
                else
                {
                    if (questDetailsPanel != null)
                        questDetailsPanel.SetActive(false);
                }
            }
        }
        
        public void UpdateQuestList()
        {
            if (questManager == null || questListContainer == null || questItemPrefab == null)
                return;
                
            // Limpar lista atual
            foreach (Transform child in questListContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Adicionar quests ativas
            foreach (global::Quest quest in questManager.activeQuests) // Usar global::Quest
            {
                GameObject questItemObject = Instantiate(questItemPrefab, questListContainer);
                QuestListItem questItem = questItemObject.GetComponent<QuestListItem>();
                
                if (questItem != null)
                {
                    questItem.SetupQuest(quest, this);
                }
            }
        }
        
        public void ShowQuestDetails(global::Quest quest) // Usar global::Quest
        {
            if (questDetailsPanel == null || quest == null)
                return;
                
            selectedQuest = quest;
            
            // Configurar textos
            if (questTitleText != null)
                questTitleText.text = quest.title;
                
            if (questDescriptionText != null)
                questDescriptionText.text = quest.description;
                
            // Configurar barra de progresso
            if (questProgressSlider != null)
            {
                questProgressSlider.minValue = 0;
                questProgressSlider.maxValue = quest.requiredAmount;
                questProgressSlider.value = quest.currentAmount;
            }
            
            // Configurar texto de progresso
            if (questProgressText != null)
            {
                questProgressText.text = quest.currentAmount + " / " + quest.requiredAmount;
            }
            
            // Mostrar painel
            questDetailsPanel.SetActive(true);
        }
        
        private void AbandonSelectedQuest()
        {
            if (questManager != null && selectedQuest != null)
            {
                questManager.AbandonQuest(selectedQuest);
                UpdateQuestList();
                
                if (questDetailsPanel != null)
                    questDetailsPanel.SetActive(false);
                    
                selectedQuest = null;
            }
        }
        
        public void ShowQuestDialog(NPCController questGiver)
        {
            if (questDialogPanel == null || questGiver == null)
                return;
                
            // Configurar nome do NPC
            if (npcNameText != null)
                npcNameText.text = questGiver.npcName;
                
            // Configurar texto inicial
            if (questDialogText != null)
                questDialogText.text = questGiver.greeting;
                
            // Limpar opções anteriores
            if (questOptionsContainer != null)
            {
                foreach (Transform child in questOptionsContainer)
                {
                    Destroy(child.gameObject);
                }
                
                // Adicionar opções para quests disponíveis
                foreach (global::Quest quest in questGiver.availableQuests) // Usar global::Quest
                {
                    // Verificar se a quest já está ativa ou completa
                    if (questManager.activeQuests.Contains(quest) || 
                        questManager.completedQuests.Contains(quest))
                        continue;
                        
                    GameObject optionObject = Instantiate(questOptionPrefab, questOptionsContainer);
                    QuestDialogOption option = optionObject.GetComponent<QuestDialogOption>();
                    
                    if (option != null)
                    {
                        option.SetupOption(quest, questGiver, this);
                    }
                }
                
                // Opção para fechar diálogo
                GameObject closeOption = Instantiate(questOptionPrefab, questOptionsContainer);
                QuestDialogOption closeButton = closeOption.GetComponent<QuestDialogOption>();
                
                if (closeButton != null)
                {
                    closeButton.SetupCloseButton(this);
                }
            }
            
            // Mostrar painel
            questDialogPanel.SetActive(true);
        }
        
        public void CloseQuestDialog()
        {
            if (questDialogPanel != null)
                questDialogPanel.SetActive(false);
        }
    }
}