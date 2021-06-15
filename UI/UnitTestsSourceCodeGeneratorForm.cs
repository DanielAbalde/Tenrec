using System;
using System.Drawing;
using System.Windows.Forms;

namespace Tenrec.UI
{
    public partial class UnitTestsSourceCodeGeneratorForm : Form
    {
        public UnitTestsSourceCodeGeneratorForm()
        {
            InitializeComponent();
            comboBoxLanguage.SelectedIndex = 0;
            comboBoxFramework.SelectedIndex = 0;
            buttonGenerate.Enabled = false;
        }

        #region Methods
        private string[] GetFileFolders()
        {
            if (string.IsNullOrEmpty(textBoxFiles.Text))
                return null;
            return textBoxFiles.Text.Split(Environment.NewLine.ToCharArray());
        }
        private string GetOutputFolder()
        {
            return textBoxOutputFolder.Text;
        }
        private string GetOutputName()
        {
            return textBoxName.Text;
        }
        private string GetLanguage()
        {
            var lan = comboBoxLanguage.Items[comboBoxLanguage.SelectedIndex].ToString();
            if (lan.Equals("C#"))
                return "cs";
            return lan;
        }
        private string GetFramework()
        {
            var fra = comboBoxFramework.Items[comboBoxFramework.SelectedIndex].ToString();
            return fra.ToLower();
        }
        private bool CanGenerate(out string message)
        {
            var folders = GetFileFolders();
            if (folders == null || folders.Length == 0)
            {
                message = "Missing Grasshopper file folders.";
                return false;
            }
            else
            {
                foreach(var folder in folders)
                {
                    if (!System.IO.Directory.Exists(folder))
                    {
                        message = $"File folder {folder} doesn't exists.";
                        return false;
                    }
                }
            } 
            var outputFolder = GetOutputFolder();
            if (string.IsNullOrEmpty(outputFolder))
            {
                message = "Missing output folders.";
                return false;
            }
            if (!System.IO.Directory.Exists(outputFolder))
            {
                message = "Output folder doesn't exists.";
                return false;
            }
            if (string.IsNullOrEmpty(GetOutputName()))
            {
                message = "Missing output name.";
                return false;
            }
            if (string.IsNullOrEmpty(GetLanguage()))
            {
                message = "Missing langugage.";
                return false;
            }
            if (string.IsNullOrEmpty(GetFramework()))
            {
                message = "Missing framework.";
                return false;
            }
            message = "Ready to generate.";
            return true;
        }
        private void UpdateState()
        {
            buttonGenerate.Enabled = CanGenerate(out string message);
            textBoxLog.Text = message;
            textBoxLog.ForeColor = Color.Black;
        }
        #endregion

        #region Handlers 
        private void UnitTestsSourceCodeGeneratorForm_Load(object sender, EventArgs e)
        {
            var activeDoc = Grasshopper.Instances.ActiveCanvas?.Document;
            if(activeDoc != null && !string.IsNullOrEmpty(activeDoc.FilePath))
            {
                textBoxFiles.Text = System.IO.Path.GetDirectoryName(activeDoc.FilePath);
            }
            textBoxOutputFolder.Text = Grasshopper.Instances.Settings.GetValue("Tenrec.OutputFolder", string.Empty);
            textBoxName.Text = Grasshopper.Instances.Settings.GetValue("Tenrec.OutputName", "TenrecAutomaticTests");
            Focus();
        }

        private void UnitTestsSourceCodeGeneratorForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Grasshopper.Instances.Settings.SetValue("Tenrec.OutputFolder", textBoxOutputFolder.Text);
            Grasshopper.Instances.Settings.SetValue("Tenrec.OutputName", textBoxName.Text);
        }

        private void buttonGenerate_Click(object sender, EventArgs e)
        {
            var folderFiles = GetFileFolders();
            var outputFolder = GetOutputFolder();
            var outputName = GetOutputName();
            var language = GetLanguage();
            var framework = GetFramework(); 
            textBoxLog.Text = Generator.CreateAutoTestSourceFile(folderFiles, outputFolder, outputName, language, framework);
            if (textBoxLog.Text.Contains("successfully"))
                textBoxLog.ForeColor = Grasshopper.GUI.GH_GraphicsUtil.BlendColour(Color.Green, Color.Black, 0.5);
            else
                textBoxLog.ForeColor = Grasshopper.GUI.GH_GraphicsUtil.BlendColour(Color.Red, Color.Black, 0.5);

        }

        private void textBoxFiles_TextChanged(object sender, EventArgs e)
        {
            UpdateState();
        }
        private void textBoxOutputFolder_TextChanged(object sender, EventArgs e)
        {
            UpdateState();
        }
        private void textBoxName_TextChanged(object sender, EventArgs e)
        {
            UpdateState();
        }
        private void comboBoxLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateState();
        }
        private void comboBoxFramework_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateState();
        }
        #endregion

    }
}
