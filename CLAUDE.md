# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 6 (`6000.3.22f1`), URP 기반 **2.5D MMORPG 프로토타입**. 3D 월드 + 8방향 도트 스프라이트(빌보드) 캐릭터, C# 서버 권한형 구조.
전체 계획과 페이즈별 구현 순서는 `Docs/Plan.md`에 있다. 작업은 페이즈 단위로 진행하고, 끝나면 `Docs/PhaseLog.md`에 결과를 남긴다.

- 클라이언트: 이 Unity 프로젝트. 서버: `Server/` (.NET 8 콘솔, 직접 구현 TCP + SocketAsyncEventArgs).
- 직렬화: MemoryPack. 클라/서버 공용 코드는 `Assets/01.Scripts/Shared/` (UnityEngine 참조 금지, 서버 csproj가 링크 컴파일).
- `.unity`, `.prefab`, `.asset` 파일은 직접 편집하지 않는다. Unity 에디터가 열려 있으면 Unity CLI(`unity command eval ...`)로 조작한다.

## 폴더 규칙

`Assets/` 아래에 **우리가 만들어서 쓰는 폴더는 번호 접두어**를 붙인다.

| 폴더 | 내용 |
|---|---|
| `01.Scripts` | `Shared/`, `Client/`, `Editor/` (asmdef 분리) |
| `02.Scenes` | 씬 |
| `03.Prefabs` | 프리팹 |
| `04.Materials` | 머티리얼 (외부 에셋을 URP로 변환한 것 포함) |
| `05.Shaders` | 셰이더 |
| `06.Animations` | SpriteAnimSet 등 애니메이션 데이터 |
| `07.Data` | DataBuilder가 만든 테이블 바이너리 |
| `08.Fonts` | TMP 폰트 에셋 |
| `09.Audio` | AudioMixer 등 |
| `10.Settings` | URP RP Asset, Volume Profile |
| `90.ExternalAssets` | 외부 에셋 원본 (`<분류>/<작가>_<팩>/`). **수정 금지**, 라이선스 파일 유지 |

외부 에셋 중 재배포가 허용되지 않는 팩은 `.gitignore`에 있고, `Docs/Credits.md`의 링크에서 받아 넣는다.

## 커밋 / 포맷 규칙 (Husky.Net 훅)

클론 직후 1회 `Tools/SetupHooks.bat` 실행 (Git LFS + `dotnet tool restore` + `dotnet husky install`).

- **commit-msg**: `type: 설명` 형식. 허용 type: `asset|build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test` (선택적 `(scope)`, `!`). 설명은 한국어. 예: `feat: 몬스터 스포너 추가`
- **pre-commit**: 서식·네이밍 검사가 통과해야 커밋된다.
  - Unity: `dotnet format 2.5DProject.sln --verify-no-changes --severity warn --include Assets/01.Scripts/`
  - 서버: `dotnet format Server/Server.sln --verify-no-changes --severity warn`
  - 자동 수정: `Tools/FormatAll.bat`, 검사만: `Tools/VerifyFormat.bat`
  - `.sln`/`.csproj`는 Unity가 생성한다. 없으면 에디터에서 재생성(Assets > Open C# Project).
- `core.hooksPath`가 `.husky`이므로 Git LFS 훅(`pre-push` 등)도 `.husky/`에 있다. 지우지 말 것.
- 바이너리(png, fbx, wav, ogg, ttf 등)는 Git LFS (`.gitattributes`).

## 코드 컨벤션 (`.editorconfig`, IDE1006 = warning)

- 들여쓰기 4칸, Allman 중괄호, `using`은 System 먼저.
- 띄어쓰기: 제어문 키워드 뒤 공백 `if (x)`, 이항 연산자 양옆 공백, 쉼표 뒤 공백, 괄호 안쪽 공백 없음, 캐스트 뒤 공백 없음 `(int)x`.
- 네이밍 (.NET 규칙):
  - 타입·메서드·프로퍼티·이벤트·네임스페이스: `PascalCase`
  - 인터페이스: `IPascalCase`, 제네릭 타입 매개변수: `TPascalCase`
  - private/protected 필드: `_camelCase` (Unity 직렬화 필드도 `[SerializeField] private float _speed;`)
  - private static 필드: `s_camelCase`
  - const, static readonly: `PascalCase`
  - public/internal 필드: `PascalCase` (가능하면 프로퍼티 사용)
  - 매개변수·지역변수: `camelCase`
  - async 메서드: `XxxAsync`
- 파일 상단에 `// 역할 : ...` 한국어 주석으로 클래스 책임을 적는다. 주석은 한국어.
- 코루틴 메서드 이름은 `Xxx_Coroutine`.
