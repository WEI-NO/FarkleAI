using TMPro;
using UnityEngine;

public class GameStatsUIController : MonoBehaviour
{
    [SerializeField] private FarkleGame farkleGame;
    private FarkleGameState gameState;

    [Header("References")]
    [SerializeField] private ScorePanel playerScorePanel;
    [SerializeField] private ScorePanel botScorePanel;
    [SerializeField] private DiceManager diceManager;

    [SerializeField] private TextMeshProUGUI turnDisplay;


    private void Awake()
    {
        if (farkleGame != null)
        {
            farkleGame.OnRolledDice += OnDiceRolled;
            farkleGame.OnTurnChange += OnTurnChange;

            gameState = farkleGame.GameState;

            if (gameState != null)
            {
                gameState.OnBotBankedScoreChanged += SetBotBankedScoreText;
                gameState.OnPlayerBankedScoreChanged += SetPlayerBankedScoreText;
                gameState.OnPlayerUnbankedScoreChanged += SetPlayerUnbankedScoreText;
                gameState.OnBotUnbankedScoreChanged += SetBotUnbankedScoreText;
                gameState.OnGameReset += OnReset;
            }
        }
    }

    private void SetBotBankedScoreText(int newScore)
    {
        if (botScorePanel != null)
        {
            botScorePanel.SetBankedScore(newScore);
        }
    }

    private void SetPlayerBankedScoreText(int newScore)
    {
        if (playerScorePanel != null)
        {
            playerScorePanel.SetBankedScore(newScore);
        }
    }
    private void SetBotUnbankedScoreText(int newScore)
    {
        if (botScorePanel != null)
        {
            botScorePanel.SetUnbankedScore(newScore);
        }
    }

    private void SetPlayerUnbankedScoreText(int newScore)
    {
        if (playerScorePanel != null)
        {
            playerScorePanel.SetUnbankedScore(newScore);
        }
    }

    private void OnDiceRolled(int[] rolledDice, FarkleTurnState state)
    {
        diceManager.SpawnDice(6);
        diceManager.SetDice(rolledDice);
    }

    private void OnTurnChange(FarkleTurnState state)
    {
        if (state == FarkleTurnState.PlayerTurn)
        {
            // Player's turn
            turnDisplay.text = "Player's turn";

        } else
        {
            // Bot's turn
            turnDisplay.text = "Bot's turn";
        }
    }

    private void OnReset()
    {
        diceManager.SpawnDice(0);
        playerScorePanel.SetBankedScore(0);
        botScorePanel.SetBankedScore(0);
        playerScorePanel.SetUnbankedScore(0);
        botScorePanel.SetUnbankedScore(0);
    }

}
