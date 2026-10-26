using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Elementos de UI - Jogador 1")]
    public TextMeshProUGUI coinsTextP1;
    public TextMeshProUGUI starsTextP1;

    [Header("Elementos de UI - Jogador 2")]
    public TextMeshProUGUI coinsTextP2;
    public TextMeshProUGUI starsTextP2;

    [Header("Mensagem de Vitória")]
    public TextMeshProUGUI winMessageText;

    private void OnEnable()
    {
        GameEvents.OnCoinScoreChanged += UpdateCoinsUI;
        GameEvents.OnStarScoreChanged += UpdateStarsUI;
        GameEvents.OnGameOver += ShowWinMessage;
    }

    private void OnDisable()
    {
        GameEvents.OnCoinScoreChanged -= UpdateCoinsUI;
        GameEvents.OnStarScoreChanged -= UpdateStarsUI;
        GameEvents.OnGameOver -= ShowWinMessage;
    }

    private void Start()
    {
        if (winMessageText != null)
            winMessageText.gameObject.SetActive(false);
    }

    private void UpdateCoinsUI(int playerID, int score)
    {
        if (playerID == 1 && coinsTextP1 != null)
            coinsTextP1.text = "P1 Moedas: " + score;
        else if (playerID == 2 && coinsTextP2 != null)
            coinsTextP2.text = "P2 Moedas: " + score;
    }

    private void UpdateStarsUI(int playerID, int score)
    {
        if (playerID == 1 && starsTextP1 != null)
            starsTextP1.text = "P1 Estrelas: " + score;
        else if (playerID == 2 && starsTextP2 != null)
            starsTextP2.text = "P2 Estrelas: " + score;
    }

    private void ShowWinMessage(string message)
    {
        if (winMessageText != null)
        {
            winMessageText.text = message;
            winMessageText.gameObject.SetActive(true);
        }
    }
}