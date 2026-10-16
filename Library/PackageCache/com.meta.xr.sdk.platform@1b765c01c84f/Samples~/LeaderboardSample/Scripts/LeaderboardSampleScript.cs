// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardSampleScript : MonoBehaviour
{
    //TODO: replace "sample_leaderboard_visible" with your leaderboard name
    private const string LEADERBOARD_NAME = "sample_leaderboard_visible";
    private const int ROWS_TO_RETRIEVE = 20;

    [SerializeField]
    private GameObject leaderboardContent;

    [SerializeField]
    private ScrollRect leaderboardScrollView;

    [SerializeField]
    private LeaderboardRow rowPrefab;

    [SerializeField]
    private InputField inputField;

    private void Start()
    {
        Core.Initialize();
        Entitlements.IsUserEntitledToApplication().OnComplete((Message m) =>
        {
            Debug.Log(m.IsError);
        });
    }

    // Fill the leaderboard with real entries in the platform app
    public void FillLeaderboardRealEntries()
    {
        // Clear the leaderboard view first
        ClearLeaderboardEntries();

        // Call platform API to get entries.
        Leaderboards.GetEntries(LEADERBOARD_NAME, ROWS_TO_RETRIEVE, LeaderboardFilterType.None, LeaderboardStartAt.Top)
                .OnComplete((Message<LeaderboardEntryList> message) =>
        {
            foreach (LeaderboardEntry entry in message.Data)
            {
                CreateRow(entry.User.OculusID, entry.Score.ToString());
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(leaderboardScrollView.content);
        });
    }

    // Fill the leaderboard with mocked entries to see how the leaderboard function
    public void FillLeaderboardMockEntries()
    {
        ClearLeaderboardEntries();
        for (int i = 0; i < ROWS_TO_RETRIEVE; i++)
        {
            CreateRow($"User{i}", (100 - i).ToString());
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(leaderboardScrollView.content);
    }

    // Write a leaderboard entry for the current logged in account with the score in the input field
    public void WriteLeaderboardScore()
    {
        string inputValue = inputField.text;
        if (int.TryParse(inputValue, out int score))
        {
            // Call platform API to write entry for current account.
            Leaderboards.WriteEntry(LEADERBOARD_NAME, score, null, true).OnComplete(msg =>
            {
                Debug.Log("wrote score: " + msg.Data);
            });

            ClearLeaderboardEntries();
        }
        else
        {
            Debug.LogError("Parse input score value failed, please enter an integer value");
        }
    }

    // Clear all the entries in the leaderboard UI
    public void ClearLeaderboardEntries()
    {
        foreach (Transform child in leaderboardContent.transform)
        {
            if (child.name != "Leaderboard Title")
            {
                Destroy(child.gameObject);
            }
        }
    }

    // Create row object based on name and score
    private void CreateRow(string name, string score)
    {
        LeaderboardRow leaderboardRow = Instantiate(rowPrefab, leaderboardContent.transform);
        leaderboardRow.Setup(name, score);
    }
}
