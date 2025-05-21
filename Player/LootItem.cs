using UnityEngine;
using System.Collections;

public class LootItem : MonoBehaviour
{
    [Header("Item")]
    public Item item;
    
    [Header("Animação")]
    public float rotationSpeed = 100f;
    public float bobHeight = 0.2f;
    public float bobSpeed = 2f;
    public float attractSpeed = 5f;
    public float pickupDistance = 1.5f;
    
    [Header("Efeitos")]
    public GameObject pickupEffectPrefab; // Opcional - um efeito para quando o item for coletado
    
    // Variáveis internas
    private Vector3 startPosition;
    private Rigidbody rb;
    private bool isAttracting = false;
    private Transform playerTransform;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        
        // Desativar física após o "pulo" inicial
        StartCoroutine(DisablePhysicsAfterDelay(0.5f));
    }
    
    private IEnumerator DisablePhysicsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
    
    private void Update()
    {
        // Rotação constante
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        // Movimento de flutuação (bob)
        if (!isAttracting && rb.isKinematic)
        {
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = startPosition + new Vector3(0, yOffset, 0);
        }
        
        // Verificar se o jogador está próximo para atração automática
        if (!isAttracting)
        {
            CheckPlayerProximity();
        }
        else
        {
            // Mover em direção ao jogador
            MoveTowardsPlayer();
        }
    }
    
    private void CheckPlayerProximity()
    {
        if (playerTransform == null)
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distance < pickupDistance)
            {
                isAttracting = true;
                
                // Desabilitar o collider para evitar coleta prematura
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = false;
                }
            }
        }
    }
    
    private void MoveTowardsPlayer()
    {
        if (playerTransform != null)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            transform.position += direction * attractSpeed * Time.deltaTime;
            
            // Verificar se chegou próximo o suficiente para coleta
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distance < 0.5f)
            {
                CollectItem();
            }
        }
    }
    
    private void CollectItem()
    {
		
        PlayerController player = playerTransform.GetComponent<PlayerController>();
        if (player != null)
        {
            bool added = player.inventory.AddItem(item);
            if (added)
            {
                // Mostrar um efeito de coleta
                if (pickupEffectPrefab != null)
                {
                    Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
                }
                
                // Tocar um som de coleta (se implementado)
                // AudioManager.Instance.PlaySound("ItemPickup");
                
                // Destruir o objeto
                Destroy(gameObject);
            }
            else
            {
                // Inventário cheio - restaurar a colisão e parar de atrair
                isAttracting = false;
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = true;
                }
                
                // Mostrar mensagem de inventário cheio
                Debug.Log("Inventário cheio!");
            }
			           // Notificar o QuestManager sobre a coleta de item
            QuestManager questManager = FindObjectOfType<QuestManager>();
            if (questManager != null)
            {
                questManager.UpdateQuestProgress(QuestType.CollectItems, 1);
            }
            

        }
    }
    
    // Isso ainda é útil se o jogador coletar o item antes da atração
    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && !isAttracting)
        {
            bool added = player.inventory.AddItem(item);
            if (added)
            {
                // Mostrar um efeito de coleta
                if (pickupEffectPrefab != null)
                {
                    Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
                }
                
                // Destruir o objeto
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("Inventário cheio!");
            }
        }
    }
}