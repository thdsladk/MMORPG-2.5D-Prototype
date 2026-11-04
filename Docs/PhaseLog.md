# 페이즈 로그

## Phase A — 에셋 설정 (2026-11-04)

| 단계 | 결과 | 커밋 |
|---|---|---|
| A-1 | Git LFS, Husky.Net 훅(commit-msg, pre-commit dotnet format), .editorconfig 네이밍·띄어쓰기 규칙, Tools/*.bat | `a6ab44b` |
| A-1b | GitHub 공개 저장소 생성·푸시 (thdsladk/MMORPG-2.5D-Prototype) | — |
| A-2 | Assets 번호 폴더 구조, GUID 유지 이동, 템플릿 파일 삭제 | `54cc6f0` |
| A-3~A-5 | 외부 에셋 11팩 배치, `ExternalAssetImportRules` (Point/무압축/PPU, SFX 모노·ADPCM) | `b34b69b` |
| A-5 보강 | 대체 팩(Gamekrazzy, Sova Skeleton/Wolf, Eris Esra) 추가 | `16ce10a` |
| A-6 | 캐릭터·몬스터 시트 303장 슬라이스 (아래) | `8f93180` |
| A-7 | Silver TMP 폰트 에셋(Raster·SDF), UI 9-slice 자동 지정 (아래) | 이번 커밋 |

### A-6 스프라이트 시트 슬라이스

- 메뉴: `Tools/Sprite/외부 캐릭터 시트 슬라이스 (전체)` (`ExternalSheetSlicer`, 2D Sprite 패키지의 Sprite Editor Data Provider 사용)
- 스프라이트 이름: `<애니메이션>_<방향>_<프레임>` — 방향은 S, SE, E, NE, N, NW, W, SW (예: `Walk_NE_03`, `Attack_W_11`)
- 같은 이름이면 spriteID 를 재사용하므로 다시 잘라도 참조가 유지된다.

| 팩 | 형식 | 시트 | 프레임 크기 | 피벗(발밑, 아래 기준 px) |
|---|---|---|---|---|
| Hormelz 기사 | 애니×방향마다 png + PixelOver json, `dir1~8` = SW, W, NW, N, NE, E, SE, S | 264 | 256×256 | (122.5, 113) — Idle S 첫 프레임 알파 기준 |
| Sova Skeleton | 애니마다 8행(S~SW) × 열 | 6 | 226×222 | (113, 33) — json |
| Sova Wolf | 위와 같음 | 9 | 244×220 | (122, 27) — json |
| Gamekrazzy | 가로 스트립, 파일명에 방향 | 24 | 32 (무기 64) | 캐릭터 (16, 6), 무기는 중심을 캐릭터 중심에 맞춤 |

방향 확인 (기사 Walk 첫 프레임, 왼쪽부터 S·SE·E·NE·N·NW·W·SW, 빨간 십자 = 피벗). Sova 팩도 같은 방법으로 확인했다 (재배포 금지라 이미지는 저장소에 넣지 않음):

![knight zoom](Images/A6_Directions_KnightZoom.png)

**Phase 1 에서 조정할 것**
- 화면상 크기 차이: 기사는 256 캔버스 안에서 실제 캐릭터가 약 80px, 스켈레톤은 약 190px. 같은 PPU(64)면 스켈레톤이 2배 이상 크다 → Sova 팩 PPU 를 올리거나 SpriteAnimSet 에 스케일 값을 둔다.
- 기사 피벗은 정면 대기 프레임의 앞발 기준이라 걷기 프레임에서는 발이 약 9px 위에 뜬다. 1-7 픽셀 안정화 단계에서 확인.
- Eris Esra 템플릿은 4방향·16px 템플릿이라 이번 슬라이스 대상에서 제외.

### A-7 폰트·UI

**폰트** — 메뉴 `Tools/Font/Silver TMP 폰트 에셋 생성` (`PixelFontAssetBuilder`)

| 에셋 | 굽는 크기 | 방식 | 아틀라스 | 용도 |
|---|---|---|---|---|
| `08.Fonts/Silver_Raster` | 19pt | RASTER_HINTED, Point 필터 | 1024² 1장 | 기본 UI. **19, 38, 57 처럼 19의 배수**로 쓰면 도트가 선명 |
| `08.Fonts/Silver_SDF` | 38pt | SDFAA, padding 4 | 2048² 1장 | 확대·외곽선이 필요한 글자 (데미지 숫자, 타이틀). 외곽선은 0.1 안팎 |

- 기준 크기 19pt 는 프로브로 확인: 19pt 에서 `H` = 5×9, `가` = 10×11 픽셀로 정수가 된다.
- 문자 세트: ASCII 95 + 한글 자모 51 + KS X 1001 한글 2350자(`08.Fonts/KSX1001_Hangul2350.txt`) + 기호 → **2547자, 누락 0**. Silver 원본에는 한글 음절 2358자가 들어 있다.
- TMP Essential Resources 를 임포트했다 (`Assets/TextMesh Pro/`, Unity 관리 폴더라 번호 규칙 예외).
- 폰트 에셋(.asset)은 아틀라스를 품어 크기가 커서 Git LFS 로 관리한다.

**UI 9-slice** — 메뉴 `Tools/Sprite/UI 9-slice 자동 지정` (`UiNineSliceDetector`)
- 대상: 파일 이름에 `Empty_`, `Window`, `CountBar`, `Inventory_background`, `Inventory_Slot` 이 있는 스프라이트. 아이콘·글자 버튼은 border 0 (원래 크기로 사용).
- 가운데 열/행과 같은 색이 이어지는 구간을 늘어나는 영역으로 잡는다. 한 축이 그라데이션이면 그 축은 투명도 모양만 비교해 모서리만 border 로 잡는다.
- 결과 9개: Window L6 B2 R6 T2, Translucent_Window L6 B6 R6 T22, Empty_Button L5 B6 R5 T6, Inventory_background L5, Inventory_Slot L2 등. Energy/Gem CountBar 는 자동 판정 실패 → 필요하면 수동 지정.
- Unity 6 은 새 스프라이트를 Multiple 로 들이므로, 임포트 규칙에서 UI 폴더는 Single 로 들이도록 고쳤다.
- 렌더 확인: 창·빈 버튼·인벤토리 배경 확대 시 테두리 유지, Raster 19/38pt 선명, SDF 57pt 확대·외곽선 정상. (확인 이미지는 재배포 불가 UI 팩이 들어가 저장소에 넣지 않음)
- CountBar 는 왼쪽 아이콘이 border 안에 완전히 들어가지 않아 세로로 늘리면 아이콘이 찌그러진다 → 원래 높이로만 쓴다.

**환경 메모**: 이 PC 에는 .NET **10** SDK/런타임만 있고 .NET 8 런타임이 없다. 서버(Phase 0-3)는 `net10.0` 으로 만들거나 .NET 8 런타임을 설치해야 한다.
