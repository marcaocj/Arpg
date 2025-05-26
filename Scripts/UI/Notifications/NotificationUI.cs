/*using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using RPG.Core;
using RPG.Combat.Effects;
using RPG.Inventory;
using RPG.Combat;
using RPG.Player;
using RPG.Player.Combat;
using RPG.Player.Stats;
using RPG.Inventory.UI;
using RPG.Player.Inventory;
using RPG.UI; // Para UIManager
using RPG.Quest;
using RPG.UI.Quest;

namespace RPG.UI
{
public class NotificationUI : MonoBehaviour
{
    [Header("Notification Panel")]
    public GameObject notificationPanel;
    public TextMeshProUGUI experienceText;
    public TextMeshProUGUI goldText;
    public Image backgroundImage;

    [Header("Animation Settings")]
    public float displayDuration = 3f;
    public float fadeInTime = 0.3f;
    public float fadeOutTime = 0.5f;
    public AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve slideOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Colors")]
    public Color experienceColor = Color.yellow;
    public Color goldColor = new Color(1f, 0.84f, 0f); // Dourado
    public Color backgroundNormalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color backgroundHighlightColor = new Color(0.3f, 0.1f, 0.1f, 0.9f);

    [Header("Position Settings")]
    public Vector2 hiddenPosition = new Vector2(-300f, 0f);
    public Vector2 visiblePosition = new Vector2(10f, 0f);

    private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
    private bool isShowingNotification = false;
    private Coroutine currentNotificationCoroutine;
    private RectTransform panelRectTransform;
    private CanvasGroup panelCanvasGroup;

    private int accumulatedExperience = 0;
    private int accumulatedGold = 0;
    private float accumulationTime = 1f;
    private Coroutine accumulationCoroutine;

    [System.Serializable]
    private class NotificationData
    {
        public int experience;
        public int gold;
        public float timestamp;

        public NotificationData(int exp, int goldAmount)
        {
            experience = exp;
            gold = goldAmount;
            timestamp = Time.time;
        }
    }

    private void Awake()
    {
        InitializeComponents();
        SubscribeToEvents();
        InitializeUI();
    }

    private void InitializeComponents()
    {
        if (notificationPanel == null)
        {
            Debug.LogError("NotificationUI: notificationPanel não configurado!");
            return;
        }

        panelRectTransform = notificationPanel.GetComponent<RectTransform>();
        panelCanvasGroup = notificationPanel.GetComponent<CanvasGroup>();

        if (panelCanvasGroup == null)
            panelCanvasGroup = notificationPanel.AddComponent<CanvasGroup>();

        if (backgroundImage == null)
            backgroundImage = notificationPanel.GetComponent<Image>();
    }

    private void SubscribeToEvents()
    {
        EventManager.Subscribe<PlayerExperienceGainedEvent>(OnExperienceGained);
        EventManager.Subscribe<GoldCollectedEvent>(OnGoldCollected);
    }

    private void InitializeUI()
    {
        if (panelRectTransform != null)
            panelRectTransform.anchoredPosition = hiddenPosition;

        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 0f;

        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }

    private void OnExperienceGained(PlayerExperienceGainedEvent eventData)
    {
        AddExperience(eventData.experienceGained);
    }

    private void OnGoldCollected(GoldCollectedEvent eventData)
    {
        AddGold(eventData.amount);
    }

    public void AddExperience(int amount)
    {
        accumulatedExperience += amount;
        StartAccumulation();
    }

    public void AddGold(int amount)
    {
        accumulatedGold += amount;
        StartAccumulation();
    }

    private void StartAccumulation()
    {
        if (accumulationCoroutine != null)
            StopCoroutine(accumulationCoroutine);

        accumulationCoroutine = StartCoroutine(AccumulateAndShow());
    }

    private IEnumerator AccumulateAndShow()
    {
        yield return new WaitForSeconds(accumulationTime);

        if (accumulatedExperience > 0 || accumulatedGold > 0)
        {
            var notification = new NotificationData(accumulatedExperience, accumulatedGold);
            QueueNotification(notification);
            accumulatedExperience = 0;
            accumulatedGold = 0;
        }

        accumulationCoroutine = null;
    }

    private void QueueNotification(NotificationData notification)
    {
        notificationQueue.Enqueue(notification);

        if (!isShowingNotification)
            ProcessNextNotification();
    }

    private void ProcessNextNotification()
    {
        if (notificationQueue.Count == 0)
        {
            isShowingNotification = false;
            return;
        }

        // 🔧 Garante que este GameObject esteja ativo
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        var notification = notificationQueue.Dequeue();
        isShowingNotification = true;

        if (currentNotificationCoroutine != null)
            StopCoroutine(currentNotificationCoroutine);

        currentNotificationCoroutine = StartCoroutine(ShowNotificationCoroutine(notification));
    }

    private IEnumerator ShowNotificationCoroutine(NotificationData notification)
    {
        UpdateNotificationContent(notification);

        if (notificationPanel != null)
            notificationPanel.SetActive(true);

        yield return StartCoroutine(AnimateIn());
        yield return new WaitForSeconds(displayDuration);
        yield return StartCoroutine(AnimateOut());

        if (notificationPanel != null)
            notificationPanel.SetActive(false);

        ProcessNextNotification();
    }

    private void UpdateNotificationContent(NotificationData notification)
    {
        if (experienceText != null)
        {
            if (notification.experience > 0)
            {
                experienceText.text = $"Exp: +{notification.experience}";
                experienceText.color = experienceColor;
                experienceText.gameObject.SetActive(true);
            }
            else
            {
                experienceText.gameObject.SetActive(false);
            }
        }

        if (goldText != null)
        {
            if (notification.gold > 0)
            {
                goldText.text = $"Ouro: +{notification.gold}";
                goldText.color = goldColor;
                goldText.gameObject.SetActive(true);
            }
            else
            {
                goldText.gameObject.SetActive(false);
            }
        }

        if (backgroundImage != null)
        {
            bool hasContent = notification.experience > 0 || notification.gold > 0;
            Color targetColor = hasContent ? backgroundHighlightColor : backgroundNormalColor;
            backgroundImage.color = targetColor;
        }
    }

    private IEnumerator AnimateIn()
    {
        if (panelRectTransform == null || panelCanvasGroup == null) yield break;

        float elapsed = 0f;
        Vector2 startPos = hiddenPosition;
        Vector2 endPos = visiblePosition;

        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeInTime;
            float curveValue = slideInCurve.Evaluate(progress);

            panelRectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, curveValue);
            panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);

            yield return null;
        }

        panelRectTransform.anchoredPosition = endPos;
        panelCanvasGroup.alpha = 1f;
    }

    private IEnumerator AnimateOut()
    {
        if (panelRectTransform == null || panelCanvasGroup == null) yield break;

        float elapsed = 0f;
        Vector2 startPos = visiblePosition;
        Vector2 endPos = hiddenPosition;

        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeOutTime;
            float curveValue = slideOutCurve.Evaluate(progress);

            panelRectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, curveValue);
            panelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);

            yield return null;
        }

        panelRectTransform.anchoredPosition = endPos;
        panelCanvasGroup.alpha = 0f;
    }

    public void ShowTestNotification(int experience = 25, int gold = 15)
    {
        var notification = new NotificationData(experience, gold);
        QueueNotification(notification);
    }

    public void ClearNotifications()
    {
        notificationQueue.Clear();
        accumulatedExperience = 0;
        accumulatedGold = 0;

        if (accumulationCoroutine != null)
        {
            StopCoroutine(accumulationCoroutine);
            accumulationCoroutine = null;
        }

        if (currentNotificationCoroutine != null)
        {
            StopCoroutine(currentNotificationCoroutine);
            currentNotificationCoroutine = null;
        }

        isShowingNotification = false;

        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }

    public void SetNotificationPosition(Vector2 visible, Vector2 hidden)
    {
        visiblePosition = visible;
        hiddenPosition = hidden;

        if (!isShowingNotification && panelRectTransform != null)
            panelRectTransform.anchoredPosition = hiddenPosition;
    }

    private void OnDestroy()
    {
        EventManager.Unsubscribe<PlayerExperienceGainedEvent>(OnExperienceGained);
        EventManager.Unsubscribe<GoldCollectedEvent>(OnGoldCollected);

        ClearNotifications();
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 300, 200, 100));
        GUILayout.Label("Notification Debug");

        if (GUILayout.Button("Test +25 EXP")) AddExperience(25);
        if (GUILayout.Button("Test +15 Gold")) AddGold(15);
        if (GUILayout.Button("Test Both")) { AddExperience(25); AddGold(15); }

        GUILayout.EndArea();
    }
#endif
}
} */