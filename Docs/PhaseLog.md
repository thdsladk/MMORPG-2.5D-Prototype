# 페이즈 로그

## Phase A — 에셋 설정 (2026-11-04)

| 단계 | 결과 | 커밋 |
|---|---|---|
| A-1 | Git LFS, Husky.Net 훅(commit-msg, pre-commit dotnet format), .editorconfig 네이밍·띄어쓰기 규칙, Tools/*.bat | `a6ab44b` |
| A-1b | GitHub 공개 저장소 생성·푸시 (thdsladk/MMORPG-2.5D-Prototype) | — |
| A-2 | Assets 번호 폴더 구조, GUID 유지 이동, 템플릿 파일 삭제 | `54cc6f0` |
| A-3~A-5 | 외부 에셋 11팩 배치, `ExternalAssetImportRules` (Point/무압축/PPU, SFX 모노·ADPCM) | `b34b69b` |
| A-5 보강 | 대체 팩(Gamekrazzy, Sova Skeleton/Wolf, Eris Esra) 추가 | `16ce10a` |
| A-6 | 캐릭터·몬스터 시트 303장 슬라이스 (아래) | 이번 커밋 |

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
