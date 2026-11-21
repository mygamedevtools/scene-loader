using System.IO;
using MyGameDevTools.SceneLoading.Editor;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class AISkillsInstallerTests
    {
        string _root;
        string _source;
        string _project;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "AISkillsInstallerTests-" + Path.GetRandomFileName());
            _source = Path.Combine(_root, "source");
            _project = Path.Combine(_root, "project");
            Directory.CreateDirectory(_project);

            WriteFile(_source, "my-scene-manager-alpha/SKILL.md", "alpha v2");
            WriteFile(_source, "my-scene-manager-alpha/SKILL.md.meta", "meta");
            WriteFile(_source, "my-scene-manager-alpha/references/notes.md", "notes");
            WriteFile(_source, "my-scene-manager-beta/SKILL.md", "beta");
            WriteFile(_source, "not-a-skill/readme.txt", "no SKILL.md here");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Test]
        public void CopiesEverySkillIntoEveryAgentFolder()
        {
            var installed = AISkillsInstaller.Install(_source, _project);

            CollectionAssert.AreEqual(new[] { "my-scene-manager-alpha", "my-scene-manager-beta" }, installed);
            foreach (string agentFolder in AISkillsInstaller.AgentSkillFolders)
            {
                string folder = Path.Combine(_project, agentFolder);
                Assert.AreEqual("alpha v2", File.ReadAllText(Path.Combine(folder, "my-scene-manager-alpha/SKILL.md")));
                Assert.AreEqual("notes", File.ReadAllText(Path.Combine(folder, "my-scene-manager-alpha/references/notes.md")));
                Assert.IsTrue(File.Exists(Path.Combine(folder, "my-scene-manager-beta/SKILL.md")));
                Assert.IsFalse(File.Exists(Path.Combine(folder, "my-scene-manager-alpha/SKILL.md.meta")), ".meta files must not be copied.");
                Assert.IsFalse(Directory.Exists(Path.Combine(folder, "not-a-skill")), "Folders without a SKILL.md are not skills.");
            }
        }

        [Test]
        public void ReplacesOlderCopiesAndRemovesDroppedSkills()
        {
            string folder = Path.Combine(_project, AISkillsInstaller.AgentSkillFolders[0]);
            WriteFile(folder, "my-scene-manager-alpha/SKILL.md", "alpha v1");
            WriteFile(folder, "my-scene-manager-alpha/stale.md", "left over from v1");
            WriteFile(folder, "my-scene-manager-renamed/SKILL.md", "a skill this version no longer ships");

            AISkillsInstaller.Install(_source, _project);

            Assert.AreEqual("alpha v2", File.ReadAllText(Path.Combine(folder, "my-scene-manager-alpha/SKILL.md")));
            Assert.IsFalse(File.Exists(Path.Combine(folder, "my-scene-manager-alpha/stale.md")));
            Assert.IsFalse(Directory.Exists(Path.Combine(folder, "my-scene-manager-renamed")));
        }

        [Test]
        public void LeavesOtherSkillsAlone()
        {
            string folder = Path.Combine(_project, AISkillsInstaller.AgentSkillFolders[0]);
            WriteFile(folder, "someone-elses-skill/SKILL.md", "theirs");

            AISkillsInstaller.Install(_source, _project);

            Assert.AreEqual("theirs", File.ReadAllText(Path.Combine(folder, "someone-elses-skill/SKILL.md")));
        }

        [Test]
        public void ThePackageShipsSkillsWhereUnityAIAssistantFindsThem()
        {
            PackageInfo package = PackageInfo.FindForAssembly(typeof(AISkillsInstaller).Assembly);
            Assert.IsNotNull(package);

            string skillsRoot = Path.Combine(package.resolvedPath, AISkillsInstaller.SkillsFolderName);
            Assert.IsTrue(Directory.Exists(skillsRoot), $"No '{AISkillsInstaller.SkillsFolderName}' folder in the package.");

            string[] skills = Directory.GetDirectories(skillsRoot, AISkillsInstaller.SkillPrefix + "*");
            Assert.IsNotEmpty(skills);
            foreach (string skill in skills)
                Assert.IsTrue(File.Exists(Path.Combine(skill, "SKILL.md")), $"'{skill}' has no SKILL.md.");
        }

        static void WriteFile(string root, string relativePath, string contents)
        {
            string path = Path.Combine(root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, contents);
        }
    }
}
