using System;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;

namespace EmbeddedPluginHelper.Editor
{
    public static class Installer
    {
        public static readonly string tempPath = FileUtil.GetUniqueTempPathInProject();

        private const string git = "git";
        private const string metaFileExtension = "meta";

        public static bool GitInstall(string installPath, RepositoryInfo repositoryInfo, bool forceInstall = false)
        {
            return repositoryInfo switch
            {
                { type: git } => GitInstall(installPath, repositoryInfo.url, repositoryInfo.revision, repositoryInfo.path, forceInstall),
                _ => false
            };
        }

        public static bool GitInstall(string installPath, string gitURL, string gitRevision = null, string gitPath = null, bool forceInstall = false)
        {
            var target = string.IsNullOrEmpty(gitPath)
                ? ""
                : gitPath.Split('/')[^1];
            var targetPath = Path.Combine(installPath, target);

            if (!forceInstall)
            {
                if (Directory.Exists(targetPath))
                    return true;
            }

            var repositoryName = gitURL.Split('/')[^1].Split('.')[0];
            var tempInstallPath = Path.Combine(tempPath, repositoryName);
            var tempTargetPath = Path.Combine(tempInstallPath, gitPath);

            bool success;
            try
            {
                if (!Directory.Exists(tempPath))
                    Directory.CreateDirectory(tempPath);

                Clone(gitURL, repositoryName, tempInstallPath);

                if (!string.IsNullOrEmpty(gitRevision))
                    Checkout(gitRevision, tempInstallPath);

                Move(tempTargetPath, targetPath, installPath);

                success = true;
            }
            catch
            {
                success = false;
            }

            CleanUp();
            return success;
        }

        private static void Clone(string gitURL, string repositoryName, string tempInstallPath)
        {
            var cloneInfo = new System.Diagnostics.ProcessStartInfo()
            {
                WorkingDirectory = tempPath,
                FileName = git,
                Arguments = $"clone {gitURL} {repositoryName}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var cloneProcess = System.Diagnostics.Process.Start(cloneInfo);
            cloneProcess.WaitForExit();

            if (!Directory.Exists(tempInstallPath))
                throw new Exception();

#if DEBUG
            UnityEngine.Debug.Log($"clone {gitURL} to {tempPath}");
#endif
        }

        private static void Checkout(string gitRevision, string tempInstallPath)
        {
            var checkoutInfo = new System.Diagnostics.ProcessStartInfo()
            {
                WorkingDirectory = tempInstallPath,
                FileName = git,
                Arguments = $"checkout {gitRevision}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var checkoutProcess = System.Diagnostics.Process.Start(checkoutInfo);
            checkoutProcess.WaitForExit();

#if DEBUG
            UnityEngine.Debug.Log($"checkout {gitRevision}");
#endif
        }

        private static void Move(string tempTargetPath, string targetPath, string installPath)
        {
            if (!Directory.Exists(tempTargetPath))
                throw new Exception();

            if (!Directory.Exists(installPath))
                Directory.CreateDirectory(installPath);

            Directory.Move(tempTargetPath, targetPath);

            if (File.Exists($"{tempTargetPath}.{metaFileExtension}"))
                File.Move($"{tempTargetPath}.{metaFileExtension}", $"{targetPath}.{metaFileExtension}");

#if DEBUG
            UnityEngine.Debug.Log($"move {tempTargetPath} to {targetPath}");
#endif
        }

        private static void CleanUp()
        {
            if (Directory.Exists(tempPath))
            {
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch
                {
                    foreach (var path in Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories))
                        File.SetAttributes(path, FileAttributes.Normal);
                }
                finally
                {
                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath, true);
                }
            }
        }
    }
}
