using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    public DiceManager diceManager;


    [Header("Game Events")]
    public bool PlayerScoredThisTurn = false;

    [Header("Events")]
    // Turn Change
    public Action<FarkleTurnState> OnTurnChange;
    public Action<int[], FarkleTurnState> OnRolledDice;
    public Action<bool> OnPlayerScoredThisTurn;


    public void Start()
    {
        UnityEngine.Random.InitState(Seed);

        StartGame();

    }

    public void StartGame()
    {
        ResetGame();
        StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        while (GameState.PlayerBankedScore < FarkleGameState.MaxScore && GameState.BotBankedScore < FarkleGameState.MaxScore)
        {
            if (GameState.TurnState == FarkleTurnState.PlayerTurn)
            {
                // Player's turn logic here
                // Wait for player input or actions
                int[] rolledDice = GameState.RollPlayerDice(GameState.PlayerDiceRemaining);
                OnRolledDice?.Invoke(rolledDice, FarkleTurnState.PlayerTurn);
                //PlayerDiceManager.SpawnDice(rolledDice.Length);
                //PlayerDiceManager.SetDice(rolledDice);

                bool[] actionMask = GameState.GetActionMask(
                    GameState.PlayerDiceRemaining,
                    GameState.PlayerCurrentDice,
                    out bool hasValid
                );

                if (!hasValid)
                {
                    yield return new WaitForSeconds(3.0f);
                    // Player Farkled
                    GameState.PlayerUnbankedScore = 0;
                    EndPlayerTurn();
                    continue;
                }


                yield return new WaitUntil(() => GameState.TurnState != FarkleTurnState.PlayerTurn);
            }
            else if (GameState.TurnState == FarkleTurnState.BotTurn)
            {
                // Bot's turn logic here
                // Simulate bot actions
                int[] rolledDice = GameState.RollBotDice(GameState.BotDiceRemaining);
                print($"Bot Rolled {GameState.BotCurrentDice}");
                OnRolledDice?.Invoke(rolledDice, FarkleTurnState.BotTurn);
                //BotDiceManager.SpawnDice(rolledDice.Length);
                //BotDiceManager.SetDice(rolledDice);

                float[] observation = GameState.GetObservation();
                bool[] actionMask = GameState.GetActionMask(
                    GameState.BotDiceRemaining,
                    GameState.BotCurrentDice,
                    out bool hasValid
                );

                if (!hasValid)
                {
                    print("BOT FARKLED");
                    yield return new WaitForSeconds(3.0f);
                    // Bot Farkled
                    GameState.BotUnbankedScore = 0;
                    EndBotTurn();
                    continue;
                }


                int result = BotModelLoader.Predict(observation, actionMask);
                bool bank = result <= 63;
                yield return new WaitForSeconds(3f); // Simulate thinking time

                int[] indexes = TranslateBotAction(result);
                List<int> chosenDice = new List<int>(ConvertToSelectedDice(indexes, GameState.BotCurrentDice));
                if (FarkleGameState.TryCalculateScore(chosenDice, out int score))
                {
                    // Simulate Visuals for dice selection
                    for (int i = 0; i < indexes.Length; i++)
                    {
                        print($"Bot Toggling: {indexes[i]}");
                        print($"Bot Chose: {chosenDice[i]}");
                        diceManager.ToggleDice(indexes[i]);
                        yield return new WaitForSeconds(0.5f);
                    }

                    GameState.BotUnbankedScore += score;
                    GameState.BotDiceRemaining -= chosenDice.Count;
                    if (GameState.BotDiceRemaining <= 0)
                    {
                        GameState.BotDiceRemaining = FarkleGameState.MaxDiceRolls;
                    }
                }

                yield return new WaitForSeconds(2f);
                if (bank)
                {
                    EndBotTurn();
                }

            }
        }
        yield return null;
    }

    public void ResetGame()
    {
        GameState.Reset();
        OnTurnChange?.Invoke(GameState.TurnState);
    }

    public bool RerollPlayerDice()
    {
        if (!PlayerScoredThisTurn) return false;

        if (GameState.PlayerDiceRemaining > 0)
        {    
            var rolledDice = GameState.RollPlayerDice(GameState.PlayerDiceRemaining);
            //PlayerDiceManager.SpawnDice(rolledDice.Length);
            //PlayerDiceManager.SetDice(rolledDice);
            OnRolledDice?.Invoke(rolledDice, FarkleTurnState.PlayerTurn);
            PlayerScoredThisTurn = false;
            OnPlayerScoredThisTurn?.Invoke(false);
            return true;
        }
        return false;
    }

    public bool BankPlayerScore()
    {
        if (PlayerScoredThisTurn)
        {
            EndPlayerTurn();
            return true;
        }
        return false;
    }

    public bool ScorePlayerDice(int[] chosenDiceIndex)
    {
        if (PlayerScoredThisTurn) return false;

        List<int> chosenDice = new List<int>(ConvertToSelectedDice(chosenDiceIndex, GameState.PlayerCurrentDice));
        if (FarkleGameState.TryCalculateScore(chosenDice, out int score))
        {
            GameState.PlayerUnbankedScore += score;
            PlayerScoredThisTurn = true;
            OnPlayerScoredThisTurn?.Invoke(true);
            GameState.PlayerDiceRemaining -= chosenDiceIndex.Length;
            if (GameState.PlayerDiceRemaining <= 0)
            {
                GameState.PlayerDiceRemaining = FarkleGameState.MaxDiceRolls;
            }
            return true;
        }
        print("Invalid Dice Selection");
        return false;
    }

    public int[] ConvertToSelectedDice(int[] selectedIndex, int[] dice)
    {
        List<int> chosenDice = new List<int>();
        for (int i = 0; i < selectedIndex.Length; i++)
        {
            int index = selectedIndex[i];
            if (index < dice.Length)
            {
                chosenDice.Add(dice[index]);
            }
        }

        return chosenDice.ToArray();
    }

    public void EndPlayerTurn()
    {
        GameState.PlayerBankedScore += GameState.PlayerUnbankedScore;
        PlayerScoredThisTurn = false;
        GameState.TurnState = FarkleTurnState.BotTurn;
        OnTurnChange?.Invoke(GameState.TurnState);
        //PlayerDiceManager.SpawnDice(0);
        GameState.ResetTurn();
    }

    public void EndBotTurn()
    {
        GameState.BotBankedScore += GameState.BotUnbankedScore;

        GameState.TurnState = FarkleTurnState.PlayerTurn;
        OnTurnChange?.Invoke(GameState.TurnState);
        //BotDiceManager.SpawnDice(0);
        GameState.ResetTurn();
    }

    public int[] TranslateBotAction(int bit, int bitCount = 6)
    {
        List<int> indexes = new List<int>();

        for (int i = 0; i < bitCount; i++)
        {
            if ((bit & (1 << i)) != 0)
            {
                indexes.Add(i);
            }
        }
        return indexes.ToArray();
    }
}

