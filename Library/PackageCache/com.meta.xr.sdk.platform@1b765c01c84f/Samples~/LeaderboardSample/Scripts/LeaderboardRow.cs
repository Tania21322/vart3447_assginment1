// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.UI;

public class LeaderboardRow : MonoBehaviour
{
    [SerializeField]
    private Text nameLabel;

    [SerializeField]
    private Text scoreLabel;

    public void Setup(string name, string score)
    {
        nameLabel.text = name;
        scoreLabel.text = score;
    }
}
