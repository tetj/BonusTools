# BonusTools

A Playnite extension to import data from Steam & IGDB & Senscritique & PlayStation & Nintendo Switch.
Relies on APIs and Excel files.

Inspired by https://github.com/darklinkpower/PlayniteExtensionsCollection

**Use at your own risk, take a backup of your playnite library first !**

## Installation
1. Download the latest release from the [releases](https://github.com/tetj/BonusTools/releases)
2. Open the .pnext file

## Usage
Download instructions here : [Playnite.docx](documentation/Playnite.docx?raw=1) 

## Features
- Import numbers of reviews on Steam (IGDB*300 as fallback), **overwrites PlayCount**
- Import community score from SensCritique + Links + RatingCount*50 if PlayCount is empty, **overwrites UserScore**
- Import prices paid for games from a PlayStation data report, **overwrites Version**
- Identify PlayStation Plus Essentials (monthly) and Extra games (catalogue) as **Categories**
- Import prices paid from custom spreadsheet, **overwrites Version**

## Features for Nintendo Switch :
- Import games ROMs (.nsp files), see [ImportingSwitchNSP](documentation/ImportingSwitchNSP.md)
- Import Nintendo Switch playtime data (requires Atmosphere custom firmware), **overwrites Time Played**
- Import Yu-zu / Ryu-jinx compatibility data as **Categories**
- Fix broken Nintendo Switch ROM paths by searching for a file matching the game's ID
  Example let's say you changed the file from : SuperHydorah[**010099F00B374800**]v0.NSP to Super Hydorah [**010099F00B374800**][v262144].nsp
  This feature will search for a file matching the game's ID (010099F00B374800) and update the path in Playnite to match the new file name.
  This way, Playnite will find the file instead of marking the game as missing.
  And if you update your Emulated Folder, the game won't be detected as a new game, which would cause duplicates in your library.
