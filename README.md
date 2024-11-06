# WindowPrivacy
 A tool that protects Windows Processes from being captured / recorded

https://github.com/user-attachments/assets/e9ba327c-8c81-4a83-b684-03de9d0e5e79

## Goal
1. in case of live-streaming making sure unwanted windows/ windows contains private information to not be recorded
2. in case of being a victim of a RAT, this tool will make sure all user-mode recording methods wont work on selected windows

reason #2 can be bypassed be sophisticated RAT that either running on kernel mode or hooking setWindowsAffinity,
in the future i plan to make the program attempt to take pictures every so often on protected processes and make sure it has no content to validate that setWindowsAffinity(WDA_MONITOR) works correctly.

##how it achieves it
this tool injects to the selected process (using CRT -> LoadLibrayA) a DLL that calls Win32::SetWindowsAffinity
to change the Process Windows content access either Monitor Only (WDA_MONITOR) or everything (WDA_NONE)

the reason it require injection because "The window must belong to the current process." - [SetWindowDisplayAffinity function (winuser.h)](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity).

Therefore it may not work on video games with anti-cheat and is NOT RECOMMENDED to be used as the risk of a ban since it calls openProcess

##hierarchy
Internal_RemWindowsAffinity --> dll for setWindowsAffinity(WDA_NONE)
Internal_SetWindowsAffinity --> dll for setWindowsAffinity(WDA_MONITOR)
WindowPrivacy --> The Tool itself
