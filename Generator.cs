using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tenrec.Components;

namespace Tenrec
{
    /// <summary>
    /// Produces code files automatically, generating test code based on Grasshopper files.
    /// </summary>
    /// <remarks> 
    /// This must be called once to produce the test files automatically, or every time you add or remove a <see cref="Group_UnitTest"/> or add or remove a file from the test folder.
    /// </remarks>
    public class Generator
    {
        /// <summary>
        /// Generate a code file containing all the <see cref="Group_UnitTest"/>-based tests present in Grasshopper files.
        /// </summary>
        /// <param name="ghTestFolders">The folder containing the Grasshopper files.</param>
        /// <param name="outputFolder">The folder where to save the source code file.</param>
        /// <param name="outputName">The name of the resulting code file.</param>
        /// <param name="language">Language of the code file. Currently only supports "cs"(C#).</param>
        /// <param name="testFramework">Testing framework. Currently only supports MSTest.</param>
        /// <returns>A log of the process.</returns>
        public static string CreateAutoTestSourceFile(string[] ghTestFolders,
            string outputFolder, string outputName = "TenrecGeneratedTests",
            string language = "cs", string testFramework = "mstest")
        {
            var log = new StringBuilder();
            var sb = new StringBuilder();
            var exits = false;
            var fileName = string.Empty;
            try
            {
                if (ghTestFolders == null && ghTestFolders.Length == 0)
                    throw new ArgumentNullException(nameof(outputFolder));
                if (string.IsNullOrEmpty(outputFolder))
                    throw new ArgumentNullException(nameof(outputFolder));
                if (!language.Equals("cs"))
                    throw new NotImplementedException(nameof(language));
                if (!testFramework.Equals("mstest"))
                    throw new NotImplementedException(nameof(testFramework));

                sb.AppendLine("using Microsoft.VisualStudio.TestTools.UnitTesting;");
                sb.AppendLine();
                sb.AppendLine($"namespace TenrecGeneratedTests");
                sb.AppendLine("{");
                foreach (var folder in ghTestFolders)
                {
                    var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".gh") || s.EndsWith(".ghx"));
                    if (files != null && files.Any())
                    {
                        foreach (var file in files)
                        {
                            if (OpenDocument(file, out GH_Document doc))
                            {
                                var groups = new List<IGH_DocumentObject>();
                                foreach (var obj in doc.Objects)
                                {
                                    if (obj.ComponentGuid == Group_UnitTest.ID)
                                    {
                                        groups.Add(obj);
                                    }
                                }
                                if (groups != null && groups.Any())
                                {
                                    sb.AppendLine("    [TestClass]");
                                    sb.AppendLine($"    public class AutoTest_{CodeableNickname(doc.DisplayName)}");
                                    sb.AppendLine("    {");
                                    sb.AppendLine($"        public string FilePath => @\"{doc.FilePath}\";");
                                    sb.AppendLine("        private TestContext testContextInstance;");
                                    sb.AppendLine("        public TestContext TestContext { get => testContextInstance; set => testContextInstance = value; }");
                                    foreach (var group in groups)
                                    {
                                        sb.AppendLine("        [TestMethod]");
                                        sb.AppendLine($"        public void {CodeableNickname(group.NickName)}()");
                                        sb.AppendLine("        {");
                                        sb.AppendLine($"            Tenrec.Runner.Initialize(TestContext);");
                                        sb.AppendLine($"            Tenrec.Runner.RunTenrecGroup(FilePath, new System.Guid(\"{group.InstanceGuid}\"), TestContext);");
                                        sb.AppendLine("        }");
                                    }
                                    sb.AppendLine("    }");
                                    sb.AppendLine();
                                }
                            }
                            else
                            {
                                log.AppendLine($"File {file} failed to open.");
                            }
                        }
                    }
                }
                sb.AppendLine("}");

                fileName = Path.Combine(outputFolder, outputName + ".cs");
                exits = File.Exists(fileName);
                File.WriteAllText(fileName, sb.ToString());
            }
            catch (Exception e)
            {
                log.AppendLine($"EXCEPTION: {e}.");
            }

            if (exits)
                log.AppendLine($"File successfully overwritten.");
            else
                log.AppendLine($"File successfully created.");

            return log.ToString();
        }

        private static string CodeableNickname(string nickname)
        {
            return nickname.Replace(" ", "_");
        }

        private static bool OpenDocument(string filePath, out GH_Document doc)
        {
            var io = new GH_DocumentIO();
            if (!io.Open(filePath))
            {
                doc = null;
                throw new Exception($"Failed to open file: {filePath}");
            }
            doc = io.Document;
            doc.Enabled = true;
            return true;
        }
    }
}
