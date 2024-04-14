using System;
using UnityEngine;

namespace EmbeddedPluginHelper
{
    [CreateAssetMenu]
    public class AssemblyDefinitionData : ScriptableObject
    {
        [SerializeField] public string guid;
        [SerializeField] public string[] defineSymbols;
        [SerializeField] public ReposioryInfo reposiory;
        [SerializeField] public bool autoInstall;
        [SerializeField] public string path;
    }

    [Serializable]
    public class ReposioryInfo
    {
        [SerializeField] public string type;
        [SerializeField] public string url;
        [SerializeField] public string revision;
        [SerializeField] public string path;
    }
}
