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
        public Button trackQuestButton; // NOVO: Botão para rastrear quest
        
        [Header("Diálogo de Quest")]
        public GameObject questDialogPanel;
        public TextMeshProUGUI npcNameText;
        public TextMeshProUGUI questDialogText;
        public Transform questOptionsContainer;
        public GameObject questOptionPrefab;
        
        [Header("Notificações")]
        public GameObject questCompletedNotification; // NOVO: Notificação de conclusão
        public TextMeshProUGUI questCompletedText;
        public float notificationDuration = 3f;
        public AudioClip questAcceptedSound; // NOVO: Som ao aceitar quest
        public AudioClip questCompletedSound; // NOVO: Som ao completar quest
        
        [Header("Animações")]
        public AnimationCurve questItemScaleAnimation; // NOVO: Animação ao adicionar quest
        public Color trackedQuestColor = new Color(1f, 0.8f, 0.2f); // NOVO: Cor para quest rastreada
        
        private QuestManager questManager;
        private global::Quest selectedQuest;
        private global::Quest trackedQuest; // NOVO: Quest atualmente rastreada
        private AudioSource audioSource;
        private Dictionary<string, QuestListItem> questListItems = new Dictionary<string, QuestListItem>(); // NOVO: Cache dos itens da UI
        
        // Evento para notificar outros sistemas quando uma quest é rastreada
        public delegate void QuestTrackedHandler(global::Quest quest);
        public event QuestTrackedHandler OnQuestTracked;
        
        private void Start()
        {
            questManager = FindObjectOfType<QuestManager>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            
            // Inicialmente esconder painéis
            if (activeQuestsPanel != null)
                activeQuestsPanel.SetActive(false);
                
            if (questDetailsPanel != null)
                questDetailsPanel.SetActive(false);
                
            if (questDialogPanel != null)
                questDialogPanel.SetActive(false);
                
            if (questCompletedNotification != null)
                questCompletedNotification.SetActive(false);
                
            // Adicionar listener ao botão de abandonar
            if (abandonButton != null)
                abandonButton.onClick.AddListener(AbandonSelectedQuest);
                
            // Adicionar listener ao botão de rastrear
            if (trackQuestButton != null)
                trackQuestButton.onClick.AddListener(ToggleTrackSelectedQuest);
            
            // Subscrever para eventos do QuestManager
            if (questManager != null)
            {
                questManager.OnQuestAccepted += OnQuestAccepted;
                questManager.OnQuestCompleted += OnQuestCompleted;
                questManager.OnQuestProgress += OnQuestProgress;
            }
        }
        
        private void OnDestroy()
        {
            // Cancelar inscrição nos eventos
            if (questManager != null)
            {
                questManager.OnQuestAccepted -= OnQuestAccepted;
                questManager.OnQuestCompleted -= OnQuestCompleted;
                questManager.OnQuestProgress -= OnQuestProgress;
            }
        }
        
        private void Update()
        {
            // Tecla de atalho para abrir/fechar o painel de quests
            if (Input.GetKeyDown(KeyCode.J))
            {
                ToggleQuestPanel();
            }
            
            // Atualização automática da UI quando visível
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
        
        // NOVOS MÉTODOS DE EVENTOS
        private void OnQuestAccepted(global::Quest quest)
        {
            // Atualizar lista
            UpdateQuestList();
            
            // Exibir notificação flutuante (implementação sugerida)
            ShowFloatingNotification($"Nova Quest: {quest.title}", Color.yellow);
            
            // Reproduzir som
            if (audioSource != null && questAcceptedSound != null)
                audioSource.PlayOneShot(questAcceptedSound);
                
            // Auto-rastrear a primeira quest aceita se não houver nenhuma rastreada
            if (trackedQuest == null)
            {
                trackedQuest = quest;
                OnQuestTracked?.Invoke(quest);
                UpdateQuestList(); // Atualizar lista para mostrar a quest rastreada
            }
        }
        
        private void OnQuestCompleted(global::Quest quest)
        {
            // Atualizar lista
            UpdateQuestList();
            
            // Mostrar notificação
            if (questCompletedNotification != null && questCompletedText != null)
            {
                questCompletedText.text = $"Quest Concluída: {quest.title}";
                questCompletedNotification.SetActive(true);
                StartCoroutine(HideNotificationAfterDelay());
            }
            
            // Reproduzir som
            if (audioSource != null && questCompletedSound != null)
                audioSource.PlayOneShot(questCompletedSound);
                
            // Se a quest completada era a rastreada, limpar o rastreamento
            if (trackedQuest == quest)
            {
                trackedQuest = null;
                OnQuestTracked?.Invoke(null);
                
                // Tentar rastrear automaticamente outra quest ativa, se houver
                if (questManager.activeQuests.Count > 0)
                {
                    trackedQuest = questManager.activeQuests[0];
                    OnQuestTracked?.Invoke(trackedQuest);
                }
            }
        }
        
        private void OnQuestProgress(global::Quest quest)
        {
            // Atualizar apenas o item da quest atualizada, não a lista inteira
            if (questListItems.ContainsKey(quest.id))
            {
                QuestListItem questItem = questListItems[quest.id];
                questItem.UpdateProgress(quest);
                
                // Se for a quest selecionada, atualizar os detalhes
                if (selectedQuest == quest && questDetailsPanel != null && questDetailsPanel.activeSelf)
                {
                    ShowQuestDetails(quest);
                }
                
                // Notificar o jogador se a quest for a rastreada
                if (trackedQuest == quest)
                {
                    int remaining = quest.requiredAmount - quest.currentAmount;
                    ShowFloatingNotification($"Progresso na quest: {quest.currentAmount}/{quest.requiredAmount}", Color.white);
                }
            }
        }
        
        private IEnumerator HideNotificationAfterDelay()
        {
            yield return new WaitForSeconds(notificationDuration);
            if (questCompletedNotification != null)
                questCompletedNotification.SetActive(false);
        }
        
        // Método de utilidade para notificações flutuantes
        private void ShowFloatingNotification(string message, Color textColor)
        {
            // Implementação sugerida - pode ser expandida conforme necessário
            Debug.Log($"[Quest Notification] {message}");
            
            // Se você tem um sistema de notificações na tela:
            // NotificationSystem.Instance.ShowNotification(message, textColor);
        }
        
        // MÉTODOS DE UI MELHORADOS
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
                
            // Registrar quais quests estão atualmente na lista
            HashSet<string> existingQuestIds = new HashSet<string>(questListItems.Keys);
                
            // Adicionar/atualizar quests ativas
            foreach (global::Quest quest in questManager.activeQuests)
            {
                existingQuestIds.Remove(quest.id); // Removemos da lista de existentes pois vamos atualizá-la
                
                if (questListItems.ContainsKey(quest.id))
                {
                    // Atualizar item existente
                    questListItems[quest.id].UpdateProgress(quest);
                }
                else
                {
                    // Criar novo item
                    GameObject questItemObject = Instantiate(questItemPrefab, questListContainer);
                    QuestListItem questItem = questItemObject.GetComponent<QuestListItem>();
                    
                    if (questItem != null)
                    {
                        questItem.SetupQuest(quest, this);
                        
                        // Animar adição de nova quest
                        StartCoroutine(AnimateQuestItem(questItem.transform));
                        
                        // Adicionar ao dicionário de itens
                        questListItems[quest.id] = questItem;
                    }
                }
                
                // Atualizar visual da quest rastreada
                if (questListItems.ContainsKey(quest.id))
                {
                    bool isTracked = (trackedQuest == quest);
                    questListItems[quest.id].SetTracked(isTracked, trackedQuestColor);
                }
            }
            
            // Remover quests que não estão mais ativas
            foreach (string questId in existingQuestIds)
            {
                if (questListItems.TryGetValue(questId, out QuestListItem item))
                {
                    Destroy(item.gameObject);
                    questListItems.Remove(questId);
                }
            }
            
            // Atualizar a visibilidade do painel de quests se estiver vazio
            if (questManager.activeQuests.Count == 0 && activeQuestsPanel.activeSelf)
            {
                // Opcional: Mostrar mensagem de "nenhuma quest ativa"
                // ou fechar o painel automaticamente
            }
        }
        
        private IEnumerator AnimateQuestItem(Transform itemTransform)
        {
            Vector3 originalScale = itemTransform.localScale;
            float duration = 0.3f;
            float time = 0f;
            
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;
                float scale = questItemScaleAnimation.Evaluate(t);
                
                itemTransform.localScale = originalScale * new Vector3(1f, scale, 1f);
                
                yield return null;
            }
            
            itemTransform.localScale = originalScale;
        }
        
        public void ShowQuestDetails(global::Quest quest)
        {
            if (questDetailsPanel == null || quest == null)
                return;
                
            selectedQuest = quest;
            
            // Configurar textos
            if (questTitleText != null)
                questTitleText.text = quest.title;
                
            if (questDescriptionText != null)
            {
                string description = quest.description;
                
                // Adicionar detalhes baseados no tipo
                switch (quest.type)
                {
                    case QuestType.KillEnemies:
                        description += $"\n\nDerrote {quest.requiredAmount} inimigos.";
                        break;
                    case QuestType.CollectItems:
                        description += $"\n\nColete {quest.requiredAmount} itens.";
                        break;
                    case QuestType.ExploreArea:
                        description += $"\n\nExplore {quest.requiredAmount}% da área.";
                        break;
                    case QuestType.DefeatBoss:
                        description += $"\n\nDerrote o chefe.";
                        break;
                }
                
                // Adicionar informações de recompensa
                if (quest.rewards.Count > 0)
                {
                    description += "\n\n<color=yellow>Recompensas:</color>";
                    foreach (QuestReward reward in quest.rewards)
                    {
                        if (reward.gold > 0)
                            description += $"\n• {reward.gold} de ouro";
                        if (reward.experience > 0)
                            description += $"\n• {reward.experience} de experiência";
                        if (reward.item != null)
                            description += $"\n• Item: {reward.item.name}";
                    }
                }
                
                questDescriptionText.text = description;
            }
                
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
            
            // Configurar botão de rastreamento
            if (trackQuestButton != null)
            {
                bool isTracked = (trackedQuest == quest);
                trackQuestButton.GetComponentInChildren<TextMeshProUGUI>().text = isTracked ? "Não Rastrear" : "Rastrear";
            }
            
            // Mostrar painel
            questDetailsPanel.SetActive(true);
        }
        
        private void ToggleTrackSelectedQuest()
        {
            if (selectedQuest == null)
                return;
                
            if (trackedQuest == selectedQuest)
            {
                // Desativar rastreamento
                trackedQuest = null;
                if (trackQuestButton != null)
                    trackQuestButton.GetComponentInChildren<TextMeshProUGUI>().text = "Rastrear";
            }
            else
            {
                // Ativar rastreamento
                trackedQuest = selectedQuest;
                if (trackQuestButton != null)
                    trackQuestButton.GetComponentInChildren<TextMeshProUGUI>().text = "Não Rastrear";
            }
            
            // Notificar outros sistemas
            OnQuestTracked?.Invoke(trackedQuest);
            
            // Atualizar visualização na lista
            UpdateQuestList();
        }
        
        private void AbandonSelectedQuest()
        {
            if (questManager != null && selectedQuest != null)
            {
                // Mostrar diálogo de confirmação
                ShowConfirmationDialog("Tem certeza que deseja abandonar esta quest?", 
                    () => {
                        questManager.AbandonQuest(selectedQuest);
                        
                        // Se a quest abandonada era a rastreada, limpar o rastreamento
                        if (trackedQuest == selectedQuest)
                        {
                            trackedQuest = null;
                            OnQuestTracked?.Invoke(null);
                        }
                        
                        UpdateQuestList();
                        
                        if (questDetailsPanel != null)
                            questDetailsPanel.SetActive(false);
                            
                        selectedQuest = null;
                    });
            }
        }
        
        // Método para mostrar diálogo de confirmação
        private void ShowConfirmationDialog(string message, System.Action onConfirm)
        {
            // Implementação básica - pode ser substituída por sua própria lógica de UI
            if (UnityEngine.Application.isEditor || Debug.isDebugBuild)
            {
                // Em modo de debug, apenas log e confirmar
                Debug.Log($"[Confirmation Dialog] {message}");
                onConfirm?.Invoke();
            }
            else
            {
                // Em build final, mostrar diálogo real
                bool confirm = UnityEngine.Windows.Dialog.DisplayDialog("Confirmação", message, "Sim", "Não");
                if (confirm)
                {
                    onConfirm?.Invoke();
                }
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
                foreach (global::Quest quest in questGiver.availableQuests)
                {
                    // Verificar se a quest já está ativa ou completa
                    if (questManager.activeQuests.Contains(quest))
                    {
                        // A quest está ativa - verificar se pode ser entregue
                        if (quest.currentAmount >= quest.requiredAmount)
                        {
                            // Criar opção para entregar quest
                            GameObject completeOption = Instantiate(questOptionPrefab, questOptionsContainer);
                            QuestDialogOption completeBtn = completeOption.GetComponent<QuestDialogOption>();
                            
                            if (completeBtn != null)
                            {
                                // Configurar botão para completar quest
                                completeBtn.SetupCompleteButton(quest, questManager, this);
                            }
                        }
                        else
                        {
                            // Quest em andamento - mostrar progresso
                            GameObject progressOption = Instantiate(questOptionPrefab, questOptionsContainer);
                            QuestDialogOption progressBtn = progressOption.GetComponent<QuestDialogOption>();
                            
                            if (progressBtn != null)
                            {
                                // Configurar botão para mostrar informações de progresso
                                progressBtn.SetupProgressButton(quest, this);
                            }
                        }
                        
                        continue;
                    }
                    
                    if (questManager.completedQuests.Contains(quest))
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
        
        // Método para verificar se uma quest está sendo rastreada
        public bool IsQuestTracked(global::Quest quest)
        {
            return trackedQuest == quest;
        }
        
        // Método para obter a quest rastreada atualmente
        public global::Quest GetTrackedQuest()
        {
            return trackedQuest;
        }
    }
}