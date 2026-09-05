using TMPro;
using UnityEngine;

public class GameStatsUIController : MonoBehaviour
{
    [SerializeField] private FarkleGame farkleGame;
    private FarkleGameState gameState;

    [Header("References")]
    public TextMeshProUGUI botBankedScoreText;
    public TextMeshProUGUI playerBankedScoreText;

    private void Start()
    {
        if (farkleGame != null)
        {
            gameState = farkleGame.GameState;

            if (gameState != null)
            {
                gameState.OnBotBankedScoreChanged = SetBotBankedScoreText;
                gameState.OnPlayerBankedScoreChanged = SetPlayerBankedScoreText;
            }
        }
    }

    private void SetBotBankedScoreText(int newScore)
    {
        if (botBankedScoreText != null)
        {
            botBankedScoreText.text = newScore.ToString();
        }
    }

    private void SetPlayerBankedScoreText(int newScore)
    {
        if (playerBankedScoreText != null)
        {
            playerBankedScoreText.text = newScore.ToString();
        }
    }

}
