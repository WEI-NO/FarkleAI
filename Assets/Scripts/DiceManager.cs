using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DiceManager : MonoBehaviour
{
    [SerializeField] private DiceController dicePrefab;

    [Header("Visual Settings")]
    public Vector2 SpawnOrigin = new Vector2(0, 0);
    public float XSpacing = 1.0f;
    [SerializeField] private FarkleGame farkleGame;

    [SerializeField] private List<DiceController> spawnedDice = new List<DiceController>();

    [SerializeField] private HashSet<int> chosenDiceIndex = new HashSet<int>();

    public void SpawnDice(int count)
    {
        for (int i = spawnedDice.Count - 1; i >= 0; i--)
        {
            if (spawnedDice[i] != null)
            {
                spawnedDice[i].OnToggleSelection -= ToggleDice;
                Destroy(spawnedDice[i].gameObject);
            }
            spawnedDice.RemoveAt(i);
        }

        float halfXOffset = (count / 2) * XSpacing - (count % 2 == 0 ? XSpacing / 2 : 0);
        Vector2 spawnOrigin = new Vector2(SpawnOrigin.x - halfXOffset, SpawnOrigin.y);

        for (int i = 0; i < count; i++)
        {
            Vector2 spawnPosition = new Vector2(spawnOrigin.x + (i * XSpacing), spawnOrigin.y);
            DiceController newDice = Instantiate(dicePrefab, spawnPosition, Quaternion.identity, transform);

            spawnedDice.Add(newDice);
            newDice.OnToggleSelection += ToggleDice;
        }
    }

    public void SetDice(int[] diceFaces)
    {
        for (int i = 0; i < diceFaces.Length; i++)
        {
            if (i < spawnedDice.Count)
            {
                if (spawnedDice[i] != null)
                {
                    spawnedDice[i].SetFace(diceFaces[i], i);
                }
                else
                {
                    Debug.LogError($"Spawned dice at index {i} is null.");
                }
            }
        }
    }

    public void ToggleDice(int index)
    {
        if (chosenDiceIndex.Contains(index))
        {
            chosenDiceIndex.Remove(index);
            spawnedDice[index].SetSelected(false);
        }
        else
        {
            chosenDiceIndex.Add(index);
            spawnedDice[index].SetSelected(true);
        }
    }

    public void Reroll()
    {
        farkleGame.RerollPlayerDice();
    }

    public void Score()
    {
        farkleGame.ScorePlayerDice(chosenDiceIndex.ToArray());
        chosenDiceIndex.Clear();
    }

    public void Bank()
    {
        farkleGame.BankPlayerScore();
    }
}
