#KFreon's WPF Rewrite Run

##Introduction
This project is a branch, if you will, of the [original ME3Explorer Project here.](https://sourceforge.net/projects/me3explorer/)

The original tools are all written using Windows Forms, an aging technology to say the least. 
This project aims to rewrite Texplorer, TPFTools, and Modmaker (basically all tools managed by me (KFreon)) using WPF, the "new" Windows visual interface technology.

Seeing as I'm in there and rewriting a bunch of stuff to fit the new model (MVVM-ish), I'll be streamlining as much as I can to decrease code clutter etc, but also increase performance.


##Instructions
[YOU WILL NEED THESE IN THE BIN DIRECTORY WITH THE MAIN EXE](https://dl.dropboxusercontent.com/u/37301843/Bits%20and%20Bobs.7z)
###Developers
Go to Code tab.
Learn Git and clone to local repository.
OR
Right click on the top folder -> Download as Zip.  NOTE this is the source code only. It isn't like you're used to with the svn. You NEED Visual Studio to test this for now.



##Progress
You can/will be able to see some progress in the cool bug reporting/progress thing in the Work tab, and maybe in the Overview tab if I can figure out how to do it.
Without further ado, some progress reports.

###General
- Viewing and changing game information = simple now.  Click game indicator button thing (the things that change colour to denote presence of game), and all game details are displayed for you in nice pretty changable textboxes!
![](https://dl.dropboxusercontent.com/u/37301843/KFreonVisualStudio/pathinfo.jpg)

- Underlying image engine now does two things. ResIL handles all textures properly for the most part, but Windows 8.1 users will enjoy a super fast viewing experience as the engine can use the built in Windows codecs to read some images!
All other users will have to live with the slower ResIL image handling.



###Modmaker
####Details
- Job specific buttons now part of the job entry itself (I really like being able to do that)
- Ability to edit pathing and expID's for job pccs directly in place (Rather than clicking on one and having a little panel pop up to edit it there)
- Can be started from Main Window with Crtl+M

Functionally the same as all other Modmakers.

####Piccies :D
![imgggg](https://dl.dropboxusercontent.com/u/37301843/KFreonVisualStudio/modmaker.jpg)




###Texplorer
####Details
- First Time Setup now integrated into a general game/tree informat panel accessible with a button. Haven't decided what this button will look like yet.
--- Will contain all game files and allow selecting/deselecting of specific files as with FTCS before.
--- Shows dates modified etc, and allows filtering to show only modified files.
--- Also contains tree information and functions such as tree location, import/export tree, remove/regenerate tree.
- Old trees still supported.  I learned my lesson with that last time. 
- Can be started from Main Window with Crtl+T.


####More piccies.
New general panel showing FTCS
![](https://dl.dropboxusercontent.com/u/37301843/KFreonVisualStudio/WPFFTCS.jpg)

And once tree is generated
![]()



###TPFTools
Haven't started yet :(