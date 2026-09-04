using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DiceController : MonoBehaviour, IPointerClickHandler
{
    [Header("Dice References")]
    public int DiceIndex;
    private TextMeshProUGUI faceText;
    [SerializeField] private SpriteRenderer diceSpriteRenderer;

    [SerializeField] private Color selectedColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    [SerializeField] private Color unselectedColor = Color.white;

    public Action<int> OnToggleSelection;

    private void Awake()
    {
        faceText = GetComponentInChildren<TextMeshProUGUI>();
        diceSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void SetFace(int face, int diceIndex)
    {
        DiceIndex = diceIndex;
        if (faceText == null)
        {
            Debug.LogError("Face Text reference is not assigned");
            return;
        }

        if (face == 0)
        {
            faceText.text = "SCORED";
        }
        else
        {
            faceText.text = face.ToString();
        }
    }

    public void ToggleSelect()
    {
        OnToggleSelection?.Invoke(DiceIndex);
    }

    public void SetSelected(bool state)
    {
        if (diceSpriteRenderer == null)
        {
            Debug.LogWarning($"diceSpriteRenderer is not assigned");
        }

        if (state)
        {
            // Selected
            diceSpriteRenderer.color = selectedColor;
        }
        else
        {
            diceSpriteRenderer.color = unselectedColor;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleSelect();
        //print("Clicked " + DiceIndex.ToString());
    }
}
