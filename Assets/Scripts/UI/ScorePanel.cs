using TMPro;
using UnityEngine;

public class ScorePanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI bankedDisplay;
    [SerializeField] private TextMeshProUGUI unbankedDisplay;

    public void SetBankedScore(int score)
    {
        bankedDisplay.text = score.ToString();
    }

    public void SetUnbankedScore(int score)
    {
        unbankedDisplay.text = score.ToString();
    }
}
