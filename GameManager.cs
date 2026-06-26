using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Text;

namespace SimpleRollTracker;

/// <summary>
/// Core logic for parsing chat messages and tracking player rolls.
/// Manages the active recording session and maintains the state of the current game.
/// </summary>
public class GameManager : IDisposable {
    private readonly Configuration config;
    public bool IsActive { get; private set; }
    public DateTime? StartTime { get; private set; }
    public List<PlayerRoll> CurrentRolls { get; private set; } = new();
    private readonly Regex rollRegex = new Regex(@"Random! (.*?) roll(?:s)? a (\d+)", RegexOptions.Compiled);

    public GameManager(Configuration config) {
        this.config = config;
    }

    public void Dispose() {
        if (IsActive) {
            Services.ChatGui.ChatMessage -= OnChatMessage;
        }
    }

    public void StartRecording() {
        CurrentRolls.Clear();
        IsActive = true;
        StartTime = DateTime.Now;
        Services.ChatGui.ChatMessage += OnChatMessage;
    }

    public void StopRecording(string label) {
        if (!IsActive) return;
        IsActive = false;
        Services.ChatGui.ChatMessage -= OnChatMessage;

        var result = new GameResult {
            Timestamp = DateTime.Now,
            Label = label,
            Rolls = CurrentRolls.ToList()
        };

        if (result.Rolls.Count > 0) {
            config.HistoricGames.Add(result);
            config.Save();
        }
    }

    private void OnChatMessage(Dalamud.Game.Chat.IHandleableChatMessage chatMessage) {
        if (!IsActive) return;

        var text = chatMessage.Message.TextValue;
        var match = rollRegex.Match(text);
        
        if (chatMessage.LogKind == XivChatType.RandomNumber) {
            Services.PluginLog.Debug($"[RollTracker] RandomNumber detected. Match success: {match.Success}");
        }

        if (match.Success) {
            string playerName = match.Groups[1].Value.Trim();
            
            // If the local player rolled, the message says "You". Resolve this to their actual name.
            if (playerName.Equals("You", StringComparison.OrdinalIgnoreCase)) {
                if (Services.ObjectTable.LocalPlayer != null) {
                    playerName = Services.ObjectTable.LocalPlayer.Name.TextValue;
                }
            }

            // Players from other worlds have their server name appended with a capital letter (e.g. KuroganeExcalibur)
            // FFXIV player names strictly forbid internal capitals, so we can strip anything starting with a capital after a lowercase letter.
            playerName = Regex.Replace(playerName, @"([a-z])[A-Z][a-zA-Z]*$", "$1");

            int roll = int.Parse(match.Groups[2].Value);
            int max = 999;
            
            // Try to find if there is a custom maximum
            var outOfMatch = Regex.Match(text, @"out of (\d+)");
            if (outOfMatch.Success) {
                max = int.Parse(outOfMatch.Groups[1].Value);
            }

            if (config.RandomOnly && max != 999) {
                Services.PluginLog.Debug($"[RollTracker] Ignored modified roll ({max}) due to RandomOnly rule.");
                return;
            }
            
            Services.PluginLog.Debug($"[RollTracker] Parsed roll from {playerName}: {roll}/{max}");
            
            if (config.OneRollOnly && CurrentRolls.Any(r => r.PlayerName == playerName)) {
                Services.PluginLog.Debug($"[RollTracker] Duplicate roll ignored for {playerName} due to OneRollOnly rule.");
                return;
            }
            
            CurrentRolls.Add(new PlayerRoll { PlayerName = playerName, Roll = roll, OutOf = max });
            Services.PluginLog.Debug($"[RollTracker] Roll recorded for {playerName}");
        }
    }
}