[System.Serializable]
public class FarkleGameState
{
    public const int MaxScore = 10000;
    public const int MaxDiceRolls = 6;

    // 1.  bot banked score
    public int BotBankedScore
    {
        get { return _botBankedScore; }
        set
        {
            if (_botBankedScore != value)
            {
                _botBankedScore = value;
                OnBotBankedScoreChanged?.Invoke(_botBankedScore);
            }
        }
    }
    public Action<int> OnBotBankedScoreChanged;
    public int _botBankedScore = 0;
    // 2.  bot unbanked score
    public int BotUnbankedScore
    {
        get { return _botUnbankedScore; }
        set
        {
            if (_botUnbankedScore != value)
            {
                _botUnbankedScore = value;
                OnBotUnbankedScoreChanged?.Invoke(_botUnbankedScore);
            }
        }
    }
    public Action<int> OnBotUnbankedScoreChanged;
    private int _botUnbankedScore = 0;
    // 3.  bot dice remaining
    public int BotDiceRemaining = 6;
    // 4.  player score
    public int PlayerBankedScore
    {
        get { return _playerBankedScore; }
        set
        {
            if (_playerBankedScore != value)
            {
                _playerBankedScore = value;
                OnPlayerBankedScoreChanged?.Invoke(_playerBankedScore);
            }
        }
    }
    public Action<int> OnPlayerBankedScoreChanged;
    private int _playerBankedScore = 0;
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
    public int PlayerUnbankedScore
    {
        get { return _playerUnbankedScore; }
        set
        {
            if (_playerUnbankedScore != value)
            {
                _playerUnbankedScore = value;
                OnPlayerUnbankedScoreChanged?.Invoke(_playerUnbankedScore);
            }
        }
    }
    public Action<int> OnPlayerUnbankedScoreChanged;
    private int _playerUnbankedScore = 0;
    public FarkleTurnState TurnState = FarkleTurnState.PlayerTurn;

