// 역할 : 스프라이트 시트를 프레임 목록대로 잘라 TextureImporter 에 적용하는 공용 기능.
//        (프레임 사각형 적용, 기존 spriteID 재사용, 발밑 피벗 계산, PNG 크기 읽기)
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Game.Editor.SpriteSlicing
{
    // 시트 안의 프레임 하나. y 는 이미지 위쪽 기준 픽셀 좌표다 (Unity 좌표 변환은 Apply 에서 한다).
    public readonly struct SliceFrame
    {
        public readonly string Name;
        public readonly RectInt TopLeftRect;

        public SliceFrame(string name, RectInt topLeftRect)
        {
            Name = name;
            TopLeftRect = topLeftRect;
        }
    }

    public static class SpriteSliceUtility
    {
        // 프로젝트 표준 8방향 순서. 모든 팩의 방향 이름을 이 순서로 맞춘다.
        public static readonly string[] Directions = { "S", "SE", "E", "NE", "N", "NW", "W", "SW" };

        private const byte AlphaThreshold = 8;

        public static void Apply(string assetPath, IReadOnlyList<SliceFrame> frames, Vector2 normalizedPivot)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[SpriteSlice] TextureImporter 가 없습니다: {assetPath}");
                return;
            }

            Vector2Int size = ReadPngSize(assetPath);

            // 1. Multiple 모드로 바꾼다 (데이터 프로바이더가 모드를 보고 사각형을 저장한다)
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            // 2. 같은 이름의 기존 spriteID 를 재사용해 다시 잘라도 참조가 끊기지 않게 한다
            var existingIds = new Dictionary<string, GUID>();
            foreach (SpriteRect old in provider.GetSpriteRects())
            {
                existingIds[old.name] = old.spriteID;
            }

            var rects = new SpriteRect[frames.Count];
            var nameIdPairs = new List<SpriteNameFileIdPair>(frames.Count);
            for (int i = 0; i < frames.Count; i++)
            {
                SliceFrame frame = frames[i];
                RectInt r = frame.TopLeftRect;
                GUID id = existingIds.TryGetValue(frame.Name, out GUID found) ? found : GUID.Generate();

                rects[i] = new SpriteRect
                {
                    name = frame.Name,
                    spriteID = id,
                    rect = new Rect(r.x, size.y - r.y - r.height, r.width, r.height),
                    alignment = SpriteAlignment.Custom,
                    pivot = normalizedPivot,
                };
                nameIdPairs.Add(new SpriteNameFileIdPair(frame.Name, id));
            }

            // 3. 적용 후 재임포트
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(nameIdPairs);
            provider.Apply();
            importer.SaveAndReimport();
        }

        // 프레임 안에서 불투명 픽셀의 아래 끝 중앙을 발밑으로 보고 정규화 피벗을 구한다.
        public static Vector2 FindFeetPivot(string assetPath, RectInt topLeftFrame)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                texture.LoadImage(File.ReadAllBytes(assetPath));
                Color32[] pixels = texture.GetPixels32();
                int width = texture.width;
                int height = texture.height;

                int minX = int.MaxValue;
                int maxX = int.MinValue;
                int maxTopY = int.MinValue;
                for (int y = 0; y < topLeftFrame.height; y++)
                {
                    int topY = topLeftFrame.y + y;
                    int row = height - 1 - topY;
                    for (int x = 0; x < topLeftFrame.width; x++)
                    {
                        if (pixels[row * width + topLeftFrame.x + x].a < AlphaThreshold)
                        {
                            continue;
                        }

                        minX = Math.Min(minX, x);
                        maxX = Math.Max(maxX, x);
                        maxTopY = Math.Max(maxTopY, y);
                    }
                }

                if (maxTopY < 0)
                {
                    return new Vector2(0.5f, 0f);
                }

                float pivotX = (minX + maxX + 1) * 0.5f / topLeftFrame.width;
                float pivotY = 1f - (maxTopY + 1f) / topLeftFrame.height;
                return new Vector2(pivotX, pivotY);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        // 위쪽 기준 픽셀 피벗을 Unity 정규화 피벗으로 바꾼다.
        public static Vector2 ToNormalizedPivot(Vector2Int pixelPivotTopLeft, Vector2Int frameSize)
        {
            return new Vector2(
                (float)pixelPivotTopLeft.x / frameSize.x,
                1f - (float)pixelPivotTopLeft.y / frameSize.y);
        }

        // 텍스처를 로드하지 않고 PNG 헤더(IHDR)에서 크기만 읽는다.
        public static Vector2Int ReadPngSize(string assetPath)
        {
            using (FileStream stream = File.OpenRead(assetPath))
            {
                var header = new byte[24];
                if (stream.Read(header, 0, header.Length) != header.Length)
                {
                    throw new InvalidDataException($"PNG 헤더를 읽을 수 없습니다: {assetPath}");
                }

                int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
                int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
                return new Vector2Int(width, height);
            }
        }
    }
}
