using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace MyGameDevTools.SceneLoading.Editor
{
    static class AISkillsInstaller
    {
        internal const string SkillsFolderName = "AIAssistantSkills";
        internal const string SkillPrefix = "my-scene-manager-";

        internal static readonly string[] AgentSkillFolders = { ".agents/skills", ".claude/skills" };

        [MenuItem("Tools/My Scene Manager/Install AI Skills for Coding Agents")]
        static void InstallFromMenu()
        {
            PackageInfo package = PackageInfo.FindForAssembly(typeof(AISkillsInstaller).Assembly);
            if (package == null)
            {
                Debug.LogError("[MySceneManager] Could not find the My Scene Manager package, so the AI Skills were not installed.");
                return;
            }

            string sourceRoot = Path.Combine(package.resolvedPath, SkillsFolderName);
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;

            IReadOnlyList<string> skills = Install(sourceRoot, projectRoot);
            Debug.Log($"[MySceneManager] Installed {skills.Count} AI Skills into {string.Join(" and ", AgentSkillFolders)}: {string.Join(", ", skills)}.");
        }

        internal static IReadOnlyList<string> Install(string sourceRoot, string projectRoot)
        {
            if (!Directory.Exists(sourceRoot))
                throw new DirectoryNotFoundException($"No AI Skills folder at '{sourceRoot}'.");

            string[] skillFolders = Directory.GetDirectories(sourceRoot)
                .Where(folder => File.Exists(Path.Combine(folder, "SKILL.md")))
                .OrderBy(folder => folder)
                .ToArray();
            string[] skillNames = skillFolders.Select(Path.GetFileName).ToArray();

            foreach (string agentFolder in AgentSkillFolders)
            {
                string destinationRoot = Path.Combine(projectRoot, agentFolder);
                Directory.CreateDirectory(destinationRoot);

                foreach (string existing in Directory.GetDirectories(destinationRoot, SkillPrefix + "*"))
                {
                    if (!skillNames.Contains(Path.GetFileName(existing)))
                        Directory.Delete(existing, true);
                }

                foreach (string skillFolder in skillFolders)
                {
                    string destination = Path.Combine(destinationRoot, Path.GetFileName(skillFolder));
                    if (Directory.Exists(destination))
                        Directory.Delete(destination, true);
                    CopyFolder(skillFolder, destination);
                }
            }

            return skillNames;
        }

        static void CopyFolder(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (string file in Directory.GetFiles(source).Where(file => !file.EndsWith(".meta")))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));

            foreach (string folder in Directory.GetDirectories(source))
                CopyFolder(folder, Path.Combine(destination, Path.GetFileName(folder)));
        }
    }
}
