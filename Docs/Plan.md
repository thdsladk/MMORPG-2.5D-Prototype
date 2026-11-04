# 2.5D MMORPG 프로토타입 계획서 (Unity 클라이언트 + C# 서버)

## Context
- 참고: 「도깨비의세계 역기획서 + 개발 개요」 문서. 핵심 기술 3가지를 재현하는 포트폴리오용 프로토타입이 목표.
  1. **3D 월드 + 8방향 도트 스프라이트(빌보드) + 라이팅/그림자** (2.5D 비주얼)
  2. **서버 권한형 MMO 구조** (C# 서버, AOI 동기화, 몬스터 AI, 전투 판정)
  3. **데이터 드리븐 스킬 + 8슬롯 덱 + "같은 스타일 3개 = 세트 보너스"**
- 원작 IP·아트·명칭은 쓰지 않음. **무료(CC0) 에셋 기반 판타지 컨셉**으로 자체 가칭을 붙이고, 장르/메커니즘만 참고.
- 현재 프로젝트: Unity **6000.3.22f1**, URP(PC/Mobile RP Asset), Input System, AI Navigation 설치된 **빈 템플릿**. git 미초기화.
- 확정 사항: **직접 구현 TCP 서버(.NET 8, SocketAsyncEventArgs)**, **MemoryPack 직렬화**, **자유 이동(XZ 평면) + 서버 검증**.

---

## 1. 전체 구조

```
C:\UnityProject\2.5DProject\
├─ _AssetSource\        ← 사용자가 받은 원본 zip 보관 (Assets 밖, .gitignore 대상)
├─ Assets\              ← 우리가 만드는 폴더는 번호 접두어
│  ├─ 01.Scripts\
│  │  ├─ Shared\        ← asmdef "Game.Shared" (noEngineReferences=true). 클라/서버 공용 코드
│  │  │   ├─ Packets\   (MemoryPack 패킷, PacketId enum)
│  │  │   ├─ Data\      (테이블 클래스: Stat/Monster/Skill/Spawn)
│  │  │   ├─ Math\      (Vec2/Vec3 struct, 8방향 양자화 등)
│  │  │   └─ Combat\    (데미지 공식, 범위 판정 shape — 서버가 진실, 클라는 미리보기)
│  │  ├─ Client\        ← asmdef "Game.Client"
│  │  │   ├─ Core\ (Bootstrap, SceneFlow, EventBus, Pool, TableLoader)
│  │  │   ├─ Network\ (NetworkClient, PacketDispatcher, ServerTime)
│  │  │   ├─ World\ (EntityManager, EntityView, Interpolator, LocalPlayerController)
│  │  │   ├─ Presentation\ (SpriteCharacterRenderer, Billboard, CameraRig, DamageText, VFX)
│  │  │   ├─ Content\ (Skill, Deck, AutoBattle, Inventory)
│  │  │   └─ UI\ (UIManager 화면 스택, HUD)
│  │  └─ Editor\        ← asmdef "Game.Editor" (AssetImport 규칙, 시트 슬라이서, 맵 그리드 익스포터)
│  ├─ 02.Scenes\        (기존 Scenes 이동: SampleScene → AssetShowcase/Game/Title)
│  ├─ 03.Prefabs\       (Characters, Monsters, Environment, VFX, UI)
│  ├─ 04.Materials\     (URP 변환 머티리얼, 스프라이트 Lit 머티리얼)
│  ├─ 05.Shaders\
│  ├─ 06.Animations\    (SpriteAnimSet ScriptableObject: 상태×8방향 프레임)
│  ├─ 07.Data\          (DataBuilder가 만든 .bytes 테이블)
│  ├─ 08.Fonts\         (TMP 폰트 에셋: Silver + 한글 2350자)
│  ├─ 09.Audio\         (AudioMixer, 우리가 정리한 클립 참조)
│  ├─ 10.Settings\      (기존 Settings 이동: URP PC/Mobile RP Asset, Volume Profile)
│  └─ 90.ExternalAssets\  ← 외부 에셋 원본(수정 금지, 라이선스 파일 동봉)
│      ├─ Characters\  Monsters\  Environment\  VFX\  UI\  Icons\  Fonts\  SFX\
│      └─ (각 팩 = <작가>_<팩이름> 폴더)
├─ Server\  (Unity 밖, .NET 8 솔루션)
│  ├─ GameServer\       Network / Session / Job / World(Zone, AOI) / Entity / AI / Combat / Data
│  ├─ DummyClient\      부하·자동 테스트 봇 (Shared 링크)
│  ├─ DataBuilder\      CSV → MemoryPack .bytes 변환 (클라·서버 동일 바이너리)
│  └─ Shared.csproj     ← Assets/Scripts/Shared/**/*.cs 를 <Compile Include Link>로 포함
├─ Data\  (원본 CSV: Character, Monster, Skill, SkillEffect, Spawn, Drop)
└─ Docs\  (계획서, 페이즈 로그, 측정 결과)
```

**공용 코드 원칙**: Shared는 UnityEngine 참조 금지 → 서버와 바이트 단위로 같은 패킷/공식/테이블을 사용. MemoryPack 소스 제너레이터는 양쪽에서 각각 실행(Unity는 Generator DLL에 `RoslynAnalyzer` 라벨).

### 서버 런타임 구조
- **Network 스레드(IOCP)**: Accept → Session(Recv/Send SAEA) → 패킷 조립([ushort size][ushort id][payload]) → `PacketHandler`가 해당 Zone의 **JobQueue에 Push**.
- **Zone 로직 스레드**: 고정 틱 **20Hz(50ms)**. 틱마다 Job 처리 → AI 업데이트 → 전투/버프 → AOI 갱신 → Send 큐 Flush. Zone 내부는 단일 스레드라 락 없음.
- **권한**: 이동 검증, 타겟·사거리·쿨타임·데미지·사망·드랍 모두 서버. 클라이언트는 입력·예측·연출만.

### 클라이언트 2.5D 렌더링 방침
- 고정 피치(약 35~40°) **Perspective 쿼터뷰**, 요(yaw) 회전 금지(또는 90° 단위만).
- 캐릭터 = **Y축 빌보드 Quad** + 8방향 스프라이트. 방향 = (이동 벡터 vs 카메라 forward)를 8구간 양자화. 좌우 대칭은 flip으로 5방향 제작 허용.
- **Lit 스프라이트 셰이더**(HLSL, URP): 알파 클립, 메인/추가 라이트 수신, ShadowCaster 패스로 바닥 그림자. 실패 시 블롭 그림자 폴백.
- Point 필터·밉맵 off·픽셀 스냅. 배경은 로우폴리 + 저해상도 텍스처로 해상도 감 통일.

---

## 2. 페이즈별 구현 순서
각 하위 페이즈 = 1회 실행 단위. **완료 조건(✔)** 을 통과해야 다음으로 진행.

### Phase A. 에셋 설정 (가장 먼저 실행)
대상 = 문서의 「itch.io 무료 에셋」 탭 중 **프로토타입 핵심 14개**. 다운로드는 사용자가 직접, 이후 작업은 전부 Claude.

| 분류 | 팩 | 용도 | 라이선스 |
|---|---|---|---|
| Characters | Hormelz 8 Directional Knight | 플레이어 기본 | CC0 |
| Characters | Hormelz 8 Directional 2DHD Character | 타 플레이어/NPC | CC0 |
| Characters | SmallScale TopDown 8-dir Knight | 적 NPC(색 변경) | 확인 필요 |
| Monsters | hensji Monsters | 필드 잡몬스터 | CC0 |
| Environment | KayKit Forest Nature Pack | 필드 나무·바위 | CC0 |
| Environment | WizP Stylized Asian Village | 마을 건물 | 상업 가능, 재판매 금지 |
| Environment | Ely Dev Mini Japan | 대나무·등롱·소품 | 확인 필요 |
| VFX | CodeManu Free VFX Pack | 타격·폭발 | CC BY 4.0 |
| VFX | nerijs PVFX Foundry Thirteen | 버프·방어 | CC0 |
| UI | soiam Pixel UI Mobile Casual | HUD·팝업(한국어판) | 무료, 크레딧 권장 |
| UI | ELV Free Inventory for RPG | 덱 8슬롯·인벤 | 크레딧 필수 |
| Icons | Caz Pixel Fantasy RPG Icons | 아이템·스킬 아이콘 | CC BY 4.0 |
| Fonts | Poppy Works Silver | 본문 한글 픽셀 폰트 | CC BY 4.0 |
| SFX | Leohpaz RPG Essentials SFX | UI·타격·스킬 | 무료, 재배포 금지 |

- **A-1 저장소 + 코드 규칙 자동화** (기존 `C:\UnityProject\3DFPSGame` 설정을 그대로 가져와 확장. 기존 bat 파일은 찾지 못해 새로 작성)
  - `git init`, `.gitignore`(3DFPSGame 것 + `_AssetSource/`, `Server/**/bin|obj/`, 재배포 불가 팩 경로), `.gitattributes`(`.husky/* eol=lf` + **Git LFS**: png/psd/fbx/glb/wav/ogg/mp3/ttf/otf).
  - `dotnet-tools.json`(Husky.Net 0.9.1), `.husky/commit-msg`(`type: 설명` 검사, 기존과 동일 type 목록), `.husky/pre-commit`:
    `dotnet format 2.5DProject.sln --verify-no-changes --severity warn --include Assets/01.Scripts/` + `Server/Server.sln`도 같은 검사(Unity .sln이 없으면 경고만 출력하고 건너뜀).
  - `.editorconfig`: 기존 서식(4칸, resharper 필드 속성, case 블록) + **.NET 네이밍 규칙**(`IDE1006`=warning → pre-commit에서 차단):
    타입·메서드·프로퍼티·이벤트·enum 멤버 = PascalCase / 인터페이스 = `I` + PascalCase / private·protected 필드 = `_camelCase` / private static = `s_camelCase` / const·static readonly = PascalCase / 매개변수·지역변수 = camelCase / 비동기 메서드 = `Async` 접미어. 띄어쓰기: `csharp_space_*` 규칙(키워드 뒤 공백, 연산자 양옆 공백, 괄호 안 공백 없음), Allman 중괄호, `using` 정렬.
  - **bat 자동화** `Tools/`:
    - `SetupHooks.bat`: `git lfs install` → `dotnet tool restore` → `dotnet husky install` (클론 직후 1회).
    - `FormatAll.bat`: 두 솔루션에 `dotnet format`(자동 수정) 실행.
    - `VerifyFormat.bat`: pre-commit과 같은 검사만 실행.
  - `CLAUDE.md`: 커밋/포맷 규칙, 네이밍, 폴더 번호 규칙, 서버·클라 구조 요약 (3DFPSGame CLAUDE.md 형식).
  - ✔ 잘못된 이름(`int myField;`)이 든 테스트 커밋이 pre-commit에서 막히고, 형식이 틀린 메시지가 commit-msg에서 막힘 → 확인 후 되돌림. `chore: 프로젝트 초기 설정` 첫 커밋.
- **A-1b GitHub 저장소**: `winget install GitHub.cli` → 사용자가 터미널에서 `gh auth login` 1회 → `gh repo create MMORPG-2.5D-Prototype --public --source . --remote origin --push`. ✔ github.com/thdsladk/MMORPG-2.5D-Prototype 에 main 푸시.
  - 이후 각 하위 페이즈 완료 시 커밋 + 푸시.
- **A-2 폴더 재구성**: 위 번호 폴더 생성. `Scenes`→`02.Scenes`, `Settings`→`10.Settings`는 **.meta와 함께 이동해 GUID 유지**(에디터 열려 있으면 Unity CLI의 `AssetDatabase.MoveAsset`, 닫혀 있으면 파일 이동). `TutorialInfo/`, `Readme.asset` 삭제. ✔ Unity 재임포트 후 URP 설정·씬 참조 유지, 콘솔 에러 0.
- **A-3 다운로드(사용자)**: 위 14개 링크 체크리스트 제공 → zip을 `_AssetSource/`에 넣기. ✔ 14개 파일 존재 확인.
- **A-4 압축 해제·배치**: `90.ExternalAssets/<분류>/<작가>_<팩>/`로 해제. Unity에 필요 없는 원본(.psd/.aseprite/.blend/샘플 프로젝트) 제외, 라이선스·readme는 유지. `.unitypackage`(soiam)는 임포트 후 같은 규칙 위치로 이동. ✔ 임포트 에러 0.
- **A-5 임포트 규칙 자동화**: `01.Scripts/Editor/AssetImport/` AssetPostprocessor — 경로별 규칙.
  - 스프라이트(Characters/Monsters/VFX/UI/Icons): Sprite, **Point 필터, 밉맵 off, 압축 없음**, 팩별 PPU, Max Size 맞춤.
  - 3D 모델(Environment): Scale Factor 통일, 머티리얼 Extract → `04.Materials/Environment/`에 **URP Lit**로 재생성, 텍스처 Point.
  - 오디오: SFX = Decompress On Load, 긴 클립 = Streaming, Force Mono(SFX).
  - 규칙 적용 후 전체 Reimport. ✔ 샘플 파일 Inspector 값 확인.
- **A-6 스프라이트 시트 슬라이스**: 팩마다 시트 구조(프레임 크기·행=방향 순서)가 달라 **받은 뒤 확인하고** 팩별 `SpriteSliceRule`(SO: 셀 크기, 방향 순서, 피벗=발 밑)로 일괄 슬라이스. ✔ 캐릭터 3·몬스터 1팩 프레임 분할 완료.
- **A-7 폰트·UI**: Silver → TMP Font Asset(ASCII + KS X 1001 한글 2350자, 픽셀 폰트용 Point/SDF 설정 비교). UI 스프라이트 9-slice border 지정. ✔ 한글 문장 렌더.
- **A-7b 공개 저장소 업로드 구분**(public 저장소이므로 팩별 판단):
  - 올림: CC0(Hormelz 2종, hensji, KayKit Forest, nerijs), CC BY 4.0(CodeManu, Caz, Silver — 크레딧 기재).
  - 제외(.gitignore): WizP Asian Village(재판매 금지), Leohpaz SFX(재배포 금지), 그리고 "확인 필요" 팩(SmallScale, Mini Japan, soiam, ELV)은 zip 안 라이선스가 재배포를 허용할 때만 올림.
  - 제외 팩은 `Docs/Credits.md`에 받는 링크와 넣을 경로를 적어, 클론한 사람이 직접 받아 넣게 함. 이 팩을 참조하는 프리팹/씬은 없을 때 깨져도 되는 쇼케이스·배경에만 사용.
  - ✔ `git status`에 제외 팩 파일이 안 잡힘.
- **A-8 확인 씬 + 크레딧**: `02.Scenes/AssetShowcase.unity`에 팩별 샘플 배치(배경 모델, 캐릭터 정지 프레임, VFX, UI, 폰트). `Docs/Credits.md`에 팩·작가·라이선스·URL 기록, "확인 필요" 항목은 zip 내 라이선스 확인 결과 기입. ✔ 씬 스크린샷 + 크레딧 문서.

### Phase 0. 코드 기반
- **0-1 계획 문서화**: `Docs/Plan.md`에 본 계획 복사. ✔ 커밋.
- **0-2 asmdef 분리**: Game.Shared / Game.Client / Game.Editor (`01.Scripts` 아래). ✔ 컴파일 에러 0.
- **0-3 서버 솔루션 생성**: `Server/` .NET 8 콘솔 GameServer, DummyClient, DataBuilder, Shared(링크 컴파일: `Assets/01.Scripts/Shared/**/*.cs`). ✔ `dotnet build` 성공.
- **0-4 MemoryPack 도입(양쪽)**: 서버 NuGet, Unity는 NuGetForUnity(또는 DLL+Generator 수동). Shared에 테스트 패킷 1개. ✔ 서버·Unity EditMode 테스트에서 직렬화 왕복이 같은 바이트.
- **0-5 공용 로깅/설정**: Shared `ILog` 인터페이스(서버=Console, 클라=Debug.Log), 서버 config(json: 포트, 틱레이트). ✔ 양쪽 로그 출력.

### Phase 1. 오프라인 2.5D 비주얼 기반 (서버 없이 눈으로 확인)
- **1-1 테스트 맵**: 바닥 + 로우폴리 프롭 몇 개(임시 큐브), Directional Light 그림자 설정, URP 셋업 점검. ✔ 씬 렌더.
- **1-2 쿼터뷰 카메라 리그**: 고정 피치, 타겟 추적(댐핑), 줌 한계. ✔ 큐브 따라다님.
- **1-3 빌보드 스프라이트**: Y축 빌보드 Quad + 스프라이트 임포트 규칙(AssetPostprocessor: Point, no mip, PPU 고정). 임시 8방향 시트(색 화살표). ✔ 어떤 위치에서도 카메라를 향함.
- **1-4 8방향 애니메이터**: `SpriteAnimSet`(ScriptableObject: 상태×방향 → 프레임, fps). 상태 Idle/Walk/Attack/Hit/Die. ✔ 방향 전환·상태 전환 재생.
- **1-5 Lit 스프라이트 셰이더**: 라이팅 수신 + 알파클립 ShadowCaster. 포인트 라이트 반응 확인. ✔ 스프라이트 그림자가 바닥에 생김.
- **1-6 로컬 이동**: Input System WASD(+ 클릭 이동), 카메라 기준 방향 변환, 단순 충돌. ✔ 걸으면 8방향 걷기 애니.
- **1-7 픽셀 안정화**: 픽셀 스냅, 지글거림 점검, 깊이/정렬(캐릭터 겹침) 처리. ✔ 이동 중 흐림/떨림 없음.
- (1-1~1-7은 Phase A에서 들인 KayKit/Asian Village 배경과 Hormelz 기사 스프라이트를 바로 사용)

### Phase 2. 네트워크 기반
- **2-1 서버 Listener/Session**: 비동기 Accept, SAEA Recv/Send, Session 생명주기. ✔ telnet 수준 연결/해제 로그.
- **2-2 버퍼**: RecvBuffer(링), SendBuffer(청크 풀) — 할당 최소화. ✔ 단위 테스트.
- **2-3 패킷 프레이밍**: `PacketSession` 헤더 조립/분할 수신 처리. ✔ 쪼개진/뭉친 패킷 테스트 통과.
- **2-4 패킷 정의·레지스트리**: Shared `PacketId` enum + id→역직렬화 함수 테이블(+ 핸들러 바인딩). ✔ 미등록 id 거절.
- **2-5 서버 디스패치→JobQueue**: 네트워크 스레드에서 로직 큐로 넘기기. ✔ 스레드 경계 로그 확인.
- **2-6 클라 NetworkClient**: Socket 비동기 수신 스레드 → ConcurrentQueue → 메인스레드 Update에서 디스패치. ✔ 연결/끊김 이벤트.
- **2-7 Ping/Pong + RTT**: C_Ping/S_Pong, 화면에 RTT 표시. ✔ 로컬 RTT 표시.
- **2-8 하트비트/타임아웃/정상 종료**. ✔ 클라 강제 종료 시 서버 세션 정리.
- **2-9 DummyClient 100 접속 핑 테스트**. ✔ 누수·예외 없음.

### Phase 3. 로그인과 월드 입장 (DB 없이 메모리)
- **3-1 C_Login(닉네임) → S_LoginResult(playerId)**. ✔
- **3-2 Zone + 고정 틱 루프(20Hz)** + JobSerializer, 틱 시간 측정 로그. ✔ 틱 안정.
- **3-3 C_EnterGame → S_EnterGame(내 정보)**, Zone에 Player 엔티티 생성. ✔
- **3-4 클라 씬 흐름**: Title(로그인 UI) → Game 씬, 서버 데이터로 내 캐릭터 스폰. ✔
- **3-5 S_Spawn / S_Despawn 브로드캐스트(전체)**: 다른 플레이어 표시. ✔ 클라 2개가 서로 보임.

### Phase 4. 이동 동기화
- **4-1 Shared 수학 타입**(Vec2/Vec3, 방향 enum) + Unity 변환 확장. ✔
- **4-2 C_Move 송신**: 이동 중 10Hz + 정지/방향 전환 즉시 (pos, dir, state, clientTime). ✔
- **4-3 서버 검증**: 최대 속도·맵 경계·이동 가능 여부, S_Move 브로드캐스트. ✔ 스피드핵 시도 거절.
- **4-4 원격 엔티티 보간**: 스냅샷 버퍼, 렌더 지연 ~100ms, 짧은 외삽. ✔ 원격 캐릭터 부드러움.
- **4-5 내 캐릭터 보정**: S_MoveCorrection 수신 시 오차 크면 스냅, 작으면 lerp. ✔
- **4-6 맵 그리드 익스포터**: Editor 툴이 NavMesh/콜라이더를 샘플링해 걷기 가능 그리드(.bytes) 생성 → 서버 로드. ✔ 벽 통과 불가(서버 기준).
- **4-7 테스트**: 클라 2개 + 랜덤워커 봇 50. ✔ 보간·보정 육안 확인.

### Phase 5. AOI (시야 관리)
- **5-1 그리드 셀 분할**(예: 셀 16m, 시야 3×3 셀). ✔
- **5-2 시야 집합 diff** → 입장/이탈 시 Spawn/Despawn. ✔ 멀어지면 사라짐.
- **5-3 시야 내 대상에게만 Move 등 브로드캐스트**. ✔ 송신량 감소 로그.
- **5-4 클라 EntityView 풀링**. ✔ 반복 스폰 시 GC 미발생.
- **5-5 부하 측정**: 봇 300~500, 틱 ms·초당 패킷·대역폭 기록(`Docs/Perf.md`). ✔ 틱 < 50ms.

### Phase 6. 데이터 테이블
- **6-1 CSV 스키마**: Character(Stat), Monster, Skill, SkillEffect, Spawn. ✔
- **6-2 DataBuilder**: CSV → Shared 테이블 클래스 → MemoryPack `.bytes`, 클라(`Assets/Data`)·서버로 출력. ✔
- **6-3 로더**: 서버 시작 시, 클라 부트 시 로드 + ID 참조 검증. ✔ 잘못된 참조는 빌드 실패.

### Phase 7. 몬스터
- **7-1 서버 Monster 엔티티 + 스탯(테이블)**. ✔
- **7-2 MonsterSpawner**: 스폰 영역, 최대 수, 리스폰 타이머. ✔ 서버 로그에 스폰.
- **7-3 클라 몬스터 표시**: 같은 SpriteCharacterRenderer 재사용, 테이블의 AnimSet. ✔ AOI 따라 보임.
- **7-4 AI FSM(서버)**: Idle → Wander(이동은 기존 S_Move 경로). ✔ 배회.
- **7-5 감지·추적·복귀(리쉬)**: 어그로 반경, 최대 추적 거리, 스폰 지점 복귀 시 HP 회복. ✔
- **7-6 서버 경로찾기**: 걷기 그리드 A* (+ 경로 캐시, 재계산 주기). ✔ 장애물 우회.

### Phase 8. 기본 전투 (★ 프로토타입 1차 목표: 플레이어·몬스터·전투)
- **8-1 HP/스탯 컴포넌트(서버)** + S_ChangeHp, 클라 월드 HP바. ✔
- **8-2 타겟팅**: 클릭/Tab 선택, 타겟 표시 UI, C_SelectTarget(선택 사항). ✔
- **8-3 기본 공격**: C_UseSkill(skillId=기본공격, targetId) → 서버 검증(사거리·쿨·생존·시야) → 데미지 공식(Shared) → S_SkillCast + S_Damage. ✔ 몬스터 HP 감소.
- **8-4 클라 연출**: 공격 애니, 피격 플래시, 데미지 숫자(풀링), 임시 사운드. ✔
- **8-5 몬스터 공격**: AI Attack 상태, 플레이어 피격. ✔
- **8-6 사망/부활**: 몬스터 사망 → 연출 후 Despawn → 리스폰. 플레이어 사망 → 부활 UI → 부활 지점. ✔
- **8-7 어그로 테이블**: 누적 데미지 기반 타겟 결정. ✔ 2명이 때리면 더 센 쪽 추적.
- **8-8 마일스톤 점검**: 클라 2~3개 + 봇으로 협동 사냥 녹화, 버그 목록 정리. ✔ 데모 영상.

### Phase 9. 데이터 드리븐 스킬
- **9-1 서버 시간 동기화**: S_ServerTime, RTT 기반 오프셋 추정(쿨타임 표시 기준). ✔
- **9-2 스킬 테이블 확장**: 스타일, 쿨, 시전시간, 사거리, 타겟 타입(단일/자신/원/부채꼴/직선), 효과 목록. ✔
- **9-3 효과 블록**: Damage / Heal / Buff·Debuff / Knockback / DoT 를 조합. ✔ 코드 추가 없이 CSV로 신규 스킬.
- **9-4 서버 SkillExecutor**: 시전 → 범위 판정(Shared shape, XZ) → 효과 적용. ✔
- **9-5 버프/디버프 시스템**: 지속시간, 틱, 중첩 규칙, S_BuffAdd/Remove. ✔
- **9-6 클라 스킬바(1~8키) + 쿨다운 UI**. ✔
- **9-7 스킬 연출 훅**: 테이블의 VFX/애니 키 → 임시 파티클·범위 인디케이터. ✔

### Phase 10. 8슬롯 덱 + 스타일 세트 보너스
- **10-1 덱 모델 + C_SetDeck 검증**(중복·보유·슬롯 수). ✔
- **10-2 세트 보너스**: 스타일별 개수 ≥3 → 패시브/궁극기 개방(서버 판정). ✔
- **10-3 덱 편집 UI** + 보너스 미리보기(Shared 규칙 재사용). ✔
- **10-4 프리셋 2~3개 전환**. ✔
- (스타일 4계열·12종 중 프로토타입은 **3스타일×4스킬 = 12종**만 제작)

### Phase 11. 자동 전투
- **11-1 클라 AutoBattle 상태 머신**: 타겟 선택(가까운/어그로), 스킬 우선순위, 물약. 일반 패킷만 사용(서버 권한 유지). ✔
- **11-2 자동 설정 UI**. ✔

### Phase 12. 성장·보상
- **12-1 경험치/레벨업**(스탯 테이블). ✔
- **12-2 드랍 테이블 → 인벤토리 직행**. ✔
- **12-3 서버 인벤토리 + 기본 UI, 골드**. ✔

### Phase 13. 영속화
- **13-1 DB 계층**: SQLite(개발) + Dapper, Repository 인터페이스(나중 MySQL 교체). ✔
- **13-2 계정/캐릭터/인벤 저장·로드**(로그아웃 + 주기 저장, DB 작업은 별도 스레드). ✔ 재접속 시 상태 유지.

### Phase 14. 최적화 & 측정 (포트폴리오 자료)
- **14-1 렌더**: Sprite Atlas, SRP Batcher/GPU Instancing, 캐릭터 300명 화면 FPS·드로우콜 전/후. ✔
- **14-2 GC**: 패킷 경로 무할당(ArrayPool, 풀링), 프로파일러 전/후 캡처. ✔
- **14-3 서버 지표**: 틱 ms, 패킷/s, 세션 수 콘솔 대시보드. ✔

### Phase 15. 비주얼 확장
- **15-1 파츠 레이어 합성**(몸/머리/의상, 방향별 sorting). ✔
- **15-2 팔레트/마스크 염색 셰이더**. ✔
- **15-3 가림 실루엣**(배경 뒤 캐릭터 디더/아웃라인), 포스트프로세싱 색감. ✔

### 백로그 (이후)
채팅, 파티, 길드(비동기 협력·분신), 필드 보스, 존 이동/다중 Zone, 로그인 서버 분리, 모바일 입력/UI 분기, Addressables 패치.

---

## 3. 진행 방식
- 실행 순서: **Phase A(규칙·저장소·에셋) → 0 → 1/2 → 3 …**. 승인 직후 A-1(규칙 자동화·첫 커밋) → A-2(폴더 재구성·커밋) → A-1b(gh 설치, 사용자 `gh auth login` 후 저장소 생성·푸시) 순으로 진행하고, A-3에서 다운로드 체크리스트를 드린 뒤 파일이 들어오면 A-4부터 이어감.
- 커밋 메시지: `type: 한국어 설명` + `Co-Authored-By` 줄. 에셋 커밋은 `asset:` type.
- 하위 페이즈 1개 = 1회 요청 단위. 끝날 때마다 커밋 + `Docs/PhaseLog.md`에 결과/측정값 기록.
- Phase 1(오프라인 비주얼)과 Phase 2(네트워크)는 서로 의존 없음 → 순서 바꾸거나 번갈아 진행 가능.
- 프로토타입 1차 완료 = **Phase 8 종료**.

## 4. 검증 방법
- **서버**: `dotnet build` / `dotnet test`(버퍼·프레이밍·직렬화·데미지 공식 단위 테스트), DummyClient 부하 시나리오.
- **클라**: Unity Test Framework EditMode(Shared 로직, 8방향 양자화, 덱 보너스), Play 모드 육안 확인 + 스크린샷.
- **통합**: 서버 실행 → Unity 에디터 + 빌드 클라 1개 동시 접속 → 이동/전투 상호 확인.
- 각 페이즈 완료 조건(✔)을 체크리스트로 사용.

## 5. 리스크 / 확인 필요
- 에셋 스타일(캐릭터 64px~256px 캔버스, 3D 팩 3종)이 제각각 → PPU로 화면상 키를 맞추고, 색감은 Phase 15 팔레트/포스트프로세싱으로 통일.
- "확인 필요" 라이선스 3팩(SmallScale, Mini Japan, 기타)은 A-8에서 zip 안 라이선스로 확정, 공개 불가면 대체.
- MemoryPack의 Unity 6 설치 방식(NuGetForUnity vs 수동 DLL + 소스 제너레이터)은 0-4에서 실제 검증.
- Lit 스프라이트 ShadowCaster가 빌보드 회전 때문에 그림자 모양이 어색할 수 있음 → 그림자 전용 고정 방향 쿼드 또는 블롭 폴백.
- 서버 충돌 판정 정밀도(그리드 해상도) vs 클라 물리 차이 → 4-6에서 그리드 셀 0.5m 기준으로 시작.
