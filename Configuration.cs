using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace SimpleRollTracker;

/// <summary>
/// Manages persistent storage of plugin settings and historical game records.
/// </summary>
[Serializable]
public class Configuration : IPluginConfiguration {
    public int Version { get; set; } = 0;
    
    [JsonProperty] public bool RandomOnly { get; set; } = true;
    [JsonProperty] public bool OneRollOnly { get; set; } = true;
    
    [JsonProperty] public List<GameResult> HistoricGames = new();

    public void Save() {
        Services.PluginInterface.SavePluginConfig(this);
    }
}
