# Lazy Doctor

This is a Windows app I made to simplify my daily maintenance tasks in Arknights.

Tech stack:
- [Avalonia](https://avaloniaui.net) (with [FluentAvalonia](https://github.com/amwx/FluentAvalonia)) as frontend (plus [HotAvalonia](https://github.com/Kir-Antipov/HotAvalonia) to save on iteration time)
- [ReactiveUI](https://www.reactiveui.net/) + [DynamicData](https://github.com/reactivemarbles/DynamicData) for view/viewmodel bindings and collections plumbing
- [ReactiveMarbles.CacheDatabase](https://github.com/reactivemarbles/CacheDatabase) (really just [Akavache](https://github.com/reactiveui/Akavache) but this reimplementation lets me use System.Text.Json)
- [GitHub API](https://docs.github.com/en/rest/repos/contents) to fetch/update game data
- [WinRT](https://en.wikipedia.org/wiki/Windows_Runtime) for OCR and clipboard access

## Features
### Recruitment Calculator
Classic recruitment calculator. I don't want to remember tag combinations, my brain is busy being full of air.

![image](https://github.com/user-attachments/assets/998d8007-69f0-418d-8922-659bbb65606b)

#### OCR
Having to Alt-Tab away to [Aceship](https://aceship.github.io/AN-EN-Tags/akhr.html) or [akgcc](https://akgcc.github.io/cc/recruit.html) is slow. Visually locating the tags you need to click and... clicking them... no, too much work.

Just grab a screenshot of the tags (e.g. with [Snipping Tool](https://support.microsoft.com/en-us/windows/use-snipping-tool-to-capture-screenshots-00246869-1843-655f-f220-97299b865f6b#ID0EDZBBBDD)) and immediately see the results.<br/>
The OCR engine used is internal to Windows. This means that, to OCR a specific language, you need to install the corresponding language pack in Windows.<br/>
You can also use your own OCR, if it's better - it just needs to copy its output to the clipboard.

> [!TIP]
> `Alt+PrintScreen` will capture the active window. This is just one keystroke and doesn't need the mouse.<br/>
> You can also use something like [keymapper](https://github.com/houmain/keymapper/) or [AutoHotkey](https://www.autohotkey.com/) to trigger it with one key instead of two - but that's beyond lazy, even for me.

#### Rarity filters
> "Ah yes, I would certainly like to see which combinations of these tags guarantee me a 3★"<br/>
> (said nobody ever)

![image](https://github.com/user-attachments/assets/5f00c753-d567-4d8a-b2df-c0b95b6e0dbc)

You can choose whether you want to see results for rarities below 4★.<br/>
The defaults (hide 1★/2★, exclude 3★) correspond to the typical daily recruitment (at least 7h40m to remove 1★/2★, only care about 4★ for yellow certs).<br/>
Selecting the `Robot` or `Starter` tags manually will include them in results regardless of the filter.
#### Updates
When new operators are added to recruitment... I don't want to have to do anything. Me having to publish an update... you having to download it... slow and inconvenient. If I get hit by a bus tomorrow, why should your app stop working?

So, the app automatically pulls the most recent data from [Kengxxiao's data repository](https://github.com/Kengxxiao/ArknightsGameData_YoStar/tree/main/en_US). It also picks up any new tags, though new tags should get added basically never (`Elemental` is the only tag added post-release so far).

### Resource Planner

Have you ever wondered how many Guard chips you would have if you farmed nothing but Guard chips for three straight weeks? Is it 61? Or maybe 63? What an exciting question.

It all started with this [spreadsheet](https://docs.google.com/spreadsheets/d/17pi3KdViPWyNmGcCdPMmZzlS1sFKgOiftdiAheA8Qr0/edit), motivated by not knowing whether I'll be able to farm up enough red certs from AP-5 to buy out the 6★ tokens before they rotate out of the shop. Well, never again.

This planner simulates a very normal being whose sole purpose is to dump as much sanity as humanly possible into a single supply stage of your choosing. It takes into account things like:
- When the stage is open (including always-open periods like CC[^1])
- Saving sanity for tomorrow if today the stage is closed but tomorrow it's open (it adds up!)
- Banked sanity from potions or OP (including usual reoccurring gains like potions from weekly missions and the daily potion from monthly card)
- A bunch of extra details like EXP gains/leveling up, annihilation, etc.

[^1]: The data for these periods usually doesn't appear until very close to/after the event starts. So, you should probably re-calculate the sim when the event comes out.

The sim is probably not *completely* optimal, but I'd argue you still get way better results with this planner than without. Just don't forget to set an alarm to spend sanity ;)

#### Updates

Everything about the game data mentioned in the Recruitment Calculator section above applies here as well. If the schedule for the stages suddenly changes, the planner will automatically pull those changes.

## Roadmap
Right now these are all from spreadsheets I made over the years - I just need to find the time and motivation to port them over.
- IS relic tracker/completion checklist (spreadsheets - [IS2](https://docs.google.com/spreadsheets/d/1XjMUNHfIUqqRiXLNeKoioEg64PneYOUGhHBHw58fNDg/edit), [IS3](https://docs.google.com/spreadsheets/d/1g7PBeaU0BrAJ25g8Xj43Jyu11zZUXCbK44XimNgAc8w/edit), [IS4](https://docs.google.com/spreadsheets/d/1ulX-GO5D9PM9_5tX_gnzgLyp7_W7zslFQBiYg9vFqSY/edit))
	- Rawdogging the relic collection process is like licking sandpaper
- Pulls calculator+ ([spreadsheet](https://docs.google.com/spreadsheets/d/1JbBpZoj2q6gf7VtKlZArBx-e4UuNR1UEjS7AlCXTmME/edit) - based on [u/lhc987's spreadsheet](https://docs.google.com/spreadsheets/d/12nugJxtTLFafudEJ_NrFuEm2X4VHrfCRkbyzMxia63A/edit))
    - It's a pain to maintain it even with me only checking in basically once every ~~three~~ six months
    - Probably not going to be ported over unless I manage to automate pulling data for future events (if you have/know of an available API, feel free to open an issue)
