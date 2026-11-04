@echo off
chcp 65001 > nul
rem C# 서식·네이밍 자동 수정 (Unity 스크립트 + 서버)
cd /d "%~dp0.."

if exist "2.5DProject.sln" (
    echo [Unity] Assets/01.Scripts/ 자동 수정
    dotnet format 2.5DProject.sln --severity warn --include Assets/01.Scripts/
) else (
    echo [Unity] 2.5DProject.sln 이 없습니다. Unity 에디터에서 Assets ^> Open C# Project 로 생성하세요.
)

if exist "Server\Server.sln" (
    echo [Server] 자동 수정
    dotnet format Server\Server.sln --severity warn
)

echo 완료. 남은 경고는 직접 수정해야 하는 항목입니다. Tools\VerifyFormat.bat 로 확인하세요.
pause
