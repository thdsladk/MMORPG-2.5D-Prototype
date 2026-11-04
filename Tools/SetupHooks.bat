@echo off
chcp 65001 > nul
rem 클론 직후 1회 실행: Git LFS + Husky.Net 훅 설치
cd /d "%~dp0.."

echo [1/3] Git LFS 설치
git lfs install --local || goto :error

echo [2/3] dotnet 도구 복원 (Husky.Net)
dotnet tool restore || goto :error

echo [3/3] Husky 훅 설치
dotnet husky install || goto :error

echo 완료. 커밋 시 서식/네이밍 검사와 커밋 메시지 검사가 자동으로 실행됩니다.
pause
exit /b 0

:error
echo 실패했습니다. 위 오류를 확인하세요.
pause
exit /b 1
