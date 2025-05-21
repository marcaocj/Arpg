using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    // Singleton para acesso fácil
    public static DamagePopupManager Instance { get; private set; }
    
    // Prefab do popup de dano (deve ser configurado no Inspector)
    public GameObject damagePopupPrefab;
    
    // Referência ao Canvas pai dos popups
    public Transform canvasTransform;
    
    // Altura adicional para os popups (offset acima dos personagens)
    public float heightOffset = 1.5f;
    
    private void Awake()
    {
        // Configuração do singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Verificar dependências
        if (damagePopupPrefab == null)
        {
            Debug.LogError("DamagePopupManager: damagePopupPrefab não configurado!");
        }
        
        if (canvasTransform == null)
        {
            // Tentar encontrar automaticamente o canvas principal
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas != null)
            {
                canvasTransform = mainCanvas.transform;
                Debug.Log("DamagePopupManager: Canvas encontrado automaticamente.");
            }
            else
            {
                Debug.LogError("DamagePopupManager: canvasTransform não configurado e nenhum Canvas encontrado!");
            }
        }
    }
    
    // Método para mostrar dano sobre uma entidade
    public void ShowDamage(Vector3 worldPosition, int damageAmount, bool isCritical = false)
    {
        // Adicionar offset de altura
        Vector3 popupPosition = worldPosition + Vector3.up * heightOffset;
        
        // Criar popup de dano
        DamagePopup.Create(popupPosition, damageAmount, isCritical);
    }
    
    // Método para mostrar cura sobre uma entidade
    public void ShowHealing(Vector3 worldPosition, int healAmount)
    {
        // Adicionar offset de altura
        Vector3 popupPosition = worldPosition + Vector3.up * heightOffset;
        
        // Criar popup de cura
        DamagePopup.Create(popupPosition, healAmount, false, true);
    }
}