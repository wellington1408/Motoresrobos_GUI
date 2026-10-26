using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    
    private int coinsPlayer1 = 0;
    private int coinsPlayer2 = 0;

   
    private int starsPlayer1 = 0;
    private int starsPlayer2 = 0;

   
    private int totalStarsInScene = 0;
    private int collectedStarsCount = 0;

    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance != this && Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        
        SceneManager.LoadSceneAsync("GUI", LoadSceneMode.Additive);

      
        totalStarsInScene = GameObject.FindGameObjectsWithTag("Star").Length;
        Debug.Log("Total de estrelas encontradas na cena: " + totalStarsInScene);
    }

  
    public void AddCoin(int playerID)
    {
        if (isGameOver) return;

        if (playerID == 1)
        {
            coinsPlayer1++;
            GameEvents.TriggerCoinScoreChanged(1, coinsPlayer1);
            GameEvents.TriggerCoinCollected(1); 
        }
        else if (playerID == 2)
        {
            coinsPlayer2++;
            GameEvents.TriggerCoinScoreChanged(2, coinsPlayer2);
            GameEvents.TriggerCoinCollected(2); 
        }
    }

    
    public void AddStar(int playerID)
    {
        if (isGameOver) return;

        if (playerID == 1)
        {
            starsPlayer1++;
            GameEvents.TriggerStarScoreChanged(1, starsPlayer1);
        }
        else if (playerID == 2)
        {
            starsPlayer2++;
            GameEvents.TriggerStarScoreChanged(2, starsPlayer2);
        }

        collectedStarsCount++;

       
        if (collectedStarsCount >= totalStarsInScene)
        {
            CheckWinner();
        }
    }

    private void CheckWinner()
    {
        isGameOver = true;
        string winnerMessage = "";

        if (starsPlayer1 > starsPlayer2)
        {
            winnerMessage = "Jogador 1 Venceu o Jogo!";
        }
        else if (starsPlayer2 > starsPlayer1)
        {
            winnerMessage = "Jogador 2 Venceu o Jogo!";
        }
        else
        {
            winnerMessage = "Empate!";
        }

        GameEvents.TriggerGameOver(winnerMessage);
        Debug.Log(winnerMessage);
    }
}