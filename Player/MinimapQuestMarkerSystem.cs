using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinimapQuestMarkerSystem : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject questMarkerPrefab;
    public GameObject objectiveMarkerPrefab;
    
    [Header("Container")]
    public Transform markersContainer;
    public RectTransform minimapRect;
    
    [Header("Marker Settings")]
    public float markerScale = 1.0f;
    public Color defaultQuestColor = Color.yellow;
    public Color trackedQuestColor = Color.green;
    public Color completedQuestColor = Color.grey;
    public Color objectiveColor = Color.cyan;
    
    [Header("Icons")]
    public Sprite questIcon;
    public Sprite killIcon;
    public Sprite collectIcon;
    public Sprite exploreIcon;
    public Sprite bossIcon;
    public Sprite npcIcon;
    public Sprite escortIcon;
    public Sprite defendIcon;
    
    [Header("Pulse Animation")]
    public bool animateTrackedQuest = true;
    public float pulseSpeed = 1.5f;
    public float pulseMinScale = 0.8f;
    public float pulseMaxScale = 1.2f;
    
    // Referências
    private QuestManager questManager;
    private Camera minimapCamera;
    private Transform playerTransform;
    
    // Controle de marcadores
    private Dictionary<string, GameObject> questMarkers = new Dictionary<string, GameObject>();
    private Dictionary<string, List<GameObject>> objectiveMarkers = new Dictionary<string, List<GameObject>>();
    
    // Quest atualmente rastreada
    private Quest trackedQuest;
    
    // Para animação de pulse
    private float pulseTimer = 0f;
    
    private void Start()
    {
        questManager = FindObjectOfType<QuestManager>();
        minimapCamera = GameObject.FindGameObjectWithTag("MinimapCamera")?.GetComponent<Camera>();
        playerTransform = FindObjectOfType<PlayerController>()?.transform;
        
        if (questManager == null)
            Debug.LogError("MinimapQuestMarkerSystem: QuestManager não encontrado!");
            
        if (minimapCamera == null)
            Debug.LogError("MinimapQuestMarkerSystem: Câmera do minimapa não encontrada!");
            
        if (playerTransform == null)
            Debug.LogError("MinimapQuestMarkerSystem: Transform do jogador não encontrado!");
            
        // Subscrever para eventos do QuestManager
        if (questManager != null)
        {
            questManager.OnQuestAccepted += OnQuestAccepted;
            questManager.OnQuestCompleted += OnQuestCompleted;
            questManager.OnQuestAbandoned += OnQuestAbandoned;
        }
        
        // Subscrever para evento de rastreamento de quest
        QuestUI questUI = FindObjectOfType<QuestUI>();
        if (questUI != null)
        {
            questUI.OnQuestTracked += OnQuestTracked;
        }
        
        // Inicializar marcadores para quests existentes
        InitializeExistingQuestMarkers();
    }
    
    private void OnDestroy()
    {
        // Desinscrever de eventos
        if (questManager != null)
        {
            questManager.OnQuestAccepted -= OnQuestAccepted;
            questManager.OnQuestCompleted -= OnQuestCompleted;
            questManager.OnQuestAbandoned -= OnQuestAbandoned;
        }
        
        QuestUI questUI = FindObjectOfType<QuestUI>();
        if (questUI != null)
        {
            questUI.OnQuestTracked -= OnQuestTracked;
        }
    }
    
    private void Update()
    {
        // Atualizar posição de todos os marcadores
        UpdateMarkerPositions();
        
        // Atualizar animação de pulse
        if (animateTrackedQuest && trackedQuest != null)
        {
            UpdatePulseAnimation();
        }
    }
    
    private void UpdatePulseAnimation()
    {
        pulseTimer += Time.deltaTime * pulseSpeed;
        
        // Calcular escala de pulse usando uma função seno
        float pulseScale = Mathf.Lerp(pulseMinScale, pulseMaxScale, (Mathf.Sin(pulseTimer) + 1f) * 0.5f);
        
        // Aplicar escala ao marcador da quest rastreada
        if (questMarkers.ContainsKey(trackedQuest.id))
        {
            RectTransform rect = questMarkers[trackedQuest.id].GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one * pulseScale;
            }
        }
    }
    
    private void InitializeExistingQuestMarkers()
    {
        // Criar marcadores para quests já ativas
        foreach (Quest quest in questManager.activeQuests)
        {
            CreateQuestMarker(quest);
        }
    }
    
    private void OnQuestAccepted(Quest quest)
    {
        CreateQuestMarker(quest);
    }
    
    private void OnQuestCompleted(Quest quest)
    {
        // Atualizar cor do marcador para indicar conclusão
        if (questMarkers.ContainsKey(quest.id))
        {
            Image markerImage = questMarkers[quest.id].GetComponent<Image>();
            if (markerImage != null)
            {
                markerImage.color = completedQuestColor;
            }
            
            // Opcional: Remover marcador após alguns segundos
            Destroy(questMarkers[quest.id], 5f);
            questMarkers.Remove(quest.id);
        }
        
        // Remover marcadores de objetivos
        RemoveObjectiveMarkers(quest);
    }
    
    private void OnQuestAbandoned(Quest quest)
    {
        // Remover marcador da quest abandonada
        if (questMarkers.ContainsKey(quest.id))
        {
            Destroy(questMarkers[quest.id]);
            questMarkers.Remove(quest.id);
        }
        
        // Remover marcadores de objetivos
        RemoveObjectiveMarkers(quest);
        
        // Se a quest rastreada foi abandonada, limpar referência
        if (trackedQuest == quest)
        {
            trackedQuest = null;
        }
    }
    
    private void OnQuestTracked(Quest quest)
    {
        // Atualizar referência da quest rastreada
        trackedQuest = quest;
        
        // Resetar cronômetro de pulse
        pulseTimer = 0f;
        
        // Atualizar aparência de todos os marcadores
        foreach (var pair in questMarkers)
        {
            string questId = pair.Key;
            GameObject marker = pair.Value;
            
            // Encontrar a quest correspondente
            Quest markerQuest = questManager.GetQuestByID(questId);
            if (markerQuest != null)
            {
                UpdateMarkerAppearance(marker, markerQuest);
            }
        }
        
        // Se uma quest está sendo rastreada, criar marcadores para seus objetivos
        if (quest != null)
        {
            CreateObjectiveMarkers(quest);
        }
        else
        {
            // Se nenhuma quest está sendo rastreada, remover todos os marcadores de objetivos
            RemoveAllObjectiveMarkers();
        }
    }
    
    private void CreateQuestMarker(Quest quest)
    {
        if (questMarkerPrefab == null || markersContainer == null || quest == null)
            return;
            
        // Verificar se já existe um marcador para esta quest
        if (questMarkers.ContainsKey(quest.id))
            return;
            
        // Criar marcador
        GameObject marker = Instantiate(questMarkerPrefab, markersContainer);
        questMarkers.Add(quest.id, marker);
        
        // Configurar aparência do marcador
        UpdateMarkerAppearance(marker, quest);
        
        // Se é a primeira quest aceita e nenhuma está sendo rastreada, rastrear esta automaticamente
        if (questManager.activeQuests.Count == 1 && trackedQuest == null)
        {
            OnQuestTracked(quest);
        }
    }
    
    private void UpdateMarkerAppearance(GameObject marker, Quest quest)
    {
        // Configurar cor e ícone baseado no tipo e status
        Image markerImage = marker.GetComponent<Image>();
        if (markerImage != null)
        {
            // Definir cor
            if (quest == trackedQuest)
                markerImage.color = trackedQuestColor;
            else if (quest.isCompleted)
                markerImage.color = completedQuestColor;
            else
                markerImage.color = quest.questColor != Color.white ? quest.questColor : defaultQuestColor;
                
            // Definir ícone baseado no tipo
            if (quest.questIcon != null)
            {
                markerImage.sprite = quest.questIcon;
            }
            else
            {
                // Usar ícone padrão baseado no tipo
                switch (quest.type)
                {
                    case QuestType.KillEnemies:
                        markerImage.sprite = killIcon;
                        break;
                    case QuestType.CollectItems:
                        markerImage.sprite = collectIcon;
                        break;
                    case QuestType.ExploreArea:
                        markerImage.sprite = exploreIcon;
                        break;
                    case QuestType.DefeatBoss:
                        markerImage.sprite = bossIcon;
                        break;
                    case QuestType.TalkToNPC:
                        markerImage.sprite = npcIcon;
                        break;
                    case QuestType.EscortNPC:
                        markerImage.sprite = escortIcon;
                        break;
                    case QuestType.DefendLocation:
                        markerImage.sprite = defendIcon;
                        break;
                    default:
                        markerImage.sprite = questIcon;
                        break;
                }
            }
        }
        
        // Configurar tooltip (se houver)
        QuestMarkerTooltip tooltip = marker.GetComponent<QuestMarkerTooltip>();
        if (tooltip != null)
        {
            tooltip.SetQuestInfo(quest);
        }
        
        // Configurar tamanho
        RectTransform rect = marker.GetComponent<RectTransform>();
        if (rect != null)
        {
            // Ajustar escala baseado na prioridade
            float scale = markerScale;
            switch (quest.priority)
            {
                case QuestPriority.Low:
                    scale *= 0.8f;
                    break;
                case QuestPriority.High:
                    scale *= 1.2f;
                    break;
                case QuestPriority.Critical:
                    scale *= 1.5f;
                    break;
            }
            
            rect.sizeDelta = new Vector2(30 * scale, 30 * scale);
            
            // Resetar escala local para quest rastreada (será animada pelo pulse)
            if (quest == trackedQuest)
            {
                rect.localScale = Vector3.one;
            }
        }
    }
    
    private void CreateObjectiveMarkers(Quest quest)
    {
        // Remover marcadores de objetivos antigos
        RemoveAllObjectiveMarkers();
        
        // Não criar novos marcadores se a quest não tem objetivos ou não tem posição
        if (quest.objectives.Count == 0 || quest.locationPosition == Vector3.zero)
            return;
            
        // Lista para guardar os novos marcadores
        List<GameObject> markers = new List<GameObject>();
        
        // Se a quest tem uma posição global, adicionar marcador principal
        if (quest.locationPosition != Vector3.zero)
        {
            GameObject mainMarker = Instantiate(objectiveMarkerPrefab, markersContainer);
            
            // Configurar aparência
            Image markerImage = mainMarker.GetComponent<Image>();
            if (markerImage != null)
            {
                markerImage.color = objectiveColor;
                
                // Usar ícone baseado no tipo
                switch (quest.type)
                {
                    case QuestType.KillEnemies:
                        markerImage.sprite = killIcon;
                        break;
                    case QuestType.CollectItems:
                        markerImage.sprite = collectIcon;
                        break;
                    // ... outros tipos
                    default:
                        markerImage.sprite = questIcon;
                        break;
                }
            }
            
            markers.Add(mainMarker);
        }
        
        // Adicionar à lista de marcadores de objetivos
        objectiveMarkers[quest.id] = markers;
    }
    
    private void RemoveObjectiveMarkers(Quest quest)
    {
        if (objectiveMarkers.ContainsKey(quest.id))
        {
            foreach (GameObject marker in objectiveMarkers[quest.id])
            {
                Destroy(marker);
            }
            
            objectiveMarkers.Remove(quest.id);
        }
    }
    
    private void RemoveAllObjectiveMarkers()
    {
        foreach (var pair in objectiveMarkers)
        {
            foreach (GameObject marker in pair.Value)
            {
                Destroy(marker);
            }
        }
        
        objectiveMarkers.Clear();
    }
    
    private void UpdateMarkerPositions()
    {
        if (minimapCamera == null || minimapRect == null)
            return;
            
        // Atualizar posição dos marcadores de quests
        foreach (var pair in questMarkers)
        {
            string questId = pair.Key;
            GameObject marker = pair.Value;
            
            Quest quest = questManager.GetQuestByID(questId);
            if (quest != null && quest.locationPosition != Vector3.zero)
            {
                UpdateMarkerPosition(marker, quest.locationPosition);
            }
        }
        
        // Atualizar posição dos marcadores de objetivos
        foreach (var pair in objectiveMarkers)
        {
            string questId = pair.Key;
            List<GameObject> markers = pair.Value;
            
            Quest quest = questManager.GetQuestByID(questId);
            if (quest != null)
            {
                // Atualizar marcador principal
                if (markers.Count > 0 && quest.locationPosition != Vector3.zero)
                {
                    UpdateMarkerPosition(markers[0], quest.locationPosition);
                }
                
                // Atualizar marcadores de objetivos específicos
                // (implementar quando tiver objetivos com posições individuais)
            }
        }
    }
    
    private void UpdateMarkerPosition(GameObject marker, Vector3 worldPosition)
    {
        if (marker == null || minimapCamera == null)
            return;
            
        // Converter posição do mundo para viewport
        Vector3 viewportPoint = minimapCamera.WorldToViewportPoint(worldPosition);
        
        // Verificar se está no campo de visão da câmera
        bool isVisible = (viewportPoint.z > 0 && 
                          viewportPoint.x > 0 && viewportPoint.x < 1 && 
                          viewportPoint.y > 0 && viewportPoint.y < 1);
        
        if (isVisible)
        {
            // Converter viewport para coordenadas do minimapa
            Vector2 minimapPoint = new Vector2(
                (viewportPoint.x * minimapRect.sizeDelta.x) - (minimapRect.sizeDelta.x * 0.5f),
                (viewportPoint.y * minimapRect.sizeDelta.y) - (minimapRect.sizeDelta.y * 0.5f)
            );
            
            // Aplicar posição ao marcador
            RectTransform markerRect = marker.GetComponent<RectTransform>();
            markerRect.anchoredPosition = minimapPoint;
            
            // Mostrar marcador
            marker.SetActive(true);
        }
        else
        {
            // Calcular posição na borda do minimapa
            Vector2 minimapCenter = Vector2.zero;
            Vector2 directionFromCenter = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f).normalized;
            
            // Tamanho efetivo do minimapa (considerando um buffer para manter o marcador visível)
            float bufferSize = 20f; // Pixels de buffer da borda
            Vector2 effectiveSize = minimapRect.sizeDelta * 0.5f - new Vector2(bufferSize, bufferSize);
            
            // Calcular posição na borda
            Vector2 edgePosition = minimapCenter + directionFromCenter * effectiveSize;
            
            // Aplicar posição ao marcador
            RectTransform markerRect = marker.GetComponent<RectTransform>();
            markerRect.anchoredPosition = edgePosition;
            
            // Mostrar marcador
            marker.SetActive(true);
            
            // Opcional: rotacionar marcador para apontar na direção correta
            float angle = Mathf.Atan2(directionFromCenter.y, directionFromCenter.x) * Mathf.Rad2Deg - 90f;
            markerRect.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    // Método para uso externo - obter a quest rastreada atual
    public Quest GetTrackedQuest()
    {
        return trackedQuest;
    }
    
    // Método para uso externo - rastrear uma quest específica
    public void TrackQuest(Quest quest)
    {
        OnQuestTracked(quest);
    }
    
    // Método para uso externo - rastrear uma quest pelo ID
    public void TrackQuestById(string questId)
    {
        Quest quest = questManager.GetQuestByID(questId);
        if (quest != null)
        {
            OnQuestTracked(quest);
        }
    }
}