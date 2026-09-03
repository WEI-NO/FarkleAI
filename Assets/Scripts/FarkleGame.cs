using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FarkleTurnState
{
    PlayerTurn,
    BotTurn
}

public class FarkleGame : MonoBehaviour
{
    public int Seed = 42;

    [Header("Bot")]
    public ONNXModelLoader BotModelLoader;

    [Header("Game States")]
    public FarkleGameState GameState = new FarkleGameState();

    [Header("Game Events")]
    public Action test;

    public void Start()
    {
        UnityEngine.Random.InitState(Seed);

        ResetGame();
        var botDice = GameState.RollBotDice(FarkleGameState.MaxDiceRolls);
        for (int i = 0; i < botDice.Length; i++)
        {
            Debug.Log($"Bot Dice {i + 1}: {botDice[i]}");
        }

        var mask = GameState.GetBotActionMask();
        for (int i = 0; i < mask.Length; i++)
        {
            Debug.Log($"Action Mask {i}: {mask[i]}");
        }

        var observation = GameState.GetObservation();

        int predictedAction = BotModelLoader.Predict(observation, mask);
        print("Predicted Action: " + predictedAction);
        print("Described Action: " + ONNXModelLoader.DescribeAction(predictedAction, GameState.BotCurrentDice));
    }

    public void StartGame()
    {
        ResetGame();
        StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        while (GameState.PlayerScore < FarkleGameState.MaxScore && GameState.BotBankedScore < FarkleGameState.MaxScore)
        {
            if (GameState.TurnState == FarkleTurnState.PlayerTurn)
            {
                // Player's turn logic here
                // Wait for player input or actions
                yield return new WaitUntil(() => GameState.TurnState != FarkleTurnState.PlayerTurn);
            }
            else if (GameState.TurnState == FarkleTurnState.BotTurn)
            {
                // Bot's turn logic here
                // Simulate bot actions
                yield return new WaitForSeconds(1f); // Simulate thinking time
                GameState.TurnState = FarkleTurnState.PlayerTurn; // Switch back to player's turn
            }
        }
        yield return null;
    }

    public void ResetGame()
    {
        GameState.Reset();
    }



}

[System.Serializable]
public class FarkleGameState
{
    public const int MaxScore = 10000;
    public const int MaxDiceRolls = 6;

    // 1.  bot banked score
    public int BotBankedScore = 0;
    // 2.  bot unbanked score
    public int BotUnbankedScore = 0;
    // 3.  bot dice remaining
    public int BotDiceRemaining = 6;
    // 4.  player score
    public int PlayerScore = 0;
    // 5.  bot dice 1
    // 6.  bot dice 2
    // 7.  bot dice 3
    // 8.  bot dice 4
    // 9.  bot dice 5
    // 10. bot dice 6
    public int[] BotCurrentDice = new int[MaxDiceRolls];
    // player dice
    public int[] PlayerCurrentDice = new int[MaxDiceRolls];
    public int PlayerDiceRemaining = 6;
    public FarkleTurnState TurnState = FarkleTurnState.PlayerTurn;

    public FarkleGameState Reset()
    {
        BotBankedScore = 0;
        BotUnbankedScore = 0;
        BotDiceRemaining = 6;
        PlayerScore = 0;
        TurnState = FarkleTurnState.PlayerTurn;
        // Reset dice
        for (int i = 0; i < MaxDiceRolls; i++)
        {
            BotCurrentDice[i] = 0;
            PlayerCurrentDice[i] = 0;
        }
        return this;
    }

    #region Dice Rolls
    public int[] RollPlayerDice(int rollCount)
    {
        Array.Clear(PlayerCurrentDice, 0, PlayerCurrentDice.Length); ;

        for (int i = 0; i < rollCount; i++)
        {
            PlayerCurrentDice[i] = UnityEngine.Random.Range(1, 7);
        }

        Array.Sort(PlayerCurrentDice, 0, rollCount);

        PlayerDiceRemaining = rollCount;

        return PlayerCurrentDice;
    }

    public int[] RollBotDice(int rollCount)
    {
        Array.Clear(BotCurrentDice, 0, BotCurrentDice.Length);

        for (int i = 0; i < rollCount; i++)
        {
            BotCurrentDice[i] = UnityEngine.Random.Range(1, 7);
        }

        // Sort Active Dice
        Array.Sort(BotCurrentDice, 0, rollCount);

        BotDiceRemaining = rollCount;

        return BotCurrentDice;
    }
    #endregion

