using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace EmbeddedPluginHelper.Editor
{
    public class AssemblyDefinitionDataImporter : AssetPostprocessor
    {
        public static readonly string assetFileExtension = "asset";

        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            foreach (var path in importedAssets.Where(path => path.Split('.')[^1] == assetFileExtension))
            {
                var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionData>(path);
                if (asset == null)
                    continue;

                var assemblyPath = AssetDatabase.GUIDToAssetPath(asset.guid);
                var assembly = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(assemblyPath);
                if (!GUID.TryParse(asset.guid, out _))
                    continue;

                if (assembly == null && asset.autoInstall)
                {
                    Installer.GitInstall(asset.path, asset.reposiory.url, asset.reposiory.revision, asset.reposiory.path, true);
                    AssetDatabase.Refresh();
                }

                var requireDefines = PlayerSettings
                    .GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
                    .Split(';')
                    .Concat(asset.defineSymbols)
                    .Distinct()
                    .ToArray();
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, requireDefines);
            }
        }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(AssemblyDefinitionData))]
    public class AssemblyDefinitionDataEditor : UnityEditor.Editor
    {
        private static class Style
        {
            public static GUIContent GUID = new("GUID", "");
            public static GUIContent AssemblyDefinition = new("Assembly Definition", "");
            public static GUIContent DefineSymbols = new("Define Symbols", "");
            public static GUIContent Repository = new("Repository", "");
            public static GUIContent AutoInstall = new("Auto Install", "");
            public static GUIContent Path = new("Install Path", "");
        }

        private SerializedProperty sp_defineSymbols;
        private SerializedProperty sp_repository;
        private SerializedProperty sp_autoInstall;
        private SerializedProperty sp_path;

        private void OnEnable()
        {
            sp_defineSymbols = serializedObject.FindProperty(nameof(AssemblyDefinitionData.defineSymbols));
            sp_repository = serializedObject.FindProperty(nameof(AssemblyDefinitionData.reposiory));
            sp_autoInstall = serializedObject.FindProperty(nameof(AssemblyDefinitionData.autoInstall));
            sp_path = serializedObject.FindProperty(nameof(AssemblyDefinitionData.path));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var assemblyDefinitionData = target as AssemblyDefinitionData;

            var assemblyPath = AssetDatabase.GUIDToAssetPath(assemblyDefinitionData.guid);
            var assembly = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(assemblyPath);
            var newAssembly = EditorGUILayout.ObjectField(Style.AssemblyDefinition, assembly, typeof(AssemblyDefinitionAsset), allowSceneObjects: false) as AssemblyDefinitionAsset;

            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.LabelField(Style.GUID, new GUIContent(assemblyDefinitionData.guid));
                EditorGUILayout.LabelField(Style.Path, new GUIContent(assemblyPath));
            }

            EditorGUILayout.PropertyField(sp_defineSymbols, Style.DefineSymbols, includeChildren: true);

            if (newAssembly != assembly)
            {
                var goid = GlobalObjectId.GetGlobalObjectIdSlow(newAssembly);
                assemblyDefinitionData.guid = goid.assetGUID.ToString();
                EditorUtility.SetDirty(assemblyDefinitionData);
            }

            EditorGUILayout.PropertyField(sp_repository, Style.Repository, includeChildren: true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(sp_autoInstall, Style.AutoInstall, includeChildren: true);
            EditorGUILayout.PropertyField(sp_path, Style.Path, includeChildren: true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
