using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class QuestMarkerTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI distanceText;
    public Slider progressBar;
    
    [Header("Settings")]
    public Vector2 tooltipOffset = new Vector2(20, 20);
    public float showDelay = 0.5f;
    
    private Quest questInfo;
    private float pointerEnterTime;
    private bool isPointerOver = false;
    private RectTransform tooltipRect;
    private RectTransform canvasRect;
    private PlayerController player;
    
    private void Start()
    {
        // Inicialmente esconder o tooltip
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
            
        // Obter componentes
        tooltipRect = tooltipPanel?.GetComponent<RectTransform>();
        canvasRect = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        player = FindObjectOfType<PlayerController>();
    }
    
    private void Update()
    {
        // Mostrar tooltip após delay quando mouse está sobre o marcador
        if (isPointerOver && !tooltipPanel.activeSelf)
        {
            if (Time.time - pointerEnterTime >= showDelay)
            {
                ShowTooltip();
            }
        }
        
        // Atualizar posição do tooltip para seguir o mouse
        if (tooltipPanel.activeSelf)
        {
            PositionTooltip();
            
            // Atualizar distância para o objetivo se tiver um jogador e a quest tiver posição
            if (player != null && questInfo != null && questInfo.locationPosition != Vector3.zero)
            {
                UpdateDistanceText();
            }
        }
    }
    
    public void SetQuestInfo(Quest quest)
    {
        questInfo = quest;
    }
    
    private void ShowTooltip()
    {
        if (tooltipPanel != null && questInfo != null)
        {
            // Definir textos
            if (titleText != null)
                titleText.text = questInfo.title;
                
            if (descriptionText != null)
                descriptionText.text = questInfo.description;
                
            if (progressText != null)
                progressText.text = questInfo.GetProgressText();
                
            // Atualizar barra de progresso
            if (progressBar != null)
            {
                progressBar.value = questInfo.GetTotalProgress();
            }
            
            // Atualizar distância
            UpdateDistanceText();
            
            // Mostrar o painel
            tooltipPanel.SetActive(true);
            
            // Posicionar tooltip
            PositionTooltip();
        }
    }
    
    private void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
    
    private void PositionTooltip()
    {
        if (tooltipRect == null || canvasRect == null)
            return;
            
        // Obter posição do mouse
        Vector2 mousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, Input.mousePosition, null, out mousePos);
            
        // Adicionar offset
        Vector2 tooltipPos = mousePos + tooltipOffset;
        
        // Verificar limites da tela para evitar que o tooltip saia da canvas
        Vector2 tooltipSize = tooltipRect.sizeDelta;
        Vector2 canvasSize = canvasRect.sizeDelta;
        
        // Ajustar posição X se necessário
        if (tooltipPos.x + tooltipSize.x > canvasSize.x / 2)
        {
            tooltipPos.x = mousePos.x - tooltipSize.x - tooltipOffset.x;
        }
        
        // Ajustar posição Y se necessário
        if (tooltipPos.y - tooltipSize.y < -canvasSize.y / 2)
        {
            tooltipPos.y = mousePos.y + tooltipSize.y;
        }
        
        // Aplicar posição
        tooltipRect.anchoredPosition = tooltipPos;
    }
    
    private void UpdateDistanceText()
    {
        if (distanceText == null || player == null || questInfo == null || questInfo.locationPosition == Vector3.zero)
            return;
            
        float distance = Vector3.Distance(player.transform.position, questInfo.locationPosition);
        
        // Formatar distância
        string distanceString;
        if (distance < 10)
            distanceString = distance.ToString("F1") + "m"; // Com 1 casa decimal se menos de 10m
        else
            distanceString = Mathf.RoundToInt(distance) + "m"; // Arredondado se mais de 10m
            
        distanceText.text = "Distância: " + distanceString;
    }
    
    // Implementação das interfaces de eventos
    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        pointerEnterTime = Time.time;
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        HideTooltip();
    }
}