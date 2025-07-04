@echo off
echo =========================================
echo Building MicControlX v3.1.1-gamma
echo =========================================
echo.

echo Cleaning previous builds...
if exist "publish" rmdir /s /q "publish"
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
if exist "release" rmdir /s /q "release"

echo.
echo Building single-file executable...
dotnet publish src\MicControlX.csproj --configuration Release --runtime win-x64 --output publish --verbosity quiet /p:DebugType=none /p:DebugSymbols=false

if errorlevel 1 (
    echo.
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo Cleaning debug files...
if exist "publish\*.pdb" del /q "publish\*.pdb"
if exist "publish\*.xml" del /q "publish\*.xml"

echo.
echo Creating release package...
mkdir "release"
copy "publish\MicControlX.exe" "release\"
copy "README.md" "release\"
copy "LICENSE" "release\"

echo.
echo Creating version info...
echo MicControlX v3.1.1-gamma > "release\VERSION.txt"
echo Build Date: %date% %time% >> "release\VERSION.txt"
echo. >> "release\VERSION.txt"
echo Download from: https://github.com/Metanome/mic-controlx >> "release\VERSION.txt"

echo.
echo =========================================
echo SUCCESS: Release package created!
echo =========================================
echo Location: release\
echo Main executable: MicControlX.exe
echo Size: 
for %%A in ("release\MicControlX.exe") do echo %%~zA bytes
echo.
echo Ready to upload to GitHub Releases!
echo.
pause
