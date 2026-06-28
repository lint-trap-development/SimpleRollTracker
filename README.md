# SimpleRollTracker

A lightweight, efficient, and feature-rich `/random` roll tracker plugin for Final Fantasy XIV (via Dalamud). Built for players who frequently host or participate in deathrolls, giveaways, raffles, or any event requiring organized dice tracking.

## Features

- **Live Roll Tracking:** Start a recording session and instantly capture all `/random` rolls in chat.
- **Multiple Game Modes:** Support for standard Truth or Dare tracking, as well as High Roller, Low Roller, and "Closest To" raffle modes.
- **Split Pane Organization:** Automatically separates the upper half (High Rollers) and lower half (Low Rollers) into two beautifully aligned tables.
- **Clipboard Builder:** A persistent sticky footer that lets you effortlessly click a high roller, click a low roller, and instantly stitch them together with custom text (e.g., `PlayerA trades PlayerB`) straight to your clipboard.
- **Session History:** Automatically saves your past sessions. View, rename, or delete past events at any time on the History tab.
- **Live Timer:** Tracks exactly how long your recording session has been running.
- **Smart Filters:**
  - `"/random" only`: Ignore custom typed rolls or emotes, strictly capturing system-generated pure `/random` (999) results.
  - `One roll only`: Automatically ignores subsequent rolls from the same player during a single session.

## Installation

This plugin is available in the official Dalamud Plugin Repository.

1. Open the Plugin Installer (`/xlplugins`).
2. Search for **SimpleRollTracker**.
3. Click Install!

## Usage

Type `/rolltracker` in chat to open the main window.

### Truth or Dare & Raffle Tabs
- Enter an optional **Label** (e.g., "FC Giveaway 2026") to easily identify the session later.
- Choose your mode on the **Raffle** tab if applicable (High Roller, Low Roller, Closest To).
- Click **Start Recording** to begin listening to chat. The timer will begin ticking.
- When all rolls are cast, click **Stop Recording**. The final list will be frozen and automatically saved to your History.

### Clipboard Builder
At the bottom of the window is a permanent Clipboard Builder. 
1. Click a name in the High Rollers table to populate the left box.
2. Click a name in the Low Rollers table to populate the right box.
3. Type any action into the middle box (e.g., `owes 10k to`).
4. Click **Copy to Clipboard**. The plugin will safely filter out any double spaces and format your text perfectly for pasting into FFXIV chat.

### History Tab
- Expand any previous session to view the exact rolls and tables.
- Click the **Pencil Icon** to edit the label for that session.
- Click the **Trash Icon** to permanently delete the record.
