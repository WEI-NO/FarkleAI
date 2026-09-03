using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class DiceController : MonoBehaviour, IPointerClickHandler
{
    [Header("Dice References")]
    public int DiceIndex;
    private TextMeshProUGUI faceText;

    public Action<int> OnToggleSelection;

    private void Awake()
    {
        faceText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void SetFace(int face, int diceIndex)
    {
        DiceIndex = diceIndex;
        if (faceText == null)
        {
            Debug.LogError("Face Text reference is not assigned");
            return;
        }

        faceText.text = face.ToString();
    }

    public void ToggleSelect()
    {
        OnToggleSelection?.Invoke(DiceIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleSelect();
        //print("Clicked " + DiceIndex.ToString());
    }
}
