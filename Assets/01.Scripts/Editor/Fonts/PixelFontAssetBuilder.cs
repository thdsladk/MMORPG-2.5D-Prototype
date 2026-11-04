// 역할 : 픽셀 폰트 Silver 로 TMP 폰트 에셋 두 종류를 만든다.
//        - Raster : 기준 크기(19pt) 그대로 굽고 Point 필터 → 19의 배수 크기에서 도트가 선명하다 (기본 UI 용)
//        - SDF    : 2배 크기로 굽는다 → 임의 크기 확대·외곽선·그림자에 쓴다 (데미지 숫자, 타이틀 등)
//        문자 세트 = ASCII + 한글 자모 + KS X 1001 한글 2350자 + 자주 쓰는 기호. 정적 아틀라스로 저장한다.
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Game.Editor.Fonts
{
    public static class PixelFontAssetBuilder
    {
        private const string MenuPath = "Tools/Font/Silver TMP 폰트 에셋 생성";
        private const string SourceFontPath = "Assets/90.ExternalAssets/Fonts/PoppyWorks_Silver/Silver.ttf";
        private const string HangulListPath = "Assets/08.Fonts/KSX1001_Hangul2350.txt";
        private const string OutputFolder = "Assets/08.Fonts";

        // Silver 는 19pt 에서 글리프 크기가 정수 픽셀이 된다 (H = 5×9, 가 = 10×11). 프로브로 확인한 값.
        public const int NativePointSize = 19;

        private const string Symbols = "·…‥“”‘’「」『』【】〈〉《》→←↑↓↔★☆♥♡※○●◎◇◆□■△▲▽▼♪♬∞×÷±≠≤≥∴°℃￦";

        [MenuItem(MenuPath)]
        public static void BuildAll()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (font == null)
            {
                Debug.LogError($"[PixelFont] 원본 폰트가 없습니다: {SourceFontPath}");
                return;
            }

            string characters = BuildCharacterSet();
            // 아틀라스는 2547자가 들어가는 최소 크기로 잡는다 (넘치면 멀티 아틀라스로 다음 장에 채워진다)
            Build(font, characters, "Silver_Raster", NativePointSize, 1, GlyphRenderMode.RASTER_HINTED, 1024, 1024, FilterMode.Point);
            Build(font, characters, "Silver_SDF", NativePointSize * 2, 4, GlyphRenderMode.SDFAA, 2048, 2048, FilterMode.Bilinear);
            AssetDatabase.SaveAssets();
        }

        private static void Build(
            Font font,
            string characters,
            string assetName,
            int pointSize,
            int padding,
            GlyphRenderMode renderMode,
            int atlasWidth,
            int atlasHeight,
            FilterMode filterMode)
        {
            string path = $"{OutputFolder}/{assetName}.asset";
            AssetDatabase.DeleteAsset(path);

            // 1. 동적 모드로 만들어 문자를 채운 뒤 정적으로 고정한다
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, pointSize, padding, renderMode, atlasWidth, atlasHeight);
            fontAsset.name = assetName;
            fontAsset.TryAddCharacters(characters, out string missing);
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

            // 2. 폰트 에셋 + 아틀라스 텍스처 + 머티리얼을 한 파일로 저장한다
            AssetDatabase.CreateAsset(fontAsset, path);
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                Texture2D atlas = fontAsset.atlasTextures[i];
                atlas.name = $"{assetName} Atlas {i}";
                atlas.filterMode = filterMode;
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            }

            fontAsset.material.name = $"{assetName} Material";
            fontAsset.material.mainTexture = fontAsset.atlasTexture;
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            EditorUtility.SetDirty(fontAsset);

            string missingInfo = string.IsNullOrEmpty(missing) ? "없음" : $"{missing.Length}자 ({Truncate(missing, 40)})";
            Debug.Log($"[PixelFont] {assetName}: 글리프 {fontAsset.characterTable.Count}자, 아틀라스 {fontAsset.atlasTextures.Length}장, 누락 {missingInfo}");
        }

        private static string BuildCharacterSet()
        {
            var builder = new StringBuilder();

            // ASCII 출력 가능 문자
            for (char c = ' '; c <= '~'; c++)
            {
                builder.Append(c);
            }

            // 한글 호환 자모 (ㄱ ~ ㅣ)
            for (char c = 'ㄱ'; c <= 'ㅣ'; c++)
            {
                builder.Append(c);
            }

            builder.Append(File.ReadAllText(HangulListPath).Trim());
            builder.Append(Symbols);
            return builder.ToString();
        }

        private static string Truncate(string text, int length)
        {
            return text.Length <= length ? text : text.Substring(0, length) + "…";
        }
    }
}
