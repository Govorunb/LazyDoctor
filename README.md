# Lazy Doctor

This is a Windows app I made to simplify my daily maintenance tasks in Arknights.

Tech stack:
- [Avalonia](https://avaloniaui.net) (with [FluentAvalonia](https://github.com/amwx/FluentAvalonia)) as frontend
- [ReactiveUI](https://www.reactiveui.net/) + [DynamicData](https://github.com/reactivemarbles/DynamicData) for view/viewmodel bindings and collections plumbing
- [ReactiveMarbles.CacheDatabase](https://github.com/reactivemarbles/CacheDatabase) (really just [Akavache](https://github.com/reactiveui/Akavache) but this reimplementation lets me use System.Text.Json)
- [Octokit](https://github.com/octokit/octokit.net) to fetch/update game data
- [OpenCVSharp](https://github.com/shimat/opencvsharp/) for OCR
## Features
### Recruitment Calculator
Classic recruitment calculator. I don't want to remember tag combinations, my brain is busy being full of air.

#### OCR
Having to Alt-Tab away to [Aceship](https://aceship.github.io/AN-EN-Tags/akhr.html) or [akgcc](https://akgcc.github.io/cc/recruit.html) is slow. Visually locating the tags you need to click and... clicking them... no, too much work.

The app can parse text from the clipboard, so you can use something like the [OCR in PowerToys](https://learn.microsoft.com/en-us/windows/powertoys/text-extractor) to autoselect all tags in just two keyboard/mouse motions instead of five.<br/>
If you don't want to install PowerToys, OCR functionality is included in the app - so you can just grab a screenshot of the tags (e.g. with [Snipping Tool](https://support.microsoft.com/en-us/windows/use-snipping-tool-to-capture-screenshots-00246869-1843-655f-f220-97299b865f6b#ID0EDZBBBDD)) and immediately see the results. I'll admit it's not as good at the actual OCR part as PowerToys right now... Maybe I'll get around to it some day. For now, if one of them doesn't work, try the other.
> [!TIP]
> `Alt+PrintScreen` will capture the active window. This is just one keystroke and doesn't need the mouse.<br/>
> You can also use something like [keymapper](https://github.com/houmain/keymapper/) or [AutoHotkey](https://www.autohotkey.com/) to trigger it with one key instead of two - but that's beyond lazy, even for me.

##### Note on in-built OCR
The in-built OCR is currently in a minimum-viable state. It doesn't properly detect text on `Senior`/`Top Operator` tags or on tags selected in-game, because I've only put in the minimum effort.<br/>
I'm not here to automate the game, so only handling the most common workflow is good enough for me - I can use my eyes to spot top ops just fine.

#### Rarity filters
> "Ah yes, I would certainly like to see which combinations of these tags guarantee me a 3★"<br/>
> (said nobody ever)

You can choose whether you want to see results for rarities below 4★.<br/>
The defaults (hide 1★/2★, exclude 3★) correspond to the typical daily recruitment (at least 7h40m to remove 1★/2★, only care about 4★ for yellow certs).<br/>
Selecting the `Robot` or `Starter` tags manually will include them in results regardless of the filter.
#### Updates
When new operators are added to recruitment... I don't want to have to do anything. Updating the whole app is slow and inconvenient.

So, the app automatically pulls the most recent data from [Kengxxiao's data repository](https://github.com/Kengxxiao/ArknightsGameData_YoStar/tree/main/en_US). It also picks up any new tags, though new tags should get added basically never (`Elemental` is the only tag added post-release so far).

## Roadmap
Right now these are all from spreadsheets I made over the years - I just need to find the time and motivation to port them over.
- IS relic tracker/completion checklist (spreadsheets - [IS2](https://docs.google.com/spreadsheets/d/1XjMUNHfIUqqRiXLNeKoioEg64PneYOUGhHBHw58fNDg/edit), [IS3](https://docs.google.com/spreadsheets/d/1g7PBeaU0BrAJ25g8Xj43Jyu11zZUXCbK44XimNgAc8w/edit), [IS4](https://docs.google.com/spreadsheets/d/1ulX-GO5D9PM9_5tX_gnzgLyp7_W7zslFQBiYg9vFqSY/edit))
	- Rawdogging the relic collection process is like licking sandpaper
- Pulls calculator+ ([spreadsheet](https://docs.google.com/spreadsheets/d/1JbBpZoj2q6gf7VtKlZArBx-e4UuNR1UEjS7AlCXTmME/edit) - based on [u/lhc987's spreadsheet](https://docs.google.com/spreadsheets/d/12nugJxtTLFafudEJ_NrFuEm2X4VHrfCRkbyzMxia63A/edit))
    - It's a pain to maintain it even with me only checking in basically once every three months
    - Probably not going to be ported over unless I manage to automate pulling data for future events (if you have/know of an available API, feel free to open an issue)
- Red cert planner ([spreadsheet](https://docs.google.com/spreadsheets/d/17pi3KdViPWyNmGcCdPMmZzlS1sFKgOiftdiAheA8Qr0/edit))
    - The 6★ tokens in red cert shop haven't rotated in 500 years, so this calculator isn't very useful anymore
