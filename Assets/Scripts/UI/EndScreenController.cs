using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndScreenController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private TextMeshProUGUI playerScoreText;
    [SerializeField] private TextMeshProUGUI botScoreText;

    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void Show(FarkleGameState gameState)
    {
        int playerScore = gameState.PlayerBankedScore;
        int botScore = gameState.BotBankedScore;

        if (playerScore >= FarkleGameState.MaxScore)
        {
            winText.text = "Player Victory!";
        } else
        {
            winText.text = "Bot Victory!";
        }

        playerScoreText.text = playerScore.ToString();
        botScoreText.text = botScore.ToString();

        anim.SetTrigger("Show");
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
