using TMPro;
using UnityEngine;

public class ScorePanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI bankedDisplay;
    [SerializeField] private TextMeshProUGUI unbankedDisplay;

    [SerializeField] private GameObject highlight;
    public FarkleTurnState EnableState;


    private void Start()
    {
        FarkleGame.Instance.OnTurnChange += OnTurnChange;
    }

    private void OnTurnChange(FarkleTurnState state)
    {
        highlight.SetActive(state == EnableState);
    }

    public void SetBankedScore(int score)
    {
        bankedDisplay.text = score.ToString();
    }

    public void SetUnbankedScore(int score)
    {
        unbankedDisplay.text = score.ToString();
    }
}