    public Action OnGameReset;

    public FarkleGameState Reset()
    {
        BotBankedScore = 0;
        BotUnbankedScore = 0;
        BotDiceRemaining = MaxDiceRolls;
        PlayerBankedScore = 0;
        PlayerUnbankedScore = 0;
        PlayerDiceRemaining = MaxDiceRolls;
        TurnState = FarkleTurnState.PlayerTurn;
        // Reset dice
        for (int i = 0; i < MaxDiceRolls; i++)
        {
            BotCurrentDice[i] = 0;
            PlayerCurrentDice[i] = 0;
        }
        OnGameReset?.Invoke();
        return this;
    }

    public void ResetTurn()
    {
        BotUnbankedScore = 0;
        PlayerUnbankedScore = 0;
        BotDiceRemaining = MaxDiceRolls;
        PlayerDiceRemaining = MaxDiceRolls;
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
            PlayerBankedScore / (float)MaxScore,
            BotCurrentDice[0] / 6f,
            BotCurrentDice[1] / 6f,
            BotCurrentDice[2] / 6f,
            BotCurrentDice[3] / 6f,
            BotCurrentDice[4] / 6f,
            BotCurrentDice[5] / 6f
        };
    }

    public bool[] GetActionMask(int diceRemaining, int[] currentDice, out bool hasValid)
    {
        // 0-63: Select dice and bank
        // 64-127: Select dice and reroll
        const int actionCount = 128;
        const int rerollOffset = 64;
        hasValid = false;
        bool[] actionMask = new bool[actionCount];

        int diceCount = diceRemaining;

        // Remove duplicates
        HashSet<string> seenSelections = new HashSet<string>();
        Dictionary<int, int> bankScores = new Dictionary<int, int>();
        int bestScore = int.MinValue;
        int subsetLimit = 1 << diceCount;


        for (int subset = 1; subset < subsetLimit; subset++)
        {
            List<int> selectedDice = GetSelectedDice(subset, diceCount, currentDice);

            bool valid = TryCalculateScore(selectedDice, out int score);

            if (!valid)
                continue;

            hasValid = true;
            // Same as python's 'tuple(sorted(dice_number))'
            selectedDice.Sort();
            string selectionSet = string.Join(",", selectedDice);

            if (!seenSelections.Add(selectionSet))
            {
                continue;
            }

            //actionMask[subset] = true; // bank
            actionMask[subset + rerollOffset] = true; // reroll
            bankScores[subset] = score;
            bestScore = Math.Max(bestScore, score);
        }

        foreach (var selection in bankScores)
        {
            actionMask[selection.Key] = selection.Value == bestScore;
        }

        return actionMask;
    }

    private List<int> GetSelectedDice(int subset, int diceCount, int[] currentDice)
    {
        List<int> selectedDice = new List<int>();

        for (int dieIndex = 0; dieIndex < diceCount; dieIndex++)
        {
            bool isSelected = (subset & (1 << dieIndex)) != 0;

            if (isSelected)
            {
                selectedDice.Add(currentDice[dieIndex]);
            }
        }

        return selectedDice;
    }

    public static bool TryCalculateScore(List<int> dice, out int score)
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