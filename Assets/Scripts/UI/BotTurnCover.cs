using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BotTurnCover : MonoBehaviour
{
    private Image coverImage;
    private TextMeshProUGUI coverText;

    private void Awake()
    {
        coverImage = GetComponent<Image>();
        coverText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        FarkleGame.Instance.OnTurnChange += OnTurnChange;
    }

    private void OnTurnChange(FarkleTurnState state)
    {
        if (state == FarkleTurnState.BotTurn)
        {
            coverImage.enabled = true;
            coverText.enabled = true;
        } else
        {
            coverImage.enabled = false;
            coverText.enabled = false;
        }
    }
}