    public float[] GetObservation()
    {
        return new float[]
        {
            BotBankedScore / (float)MaxScore,
            BotUnbankedScore / (float)MaxScore,
            BotDiceRemaining / (float)MaxDiceRolls,
            PlayerScore / (float)MaxScore,
            BotCurrentDice[0] / 6f,
            BotCurrentDice[1] / 6f,
            BotCurrentDice[2] / 6f,
            BotCurrentDice[3] / 6f,
            BotCurrentDice[4] / 6f,
            BotCurrentDice[5] / 6f
        };
    }

    public bool[] GetBotActionMask()
    {
        // 0-63: Select dice and bank
        // 64-127: Select dice and reroll
        const int actionCount = 128;
        const int rerollOffset = 64;

        bool[] actionMask = new bool[actionCount];

        int diceCount = BotDiceRemaining;

        // Remove duplicates
        HashSet<string> seenSelections = new HashSet<string>();

        int subsetLimit = 1 << diceCount;

        for (int subset = 1; subset < subsetLimit; subset++)
        {
            List<int> selectedDice = GetSelectedBotDice(subset, diceCount);

            bool valid = TryCalculateScore(selectedDice, out int score);

            if (!valid)
                continue;

            // Same as python's 'tuple(sorted(dice_number))'
            selectedDice.Sort();
            string selectionSet = string.Join(",", selectedDice);

            if (!seenSelections.Add(selectionSet))
            {
                continue;
            }

            actionMask[subset] = true; // bank
            actionMask[subset + rerollOffset] = true; // reroll
        }

        return actionMask;
    }

    private List<int> GetSelectedBotDice(int subset, int diceCount)
    {
        List<int> selectedDice = new List<int>();

        for (int dieIndex = 0; dieIndex < diceCount; dieIndex++)
        {
            bool isSelected = (subset & (1 << dieIndex)) != 0;

            if (isSelected)
            {
                selectedDice.Add(BotCurrentDice[dieIndex]);
            }
        }

        return selectedDice;
    }

    private bool TryCalculateScore(List<int> dice, out int score)
    {
        score = 0;

        if (dice == null || dice.Count == 0)
        {
            return false;
        }

        int[] counts = new int[7];

        foreach (int die in dice)
        {
            if (die < 1 || die > 6)
            {
                return false;
            }

            counts[die]++;
        }

        int diceCount = dice.Count;

        // Special 6 Dice Scoring
        if (diceCount == 6)
        {
            int uniqueValues = 0;
            int pairCount = 0;
            int tripleCount = 0;
            bool hasFourOfAKind = false;
            bool hasPair = false;

            for (int face = 1; face <= 6; face++)
            {
                int count = counts[face];
                if (count > 0)
                {
                    uniqueValues++;
                }

                if (count == 2)
                {
                    pairCount++;
                    hasPair = true;
                }
                else if (count == 3)
                {
                    tripleCount++;
                }
                else if (count == 4)
                {
                    hasFourOfAKind = true;
                }
            }

            // Straight: 1,2,3,4,5,6
            if (uniqueValues == 6)
            {
                score = 1500;
                return true;
            }

            // Three pairs
            if (pairCount == 3)
            {
                score = 1500;
                return true;
            }

            // Two triples
            if (tripleCount == 2)
            {
                score = 2500;
                return true;
            }

            // Four of a a kind + pair
            if (hasFourOfAKind && hasPair)
            {
                score = 1500;
                return true;
            }
        }

        int scoringDiceCount = 0;
        for (int face = 1; face <= 6; face++)
        {
            int count = counts[face];

            if (count == 0)
            {
                continue;
            }

            // 6 of a kind
            if (count == 6)
            {
                score += 3000;
                scoringDiceCount += 6;
            }
            // 5 of a kind
            else if (count == 5)
            {
                score += 2000;
                scoringDiceCount += 5;
            }
            // 4 of a kind
            else if (count == 4)
            {
                score += 1000;
                scoringDiceCount += 4;
            }
            // Triple
            else if (count == 3)
            {
                score += face == 1 ? 300 : face * 100;
                scoringDiceCount += 3;
            }
            // Single 1s and 5s
            else
            {
                if (face == 1)
                {
                    score += count * 100;
                    scoringDiceCount += count;
                }
                else if (face == 5)
                {
                    score += count * 50;
                    scoringDiceCount += count;
                }
            }

        }

        // Eliminate scoring if dices contain non-scoring dice
        if (scoringDiceCount != diceCount)
        {
            score = 0;
            return false;
        }

        return true;
    }

}