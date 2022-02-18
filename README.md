# Blackjack - WORK IN PROGRESS

Simple blackjack plugin for Dalamud. Requires set macros to use and work properly.

## Features

* Simple blackjack plugin
  * Totals Bets
  * One table UI
  * REQUIRES MACROS TO USE

## Installing Repo
While this project is still a work in progress, you can use it by addin the following URL to the custom plugin repositories list in your Dalamud settings
1. `/xlsettings` -> Experimental tab
2. Copy and paste the repo.json link below
3. Click on the + button
4. Click on the "Save and Close" button
5. You will now see Penumbra listed in the Dalamud Plugin Installer

### Activating in-game

1. Next, use `/xlplugins` (chat) or `xlplugins` (console) to open up the Plugin Installer.
2. You should now be able to use `/blackjack` (chat) or `blackjack` (console)!

https://raw.githubusercontent.com/Deckerz/Plugins/main/repo.json

## MACROS

They can be set up however you want but they must do the following to resolve the criteria to active.

##### New Game Macro

- Can be anything but must have these words in it `New Game`

#### Bet macro

- has to be typed manually each time `<t> bets <amount>`
  - i.e. `<t> bets 500k`, `<t> bets 69069`, `<t> bets 2m`

#### Lock-in the Table

- This simple stops you accidentally adding more players with miss-clicks of macros
- Usage: must contain the words `all bets placed`

#### Make it so next roll applies to dealers hand

- Any of the following words do this: `reveal the dealers`, `dealers starting card`, `dealer hits`

#### Dealer Stand or Bust (This allow stops the table being edited and calculates the winnings)

- Simple have a macro that says either of the following
  - `dealer stands`
  - `dealer bust`

#### Making a player stand or bust

- must contain the word `stand` or `bust`
  - i.e. `<t> stands`, `<t> is bust!`

#### Player Split

- This macro must contain `chooses to split`
  - Afterwards you MUST roll 2 dice to hit both hands

#### Player doubles down

- This macro must contain `chooses double down`

#### Player rolls the dice

- simply type `/dice party 13` while targeting the player you want to draw a card for


## Issues

- If you have any requests or problems create a issue on github to ask :)