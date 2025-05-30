using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RPG.UI.Quest
{
    /// <summary>
    /// Sistema completo de UI para quests com filtros, busca e organização
    /// </summary>
    public class QuestUI : MonoBehaviour
    {
        [Header("Auto Update")]
        public bool autoUpdateUI = true;
        public float updateInterval = 0.5f;
        private float updateTimer = 0f;

        [Header("Main Panels")]
        public GameObject questLogPanel;
        public GameObject questDetailsPanel;
        public GameObject questDialogPanel;
        public GameObject questTrackingPanel;

        [Header("Quest Log")]
        public Transform questListContainer;
        public GameObject questItemPrefab;
        public ScrollRect questScrollRect;
        
        [Header("Filters & Search")]
        public TMP_Dropdown categoryFilter;
        public TMP_Dropdown statusFilter;
        public TMP_InputField searchField;
        public Button clearFiltersButton;
        
        [Header("Quest Details")]
        public TextMeshProUGUI questTitleText;
        public TextMeshProUGUI questDescriptionText;
        public TextMeshProUGUI questStatusText;
        public TextMeshProUGUI questTimeRemainingText;
        public Slider questProgressSlider;
        public TextMeshProUGUI questProgressText;
        public Transform objectivesContainer;
        public GameObject objectivePrefab;
        public Transform rewardsContainer;
        public GameObject rewardItemPrefab;
        public Button abandonButton;
        public Button trackButton;
        
        [Header("Quest Dialog")]
        public GameObject questDialogPanel;
        public TextMeshProUGUI npcNameText;
        public TextMeshProUGUI questDialogText;
        public Transform questOptionsContainer;
        public GameObject questOptionPrefab;
        public Button closeDialogButton;
        
        [Header("Quest Tracking")]
        public Transform trackedQuestsContainer;
        public GameObject trackedQuestPrefab;
        public int maxTrackedQuests = 5;
        
        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip openSound;
        public AudioClip closeSound;
        public AudioClip questAcceptSound;
        public AudioClip questCompleteSound;
        
        [Header("Colors")]
        public Color availableQuestColor = Color.white;
        public Color activeQuestColor = Color.yellow;
        public Color completedQuestColor = Color.green;
        public Color failedQuestColor = Color.red;
        public Color criticalPriorityColor = Color.red;
        public Color highPriorityColor = Color.orange;
        public Color normalPriorityColor = Color.white;
        public Color lowPriorityColor = Color.gray;
        
        // State
        private QuestManager questManager;
        private Quest selectedQuest;
        private NPCController currentNPC;
        private List<Quest> filteredQuests = new List<Quest>();
        private HashSet<string> trackedQuestIds = new HashSet<string>();
        
        // Filters
        private string currentCategoryFilter = "All";
        private string currentStatusFilter = "All";
        private string currentSearchText = "";
        
        #region Initialization
        
        private void Start()
        {
            InitializeUI();
            SubscribeToEvents();
            SetupFilters();
        }
        
        private void InitializeUI()
        {
            questManager = QuestManager.Instance;
            if (questManager == null)
            {
                questManager = FindObjectOfType<QuestManager>();
            }
            
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
            
            // Initially hide all panels
            SetPanelActive(questLogPanel, false);
            SetPanelActive(questDetailsPanel, false);
            SetPanelActive(questDialogPanel, false);
            
            // Setup button events
            if (abandonButton != null)
                abandonButton.onClick.AddListener(AbandonSelectedQuest);
            
            if (trackButton != null)
                trackButton.onClick.AddListener(ToggleQuestTracking);
                
            if (closeDialogButton != null)
                closeDialogButton.onClick.AddListener(CloseQuestDialog);
                
            if (clearFiltersButton != null)
                clearFiltersButton.onClick.AddListener(ClearAllFilters);
            
            // Setup search field
            if (searchField != null)
            {
                searchField.onValueChanged.AddListener(OnSearchTextChanged);
            }
        }
        
        private void SubscribeToEvents()
        {
            EventManager.Subscribe<QuestAcceptedEvent>(OnQuestAccepted);
            EventManager.Subscribe<QuestCompletedEvent>(OnQuestCompleted);
            EventManager.Subscribe<QuestProgressUpdatedEvent>(OnQuestProgressUpdated);
            EventManager.Subscribe<QuestAbandonedEvent>(OnQuestAbandoned);
        }
        
        private void SetupFilters()
        {
            // Setup category filter
            if (categoryFilter != null)
            {
                categoryFilter.ClearOptions();
                var categories = new List<string> { "All" };
                
                if (questManager != null)
                {
                    var questCategories = questManager.questDatabase
                        .Where(q => !string.IsNullOrEmpty(q.category))
                        .Select(q => q.category)
                        .Distinct()
                        .OrderBy(c => c);
                    
                    categories.AddRange(questCategories);
                }
                
                categoryFilter.AddOptions(categories);
                categoryFilter.onValueChanged.AddListener(OnCategoryFilterChanged);
            }
            
            // Setup status filter
            if (statusFilter != null)
            {
                statusFilter.ClearOptions();
                var statuses = new List<string> 
                { 
                    "All", "Available", "Active", "Completed", "Failed" 
                };
                statusFilter.AddOptions(statuses);
                statusFilter.onValueChanged.AddListener(OnStatusFilterChanged);
            }
        }
        
        #endregion
        
        #region Input Handling
        
        private void Update()
        {
            // Toggle quest log with J key
            if (Input.GetKeyDown(KeyCode.J))
            {
                ToggleQuestLog();
            }
            
            // Auto-update UI if enabled
            if (autoUpdateUI && questLogPanel != null && questLogPanel.activeSelf)
            {
                updateTimer -= Time.deltaTime;
                if (updateTimer <= 0f)
                {
                    UpdateQuestList();
                    UpdateQuestTracking();
                    
                    // Update selected quest details if showing
                    if (selectedQuest != null && questDetailsPanel != null && questDetailsPanel.activeSelf)
                    {
                        ShowQuestDetails(selectedQuest);
                    }
                    
                    updateTimer = updateInterval;
                }
            }
        }
        
        #endregion
        
        #region Panel Management
        
        public void ToggleQuestLog()
        {
            bool isActive = questLogPanel != null && questLogPanel.activeSelf;
            SetPanelActive(questLogPanel, !isActive);
            
            if (!isActive)
            {
                UpdateQuestList();
                PlaySound(openSound);
            }
            else
            {
                SetPanelActive(questDetailsPanel, false);
                PlaySound(closeSound);
            }
        }
        
        public void ShowQuestDialog(NPCController npc)
        {
            if (questDialogPanel == null || npc == null) return;
            
            currentNPC = npc;
            
            // Setup NPC name
            if (npcNameText != null)
                npcNameText.text = npc.npcName;
            
            // Setup initial dialog text
            if (questDialogText != null)
                questDialogText.text = npc.greeting;
            
            // Setup quest options
            SetupQuestDialogOptions(npc);
            
            SetPanelActive(questDialogPanel, true);
            PlaySound(openSound);
        }
        
        public void CloseQuestDialog()
        {
            SetPanelActive(questDialogPanel, false);
            currentNPC = null;
            PlaySound(closeSound);
        }
        
        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
        
        #endregion
        
        #region Quest List Management
        
        public void UpdateQuestList()
        {
            if (questManager == null || questListContainer == null || questItemPrefab == null)
                return;
            
            // Clear current list
            foreach (Transform child in questListContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Get filtered quests
            filteredQuests = GetFilteredQuests();
            
            // Create quest items
            foreach (Quest quest in filteredQuests)
            {
                GameObject questItemObject = Instantiate(questItemPrefab, questListContainer);
                QuestListItem questItem = questItemObject.GetComponent<QuestListItem>();
                
                if (questItem != null)
                {
                    questItem.SetupQuest(quest, this);
                }
            }
            
            // Update scroll position if needed
            if (questScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                questScrollRect.verticalNormalizedPosition = 1f;
            }
        }
        
        private List<Quest> GetFilteredQuests()
        {
            if (questManager == null) return new List<Quest>();
            
            var allQuests = new List<Quest>();
            
            // Collect quests based on status filter
            switch (currentStatusFilter)
            {
                case "Available":
                    allQuests.AddRange(questManager.availableQuests);
                    break;
                case "Active":
                    allQuests.AddRange(questManager.activeQuests);
                    break;
                case "Completed":
                    allQuests.AddRange(questManager.completedQuests);
                    allQuests.AddRange(questManager.turnedInQuests);
                    break;
                case "Failed":
                    allQuests.AddRange(questManager.failedQuests);
                    break;
                default: // "All"
                    allQuests.AddRange(questManager.availableQuests);
                    allQuests.AddRange(questManager.activeQuests);
                    allQuests.AddRange(questManager.completedQuests);
                    allQuests.AddRange(questManager.turnedInQuests);
                    break;
            }
            
            // Apply category filter
            if (currentCategoryFilter != "All")
            {
                allQuests = allQuests.Where(q => q.category == currentCategoryFilter).ToList();
            }
            
            // Apply search filter
            if (!string.IsNullOrEmpty(currentSearchText))
            {
                string searchLower = currentSearchText.ToLower();
                allQuests = allQuests.Where(q => 
                    q.title.ToLower().Contains(searchLower) ||
                    q.description.ToLower().Contains(searchLower) ||
                    q.category.ToLower().Contains(searchLower)
                ).ToList();
            }
            
            // Sort quests
            allQuests = allQuests.OrderBy(GetQuestSortPriority)
                               .ThenByDescending(q => (int)q.priority)
                               .ThenBy(q => q.title)
                               .ToList();
            
            return allQuests;
        }
        
        private int GetQuestSortPriority(Quest quest)
        {
            // Sort order: Active > Available > Completed > Failed
            switch (quest.status)
            {
                case QuestStatus.Active: return 0;
                case QuestStatus.NotStarted: return 1;
                case QuestStatus.Completed: return 2;
                case QuestStatus.TurnedIn: return 3;
                case QuestStatus.Failed: return 4;
                case QuestStatus.Abandoned: return 5;
                default: return 6;
            }
        }
        
        #endregion
        
        #region Quest Details
        
        public void ShowQuestDetails(Quest quest)
        {
            if (questDetailsPanel == null || quest == null)
                return;
            
            selectedQuest = quest;
            
            // Update title and status
            if (questTitleText != null)
            {
                questTitleText.text = quest.title;
                questTitleText.color = GetQuestStatusColor(quest.status);
            }
            
            if (questStatusText != null)
            {
                questStatusText.text = GetQuestStatusDisplay(quest);
                questStatusText.color = GetQuestStatusColor(quest.status);
            }
            
            // Update description
            if (questDescriptionText != null)
                questDescriptionText.text = quest.description;
            
            // Update time remaining
            UpdateTimeRemaining(quest);
            
            // Update progress
            UpdateQuestProgress(quest);
            
            // Update objectives
            UpdateObjectivesList(quest);
            
            // Update rewards
            UpdateRewardsList(quest);
            
            // Update buttons
            UpdateActionButtons(quest);
            
            SetPanelActive(questDetailsPanel, true);
        }
        
        private void UpdateTimeRemaining(Quest quest)
        {
            if (questTimeRemainingText == null) return;
            
            if (quest.hasTimeLimit && quest.status == QuestStatus.Active)
            {
                questTimeRemainingText.gameObject.SetActive(true);
                questTimeRemainingText.text = $"Tempo restante: {quest.GetTimeRemainingText()}";
                
                // Change color based on urgency
                float timePercentage = quest.GetTimePercentage();
                if (timePercentage < 0.25f)
                    questTimeRemainingText.color = Color.red;
                else if (timePercentage < 0.5f)
                    questTimeRemainingText.color = Color.yellow;
                else
                    questTimeRemainingText.color = Color.white;
            }
            else
            {
                questTimeRemainingText.gameObject.SetActive(false);
            }
        }
        
        private void UpdateQuestProgress(Quest quest)
        {
            if (questProgressSlider != null)
            {
                float progress = quest.GetOverallProgress();
                questProgressSlider.value = progress;
                questProgressSlider.gameObject.SetActive(quest.status == QuestStatus.Active);
            }
            
            if (questProgressText != null)
            {
                if (quest.status == QuestStatus.Active)
                {
                    float progress = quest.GetOverallProgress();
                    questProgressText.text = $"Progresso: {progress:P0}";
                    questProgressText.gameObject.SetActive(true);
                }
                else
                {
                    questProgressText.gameObject.SetActive(false);
                }
            }
        }
        
        private void UpdateObjectivesList(Quest quest)
        {
            if (objectivesContainer == null || objectivePrefab == null) return;
            
            // Clear existing objectives
            foreach (Transform child in objectivesContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create objective items
            foreach (var objective in quest.objectives)
            {
                GameObject objItem = Instantiate(objectivePrefab, objectivesContainer);
                QuestObjectiveUI objUI = objItem.GetComponent<QuestObjectiveUI>();
                
                if (objUI != null)
                {
                    objUI.SetupObjective(objective);
                }
                else
                {
                    // Fallback if no custom component
                    TextMeshProUGUI text = objItem.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        string prefix = objective.isCompleted ? "✓" : "○";
                        string optional = objective.isOptional ? " (Opcional)" : "";
                        Color color = objective.isCompleted ? Color.green : Color.white;
                        
                        if (objective.trackProgress && objective.requiredAmount > 1)
                        {
                            text.text = $"{prefix} {objective.description} {objective.GetProgressText()}{optional}";
                        }
                        else
                        {
                            text.text = $"{prefix} {objective.description}{optional}";
                        }
                        
                        text.color = color;
                    }
                }
            }
        }
        
        private void UpdateRewardsList(Quest quest)
        {
            if (rewardsContainer == null || rewardItemPrefab == null || quest.rewards == null) return;
            
            // Clear existing rewards
            foreach (Transform child in rewardsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create reward items
            if (quest.rewards.gold > 0)
            {
                CreateRewardItem($"{quest.rewards.gold} Ouro", Color.yellow);
            }
            
            if (quest.rewards.experience > 0)
            {
                CreateRewardItem($"{quest.rewards.experience} XP", Color.cyan);
            }
            
            if (quest.rewards.attributePoints > 0)
            {
                CreateRewardItem($"{quest.rewards.attributePoints} Pontos de Atributo", Color.green);
            }
            
            if (quest.rewards.item != null)
            {
                CreateRewardItem(quest.rewards.item.name, quest.rewards.item.GetRarityColor());
            }
            
            foreach (var item in quest.rewards.items)
            {
                CreateRewardItem(item.name, item.GetRarityColor());
            }
            
            if (quest.rewards.unlockedQuest != null)
            {
                CreateRewardItem($"Quest: {quest.rewards.unlockedQuest.title}", Color.magenta);
            }
        }
        
        private void CreateRewardItem(string rewardText, Color rewardColor)
        {
            GameObject rewardItem = Instantiate(rewardItemPrefab, rewardsContainer);
            TextMeshProUGUI text = rewardItem.GetComponentInChildren<TextMeshProUGUI>();
            
            if (text != null)
            {
                text.text = rewardText;
                text.color = rewardColor;
            }
        }
        
        private void UpdateActionButtons(Quest quest)
        {
            // Abandon button
            if (abandonButton != null)
            {
                bool canAbandon = quest.status == QuestStatus.Active && !quest.isMainQuest;
                abandonButton.gameObject.SetActive(canAbandon);
            }
            
            // Track button
            if (trackButton != null)
            {
                bool canTrack = quest.status == QuestStatus.Active;
                trackButton.gameObject.SetActive(canTrack);
                
                if (canTrack)
                {
                    bool isTracked = trackedQuestIds.Contains(quest.id);
                    TextMeshProUGUI buttonText = trackButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = isTracked ? "Não Rastrear" : "Rastrear";
                    }
                }
            }
        }
        
        #endregion
        
        #region Quest Dialog
        
        private void SetupQuestDialogOptions(NPCController npc)
        {
            if (questOptionsContainer == null || questOptionPrefab == null) return;
            
            // Clear existing options
            foreach (Transform child in questOptionsContainer)
            {
                Destroy(child.gameObject);
            }
            
            var availableQuests = npc.GetAvailableQuestsForPlayer();
            var questsToTurnIn = npc.GetQuestsToTurnInForPlayer();
            
            // Add turn-in options first
            foreach (Quest quest in questsToTurnIn)
            {
                CreateQuestDialogOption($"Entregar: {quest.title}", quest, true, npc);
            }
            
            // Add available quest options
            foreach (Quest quest in availableQuests)
            {
                // Check requirements
                bool meetsRequirements = quest.CanPlayerAccept(PlayerController.Instance);
                string optionText = meetsRequirements ? 
                    $"Aceitar: {quest.title}" : 
                    $"[Bloqueada] {quest.title}";
                
                CreateQuestDialogOption(optionText, quest, false, npc, meetsRequirements);
            }
            
            // Add close option
            CreateCloseDialogOption();
        }
        
        private void CreateQuestDialogOption(string optionText, Quest quest, bool isTurnIn, NPCController npc, bool enabled = true)
        {
            GameObject optionObject = Instantiate(questOptionPrefab, questOptionsContainer);
            QuestDialogOption option = optionObject.GetComponent<QuestDialogOption>();
            
            if (option != null)
            {
                option.SetupOption(quest, npc, this, isTurnIn);
                option.SetEnabled(enabled);
            }
            else
            {
                // Fallback setup
                Button button = optionObject.GetComponent<Button>();
                TextMeshProUGUI text = optionObject.GetComponentInChildren<TextMeshProUGUI>();
                
                if (text != null)
                {
                    text.text = optionText;
                    text.color = enabled ? Color.white : Color.gray;
                }
                
                if (button != null)
                {
                    button.interactable = enabled;
                    if (enabled)
                    {
                        button.onClick.AddListener(() => 
                        {
                            if (isTurnIn)
                                HandleQuestTurnIn(quest, npc);
                            else
                                HandleQuestAccept(quest, npc);
                        });
                    }
                }
            }
        }
        
        private void CreateCloseDialogOption()
        {
            GameObject optionObject = Instantiate(questOptionPrefab, questOptionsContainer);
            QuestDialogOption option = optionObject.GetComponent<QuestDialogOption>();
            
            if (option != null)
            {
                option.SetupCloseButton(this);
            }
            else
            {
                // Fallback setup
                Button button = optionObject.GetComponent<Button>();
                TextMeshProUGUI text = optionObject.GetComponentInChildren<TextMeshProUGUI>();
                
                if (text != null)
                    text.text = "Fechar";
                
                if (button != null)
                    button.onClick.AddListener(CloseQuestDialog);
            }
        }
        
        private void HandleQuestAccept(Quest quest, NPCController npc)
        {
            if (questManager != null && questManager.AcceptQuest(quest))
            {
                PlaySound(questAcceptSound);
                ShowFloatingText($"Quest aceita: {quest.title}!");
                
                // Notify NPC
                npc?.AcceptQuestFromDialog(quest);
                
                // Refresh dialog
                SetupQuestDialogOptions(npc);
            }
        }
        
        private void HandleQuestTurnIn(Quest quest, NPCController npc)
        {
            if (questManager != null && questManager.TurnInQuest(quest))
            {
                PlaySound(questCompleteSound);
                ShowFloatingText($"Quest entregue: {quest.title}!");
                
                // Notify NPC
                npc?.TurnInQuestFromDialog(quest);
                
                // Refresh dialog
                SetupQuestDialogOptions(npc);
            }
        }
        
        #endregion
        
        #region Quest Tracking
        
        public void UpdateQuestTracking()
        {
            if (trackedQuestsContainer == null || trackedQuestPrefab == null) return;
            
            // Clear existing tracked quests
            foreach (Transform child in trackedQuestsContainer)
            {
                Destroy(child.gameObject);
            }
            
            if (questManager == null) return;
            
            // Get active tracked quests
            var trackedQuests = questManager.activeQuests
                .Where(q => trackedQuestIds.Contains(q.id))
                .OrderByDescending(q => (int)q.priority)
                .Take(maxTrackedQuests)
                .ToList();
            
            // Create tracking UI for each quest
            foreach (var quest in trackedQuests)
            {
                GameObject trackedItem = Instantiate(trackedQuestPrefab, trackedQuestsContainer);
                QuestTrackingItem trackingUI = trackedItem.GetComponent<QuestTrackingItem>();
                
                if (trackingUI != null)
                {
                    trackingUI.SetupTrackedQuest(quest);
                }
                else
                {
                    // Fallback setup
                SetupBasicTrackedQuest(trackedItem, quest);
                }
            }
            
            // Show/hide tracking panel based on tracked quests
            if (questTrackingPanel != null)
            {
                questTrackingPanel.SetActive(trackedQuests.Count > 0);
            }
        }
        
        private void SetupBasicTrackedQuest(GameObject trackedItem, Quest quest)
        {
            TextMeshProUGUI[] texts = trackedItem.GetComponentsInChildren<TextMeshProUGUI>();
            Slider progressSlider = trackedItem.GetComponentInChildren<Slider>();
            
            if (texts.Length > 0)
            {
                texts[0].text = quest.title;
                texts[0].color = GetQuestPriorityColor(quest.priority);
            }
            
            if (texts.Length > 1)
            {
                // Show current objective
                var currentObjective = quest.objectives.FirstOrDefault(obj => !obj.isCompleted && !obj.isOptional);
                if (currentObjective != null)
                {
                    if (currentObjective.trackProgress && currentObjective.requiredAmount > 1)
                    {
                        texts[1].text = $"{currentObjective.description} {currentObjective.GetProgressText()}";
                    }
                    else
                    {
                        texts[1].text = currentObjective.description;
                    }
                }
                else
                {
                    texts[1].text = "Objetivos completos";
                }
            }
            
            if (progressSlider != null)
            {
                progressSlider.value = quest.GetOverallProgress();
            }
        }
        
        public void ToggleQuestTracking()
        {
            if (selectedQuest == null) return;
            
            if (trackedQuestIds.Contains(selectedQuest.id))
            {
                trackedQuestIds.Remove(selectedQuest.id);
                ShowFloatingText($"Parou de rastrear: {selectedQuest.title}");
            }
            else
            {
                if (trackedQuestIds.Count >= maxTrackedQuests)
                {
                    ShowFloatingText($"Máximo de {maxTrackedQuests} quests podem ser rastreadas!");
                    return;
                }
                
                trackedQuestIds.Add(selectedQuest.id);
                ShowFloatingText($"Rastreando: {selectedQuest.title}");
            }
            
            UpdateActionButtons(selectedQuest);
            UpdateQuestTracking();
        }
        
        #endregion
        
        #region Filter System
        
        private void OnCategoryFilterChanged(int index)
        {
            if (categoryFilter != null)
            {
                currentCategoryFilter = categoryFilter.options[index].text;
                UpdateQuestList();
            }
        }
        
        private void OnStatusFilterChanged(int index)
        {
            if (statusFilter != null)
            {
                currentStatusFilter = statusFilter.options[index].text;
                UpdateQuestList();
            }
        }
        
        private void OnSearchTextChanged(string searchText)
        {
            currentSearchText = searchText;
            
            // Debounce search to avoid too many updates
            CancelInvoke(nameof(UpdateQuestList));
            Invoke(nameof(UpdateQuestList), 0.3f);
        }
        
        public void ClearAllFilters()
        {
            currentCategoryFilter = "All";
            currentStatusFilter = "All";
            currentSearchText = "";
            
            if (categoryFilter != null)
                categoryFilter.value = 0;
            
            if (statusFilter != null)
                statusFilter.value = 0;
            
            if (searchField != null)
                searchField.text = "";
            
            UpdateQuestList();
            ShowFloatingText("Filtros limpos");
        }
        
        #endregion
        
        #region Action Handlers
        
        private void AbandonSelectedQuest()
        {
            if (selectedQuest == null) return;
            
            if (selectedQuest.isMainQuest)
            {
                ShowFloatingText("Não é possível abandonar quest principal!");
                return;
            }
            
            // Show confirmation dialog (simplified)
            if (questManager != null && questManager.AbandonQuest(selectedQuest))
            {
                ShowFloatingText($"Quest abandonada: {selectedQuest.title}");
                SetPanelActive(questDetailsPanel, false);
                selectedQuest = null;
                UpdateQuestList();
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnQuestAccepted(QuestAcceptedEvent eventData)
        {
            UpdateQuestList();
            UpdateQuestTracking();
        }
        
        private void OnQuestCompleted(QuestCompletedEvent eventData)
        {
            UpdateQuestList();
            UpdateQuestTracking();
            
            if (selectedQuest != null && selectedQuest.id == eventData.quest.id)
            {
                ShowQuestDetails(selectedQuest);
            }
        }
        
        private void OnQuestProgressUpdated(QuestProgressUpdatedEvent eventData)
        {
            UpdateQuestTracking();
            
            if (selectedQuest != null && selectedQuest.id == eventData.quest.id)
            {
                UpdateQuestProgress(selectedQuest);
                UpdateObjectivesList(selectedQuest);
            }
        }
        
        private void OnQuestAbandoned(QuestAbandonedEvent eventData)
        {
            UpdateQuestList();
            UpdateQuestTracking();
            
            // Remove from tracking if it was tracked
            if (trackedQuestIds.Contains(eventData.quest.id))
            {
                trackedQuestIds.Remove(eventData.quest.id);
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        private Color GetQuestStatusColor(QuestStatus status)
        {
            switch (status)
            {
                case QuestStatus.NotStarted: return availableQuestColor;
                case QuestStatus.Active: return activeQuestColor;
                case QuestStatus.Completed:
                case QuestStatus.TurnedIn: return completedQuestColor;
                case QuestStatus.Failed:
                case QuestStatus.Abandoned: return failedQuestColor;
                default: return Color.white;
            }
        }
        
        private Color GetQuestPriorityColor(QuestPriority priority)
        {
            switch (priority)
            {
                case QuestPriority.Critical: return criticalPriorityColor;
                case QuestPriority.High: return highPriorityColor;
                case QuestPriority.Normal: return normalPriorityColor;
                case QuestPriority.Low: return lowPriorityColor;
                default: return Color.white;
            }
        }
        
        private string GetQuestStatusDisplay(Quest quest)
        {
            string status = quest.GetStatusText();
            
            if (quest.isMainQuest)
                status += " [PRINCIPAL]";
            
            if (quest.hasTimeLimit && quest.status == QuestStatus.Active)
                status += " [TEMPO LIMITADO]";
            
            return status;
        }
        
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        private void ShowFloatingText(string message)
        {
            EventManager.TriggerEvent(new NotificationEvent
            {
                message = message,
                type = NotificationType.Info,
                duration = 2f,
                color = Color.white
            });
        }
        
        #endregion
        
        #region Public Interface
        
        public void RefreshUI()
        {
            UpdateQuestList();
            UpdateQuestTracking();
            
            if (selectedQuest != null)
            {
                ShowQuestDetails(selectedQuest);
            }
        }
        
        public void ForceRefresh()
        {
            // Force immediate refresh without waiting for timer
            updateTimer = 0f;
            RefreshUI();
        }
        
        public void SetMaxTrackedQuests(int max)
        {
            maxTrackedQuests = Mathf.Max(1, max);
            
            // Remove excess tracked quests if needed
            while (trackedQuestIds.Count > maxTrackedQuests)
            {
                var oldestTracked = trackedQuestIds.First();
                trackedQuestIds.Remove(oldestTracked);
            }
            
            UpdateQuestTracking();
        }
        
        public bool IsQuestTracked(string questId)
        {
            return trackedQuestIds.Contains(questId);
        }
        
        public void TrackQuest(string questId)
        {
            if (!trackedQuestIds.Contains(questId) && trackedQuestIds.Count < maxTrackedQuests)
            {
                trackedQuestIds.Add(questId);
                UpdateQuestTracking();
            }
        }
        
        public void UntrackQuest(string questId)
        {
            if (trackedQuestIds.Remove(questId))
            {
                UpdateQuestTracking();
            }
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            EventManager.Unsubscribe<QuestAcceptedEvent>(OnQuestAccepted);
            EventManager.Unsubscribe<QuestCompletedEvent>(OnQuestCompleted);
            EventManager.Unsubscribe<QuestProgressUpdatedEvent>(OnQuestProgressUpdated);
            EventManager.Unsubscribe<QuestAbandonedEvent>(OnQuestAbandoned);
        }
        
        #endregion
    }
}