using System;
using System.Collections.Generic;

namespace SimpleRollTracker;

public enum GameMode {
    TruthOrDare,
    RaffleHigh,
    RaffleLow,
    RaffleClosest
}

public class PlayerRoll {
    public string PlayerName { get; set; } = string.Empty;
    public int Roll { get; set; }
    public int OutOf { get; set; } = 999;
}

public class GameResult {
    public DateTime Timestamp { get; set; }
    public string Label { get; set; } = string.Empty;
    public GameMode Mode { get; set; } = GameMode.TruthOrDare;
    public int TargetRoll { get; set; } = 0;
    public List<PlayerRoll> Rolls { get; set; } = new();
}
