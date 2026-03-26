# Oblivion's Blade 🗡️

A fast-paced 2D action-platformer built in Unity (C#), featuring time manipulation, shadow teleportation, and a dynamic risk-reward scaling system.

![Oblivion'sBlade](https://github.com/user-attachments/assets/53c51fb3-6b01-4fcb-86b9-cbc8e7c68981)


## 📌 Overview
Inspired by high-speed action games like *Katana ZERO*, **Oblivion's Blade** follows an assassin with memory loss who utilizes death-powered abilities to navigate precision platforming and fast-paced combat. This repository highlights the custom 2D character controller, event-driven ability architecture, and scalable gameplay systems.

* **Engine:** Unity 2021.3.45f2
* **Language:** C#
* **Genre:** 2D Action-Platformer
* **Role:** Gameplay Programmer & Level Designer

## 👥 The Team
* **Lim Wei Qi:** Story Narrator, Game Programmer, Level 3 Designer, VFX Particle Designer
* **Wong Jing Le:** Story Narrator, Sprite Creator, Animator, Level 1 Designer, UI Designer, Video Editor
* **Ham Xiao Tong:** Story Narrator, Level 2 Designer, Art Designer, Audio Designer

## ⚙️ Key Technical Contributions (My Work)
If you are reviewing my code, I highly recommend checking out these specific systems located in the `Scripts/Abilities` and `Scripts/Core` folders:

* **Event-Driven Time Manipulation (`Ability_TimeStop.cs`)**: Rather than relying on rigid, global `Time.timeScale` hacks that can break physics, I implemented a decoupled broadcasting system (`GlobalEvents`). The player script triggers state changes, allowing independent enemy and hazard scripts to pause their own logic while keeping player inputs highly responsive.
* **Physics-Safe Teleportation (`Ability_PastShadow.cs`)**: Engineered a custom ability that tracks spatial states and cleanly teleports the player without clipping through colliders. Integrated decoupled camera shake (`CameraSpring.Instance`) to enhance game feel.
* **Unified Resource Management (`StaminaSystem.cs`)**: Centralized ability validation and execution. All abilities route through a unified stamina consumer that automatically handles cost calculation, error checking, and audio feedback, preventing spaghetti code in the player controller.

## 🎮 Play the Game
A playable vertical slice is available on [itch.io](https://limwq.itch.io/oblivions-blade).

## 🚀 How to Run the Project Locally
1. Clone this repository.
2. Open Unity Hub and click `Add Project from Disk`.
3. Select the cloned folder.
4. Open the boot scene located in `Assets/Scenes/` to begin playing.
