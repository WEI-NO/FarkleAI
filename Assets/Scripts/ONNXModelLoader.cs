using System;
using System.Collections;
using System.Collections.Generic;
using Unity.InferenceEngine;
using UnityEngine;

public class ONNXModelLoader : MonoBehaviour
{
    private const int ActionCount = 128;
    private const int ObservationSize = 10;

    [SerializeField] private ModelAsset onnxModel;

    private Worker worker; // Runs the model

    private void Awake()
    {
        if (onnxModel == null)
        {
            Debug.LogError("ONNX model is not assigned");
            return;
        }

        Model runtimeModel = ModelLoader.Load(onnxModel);

        worker = new Worker(runtimeModel, BackendType.CPU);
    }


    public int Predict(float[] observation, bool[] validActions)
    {
        ValidateInputs(observation, validActions);

        var actionMask = ConvertMaskToFloats(validActions);

        using var observationTensor = new Tensor<float>(
            new TensorShape(1, ObservationSize),
            observation
        );

        using var actionMaskTensor = new Tensor<float>(
            new TensorShape(1, ActionCount),
            actionMask
        );

        worker.SetInput("observation", observationTensor);
        worker.SetInput("action_mask", actionMaskTensor);

        worker.Schedule();

        Tensor<float> outputTensor = worker.PeekOutput("masked_logits") as Tensor<float>;

        

        if (outputTensor == null)
        {
            Debug.LogError("Failed to get output tensor from the model.");
            return -1;
        }

        float[] actionScores = outputTensor.DownloadToArray();

        return FindHighestLegalScoreIndex(actionScores, validActions);
    }


    private void TestModel()
    {
        int[] dice = {
            2, 3, 4, 5, 6, 6
        };

        float[] observation = {
            0f,          // Banked score
            0f,          // Unbanked score
            1f,          // Six dice remaining
            0f,          // Opponent score

            dice[0] / 6f,
            dice[1] / 6f,
            dice[2] / 6f,
            dice[3] / 6f,
            dice[4] / 6f,
            dice[5] / 6f
        };

        bool[] validActions = new bool[ActionCount];

        validActions[8] = true;   // Select index 3 and bank
        validActions[72] = true;  // Select index 3 and reroll

        int selectedAction = Predict(
            observation,
            validActions
        );

        if (!validActions[selectedAction])
        {
            Debug.LogError(
                $"The model selected invalid action {selectedAction}."
            );
            return;
        }

        Debug.Log(
            $"Model selected action {selectedAction}: " +
            DescribeAction(selectedAction, dice)
        );
    }

    private static float[] ConvertMaskToFloats(bool[] validActions)
    {
        float[] actionMask = new float[ActionCount];

        for (int i = 0; i < ActionCount; i++)
        {
            actionMask[i] = validActions[i] ? 1f : 0f;
        }

        return actionMask;
    }

    private static int FindHighestLegalScoreIndex(
        float[] actionScores,
        bool[] validActions)
    {
        int bestAction = -1;
        float highestScore = float.NegativeInfinity;

        for (int i = 0; i < actionScores.Length; i++)
        {
            if (!validActions[i])
                continue;

            if (float.IsNaN(actionScores[i]))
                continue;

            if (actionScores[i] > highestScore)
            {
                highestScore = actionScores[i];
                bestAction = i;
            }
        }

        if (bestAction == -1)
        {
            throw new InvalidOperationException(
                "The model could not select a legal action."
            );
        }

        return bestAction;
    }

    private static void ValidateInputs(float[] observation, bool[] validActions)
    {
        if (observation == null)
        {
            throw new ArgumentNullException(
                nameof(observation)
            );
        }

        if (observation.Length != ObservationSize)
        {
            throw new ArgumentException(
                $"Observation must contain exactly " +
                $"{ObservationSize} values."
            );
        }

        if (validActions == null)
        {
            throw new ArgumentNullException(
                nameof(validActions)
            );
        }

        if (validActions.Length != ActionCount)
        {
            throw new ArgumentException(
                $"Action mask must contain exactly " +
                $"{ActionCount} values."
            );
        }

        bool hasValidAction = false;

        for (int i = 0; i < validActions.Length; i++)
        {
            if (validActions[i])
            {
                hasValidAction = true;
                break;
            }
        }

        if (!hasValidAction)
        {
            throw new ArgumentException(
                "The action mask must contain at least one valid action."
            );
        }
    }
    public static string DescribeAction(int action, IReadOnlyList<int> dice)
    {
        bool shouldBank = action < 64;
        int selectionBits = action % 64;

        List<int> selectedIndices = new();
        List<int> selectedValues = new();

        for (int dieIndex = 0; dieIndex < dice.Count; dieIndex++)
        {
            bool isSelected = (selectionBits & (1 << dieIndex)) != 0;

            if (isSelected)
            {
                selectedIndices.Add(dieIndex);
                selectedValues.Add(dice[dieIndex]);
            }
        }

        string decision = shouldBank ? "Bank" : "Reroll";

        return
            $"{decision}; " +
            $"selected indices: [{string.Join(", ", selectedIndices)}], " +
            $"values: [{string.Join(", ", selectedValues)}]";
    }
}
