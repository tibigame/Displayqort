[Japanese](./README_ja.md)

# Displayqort

## Abstract

Keep window position from monitor changes.

## usage

### Startup

When startup it will be minimized to the notification area.
Every 30 seconds it gets the current window state and monitor state,
Write it to "window.dat".
When you display the main window from the menu of the notification area,
you can check the last saved time and the current monitor status.

### Window state changed

When a window state change is detected,
a warning dialog box will be displayed.
It is understood from the main window display
that the auto save is stopped.

### Restore

Please restore the monitor state and press the "Restore Window" button in the main window.
The window position will be restored from "window.dat".
* As the OS process runs right after the monitor state is changed, the window may not respond, wait a few seconds and then restore it.
* A warning dialog will be displayed if the window state is different from the save state.

## Effective software when used together

Icon potision keep software
[KH DeskKeeper](http://www.khsoft.gr.jp/software/deskkeeper2018/)


