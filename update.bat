@echo off
rem ===========================================================================
rem  One-click updater: pulls the latest build of the game.
rem
rem  Close Play mode in Unity (or just unfocus the editor), double-click this
rem  file, then refocus Unity and let it recompile.
rem
rem  Run "update.bat force" to throw away local edits and match GitHub exactly.
rem ===========================================================================

rem --- Re-run from a temp copy first. -----------------------------------------
rem  cmd.exe reads a .bat line by line WHILE it runs, so when git replaces this
rem  very file mid-pull the rest of the run is garbage and the update can die
rem  halfway. Running from a copy makes the file on disk safe to overwrite.
rem  "call" keeps the same cmd process, so the variables below carry over.
if /i "%~1"=="__relaunched" goto :main
set "SG_DIR=%~dp0"
set "SG_ARG=%~1"
copy /y "%~f0" "%TEMP%\spacegame-update.bat" >nul 2>nul
if exist "%TEMP%\spacegame-update.bat" (
    call "%TEMP%\spacegame-update.bat" __relaunched
    exit /b
)

:main
if defined SG_DIR cd /d "%SG_DIR:~0,-1%"
set BRANCH=claude/eve-online-game-build-cu1ely

echo ===========================================================================
echo   Spacegame updater
echo   Folder: %CD%
echo ===========================================================================
echo.

rem Unity rewrites this file locally, which blocks pulling. The repo copy wins.
git checkout -- ProjectSettings/ProjectVersion.txt 2>nul

set BEFORE=
for /f "delims=" %%i in ('git rev-parse --short HEAD 2^>nul') do set BEFORE=%%i
if "%BEFORE%"=="" (
    echo ERROR: this folder is not a git clone, so there is nothing to pull.
    echo.
    echo   Unity is almost certainly opening a DIFFERENT folder than the one you
    echo   cloned. In Unity, right-click any script in the Project window and
    echo   choose "Show in Explorer", then check that the path it opens sits
    echo   inside the folder printed above.
    goto :done
)

echo Fetching %BRANCH% ...
git fetch origin %BRANCH%
if errorlevel 1 (
    echo.
    echo ERROR: could not reach GitHub. Check your connection, then try again.
    echo Your files were NOT changed.
    goto :done
)

if /i "%SG_ARG%"=="force" (
    echo.
    echo FORCE: discarding local edits and matching GitHub exactly.
    git reset --hard FETCH_HEAD
    if errorlevel 1 goto :failed
    git checkout -B %BRANCH% FETCH_HEAD
    if errorlevel 1 goto :failed
    goto :report
)

rem --- Normal mode: fast-forward only, so nothing of yours is ever lost. ------
git symbolic-ref --quiet HEAD >nul
if errorlevel 1 (
    echo.
    echo PROBLEM: your clone is on a detached HEAD, so a pull cannot advance it.
    goto :stuck
)
git merge --ff-only FETCH_HEAD
if errorlevel 1 goto :stuck

:report
set AFTER=
for /f "delims=" %%i in ('git rev-parse --short HEAD') do set AFTER=%%i
echo.
if "%BEFORE%"=="%AFTER%" (
    echo ALREADY UP TO DATE at %AFTER% -- there was nothing new to pull.
) else (
    echo UPDATED: %BEFORE% -^> %AFTER%
)
echo.
echo Latest commit:
git log --oneline -1
echo.
echo This build reports itself in-game as:
findstr /c:"public const string Version" Assets\Scripts\Core\BuildInfo.cs
findstr /c:"public const string Codename" Assets\Scripts\Core\BuildInfo.cs
echo.
echo   Now click back into Unity and wait for the spinner in the bottom right.
echo   That version is printed in the middle of the HUD top bar, and in the
echo   message log, every time you press Play. If the game shows an OLDER
echo   version than the one above, Unity has not recompiled: open Window ^>
echo   General ^> Console and look for red errors, because a compile error
echo   makes Unity keep running the last build that did compile.
goto :done

:stuck
echo.
echo Your clone has drifted from GitHub, so a safe fast-forward is not possible.
echo NOTHING was changed. Local state:
echo.
git status --short --branch
echo.
echo To throw away local changes and match GitHub exactly, run:
echo.
echo     update.bat force
echo.
goto :done

:failed
echo.
echo ERROR: the update failed partway. Run "update.bat force" to reset the
echo folder so it matches GitHub exactly.

:done
echo.
pause
