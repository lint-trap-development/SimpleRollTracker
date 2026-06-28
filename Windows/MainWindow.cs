using System;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;

namespace SimpleRollTracker.Windows;

public class MainWindow : Window, IDisposable {
    private readonly GameManager gameManager;
    private readonly Configuration config;
    private string currentLabel = string.Empty;
    private GameResult? editingGame = null;
    private string editingLabel = string.Empty;
    private string middleText = string.Empty;

    // Raffle state
    private GameMode selectedRaffleMode = GameMode.RaffleHigh;

    public MainWindow(GameManager gameManager, Configuration config) : base("SimpleRollTracker") {
        this.gameManager = gameManager;
        this.config = config;
        this.SizeConstraints = new WindowSizeConstraints { MinimumSize = new System.Numerics.Vector2(300, 400), MaximumSize = new System.Numerics.Vector2(float.MaxValue, float.MaxValue) };
    }

    public override void Draw() {
        if (ImGui.BeginTabBar("MainTabs")) {
            if (ImGui.BeginTabItem("High vs Low")) {
                DrawHighVsLowTab();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Raffle")) {
                DrawRaffleTab();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("History")) {
                DrawHistoryTab();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawClipboardBuilder(System.Collections.Generic.List<PlayerRoll> rolls) {
        if (rolls == null || rolls.Count == 0) return;

        ImGui.Separator();
        ImGui.Text("Clipboard Builder:");
        
        var sorted = rolls.OrderByDescending(r => r.Roll).ToList();
        string topName = sorted.First().PlayerName;
        string bottomName = sorted.Last().PlayerName;

        ImGui.SetNextItemWidth(120);
        ImGui.InputTextWithHint("##topName", "High Roller Name", ref topName, 100, ImGuiInputTextFlags.ReadOnly);
        
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        ImGui.InputTextWithHint("##middleText", "e.g. trades", ref middleText, 100);
        
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        ImGui.InputTextWithHint("##botName", "Low Roller Name", ref bottomName, 100, ImGuiInputTextFlags.ReadOnly);
        
        ImGui.SameLine();
        if (ImGui.Button("Copy to Clipboard")) {
            string result = $"{topName} {middleText} {bottomName}".Trim();
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ");
            ImGui.SetClipboardText(result);
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Combines the names and action into a single sentence and copies it to your clipboard.");
    }

    private void DrawSharedControls(GameMode mode) {
        if (!gameManager.IsActive) {
            if (ImGui.Button("Start Recording")) {
                gameManager.StartRecording(mode);
            }
        } else {
            if (ImGui.Button("Stop Recording")) {
                gameManager.StopRecording(currentLabel);
                currentLabel = string.Empty;
            }
        }

        ImGui.SameLine();
        if (gameManager.IsActive) ImGui.BeginDisabled();
        
        bool randomOnly = config.RandomOnly;
        if (ImGui.Checkbox("\"/random\" only", ref randomOnly)) {
            config.RandomOnly = randomOnly;
            config.Save();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("If checked, only pure '/random' (out of 999) rolls will be recorded.");

        ImGui.SameLine();
        bool oneRoll = config.OneRollOnly;
        if (ImGui.Checkbox("One roll only", ref oneRoll)) {
            config.OneRollOnly = oneRoll;
            config.Save();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("If checked, ignores any subsequent rolls from the same player during a single session.");

        if (gameManager.IsActive) ImGui.EndDisabled();
        
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        ImGui.InputTextWithHint("##SessionLabel", "Session Label (Optional)", ref currentLabel, 100);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("A custom name for this session to easily identify it in the History tab.");
    }

    private void DrawHighVsLowTab() {
        DrawSharedControls(GameMode.HighVsLow);
        ImGui.Separator();

        System.Collections.Generic.List<PlayerRoll> rollsToDisplay = null;

        if (!gameManager.IsActive) {
            ImGui.Text("Most Recent Results:");
            var lastGame = config.HistoricGames.LastOrDefault(g => g.Mode == GameMode.HighVsLow);
            if (lastGame != null) {
                rollsToDisplay = lastGame.Rolls;
            } else {
                ImGui.Text("No results yet.");
            }
        } else {
            if (gameManager.CurrentMode != GameMode.HighVsLow) {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), $"Currently recording a {gameManager.CurrentMode} session.");
                return;
            }
            var elapsed = DateTime.Now - (gameManager.StartTime ?? DateTime.Now);
            ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), $"Recording rolls... [{elapsed:mm\\:ss}]");
            rollsToDisplay = gameManager.CurrentRolls.ToList();
        }

        if (rollsToDisplay != null) {
            DrawRollsTable(rollsToDisplay);
            DrawClipboardBuilder(rollsToDisplay);
        }
    }

    private void DrawRaffleTab() {
        if (gameManager.IsActive && gameManager.CurrentMode != GameMode.HighVsLow) {
            selectedRaffleMode = gameManager.CurrentMode; // lock visual mode to active mode
        }
        
        DrawSharedControls(selectedRaffleMode);
        
        if (gameManager.IsActive) ImGui.BeginDisabled();
        ImGui.SetNextItemWidth(150);
        if (ImGui.BeginCombo("Raffle Mode", selectedRaffleMode.ToString())) {
            foreach (GameMode mode in Enum.GetValues(typeof(GameMode))) {
                if (mode == GameMode.HighVsLow) continue;
                if (ImGui.Selectable(mode.ToString(), mode == selectedRaffleMode)) {
                    selectedRaffleMode = mode;
                }
            }
            ImGui.EndCombo();
        }
        if (gameManager.IsActive) ImGui.EndDisabled();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Select the winning condition:\nRaffleHigh: Highest roll wins.\nRaffleLow: Lowest roll wins.\nRaffleClosest: The roll closest to the Target Roll wins.");
        
        if (selectedRaffleMode == GameMode.RaffleClosest) {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            int tr = gameManager.TargetRoll;
            ImGui.BeginDisabled();
            ImGui.InputInt("Target Roll", ref tr, 0, 0);
            ImGui.EndDisabled();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("The target roll is automatically set to your first roll after starting the session.");
        }

        ImGui.Separator();

        if (!gameManager.IsActive) {
            ImGui.Text("Most Recent Results:");
            var lastGame = config.HistoricGames.LastOrDefault(g => g.Mode != GameMode.HighVsLow);
            if (lastGame != null) {
                DrawRaffleTable(lastGame.Rolls, lastGame.Mode, lastGame.TargetRoll);
            } else {
                ImGui.Text("No results yet.");
            }
        } else {
            if (gameManager.CurrentMode == GameMode.HighVsLow) {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "Currently recording a High vs Low session.");
                return;
            }
            var elapsed = DateTime.Now - (gameManager.StartTime ?? DateTime.Now);
            ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), $"Recording rolls... [{elapsed:mm\\:ss}]");
            DrawRaffleTable(gameManager.CurrentRolls.ToList(), gameManager.CurrentMode, gameManager.TargetRoll);
        }
    }

    private void DrawRaffleTable(System.Collections.Generic.List<PlayerRoll> rolls, GameMode mode, int target) {
        if (rolls.Count == 0) {
            ImGui.Text("No rolls collected.");
            return;
        }

        System.Collections.Generic.List<PlayerRoll> sorted;
        if (mode == GameMode.RaffleHigh) {
            sorted = rolls.OrderByDescending(r => r.Roll).ToList();
        } else if (mode == GameMode.RaffleLow) {
            sorted = rolls.OrderBy(r => r.Roll).ToList();
        } else {
            // Closest To
            sorted = rolls.OrderBy(r => Math.Abs(r.Roll - target)).ToList();
        }

        if (ImGui.BeginTable("RaffleTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Roll");
            ImGui.TableSetupColumn("Cmd");
            ImGui.TableHeadersRow();
            for (int i = 0; i < sorted.Count; i++) {
                if (i == 0) {
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new System.Numerics.Vector4(0.2f, 0.6f, 0.2f, 0.6f)));
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                if (ImGui.Selectable(sorted[i].PlayerName + "##raff" + i, false)) {
                    ImGui.SetClipboardText(sorted[i].PlayerName);
                }
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("Click to copy name to clipboard");
                }
                ImGui.TableNextColumn();
                if (mode == GameMode.RaffleClosest && i == 0) {
                    ImGui.Text($"{sorted[i].Roll} (Diff: {Math.Abs(sorted[i].Roll - target)})");
                } else {
                    ImGui.Text(sorted[i].Roll.ToString());
                }
                ImGui.TableNextColumn();
                ImGui.Text(sorted[i].OutOf == 999 ? "/random" : $"/random {sorted[i].OutOf}");
            }
            ImGui.EndTable();
        }
    }

    private void DrawRollsTable(System.Collections.Generic.List<PlayerRoll> rolls) {
        if (rolls.Count == 0) {
            ImGui.Text("No rolls collected.");
            return;
        }

        var sorted = rolls.OrderByDescending(r => r.Roll).ToList();
        int half = (int)Math.Ceiling(sorted.Count / 2.0);
        
        var topHalf = sorted.Take(half).ToList();
        var bottomHalf = sorted.Skip(half).OrderBy(r => r.Roll).ToList();

        if (ImGui.BeginTable("SplitTable", 2, ImGuiTableFlags.BordersInnerV)) {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            
            var width = ImGui.GetContentRegionAvail().X;
            var textWidth = ImGui.CalcTextSize("Top Half (High to Low)").X;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (width - textWidth) * 0.5f);
            ImGui.Text("Top Half (High to Low)");
            
            if (ImGui.BeginTable("TopHalfTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) {
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Roll");
                ImGui.TableSetupColumn("Cmd");
                ImGui.TableHeadersRow();
                for (int i = 0; i < topHalf.Count; i++) {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    if (ImGui.Selectable(topHalf[i].PlayerName + "##top" + i, false)) {
                        ImGui.SetClipboardText(topHalf[i].PlayerName);
                    }
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip("Click to copy name to clipboard");
                    }
                    ImGui.TableNextColumn();
                    ImGui.Text(topHalf[i].Roll.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(topHalf[i].OutOf == 999 ? "/random" : $"/random {topHalf[i].OutOf}");
                }
                ImGui.EndTable();
            }

            ImGui.TableNextColumn();
            var botWidth = ImGui.GetContentRegionAvail().X;
            var botTextWidth = ImGui.CalcTextSize("Bottom Half (Low to High)").X;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (botWidth - botTextWidth) * 0.5f);
            ImGui.Text("Bottom Half (Low to High)");
            
            if (ImGui.BeginTable("BottomHalfTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) {
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Roll");
                ImGui.TableSetupColumn("Cmd");
                ImGui.TableHeadersRow();
                for (int i = 0; i < bottomHalf.Count; i++) {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    if (ImGui.Selectable(bottomHalf[i].PlayerName + "##bot" + i, false)) {
                        ImGui.SetClipboardText(bottomHalf[i].PlayerName);
                    }
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip("Click to copy name to clipboard");
                    }
                    ImGui.TableNextColumn();
                    ImGui.Text(bottomHalf[i].Roll.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(bottomHalf[i].OutOf == 999 ? "/random" : $"/random {bottomHalf[i].OutOf}");
                }
                ImGui.EndTable();
            }
            ImGui.EndTable();
        }
    }

    private void DrawHistoryTab() {
        if (config.HistoricGames.Count == 0) {
            ImGui.Text("No history recorded.");
            return;
        }
        var games = config.HistoricGames.OrderByDescending(g => g.Timestamp).ToList();
        foreach (var game in games) {
            string modeStr = game.Mode == GameMode.HighVsLow ? "HighVsLow" : game.Mode.ToString().Replace("Raffle", "Raffle: ");
            string header = $"{game.Timestamp} [{modeStr}]";
            if (editingGame != game && !string.IsNullOrWhiteSpace(game.Label)) {
                header += $" - {game.Label}";
            }
            
            bool isOpen = false;
            int columns = editingGame == game ? 3 : 2;
            
            if (ImGui.BeginTable("HistoryTable_" + game.Timestamp.Ticks, columns)) {
                ImGui.TableSetupColumn("Header", editingGame == game ? ImGuiTableColumnFlags.WidthFixed : ImGuiTableColumnFlags.WidthStretch, 250f);
                if (editingGame == game) {
                    ImGui.TableSetupColumn("Input", ImGuiTableColumnFlags.WidthStretch);
                }
                ImGui.TableSetupColumn("Buttons", ImGuiTableColumnFlags.WidthFixed, 60f);
                
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                
                isOpen = ImGui.CollapsingHeader(header + "###" + game.Timestamp.Ticks);
                
                if (editingGame == game) {
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputTextWithHint("##editLabel" + game.Timestamp.Ticks, "Session Label", ref editingLabel, 100);
                }
                
                ImGui.TableNextColumn();
                if (editingGame == game) {
                    if (ImGuiComponents.IconButton((int)game.Timestamp.Ticks + 2, FontAwesomeIcon.Check)) {
                        game.Label = editingLabel;
                        config.Save();
                        editingGame = null;
                    }
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton((int)game.Timestamp.Ticks + 3, FontAwesomeIcon.Times)) {
                        editingGame = null;
                    }
                } else {
                    if (ImGuiComponents.IconButton((int)game.Timestamp.Ticks, FontAwesomeIcon.Pen)) {
                        editingGame = game;
                        editingLabel = game.Label;
                    }
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton((int)game.Timestamp.Ticks + 1, FontAwesomeIcon.Trash)) {
                        config.HistoricGames.Remove(game);
                        config.Save();
                    }
                }
                ImGui.EndTable();
            }

            if (isOpen) {
                if (game.Mode == GameMode.HighVsLow) {
                    DrawRollsTable(game.Rolls);
                    DrawClipboardBuilder(game.Rolls);
                } else {
                    DrawRaffleTable(game.Rolls, game.Mode, game.TargetRoll);
                }
            }
        }
    }

    public void Dispose() { }
}
