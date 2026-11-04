// 역할 : 외부 UI 팩의 창·빈 버튼·바·슬롯 스프라이트에 9-slice border 를 자동으로 지정한다.
//        가운데 열(행)과 똑같은 열(행)이 이어지는 구간을 늘어나는 영역으로 보고, 그 바깥을 border 로 잡는다.
//        아이콘·글자가 그려진 버튼은 픽셀만으로 구분하기 어려워, 이름 규칙(StretchKeywords)에 맞는 것만 대상으로 하고
//        나머지는 border 를 0 으로 되돌려 원래 크기로 쓰게 한다.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.UI
{
    public static class UiNineSliceDetector
    {
        private const string MenuPath = "Tools/Sprite/UI 9-slice 자동 지정";
        private const string UiRoot = "Assets/90.ExternalAssets/UI";

        // 파일 이름에 이 단어가 있으면 늘려 쓰는 그림으로 본다 (빈 버튼, 창, 수치 바, 인벤토리 배경·슬롯)
        private static readonly string[] StretchKeywords = { "Empty_", "Window", "CountBar", "Inventory_background", "Inventory_Slot" };

        // 늘어나는 구간이 이 픽셀 수보다 짧으면 9-slice 로 보지 않는다
        private const int MinStretchLength = 2;

        [MenuItem(MenuPath)]
        public static void DetectAll()
        {
            if (!Directory.Exists(UiRoot))
            {
                Debug.LogWarning($"[NineSlice] UI 폴더가 없습니다: {UiRoot}");
                return;
            }

            var applied = new List<string>();
            var skipped = new List<string>();
            foreach (string file in Directory.GetFiles(UiRoot, "*.png", SearchOption.AllDirectories))
            {
                string assetPath = file.Replace('\\', '/');
                if (!IsStretchable(Path.GetFileName(assetPath)))
                {
                    ClearBorder(assetPath);
                    continue;
                }

                if (TryApply(assetPath, out Vector4 border))
                {
                    applied.Add($"{Path.GetFileName(assetPath)} L{border.x} B{border.y} R{border.z} T{border.w}");
                }
                else
                {
                    skipped.Add(Path.GetFileName(assetPath));
                }
            }

            Debug.Log($"[NineSlice] 지정 {applied.Count}개:\n{string.Join("\n", applied)}\n건너뜀 {skipped.Count}개: {string.Join(", ", skipped)}");
        }

        private static bool IsStretchable(string fileName)
        {
            foreach (string keyword in StretchKeywords)
            {
                if (fileName.IndexOf(keyword, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ClearBorder(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite)
            {
                return;
            }

            bool changed = EnsureSingle(importer, assetPath);
            if (importer.spriteBorder != Vector4.zero)
            {
                importer.spriteBorder = Vector4.zero;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        // 비어 있는 Multiple(Unity 6 기본값)을 Single 로 바꾼다. 이미 잘린 시트는 그대로 둔다.
        private static bool EnsureSingle(TextureImporter importer, string assetPath)
        {
            if (importer.spriteImportMode != SpriteImportMode.Multiple
                || AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath).Length > 1)
            {
                return false;
            }

            importer.spriteImportMode = SpriteImportMode.Single;
            return true;
        }

        private static bool TryApply(string assetPath, out Vector4 border)
        {
            border = Vector4.zero;
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite)
            {
                return false;
            }

            // 잘린 시트(스프라이트 여러 개)는 건드리지 않는다
            EnsureSingle(importer, assetPath);
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                return false;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                texture.LoadImage(File.ReadAllBytes(assetPath));
                Color32[] pixels = texture.GetPixels32();
                int width = texture.width;
                int height = texture.height;

                // 1. 가로: 가운데 열과 색이 같은 열이 이어지는 구간
                (int left, int right) = FindStretchRun(width, x => ColumnEquals(pixels, width, height, x, width / 2, SameColor));
                bool horizontalByColor = right - left + 1 >= MinStretchLength;

                // 2. 세로: 가운데 행과 색이 같은 행이 이어지는 구간 (y 는 아래 기준)
                (int bottom, int top) = FindStretchRun(height, y => RowEquals(pixels, width, y, height / 2, SameColor));
                bool verticalByColor = top - bottom + 1 >= MinStretchLength;

                // 3. 두 축 모두 색으로 못 찾으면 가운데에 그림(아이콘·글자)이 있는 것이라 건너뛴다.
                //    한 축만 실패하면(그라데이션 창 등) 그 축은 투명도 모양만 비교해 둥근 모서리만 border 로 잡는다.
                if (!horizontalByColor && !verticalByColor)
                {
                    return false;
                }

                if (!horizontalByColor)
                {
                    (left, right) = FindStretchRun(width, x => ColumnEquals(pixels, width, height, x, width / 2, SameAlpha));
                }

                if (!verticalByColor)
                {
                    (bottom, top) = FindStretchRun(height, y => RowEquals(pixels, width, y, height / 2, SameAlpha));
                }

                if (right - left + 1 < MinStretchLength || top - bottom + 1 < MinStretchLength)
                {
                    return false;
                }

                // 3. border = 늘어나는 구간 바깥 픽셀 수 (L, B, R, T)
                border = new Vector4(left, bottom, width - 1 - right, height - 1 - top);
                importer.spriteBorder = border;
                importer.SaveAndReimport();
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        // 가운데에서 양쪽으로 넓히며 조건이 참인 연속 구간 [start, end] 를 찾는다.
        private static (int Start, int End) FindStretchRun(int length, Func<int, bool> isSameAsCenter)
        {
            int center = length / 2;
            int start = center;
            int end = center;
            while (start - 1 >= 0 && isSameAsCenter(start - 1))
            {
                start--;
            }

            while (end + 1 < length && isSameAsCenter(end + 1))
            {
                end++;
            }

            return (start, end);
        }

        private static bool ColumnEquals(Color32[] pixels, int width, int height, int x, int centerX, Func<Color32, Color32, bool> same)
        {
            for (int y = 0; y < height; y++)
            {
                if (!same(pixels[y * width + x], pixels[y * width + centerX]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool RowEquals(Color32[] pixels, int width, int y, int centerY, Func<Color32, Color32, bool> same)
        {
            for (int x = 0; x < width; x++)
            {
                if (!same(pixels[y * width + x], pixels[centerY * width + x]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SameAlpha(Color32 a, Color32 b)
        {
            return a.a == b.a;
        }

        private static bool SameColor(Color32 a, Color32 b)
        {
            // 완전히 투명한 픽셀은 색 값과 관계없이 같은 것으로 본다
            if (a.a == 0 && b.a == 0)
            {
                return true;
            }

            return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        }
    }
}
