This tool fixes your save temporarily until warhorse fixes crashing on save issue.

Note: 
The crash on save issue is caused by the [UserMods] having over 400 entries.
The saving system in KCD does not replace these entries if you update a mod, causing the save file eventually to corrupt and cause a heap corruption.
On autosave, there is a chance the entire [UserMods] gets duplicated, this can also contribute to the save eventually reaching 400 entries and crashing.

This tool fixes this issue by Clearing ﻿[UserMods] entirely from save while maintaining the binary structure so you can load it.

Special thanks to everyone on KCD2 discord who assisted me in investigating the issue.❤️

To use:

        1. Put the broken save into [TARGET SAVE]. (saves located in %USERPROFILE%\Saved Games\kingdomcome2\saves\)

        2. Choose where the Fixed save goes by clicking browse. (Output will automatically be named the same as the Target save, but can be changed manually)

        3. Click 'Run Tool'

        
