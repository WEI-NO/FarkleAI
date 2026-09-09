using NUnit.Framework;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DiceController : MonoBehaviour, IPointerClickHandler
{
    [Header("Dice References")]
    public int DiceIndex;
    [SerializeField] private Image faceSprite;
    [SerializeField] private Image backgroundSprite;

    [SerializeField] private Sprite[] diceSprites = new Sprite[7];

    [SerializeField] private Color selectedColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    [SerializeField] private Color unselectedColor = Color.white;

    public Action<int> OnToggleSelection;

    private void Awake()
    {
        faceSprite = GetComponentInChildren<Image>();
    }

    public void SetFace(int face, int diceIndex)
    {
        DiceIndex = diceIndex;
        int faceIndex = face;

        if (faceIndex >= 0 && faceIndex < 7)
        {
            faceSprite.sprite = diceSprites[faceIndex];
        }

    }

    public void ToggleSelect()
    {
        OnToggleSelection?.Invoke(DiceIndex);
    }

    public void SetSelected(bool state)
    {
        if (faceSprite == null)
        {
            Debug.LogWarning($"faceSprite is not assigned");
        }

        if (state)
        {
            // Selected
            faceSprite.color = selectedColor;
        }
        else
        {
            faceSprite.color = unselectedColor;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleSelect();
        //print("Clicked " + DiceIndex.ToString());
    }
}
