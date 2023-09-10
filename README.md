# Nucleus Co-op 

![alt text](https://github.com/SplitScreen-Me/splitscreenme-www/blob/master/static/img/hk211.png?raw=true)

Nucleus Co-op is a free and open source tool for Windows that allows split-screen play on many games that do not initially support it, the app purpose is to make it as easy as possible for the average user to play games locally using only one PC and one game copy. 

This repo is a new and improved official version of the Nucleus Co-op application and is part of the [SplitScreen.Me](https://www.splitscreen.me/docs/what-is-splitscreen-me) github organization. This new version is based off of the [Nucleus Co-op Alpha 8 Mod](https://github.com/ZeroFox5866/nucleuscoop) build and features a ton of enhancements, such as:

- New overhauled and customizable user interface with support for themes, game covers and screenshots.
- Full support for different monitor scales, UI scaling issues at more than 100% desktop scale are finally fixed (and all other issues/bugs related to it).
- New player and input order processing (left to right).
- New player nickname assignation.
- New optional splitscreen divisions setting (visually  similar to native splitscreen games).
- Massive increase to the amount of compatible games, 500+ as of now.
- Much more game handlers customization.
- Many quality of life improvements and ton of bug fixes.
- And so much more!

View the full list of features/changes and changelog in the Readme.txt inside Nucleus. Download latest [Nucleus Co-op here](https://github.com/SplitScreen-Me/splitscreenme-nucleus/releases). 

# Disclaimer
Nucleus Co-op was originally created by Lucas Assis.

- Github link to the original project: https://github.com/lucasassislar/nucleuscoop
- Official website: https://www.splitscreen.me/docs/what-is-splitscreen-me/
- Nucleus Co-op FAQ: https://www.splitscreen.me/docs/faq
- Subscribe the official Nucleus Co-op subreddit: https://www.reddit.com/r/nucleuscoop/ 
- Join the official Nucleus Co-op Discord server: https://discord.gg/QDUt8HpCvr

# How does Nucleus Co-op work?
Nucleus Co-op symlinks and opens multiple instances of the same game files (sometimes mutex killing is required for that, among other methods) that will only answer to one specific gamepad (we do this via Nucleus Co-op custom xinput libraries or xinput plus dlls) and connects those instances via LAN or online multiplayer emulation (Goldberg, Nemirtingas emulators etc.), all while making sure the game windows have focus so they can be playable at the same time with multiple controllers or that the instances are playable even in the background. Nucleus then resizes, removes borders and repositions the game windows so you can have synthetic split-screen to play locally with your friends!

Note that Nucleus does not add multiplayer or co-op to single player games, the game needs to already have some form of online or LAN multiplayer, or another way to connect the instances, like via mods for example.

# Installation:
1. Download latest [release](https://github.com/SplitScreen-Me/splitscreenme-nucleus/releases). 
2. Extract .zip archive to a non-restrictive folder, that all users have access to (i.e. do NOT extract to Program Files, Desktop, or your Documents folder, to name a few). The root folder that contains majority of your games is a good choice (e.g. C:\).

# Prerequisites:
- .NET Framework 4.7.2 or higher  
- Microsoft Visual C++ 2015-2019 Redistributable (both x86 and x64)

# How can you contribute?
Please report any bugs you may find and provide any feedback you have regarding the app. We are always open to suggestions and we want to make split-screen available for even more games, for everyone! Don't forget to create game handlers and share!

You can find us in the Nucleus subreddit/discord as well as the Splitscreen Dreams discord, a special community created specifically to split-screen games by any means.

In addition, we accept donations should you wish to support our endevor. It is greatly appreciated but completely voluntary, we will continue our best to help the community and enhance this project.

Thank you ^_^

# Credits
- Original Nucleus Co-op Project: [Lucas Assis (lucasassislar)](https://github.com/lucasassislar)  
- Nucleus Co-op Alpha 8 Mod : [ZeroFox](https://github.com/ZeroFox5866)  
- Proto Input, USS, multiple keyboards/mice & hooks: [Ilyaki](https://github.com/Ilyaki)  
- Official Nucleus Co-op 2.0 and Up: [Mikou27](https://github.com/Mikou27) 
- Website & handler API: [r-mach](https://github.com/r-mach)  
- Handlers development and general testing: [Talos91](https://github.com/Talos910), PoundlandBacon, Pizzo, dr.oldboi and many more.
  
Additional credits to all original developers of the third party utilities Nucleus Co-op uses:
- Mr_Goldberg ([Goldberg Emulator](https://gitlab.com/Mr_Goldberg/goldberg_emulator))
- syahmixp ([SmartSteamEmu](https://github.com/MAXBURAOT/SmartSteamEmu))
- atom0s ([Steamless](https://github.com/atom0s/Steamless))
- EJocys ([x360ce](https://github.com/x360ce/x360ce))
- 0dd14 Lab ([Xinput Plus](https://sites.google.com/site/0dd14lab/xinput-plus))
- r1ch ([ForceBindIP](https://r1ch.net/projects/forcebindip))
- HaYDeN ([Flawless Widescreen](https://www.flawlesswidescreen.org/))
- briankendall ([devreorder](https://github.com/briankendall/devreorder))
- VerGreeneyes ([DirectXWrapper](https://community.pcgamingwiki.com/files/file/87-the-bards-tale-2005-windowed-mode/))
- wizark952 (dinput8 blocker)
- Nemirtingas ([Epic\Galaxy Emulator](https://gitlab.com/Nemirtingas) & [OpenXinput](https://github.com/Nemirtingas/OpenXinput))
- Josivan88 (SplitCalculator)

Special thanks to the SplitScreenDreams discord community, this wouldn't have been possible without all your contributions.

