// 역할 : 팔레트 텍스처를 쓰는 외부 3D 팩의 FBX 머티리얼을 URP Lit 머티리얼로 바꿔 연결한다.
//        FBX 가 팔레트 텍스처를 찾지 못해 하얗게 나오는 팩(WizP, Mini Japan)이 대상이다.
//        원본 FBX 는 건드리지 않고, 04.Materials/Environment/<팩>/ 에 머티리얼을 만들어 ModelImporter remap 으로 연결한다.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.AssetImport
{
    public static class ExternalModelMaterialRemapper
    {
        private const string MenuPath = "Tools/Asset Import/외부 3D 팩 URP 머티리얼 연결";
        private const string EnvironmentRoot = "Assets/90.ExternalAssets/Environment/";
        private const string OutputRoot = "Assets/04.Materials/Environment/";
        private const string LitShaderName = "Universal Render Pipeline/Lit";

        // 팩 폴더 이름, 팩 안 팔레트 텍스처 경로
        private static readonly (string Pack, string Palette)[] PaletteRules =
        {
            ("WizP_StylizedAsianVillage", "ColorPalette.png"),
            ("ElyDev_MiniJapan", "Textures/ColorPalette.png"),
        };

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

        [MenuItem(MenuPath)]
        public static void RemapAll()
        {
            int modelCount = 0;
            foreach ((string pack, string palette) in PaletteRules)
            {
                modelCount += RemapPack(pack, palette);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[MaterialRemap] 모델 {modelCount}개의 머티리얼을 URP Lit 으로 연결했습니다.");
        }

        private static int RemapPack(string pack, string paletteRelativePath)
        {
            string packFolder = EnvironmentRoot + pack;
            var palette = AssetDatabase.LoadAssetAtPath<Texture2D>($"{packFolder}/{paletteRelativePath}");
            if (palette == null)
            {
                return 0;
            }

            string outputFolder = OutputRoot + pack;
            Directory.CreateDirectory(outputFolder);

            // 같은 이름의 머티리얼은 팩 안에서 하나를 공유한다
            var created = new Dictionary<string, Material>();
            int count = 0;
            foreach (string file in Directory.GetFiles(packFolder, "*.fbx", SearchOption.AllDirectories))
            {
                string modelPath = file.Replace('\\', '/');
                if (!(AssetImporter.GetAtPath(modelPath) is ModelImporter importer))
                {
                    continue;
                }

                // 1. FBX 안 머티리얼 이름을 모아 같은 이름의 URP 머티리얼을 만든다
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
                {
                    if (!(asset is Material source) || created.ContainsKey(source.name))
                    {
                        continue;
                    }

                    created[source.name] = GetOrCreateMaterial($"{outputFolder}/{source.name}.mat", palette);
                }

                // 2. remap 등록 후 재임포트
                foreach (KeyValuePair<string, Material> pair in created)
                {
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key), pair.Value);
                }

                importer.SaveAndReimport();
                count++;
            }

            return count;
        }

        private static Material GetOrCreateMaterial(string path, Texture2D palette)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(LitShaderName));
                AssetDatabase.CreateAsset(material, path);
            }

            // 팔레트 색을 그대로 보이도록 흰색 + 무광
            material.SetTexture(BaseMapId, palette);
            material.SetColor(BaseColorId, Color.white);
            material.SetFloat(SmoothnessId, 0f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
