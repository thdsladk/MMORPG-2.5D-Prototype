// 역할 : Assets/90.ExternalAssets 아래 외부 에셋의 임포트 설정을 경로 규칙으로 통일한다.
//        (도트 스프라이트 = Point/밉맵 없음/무압축, 3D 팔레트 텍스처 = Point, SFX = 메모리 상주·모노)
//        최초 임포트 때만 적용하고, 이후 수동 조정은 유지한다. 강제 재적용은 메뉴에서 한다.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.AssetImport
{
    public sealed class ExternalAssetImportRules : AssetPostprocessor
    {
        public const string Root = "Assets/90.ExternalAssets/";

        private const string ReapplyMenuPath = "Tools/Asset Import/외부 에셋 임포트 규칙 재적용";
        private const int DefaultPixelsPerUnit = 100;

        // 팩 폴더별 PPU. 화면상 캐릭터 키를 맞추는 기준값이며 A-6 슬라이스 단계에서 조정한다.
        private static readonly Dictionary<string, int> PixelsPerUnitByPack = new Dictionary<string, int>
        {
            { "Characters/Hormelz_8DirKnight", 64 },
        };

        private static readonly string[] SpriteCategories = { "Characters/", "Monsters/", "VFX/", "UI/", "Icons/" };

        private static bool s_forceReapply;

        private bool ShouldApply => s_forceReapply || assetImporter.importSettingsMissing;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root) || !ShouldApply)
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            string relative = assetPath.Substring(Root.Length);

            // 1. 2D 도트 스프라이트
            if (IsSpriteCategory(relative))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = GetPixelsPerUnit(relative);
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 8192;
                return;
            }

            // 2. 3D 배경 텍스처 (팔레트 텍스처라 Point 로 색 경계를 유지)
            if (relative.StartsWith("Environment/"))
            {
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
            }
        }

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root) || !ShouldApply)
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;

            // 배경 소품은 정적 메시이므로 애니메이션·카메라·라이트를 가져오지 않는다.
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.isReadable = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root + "SFX/") || !ShouldApply)
            {
                return;
            }

            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            importer.loadInBackground = false;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.ADPCM;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
        }

        [MenuItem(ReapplyMenuPath)]
        private static void ReapplyAll()
        {
            s_forceReapply = true;
            try
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture t:Model t:AudioClip", new[] { Root.TrimEnd('/') });
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (string guid in guids)
                    {
                        AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                Debug.Log($"[ExternalAssetImportRules] {guids.Length}개 에셋에 임포트 규칙을 다시 적용했습니다.");
            }
            finally
            {
                s_forceReapply = false;
            }
        }

        private static bool IsSpriteCategory(string relative)
        {
            foreach (string category in SpriteCategories)
            {
                if (relative.StartsWith(category))
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetPixelsPerUnit(string relative)
        {
            foreach (KeyValuePair<string, int> pair in PixelsPerUnitByPack)
            {
                if (relative.StartsWith(pair.Key + "/"))
                {
                    return pair.Value;
                }
            }

            return DefaultPixelsPerUnit;
        }
    }
}
