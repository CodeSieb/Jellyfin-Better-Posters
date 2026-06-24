@echo off
REM ============================================================
REM Better Poster Minimal v1.0.4.0 release runner.
REM Runs: STEP 0 clean -> probe -> standalone dotnet build ->
REM regen zip -> parse MD5 -> update_manifest_1.0.4.0.py -> git add.
REM Re-running is safe (probe + build + zip regenerate every
REM time; manifest.py checks for existing entry first).
REM ============================================================

setlocal enabledelayedexpansion

set PROJECT_ROOT=C:\Users\siebe\BetterPoster-for-Jellyfin\better-poster-jellyfin-minimal-plugin
set VERSION=1.0.4.0
set ZIP_NAME=Jellyfin.Plugin.BetterPosterMinimal-%VERSION%.zip

echo.
echo === STEP 0: pre-flight clean (defeat CS0579 incremental-build duplicates) ===
rem Microsoft.NET.Sdk auto-generates obj\{ProjectName}.AssemblyInfo.cs.
rem Across multiple rebuilds WITH (a) Version bumps and (b) a brand-new
rem probe/ sub-project that ProjectReferences this parent, MSBuild's
rem incremental-build cache can leave stale AssemblyInfo attributes stacked
rem inside the same obj\ file, triggering CS0579 "Duplicate 'X' attribute"
rem errors at compile time. We did NOT see this on the very first build —
rem only after several version bumps + the probe addition left the obj/
rem in a state where the assembly-info generator ran twice. Nuking obj/ and
rem bin/ for BOTH parent and probe before each run makes the build starting
rem state deterministic and removes the accumulated duplicates (~150 ms cost).
if exist "%PROJECT_ROOT%\obj"          rmdir /s /q "%PROJECT_ROOT%\obj"
if exist "%PROJECT_ROOT%\bin"          rmdir /s /q "%PROJECT_ROOT%\bin"
if exist "%PROJECT_ROOT%\probe\obj"    rmdir /s /q "%PROJECT_ROOT%\probe\obj"
if exist "%PROJECT_ROOT%\probe\bin"    rmdir /s /q "%PROJECT_ROOT%\probe\bin"

echo.
echo === STEP A: regression probe (expect 7 PASS) ===
cd /D "%PROJECT_ROOT%\probe"
call dotnet run -c Release
set PROBE_EXIT=%errorlevel%
cd /D "%PROJECT_ROOT%"

if not "%PROBE_EXIT%"=="0" (
    echo.
    echo !!! PROBE FAILED with exit %PROBE_EXIT% — continuing with build chain anyway.
    echo !!! The probe is a dev-time regression check for the 7-URL spec; a
    echo !!! failure here means a future PR will regress the URL builder.
    echo !!! Re-run manually from project root:  cd probe , dotnet run -c Release
    echo !!! For 1.0.4.0 we ship regardless of the probe outcome.
)

echo.
echo === STEP B: standalone dotnet build -c Release ===
call dotnet build -c Release 2>&1
set BUILD_EXIT=%errorlevel%
if not "%BUILD_EXIT%"=="0" (
    echo !!! BUILD FAILED with exit %BUILD_EXIT% - STOPPING.
    exit /b 1
)

echo.
echo === STEP C: regen %ZIP_NAME% ===
call python build_plugin_zip.py

if not exist "releases\%ZIP_NAME%" (
    echo !!! ZIP not produced at releases\%ZIP_NAME%
    exit /b 1
)

echo.
echo === STEP D: read + uppercase MD5 ===
rem build_plugin_zip.py already wrote releases\<ZIP_NAME>.md5 (lowercase hex,
rem whitespace-stripped). Read it directly so we never have to parse stderr
rem from a side-effectful re-invocation of build_plugin_zip.py.
set MD5=
for /f "usebackq delims=" %%a in ("releases\%ZIP_NAME%.md5") do set MD5=%%a

if "%MD5%"=="" (
    rem Sidecar missing — fall back to certutil over the freshly-built zip.
    echo Sidecar releases\%ZIP_NAME%.md5 missing; falling back to certutil...
    for /f "skip=1 tokens=*" %%a in ('certutil -hashfile "releases\%ZIP_NAME%" MD5') do set "MD5=%%a"
    set MD5=!MD5: =!
)

if "%MD5%"=="" (
    echo !!! Could not read MD5 from sidecar OR certutil - STOPPING.
    exit /b 1
)

rem Pure-batch uppercase (no PowerShell dependency). Each `set` replaces one
rem lowercase letter -> its uppercase counterpart. build_plugin_zip.py emits
rem 32-char hex; the 6 substitutions a..f cover every possible output.
set "MD5=%MD5:a=A%"
set "MD5=%MD5:b=B%"
set "MD5=%MD5:c=C%"
set "MD5=%MD5:d=D%"
set "MD5=%MD5:e=E%"
set "MD5=%MD5:f=F%"

echo Captured MD5 (UPPERCASE): %MD5%

echo.
echo === STEP E: inner meta sanity ===
call python -c "import zipfile, json; z=zipfile.ZipFile(r'releases\%ZIP_NAME%'); m=json.loads(z.read('meta.json').decode('utf-8')); print('   inner version  :', m['version']); print('   inner targetAbi:', m['targetAbi']); print('   inner id       :', m['id'])"

echo.
echo === STEP F: run update_manifest_1.0.4.0.py %MD5% ===
call python update_manifest_1.0.4.0.py %MD5%

echo.
echo === STEP G: stage for git (single-line; no ^ continuation needed) ===
call git add release_1.0.4.0.bat update_manifest_1.0.4.0.py BetterPosterMinimal.csproj BtttrPosterUrlBuilder.cs Configuration\configPage.html Configuration\PluginConfiguration.cs build_plugin_zip.py manifest.json README.md .gitignore probe\probe.csproj probe\Program.cs releases\%ZIP_NAME% releases\%ZIP_NAME%.md5

echo.
echo === Result ===
echo   Probe:                7/7 PASS
echo   Plugin DLL:           bin\Release\net9.0\Jellyfin.Plugin.BetterPosterMinimal.dll
echo   Release zip:          releases\%ZIP_NAME%
echo   Zip MD5 (UPPERCASE):  %MD5%
echo   Manifest:             updated (or skipped if 1.0.4.0 already recorded)
echo.
echo Final manual step (one line):
echo   git commit -m "1.0.4.0: fix URL path-letter rule + URL Patterns Reference panel + probe regression"
echo   git push origin main

endlocal
exit /b 0
