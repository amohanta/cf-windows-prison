﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudFoundry.WindowsPrison.Tests.Rules
{
    [TestClass]
    public class TestFilesystemRule
    {
        Prison prison = null;

        [ClassInitialize]
        public static void PrisonInit(TestContext context)
        {
            Prison.Init();
        }

        [TestInitialize]
        public void PrisonTestSetup()
        {
            prison = new Prison();
            prison.Tag = "prtst";
        }

        [TestCleanup]
        public void PrisonTestCleanup()
        {
            if (prison != null)
            {
                prison.Destroy();
                prison.Dispose();
                prison = null;
            }
        }

        [TestMethod]
        public void AllowAccessInHomeDir()
        {
            // Arrange
            PrisonConfiguration prisonRules = new PrisonConfiguration();
            prisonRules.Rules = RuleTypes.FileSystem;
            prisonRules.PrisonHomeRootPath = Path.Combine(@"C:\Workspace\dea_security\PrisonHome");

            prison.Lockdown(prisonRules);

            // Act
            string exe = Utilities.CreateExeForPrison(
@"
File.WriteAllText(Guid.NewGuid().ToString(""N""), Guid.NewGuid().ToString());
", prison);
            
            Process process = prison.Execute(exe);

            process.WaitForExit();

            // Assert
            Assert.AreEqual(0, process.ExitCode);
        }

        [TestMethod]
        public void DisallowAccessEverywhereElse()
        {
            // Arrange
            PrisonConfiguration prisonRules = new PrisonConfiguration();
            prisonRules.Rules = RuleTypes.FileSystem;
            prisonRules.PrisonHomeRootPath = @"C:\Workspace\dea_security\PrisonHome";

            prison.Lockdown(prisonRules);

            // Act
            string exe = Utilities.CreateExeForPrison(
@"
  return WalkDirectoryTree(new DirectoryInfo(@""c:\""));
}

static int WalkDirectoryTree(System.IO.DirectoryInfo root)
{
    System.IO.DirectoryInfo[] subDirs = null;

    // First, process all the files directly under this folder 
    try
    {
        string adir = Guid.NewGuid().ToString(""N"");
        Directory.CreateDirectory(Path.Combine(root.FullName, adir));
        Directory.Delete(Path.Combine(root.FullName, adir));
        return 1;
    }
    catch { }

    try
    {
        string adir = Guid.NewGuid().ToString(""N"");
        File.WriteAllText(Path.Combine(root.FullName, adir), ""test"");
        File.Delete(Path.Combine(root.FullName, adir));
        return 1;
    }
    catch { }

    try
    {
        subDirs = root.GetDirectories();

        foreach (System.IO.DirectoryInfo dirInfo in subDirs)
        {
            // Resursive call for each subdirectory.
            return WalkDirectoryTree(dirInfo);
        }
    }
    catch { }
    return 0;
}

static int Dummy()
{
", prison);

            Process process = prison.Execute(exe);

            process.WaitForExit();

            // Assert
            Assert.AreEqual(0, process.ExitCode);
        }


    }
}
