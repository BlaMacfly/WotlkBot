# WotlkBot - WotLK 3.3.5a Botting Solution

![WotlkBot GUI](https://raw.githubusercontent.com/Artanidos/WotlkBot/master/Gui/screen.png)

> **Forked from [Artanidos/WotlkBot](https://github.com/Artanidos/WotlkBot)**  
> **Major Update:** Complete GUI Redesign ("Obsidian Theme"), Dashboard Layout, and Modular AI.

WotlkBot allows you to turn your World of Warcraft 3.3.5a characters into intelligent bots that can join your party, fight, heal, and quest alongside you. It is designed for private servers (e.g. AzerothCore) to enable solo play of dungeons and raids.

## 🚀 New Features (v2.0)

*   **Modern GUI Redesign**: A sleek, dark-themed "Obsidian" interface inspired by VS Code.
*   **Dashboard View**: Real-time status monitoring of all your bots in a single card-based layout.
*   **Modular AI**:
    *   **Healer Bot**: Intelligent healing priorities.
    *   **Combat Modules**: Pluggable strategies for different classes (Warrior, Paladin, Mage, etc.).
*   **Service-Based Architecture**: Separation of UI and Logic for better stability.

## 🛠️ Usage

### Prerequisites
*   Windows OS
*   .NET Framework 4.8.1 (or compatible runtime)
*   A World of Warcraft 3.3.5a Account (on a private server)

### Getting Started
1.  **Clone** this repository.
2.  **Open** `WotlkBot.sln` in Visual Studio 2019/2022.
3.  **Build** the `WotlkBotGui` project.
4.  **Run** `WotlkBotGui.exe`.

### Configuration
*   **Settings Tab**: Set your Realmlist (Host) and Master Character Name.
*   **Dashboard**: Click **"+ Add Bot"** to configure a new bot account.
    *   Select Class (Warrior, Priest, etc.).
    *   Enter Account Name / Password.
    *   Click **Start** on the bot card to connect.

## 🤖 Bot Features

*   **Follow Master**: Automatically follows the configured master character.
*   **Combat**: Attacks targets engaged by the master.
*   **Chat Commands**: Reacts to whispers (e.g., "buff", "mount", "come").
*   **Auto-Loot**: (Experimental) Loots mobs in range.

## 🤝 Contributing

Contributions are welcome! Please submit Pull Requests to the [GitHub Repository](https://github.com/BlaMacfly/WotlkBot).

## 📄 License

This project is open-source. Please credit the original authors when forking.
