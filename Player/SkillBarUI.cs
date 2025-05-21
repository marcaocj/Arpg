using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillBarUI : MonoBehaviour
{
    [Header("Skill Slots")]
    public GameObject[] skillSlots; // Array de GameObjects dos slots (4 slots)
    public Image[] skillIcons;      // Ícones das skills
    public Image[] cooldownOverlays; // Overlays de cooldown (circulares)
    public TextMeshProUGUI[] hotkeyTexts; // Textos das teclas (1, 2, 3, 4)
    public TextMeshProUGUI[] cooldownTexts; // Textos dos cooldowns
    
    [Header("Skill Info Panel")]
    public GameObject skillInfoPanel;
    public TextMeshProUGUI skillNameText;
    public TextMeshProUGUI skillDescriptionText;
    public TextMeshProUGUI skillStatsText;
    public Image skillInfoIcon;
    
    [Header("Current Selection")]
    public Image selectionIndicator; // Indicador da skill selecionada
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;
    public Color unavailableColor = Color.gray;
    
    [Header("Visual Effects")]
    public AnimationCurve iconScaleOnUse = AnimationCurve.EaseInOut(0, 1, 0.2f, 1.2f);
    public float scaleAnimationDuration = 0.2f;
    
    private PlayerController player;
    private PlayerStats playerStats;
    private float[] skillCooldownTimers;
    private bool[] isAnimatingSkill;
    
    // Cache para evitar chamadas desnecessárias
    private int lastCurrentSkillIndex = -1;
    private int lastPlayerMana = -1;
    private bool needsUpdate = true;
    
    private void Start()
    {
        // Encontrar o player
        player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            playerStats = player.GetStats();
            skillCooldownTimers = new float[player.skills.Count];
            isAnimatingSkill = new bool[player.skills.Count];
        }
        else
        {
            Debug.LogError("SkillBarUI: PlayerController não encontrado!");
            return;
        }
        
        // Configurar hotkeys
        SetupHotkeys();
        
        // Configurar eventos dos botões
        SetupButtonEvents();
        
        // Atualizar UI inicial
        RefreshSkillBar();
        
        // Esconder info panel inicial
        if (skillInfoPanel != null)
            skillInfoPanel.SetActive(false);
            
        Debug.Log("SkillBarUI inicializado com sucesso!");
    }
    
    private void Update()
    {
        if (player == null || playerStats == null) return;
        
        // Verificar se precisa atualizar
        CheckForUpdates();
        
        // Atualizar cooldowns
        UpdateCooldowns();
        
        // Atualizar seleção se mudou
        if (player.currentSkillIndex != lastCurrentSkillIndex)
        {
            UpdateSelection();
            lastCurrentSkillIndex = player.currentSkillIndex;
        }
        
        // Verificar hover para tooltip
        CheckSkillHover();
    }
    
    private void CheckForUpdates()
    {
        // Verificar se mana mudou (para atualizar cores dos ícones)
        if (playerStats.Mana != lastPlayerMana)
        {
            UpdateSkillAvailability();
            lastPlayerMana = playerStats.Mana;
        }
    }
    
    private void SetupHotkeys()
    {
        string[] hotkeys = { "1", "2", "3", "4" };
        
        for (int i = 0; i < hotkeyTexts.Length && i < hotkeys.Length; i++)
        {
            if (hotkeyTexts[i] != null)
            {
                hotkeyTexts[i].text = hotkeys[i];
            }
        }
    }
    
    private void SetupButtonEvents()
    {
        // Adicionar eventos de clique nos slots para seleção direta
        for (int i = 0; i < skillSlots.Length; i++)
        {
            int skillIndex = i; // Capturar o valor para o closure
            Button slotButton = skillSlots[i]?.GetComponent<Button>();
            
            if (slotButton != null)
            {
                slotButton.onClick.AddListener(() => SelectSkill(skillIndex));
            }
        }
    }
    
    private void SelectSkill(int skillIndex)
    {
        if (skillIndex >= 0 && skillIndex < player.skills.Count)
        {
            player.currentSkillIndex = skillIndex;
            Debug.Log($"Skill selecionada via UI: {player.skills[skillIndex].name}");
        }
    }
    
    public void RefreshSkillBar()
    {
        if (player == null || player.skills == null) return;
        
        // Redimensionar arrays se necessário
        if (skillCooldownTimers.Length != player.skills.Count)
        {
            skillCooldownTimers = new float[player.skills.Count];
            isAnimatingSkill = new bool[player.skills.Count];
        }
        
        // Atualizar cada slot
        for (int i = 0; i < skillSlots.Length; i++)
        {
            if (skillSlots[i] != null)
            {
                // Mostrar/esconder slot baseado se há skill
                bool hasSkill = i < player.skills.Count;
                skillSlots[i].SetActive(hasSkill);
                
                if (hasSkill)
                {
                    Skill skill = player.skills[i];
                    
                    // Atualizar ícone
                    if (skillIcons[i] != null)
                    {
                        skillIcons[i].color = GetSkillColor(skill.type);
                        // TODO: Definir sprite se disponível
                        // skillIcons[i].sprite = skill.iconSprite;
                    }
                    
                    // Configurar overlay de cooldown
                    if (cooldownOverlays[i] != null)
                    {
                        cooldownOverlays[i].fillMethod = Image.FillMethod.Radial360;
                        cooldownOverlays[i].fillOrigin = 0; // Top
                        cooldownOverlays[i].gameObject.SetActive(false);
                    }
                }
            }
        }
        
        // Atualizar seleção e disponibilidade
        UpdateSelection();
        UpdateSkillAvailability();
        
        needsUpdate = false;
    }
    
    private Color GetSkillColor(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Physical:
                return new Color(0.9f, 0.9f, 0.9f); // Branco/Cinza claro
            case SkillType.Fire:
                return new Color(1f, 0.4f, 0.2f); // Vermelho alaranjado
            case SkillType.Ice:
                return new Color(0.4f, 0.8f, 1f); // Azul claro
            case SkillType.Lightning:
                return new Color(1f, 1f, 0.4f); // Amarelo
            case SkillType.Poison:
                return new Color(0.6f, 1f, 0.4f); // Verde
            default:
                return Color.white;
        }
    }
    
    private void UpdateSkillAvailability()
    {
        for (int i = 0; i < player.skills.Count && i < skillIcons.Length; i++)
        {
            if (skillIcons[i] != null)
            {
                Skill skill = player.skills[i];
                
                // Verificar se tem métodos avançados
                bool hasAdvancedMethods = skill.GetType().GetMethod("CanUse") != null;
                bool canUse = true;
                
                if (hasAdvancedMethods)
                {
                    canUse = skill.CanUse(playerStats);
                }
                else
                {
                    // Sistema antigo - verificar mana
                    canUse = playerStats.Mana >= skill.baseManaoCost;
                }
                
                // Ajustar cor baseado na disponibilidade
                Color baseColor = GetSkillColor(skill.type);
                if (!canUse)
                {
                    skillIcons[i].color = Color.Lerp(baseColor, unavailableColor, 0.7f);
                }
                else
                {
                    skillIcons[i].color = baseColor;
                }
            }
        }
    }
    
    private void UpdateCooldowns()
    {
        for (int i = 0; i < player.skills.Count && i < skillCooldownTimers.Length; i++)
        {
            // Atualizar timer local
            if (skillCooldownTimers[i] > 0)
            {
                skillCooldownTimers[i] -= Time.deltaTime;
                skillCooldownTimers[i] = Mathf.Max(0, skillCooldownTimers[i]);
            }
            
            // Atualizar overlay de cooldown se existe
            if (i < cooldownOverlays.Length && cooldownOverlays[i] != null)
            {
                float cooldownPercent = 0f;
                
                if (skillCooldownTimers[i] > 0)
                {
                    Skill skill = player.skills[i];
                    float maxCooldown;
                    
                    // Verificar se tem métodos avançados
                    bool hasAdvancedMethods = skill.GetType().GetMethod("GetActualCooldown") != null;
                    
                    if (hasAdvancedMethods)
                    {
                        maxCooldown = skill.GetActualCooldown(playerStats);
                    }
                    else
                    {
                        maxCooldown = skill.baseCooldown;
                    }
                    
                    cooldownPercent = skillCooldownTimers[i] / maxCooldown;
                }
                
                cooldownOverlays[i].fillAmount = cooldownPercent;
                cooldownOverlays[i].gameObject.SetActive(cooldownPercent > 0);
            }
            
            // Atualizar texto de cooldown se existe
            if (i < cooldownTexts.Length && cooldownTexts[i] != null)
            {
                if (skillCooldownTimers[i] > 0)
                {
                    cooldownTexts[i].text = skillCooldownTimers[i].ToString("F1");
                    cooldownTexts[i].gameObject.SetActive(true);
                }
                else
                {
                    cooldownTexts[i].gameObject.SetActive(false);
                }
            }
        }
    }
    
    private void UpdateSelection()
    {
        // Mover indicador para a skill selecionada
        if (selectionIndicator != null && player.currentSkillIndex < skillSlots.Length && skillSlots[player.currentSkillIndex] != null)
        {
            selectionIndicator.transform.position = skillSlots[player.currentSkillIndex].transform.position;
            selectionIndicator.gameObject.SetActive(true);
        }
        else if (selectionIndicator != null)
        {
            selectionIndicator.gameObject.SetActive(false);
        }
        
        // Atualizar bordas/cores dos slots (opcional)
        for (int i = 0; i < skillIcons.Length && i < player.skills.Count; i++)
        {
            if (skillIcons[i] != null)
            {
                bool isSelected = (i == player.currentSkillIndex);
                
                // Aplicar efeito de seleção se necessário
                if (isSelected)
                {
                    // Pode adicionar brilho, borda, etc.
                    // Por enquanto, o selectionIndicator já mostra a seleção
                }
            }
        }
    }
    
    private void CheckSkillHover()
    {
        // Detectar mouse sobre skill slots para mostrar tooltip
        Vector2 mousePos = Input.mousePosition;
        bool showingTooltip = false;
        
        for (int i = 0; i < skillSlots.Length && i < player.skills.Count; i++)
        {
            if (skillSlots[i] != null)
            {
                RectTransform rectTransform = skillSlots[i].GetComponent<RectTransform>();
                if (rectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePos))
                {
                    ShowSkillInfo(player.skills[i]);
                    showingTooltip = true;
                    break;
                }
            }
        }
        
        // Se não está sobre nenhum slot, esconder info
        if (!showingTooltip)
        {
            HideSkillInfo();
        }
    }
    
    private void ShowSkillInfo(Skill skill)
    {
        if (skillInfoPanel == null) return;
        
        skillInfoPanel.SetActive(true);
        
        // Atualizar nome
        if (skillNameText != null)
            skillNameText.text = skill.name;
        
        // Atualizar descrição
        if (skillDescriptionText != null)
        {
            // Verificar se tem método avançado
            bool hasAdvancedMethods = skill.GetType().GetMethod("GetDetailedDescription") != null;
            
            if (hasAdvancedMethods)
            {
                skillDescriptionText.text = skill.GetDetailedDescription(playerStats);
            }
            else
            {
                skillDescriptionText.text = $"Skill básica do tipo {skill.type}";
            }
        }
        
        // Atualizar stats
        if (skillStatsText != null)
        {
            string stats = "<color=yellow>Estatísticas:</color>\n";
            
            // Verificar se tem métodos avançados
            bool hasAdvancedMethods = skill.GetType().GetMethod("GetActualDamage") != null;
            
            if (hasAdvancedMethods)
            {
                stats += $"Dano: {skill.GetActualDamage(playerStats, player.inventory?.equippedWeapon)}\n";
                stats += $"Custo de Mana: {skill.GetActualManaCost(playerStats)}\n";
                stats += $"Cooldown: {skill.GetActualCooldown(playerStats):F1}s\n";
                stats += $"Alcance: {skill.GetActualRange(playerStats):F1}m";
                
                if (skill.areaRadius > 0)
                {
                    stats += $"\nÁrea: {skill.GetActualAreaRadius(playerStats):F1}m";
                }
            }
            else
            {
                // Sistema antigo
                stats += $"Dano Base: {skill.baseDamage}\n";
                stats += $"Custo de Mana: {skill.baseManaoCost}\n";
                stats += $"Cooldown: {skill.baseCooldown:F1}s\n";
                float useRange = skill.range > 0 ? skill.range : player.attackRange;
                stats += $"Alcance: {useRange:F1}m";
            }
            
            skillStatsText.text = stats;
        }
        
        // Atualizar ícone
        if (skillInfoIcon != null)
        {
            skillInfoIcon.color = GetSkillColor(skill.type);
        }
        
        // Posicionar tooltip próximo ao mouse
        PositionTooltip();
    }
    
    private void PositionTooltip()
    {
        if (skillInfoPanel == null) return;
        
        Vector2 mousePos = Input.mousePosition;
        RectTransform panelRect = skillInfoPanel.GetComponent<RectTransform>();
        RectTransform canvasRect = transform.root.GetComponent<RectTransform>();
        
        if (panelRect != null && canvasRect != null)
        {
            Vector2 localMousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, null, out localMousePos);
            
            // Offset para não sobrepor o mouse
            Vector2 offset = new Vector2(10, -10);
            Vector2 tooltipPos = localMousePos + offset;
            
            // Verificar limites da tela
            Vector2 panelSize = panelRect.sizeDelta;
            Vector2 canvasSize = canvasRect.sizeDelta;
            
            // Ajustar X se sair da tela
            if (tooltipPos.x + panelSize.x > canvasSize.x / 2)
            {
                tooltipPos.x = localMousePos.x - panelSize.x - 10;
            }
            
            // Ajustar Y se sair da tela
            if (tooltipPos.y - panelSize.y < -canvasSize.y / 2)
            {
                tooltipPos.y = localMousePos.y + panelSize.y + 10;
            }
            
            panelRect.anchoredPosition = tooltipPos;
        }
    }
    
    private void HideSkillInfo()
    {
        if (skillInfoPanel != null)
            skillInfoPanel.SetActive(false);
    }
    
    // MÉTODO PÚBLICO CHAMADO PELO PLAYERCONTROLLER
    public void OnSkillUsed(int skillIndex)
    {
        if (skillIndex < skillCooldownTimers.Length && skillIndex < player.skills.Count)
        {
            Skill skill = player.skills[skillIndex];
            
            // Definir cooldown
            bool hasAdvancedMethods = skill.GetType().GetMethod("GetActualCooldown") != null;
            
            if (hasAdvancedMethods)
            {
                skillCooldownTimers[skillIndex] = skill.GetActualCooldown(playerStats);
            }
            else
            {
                skillCooldownTimers[skillIndex] = skill.baseCooldown;
            }
            
            // Animar o ícone
            StartCoroutine(AnimateSkillUse(skillIndex));
        }
        
        // Atualizar disponibilidade imediatamente
        UpdateSkillAvailability();
    }
    
    private System.Collections.IEnumerator AnimateSkillUse(int skillIndex)
    {
        if (skillIndex >= skillIcons.Length || skillIcons[skillIndex] == null || isAnimatingSkill[skillIndex])
            yield break;
            
        isAnimatingSkill[skillIndex] = true;
        
        Transform iconTransform = skillIcons[skillIndex].transform;
        Vector3 originalScale = iconTransform.localScale;
        
        // Animação de scale up e down
        float elapsed = 0f;
        while (elapsed < scaleAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / scaleAnimationDuration;
            float scaleMultiplier = iconScaleOnUse.Evaluate(progress);
            
            iconTransform.localScale = originalScale * scaleMultiplier;
            yield return null;
        }
        
        // Garantir que volta ao tamanho original
        iconTransform.localScale = originalScale;
        isAnimatingSkill[skillIndex] = false;
    }
    
    // MÉTODO PARA DEBUG
    public void DebugSkillBar()
    {
        Debug.Log("=== SKILL BAR DEBUG ===");
        Debug.Log($"Player: {(player != null ? "OK" : "NULL")}");
        Debug.Log($"Skills Count: {(player != null ? player.skills.Count : 0)}");
        Debug.Log($"Cooldown Timers Length: {skillCooldownTimers.Length}");
        
        for (int i = 0; i < skillSlots.Length; i++)
        {
            Debug.Log($"Slot {i}: {(skillSlots[i] != null ? "OK" : "NULL")}");
        }
    }
}