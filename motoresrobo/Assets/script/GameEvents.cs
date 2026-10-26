using System;
using UnityEngine;

public static class GameEvents
{
   
    public static event Action<int, int> OnCoinScoreChanged;
    
    public static event Action<int, int> OnStarScoreChanged;
    
    public static event Action<int> OnCoinCollected;
    
    public static event Action<string> OnGameOver;

    public static void TriggerCoinScoreChanged(int playerID, int score) => OnCoinScoreChanged?.Invoke(playerID, score);
    public static void TriggerStarScoreChanged(int playerID, int score) => OnStarScoreChanged?.Invoke(playerID, score);
    public static void TriggerCoinCollected(int playerID) => OnCoinCollected?.Invoke(playerID);
    public static void TriggerGameOver(string winnerName) => OnGameOver?.Invoke(winnerName);
}