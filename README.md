# PK Piano
#### A control code clipboard for the EarthBound Music Editor

[![N|Solid](https://vince94.neocities.org/images/pkpiano_screenshot.gif)](https://forum.starmen.net/forum/Community/PKHack/PK-Piano/page/1#post1920438)

## What is PK Piano?
The [EarthBound Music Editor](https://github.com/PKHackers/ebmused) is a comprehensive music tracker aimed at [Nintendo's proprietary SNES music driver](https://sneslab.net/wiki/N-SPC_Engine), which was used in several first-party and third-party games (with some degree of modification).

It's also a piece of legacy software, and has a pretty steep learning curve. 

This tool aims to make using it a bit easier by copying all kinds of codes to your clipboard so you can paste them in!

## Features

  - Auditory response - you can hear each note as the control code is copied to the clipboard
  - Easily calculate note lengths - no more using a hex calculator
  - Visual controls for Channel Volume, Panning, and the SNES's distinctive Echo Buffer
  - Convert note data from OpenMPT
  - Generate a sine wave for comparison [while tuning BRR samples](https://www.smwcentral.net/?p=section&a=details&id=10301)

## How to use

  - Press one of the buttons
  - CTRL+V to paste the hex into EBMusEd
  - Hover the mouse over buttons to view command syntax
  - Experiment

### Installation

As a C# application, PK Piano requires the .NET Framework to run. I reccomend using [Ninite](https://ninite.com/) to install the latest version.

To download the latest release, see ["Releases"](https://github.com/vince94/pkpiano/releases). It comes with a bunch of DLL files, but they are required for it to run properly. Unzip to a safe directory and make a shortcut to the EXE so they don't clutter things up.

If you don't have a copy of EBMusEd, you can download it [here](https://github.com/PKHackers/ebmused/releases)
