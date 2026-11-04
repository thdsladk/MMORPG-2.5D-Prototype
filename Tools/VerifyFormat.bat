@echo off
chcp 65001 > nul
rem pre-commit 과 같은 검사만 실행 (파일 수정 없음)
cd /d "%~dp0.."
set FAIL=0

if exist "2.5DProject.sln" (
    echo [Unity] Assets/01.Scripts/ 검사
    dotnet format 2.5DProject.sln --verify-no-changes --severity warn --include Assets/01.Scripts/ || set FAIL=1
) else (
    echo [Unity] 2.5DProject.sln 이 없어 건너뜁니다.
)

if exist "Server\Server.sln" (
    echo [Server] 검사
    dotnet format Server\Server.sln --verify-no-changes --severity warn || set FAIL=1
)

if "%FAIL%"=="1" (
    echo 규칙 위반이 있습니다. Tools\FormatAll.bat 로 자동 수정하세요.
    pause
    exit /b 1
)
echo 통과.
pause
