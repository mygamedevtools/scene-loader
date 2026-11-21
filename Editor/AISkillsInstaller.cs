using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace MyGameDevTools.SceneLoading.Editor
{
    /// <summary>
    /// Copies the AI Skills this package ships to the folders coding agents read skills from.
    /// </summary>
    /// <remarks>
    /// Unity's AI Assistant finds the skills in the package's <c>AIAssistantSkills</c> folder by itself.
    /// Other agents (Claude Code, Codex, Copilot, Cursor, Gemini CLI) only look in folders of their own
    /// at the project root, so the skills need a copy there.
    /// <br/><br/>
    /// Running it again updates the copies. It only touches folders named after this package's skills,
    /// and removes copies of skills a newer version no longer ships, so outdated instructions never stay
    /// behind. Other skills in those folders are left alone.
    /// </remarks>
    static class AISkillsInstaller
    {
        internal const string SkillsFolderName = "AIAssistantSkills";
        internal const string SkillPrefix = "my-scene-manager-";

        /// <summary>
        /// Project-relative folders that agents read skills from: <c>.agents/skills</c> is the shared
        /// convention most agents read, and Claude Code reads <c>.claude/skills</c>.
        /// </summary>
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

        /// <summary>
        /// Copies every skill folder under <paramref name="sourceRoot"/> into each of
        /// <see cref="AgentSkillFolders"/> under <paramref name="projectRoot"/>, replacing earlier copies.
        /// </summary>
        /// <returns>The names of the installed skills.</returns>
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

                // Copies of skills a newer version renamed or dropped would otherwise keep giving agents outdated instructions.
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

            // Unity's .meta files mean nothing to agents and would clutter folders outside the Asset Database.
            foreach (string file in Directory.GetFiles(source).Where(file => !file.EndsWith(".meta")))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));

            foreach (string folder in Directory.GetDirectories(source))
                CopyFolder(folder, Path.Combine(destination, Path.GetFileName(folder)));
        }
    }
}
