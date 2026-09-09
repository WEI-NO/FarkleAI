using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionController : MonoBehaviour
{
    [SerializeField] private Button scoreButton;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Button bankButton;
    [SerializeField] private TextMeshProUGUI bankText;
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollText;

    [SerializeField] private FarkleGame gameState;

    [SerializeField] private Color enableButtonColor;
    [SerializeField] private Color disableButtonColor;
    [SerializeField] private Color enableTextColor;
    [SerializeField] private Color disableTextColor;

    private void Awake()
    {
        gameState.OnPlayerScoredThisTurn += OnPlayerScoredThisTurn;
        gameState.OnTurnChange += OnTurnChange;
    }

    public void OnPlayerScoredThisTurn(bool state)
    {
        print("scored this turn: " + state);
        scoreButton.image.color = state ? disableButtonColor : enableButtonColor;
        bankButton.image.color = state ? enableButtonColor : disableButtonColor;
        rerollButton.image.color = state ? enableButtonColor : disableButtonColor;

        scoreText.color = state ? disableTextColor : enableTextColor;
        bankText.color = state ? enableTextColor : disableButtonColor;
        rerollText.color = state ? enableTextColor : disableButtonColor;

        scoreButton.interactable = state;
        bankButton.interactable = state;
        rerollButton.interactable = state;
    }

    public void OnTurnChange(FarkleTurnState state)
    {
        if (state == FarkleTurnState.PlayerTurn)
        {
            print("turn player");
            scoreButton.image.color = enableButtonColor;
            bankButton.image.color = disableButtonColor;
            rerollButton.image.color = disableButtonColor;

            scoreText.color = enableTextColor;
            bankText.color = disableTextColor;
            rerollText.color = disableTextColor;

            scoreButton.interactable = true;
            bankButton.interactable = false;
            rerollButton.interactable = false;
        } else
        {
            print("turn bot");
            scoreButton.image.color = disableButtonColor;
            bankButton.image.color = disableButtonColor;
            rerollButton.image.color = disableButtonColor;

            scoreText.color = disableTextColor;
            bankText.color = disableTextColor;
            rerollText.color = disableTextColor;

            scoreButton.interactable = false;
            bankButton.interactable = false;
            rerollButton.interactable = false;
        }
    }
}
