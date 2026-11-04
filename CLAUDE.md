# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 6 (`6000.3.22f1`), URP 기반 **2.5D MMORPG 프로토타입**. 3D 월드 + 8방향 도트 스프라이트(빌보드) 캐릭터, C# 서버 권한형 구조.
전체 계획과 페이즈별 구현 순서는 `Docs/Plan.md`에 있다. 작업은 페이즈 단위로 진행하고, 끝나면 `Docs/PhaseLog.md`에 결과를 남긴다.

- 클라이언트: 이 Unity 프로젝트. 서버: `Server/` (.NET 10 콘솔, 직접 구현 TCP + SocketAsyncEventArgs).
- 직렬화: MemoryPack. 클라/서버 공용 코드는 `Assets/01.Scripts/Shared/` (UnityEngine 참조 금지, 서버 csproj가 링크 컴파일).
- `.unity`, `.prefab`, `.asset` 파일은 직접 편집하지 않는다. Unity 에디터가 열려 있으면 Unity CLI(`unity command eval ...`)로 조작한다.

## 코드 구조

| 위치 | 어셈블리 | 내용 |
|---|---|---|
| `Assets/01.Scripts/Shared/` | `Game.Shared` (Unity asmdef) / `Server/Shared/Shared.csproj` (링크) | 패킷, 로그, 공용 로직. **C# 9 + netstandard2.1 범위로만 작성** (Unity 6 와 동일. 서버 빌드의 netstandard2.1 대상이 이를 검사한다) |
| `Assets/01.Scripts/Client/` | `Game.Client` | Unity 클라이언트 |
| `Assets/01.Scripts/Editor/` | `Game.Editor` | 에디터 도구 (임포트 규칙, 슬라이서, 폰트·머티리얼 생성) |
| `Assets/01.Scripts/Tests/EditMode/` | `Game.Tests.EditMode` | Unity EditMode 테스트 |
| `Assets/Plugins/MemoryPack/` | — | MemoryPack.Core 1.21.4 (netstandard2.1), System.Collections.Immutable, 소스 제너레이터(`RoslynAnalyzer` 라벨). 번호 규칙 예외 |
| `Server/` | `Server.sln` | `GameServer`, `DummyClient`, `DataBuilder`, `ServerCommon`(서버 공용), `Shared`(링크), `Tests/Shared.Tests`(xUnit) |

- `Shared.csproj`는 `netstandard2.1;net10.0` 멀티 타깃이다. MemoryPack 제너레이터가 대상마다 다른 코드를 만들기 때문(net7+ 는 C# 11 정적 추상 멤버 사용).
- 패킷 클래스 이름: `CS_Xxx`(클라→서버), `SC_Xxx`(서버→클라). `S_` 접두어는 네이밍 검사가 `s_`(static 필드)로 보고 막는다. 번호는 `PacketId` enum.
- 직렬화 바이트가 클라·서버에서 같은지 `SerializationProbe.ExpectedHex`로 양쪽 테스트가 확인한다. 직렬화 대상 필드를 바꾸면 이 값을 다시 구해야 한다.
- 로그는 `Game.Shared.Logging.Log` 를 쓴다 (서버 = `ConsoleLogSink`, 클라 = `UnityLogSink`, 시작 시 연결).

```bash
dotnet build Server/Server.sln
```

```bash
dotnet test Server/Tests/Shared.Tests
```

```bash
dotnet run --project Server/GameServer
```

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

`Assets/TextMesh Pro/`는 Unity가 관리하는 TMP Essential Resources 폴더라 번호 규칙 예외다 (TMP Settings 를 Resources 에서 찾으므로 옮기지 않는다).

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
