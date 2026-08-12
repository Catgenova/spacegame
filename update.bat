@echo off
rem One-click updater: pulls the latest build of the game.
rem Close (or just unfocus) the Unity editor, double-click this file,
rem then refocus Unity and let it recompile.

cd /d "%~dp0"

rem Unity sometimes rewrites this file locally, which blocks pulling.
rem The repo's copy is authoritative, so discard any local edit first.
git checkout -- ProjectSettings/ProjectVersion.txt 2>nul

git pull
echo.
echo Latest commit:
git log --oneline -1
echo.
pause
