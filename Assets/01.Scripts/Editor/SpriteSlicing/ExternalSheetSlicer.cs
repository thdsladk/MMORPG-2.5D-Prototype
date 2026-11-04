// 역할 : 외부 캐릭터/몬스터 팩의 스프라이트 시트를 팩별 형식에 맞춰 자른다.
//        스프라이트 이름은 "<애니메이션>_<방향>_<프레임 2자리>" (방향 = S, SE, E, NE, N, NW, W, SW) 로 통일하고,
//        피벗은 팩마다 하나의 발밑 위치로 고정해 애니메이션이 바뀌어도 캐릭터가 흔들리지 않게 한다.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.SpriteSlicing
{
    public static class ExternalSheetSlicer
    {
        private const string MenuPath = "Tools/Sprite/외부 캐릭터 시트 슬라이스 (전체)";
        private const string Root = "Assets/90.ExternalAssets/";
        private const string KnightPack = Root + "Characters/Hormelz_8DirKnight";
        private const string GamekrazzyPack = Root + "Characters/Gamekrazzy_TopDownCharacter";

        // Sova Digital 팩: 폴더, json 파일 이름. 저장소에 없는(로컬 전용) 팩은 폴더가 없으면 건너뛴다.
        private static readonly (string Folder, string Json)[] SovaPacks =
        {
            (Root + "Monsters/SovDigital_Skeleton", "skeleton.json"),
            (Root + "Monsters/SovDigital_Wolf", "wolf.json"),
        };

        // Hormelz 파일의 dir1~dir8 → 표준 방향 (dir1 = 왼쪽 아래 … dir8 = 아래)
        private static readonly string[] HormelzDirections = { "SW", "W", "NW", "N", "NE", "E", "SE", "S" };

        // Gamekrazzy 파일 이름의 방향 단어 → 표준 방향
        private static readonly Dictionary<string, string> GamekrazzyDirections = new Dictionary<string, string>
        {
            { "Down", "S" }, { "DownRight", "SE" }, { "Right", "E" }, { "UpRight", "NE" },
            { "Up", "N" }, { "UpLeft", "NW" }, { "Left", "W" }, { "DownLeft", "SW" },
        };

        [MenuItem(MenuPath)]
        public static void SliceAll()
        {
            int sheetCount = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                sheetCount += SliceHormelz(KnightPack, "Idle/Knight_Idle_dir8.png");
                foreach ((string folder, string json) in SovaPacks)
                {
                    sheetCount += SliceSova(folder, json);
                }

                sheetCount += SliceGamekrazzy(GamekrazzyPack);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[SpriteSlice] 시트 {sheetCount}개를 잘랐습니다.");
        }

        // Hormelz: <애니>/<캐릭터>_<애니>_dir<n>.png + 같은 이름의 PixelOver json (frames[].frame).
        private static int SliceHormelz(string packFolder, string pivotSheet)
        {
            if (!Directory.Exists(packFolder))
            {
                return 0;
            }

            // 1. 기준 시트(정면 대기) 첫 프레임의 발밑을 팩 전체 피벗으로 쓴다
            PixelOverSheet pivotJson = ReadJson<PixelOverSheet>(Path.ChangeExtension($"{packFolder}/{pivotSheet}", ".json"));
            Vector2 pivot = SpriteSliceUtility.FindFeetPivot($"{packFolder}/{pivotSheet}", pivotJson.frames[0].frame.ToRect());

            int count = 0;
            foreach (string pngPath in Directory.GetFiles(packFolder, "*.png", SearchOption.AllDirectories))
            {
                string assetPath = pngPath.Replace('\\', '/');
                string jsonPath = Path.ChangeExtension(assetPath, ".json");
                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                int dirIndex = fileName.LastIndexOf("_dir", StringComparison.Ordinal);
                if (!File.Exists(jsonPath) || dirIndex < 0)
                {
                    continue;
                }

                // 2. 이름: 폴더명(애니) + 방향
                string animation = Path.GetFileName(Path.GetDirectoryName(assetPath));
                int dirNumber = int.Parse(fileName.Substring(dirIndex + 4));
                string direction = HormelzDirections[dirNumber - 1];

                PixelOverSheet sheet = ReadJson<PixelOverSheet>(jsonPath);
                var frames = new List<SliceFrame>(sheet.frames.Length);
                for (int i = 0; i < sheet.frames.Length; i++)
                {
                    frames.Add(new SliceFrame($"{animation}_{direction}_{i:00}", sheet.frames[i].frame.ToRect()));
                }

                SpriteSliceUtility.Apply(assetPath, frames, pivot);
                count++;
            }

            return count;
        }

        // Sova Digital: Sheets/<애니>.png, 8행(S, SE, E, NE, N, NW, W, SW) × 열 = 프레임. json 에 프레임 크기·발밑 피벗.
        private static int SliceSova(string packFolder, string jsonName)
        {
            string jsonPath = $"{packFolder}/{jsonName}";
            if (!File.Exists(jsonPath))
            {
                return 0;
            }

            SovaCharacter info = ReadJson<SovaCharacter>(jsonPath);
            var frameSize = new Vector2Int(info.frame_size[0], info.frame_size[1]);
            Vector2 pivot = SpriteSliceUtility.ToNormalizedPivot(new Vector2Int(info.pivot[0], info.pivot[1]), frameSize);

            int count = 0;
            foreach (string pngPath in Directory.GetFiles($"{packFolder}/Sheets", "*.png"))
            {
                string assetPath = pngPath.Replace('\\', '/');
                string animation = ToPascalCase(Path.GetFileNameWithoutExtension(assetPath));
                Vector2Int size = SpriteSliceUtility.ReadPngSize(assetPath);
                int columns = size.x / frameSize.x;
                int rows = Math.Min(size.y / frameSize.y, SpriteSliceUtility.Directions.Length);

                var frames = new List<SliceFrame>(columns * rows);
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < columns; col++)
                    {
                        var rect = new RectInt(col * frameSize.x, row * frameSize.y, frameSize.x, frameSize.y);
                        frames.Add(new SliceFrame($"{animation}_{SpriteSliceUtility.Directions[row]}_{col:00}", rect));
                    }
                }

                SpriteSliceUtility.Apply(assetPath, frames, pivot);
                count++;
            }

            return count;
        }

        // Gamekrazzy: 가로 한 줄 스트립(프레임 = 정사각형, 한 변 = 이미지 높이).
        //   Character/Character_[Roll|Slash]<방향>.png, Weapon/Sword_<방향>.png
        private static int SliceGamekrazzy(string packFolder)
        {
            string pivotSheet = $"{packFolder}/Character/Character_Down.png";
            if (!File.Exists(pivotSheet))
            {
                return 0;
            }

            // 캐릭터는 발밑, 무기(2배 크기)는 중심이 캐릭터 중심과 겹치도록 피벗을 맞춘다
            int characterCell = SpriteSliceUtility.ReadPngSize(pivotSheet).y;
            Vector2 characterPivot = SpriteSliceUtility.FindFeetPivot(pivotSheet, new RectInt(0, 0, characterCell, characterCell));
            float feetFromBottom = characterPivot.y * characterCell;

            int count = 0;
            foreach (string pngPath in Directory.GetFiles(packFolder, "*.png", SearchOption.AllDirectories))
            {
                string assetPath = pngPath.Replace('\\', '/');
                string[] parts = Path.GetFileNameWithoutExtension(assetPath).Split('_');
                if (parts.Length != 2)
                {
                    continue;
                }

                bool isWeapon = parts[0] == "Sword";
                (string action, string dirWord) = SplitAction(parts[1]);
                if (!GamekrazzyDirections.TryGetValue(dirWord, out string direction))
                {
                    continue;
                }

                string animation = isWeapon ? "SwordSlash" : action;
                Vector2Int size = SpriteSliceUtility.ReadPngSize(assetPath);
                int cell = size.y;
                Vector2 pivot = isWeapon
                    ? new Vector2(0.5f, (cell * 0.5f - characterCell * 0.5f + feetFromBottom) / cell)
                    : characterPivot;

                var frames = new List<SliceFrame>(size.x / cell);
                for (int i = 0; i < size.x / cell; i++)
                {
                    frames.Add(new SliceFrame($"{animation}_{direction}_{i:00}", new RectInt(i * cell, 0, cell, cell)));
                }

                SpriteSliceUtility.Apply(assetPath, frames, pivot);
                count++;
            }

            return count;
        }

        // "RollDownLeft" → ("Roll", "DownLeft"), "DownLeft" → ("Walk", "DownLeft")
        private static (string Action, string Direction) SplitAction(string word)
        {
            foreach (string action in new[] { "Roll", "Slash" })
            {
                if (word.StartsWith(action, StringComparison.Ordinal))
                {
                    return (action, word.Substring(action.Length));
                }
            }

            return ("Walk", word);
        }

        // "attack_presenter" → "AttackPresenter" (팩마다 다른 애니메이션 이름 표기를 통일)
        private static string ToPascalCase(string snake)
        {
            var builder = new System.Text.StringBuilder(snake.Length);
            foreach (string word in snake.Split('_'))
            {
                if (word.Length > 0)
                {
                    builder.Append(char.ToUpperInvariant(word[0])).Append(word, 1, word.Length - 1);
                }
            }

            return builder.ToString();
        }

        private static T ReadJson<T>(string path)
        {
            return JsonUtility.FromJson<T>(File.ReadAllText(path));
        }

        // ---- JSON 형식 (필드 이름은 파일의 키와 같아야 해서 네이밍 규칙 예외) ----
#pragma warning disable IDE1006
        [Serializable]
        private sealed class PixelOverSheet
        {
            public PixelOverFrame[] frames;
        }

        [Serializable]
        private sealed class PixelOverFrame
        {
            public PixelOverRect frame;
        }

        [Serializable]
        private sealed class PixelOverRect
        {
            public int x;
            public int y;
            public int w;
            public int h;

            public RectInt ToRect() => new RectInt(x, y, w, h);
        }

        [Serializable]
        private sealed class SovaCharacter
        {
            public int[] frame_size;
            public int[] pivot;
        }
#pragma warning restore IDE1006
    }
}
