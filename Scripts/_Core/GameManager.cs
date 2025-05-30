using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    
    [Header("Level")]
    public List<Transform> respawnPoints = new List<Transform>();
    public int currentLevel = 1;
    
    [Header("Gameplay")]
    public int goldCollected = 0;
    public int enemiesDefeated = 0;
    
    [Header("UI")]
    public GameObject pauseMenu;
    public GameObject inventoryUI;
    
    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject lootItemPrefab; // Prefab para itens dropados

    private PlayerController player;
    private bool isPaused = false;
    
    private void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Encontrar jogador
        player = FindObjectOfType<PlayerController>();
        
        // Inicializar UI
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }
        
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Toggle pause menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
        
        // REMOVIDO: Toggle inventory - agora está no PlayerController
        // if (Input.GetKeyDown(KeyCode.I))
        // {
        //     ToggleInventory();
        // }
    }
    
    public Vector3 GetRespawnPoint()
    {
        if (respawnPoints.Count > 0)
        {
            // Escolher um ponto de respawn aleatório
            int index = Random.Range(0, respawnPoints.Count);
            return respawnPoints[index].position;
        }
        
        // Fallback
        return Vector3.zero;
    }
    
    public void AddGold(int amount)
    {
        goldCollected += amount;
        Debug.Log("Ouro total: " + goldCollected);
    }
    
    public void EnemyDefeated()
    {
        enemiesDefeated++;
        Debug.Log("Inimigos derrotados: " + enemiesDefeated);
    }
    
    public void TogglePauseMenu()
    {
        isPaused = !isPaused;
        
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(isPaused);
        }
        
        // Pausar/despausar o jogo
        Time.timeScale = isPaused ? 0f : 1f;
    }
    
    public void ToggleInventory()
    {
        if (inventoryUI != null)
        {
            bool isActive = inventoryUI.activeSelf;
            inventoryUI.SetActive(!isActive);
            
            // Desacelerar o jogo quando o inventário estiver aberto
      //      Time.timeScale = inventoryUI.activeSelf ? 0.2f : 1f;
        }
    }
    
    public void LoadLevel(int levelIndex)
    {
        SceneManager.LoadScene(levelIndex);
    }
    
    public void RestartGame()
    {
        // Reiniciar o jogo
        goldCollected = 0;
        enemiesDefeated = 0;
        
        // Recarregar a cena atual
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
        
        // Resetar pause
        isPaused = false;
        Time.timeScale = 1f;
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}