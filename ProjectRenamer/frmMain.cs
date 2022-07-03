using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjectRename
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private async void btnSelectFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fd = new FolderBrowserDialog();
            if (fd.ShowDialog() == DialogResult.OK)
            {
                txtPath.Text = fd.SelectedPath;
                var slnFiles = System.IO.Directory.GetFiles(txtPath.Text, "*.sln", System.IO.SearchOption.TopDirectoryOnly).ToList();
                if (slnFiles.Count > 0)
                {
                    txtSolutionFrom.Text = Path.GetFileNameWithoutExtension(slnFiles[0]);
                }

                var directories = System.IO.Directory.GetDirectories(txtPath.Text, "*", System.IO.SearchOption.TopDirectoryOnly).Select(c => new DirectoryInfo(c).Name).ToList();
                if (directories.Count > 0)
                {
                    txtFrom.Text = GetCommonText(directories);
                }
            }
        }

        string GetCommonText(List<string> items)
        {
            var parsedItems = items.Select(c => new { item = c, splitedItems = c.Split('.').ToList() });
            var items0 = parsedItems.Where(c => c.splitedItems.Count > 0).Select(c => c.splitedItems[0]).ToList();
            var items1 = parsedItems.Where(c => c.splitedItems.Count > 1).Select(c => c.splitedItems[1]).ToList();
            var items2 = parsedItems.Where(c => c.splitedItems.Count > 2).Select(c => c.splitedItems[2]).ToList();

            var res = new StringBuilder();
            res.Append(items0.GroupBy(c => c).OrderByDescending(c => c.Count()).FirstOrDefault()?.Key);
            var item1 = items1.GroupBy(c => c).OrderByDescending(c => c.Count()).FirstOrDefault(c => c.Count() > 1)?.Key;
            if (!string.IsNullOrEmpty(item1))
            {
                res.Append("." + item1);
            }
            var item2 = items2.GroupBy(c => c).OrderByDescending(c => c.Count()).FirstOrDefault(c => c.Count() > 1)?.Key;
            if (!string.IsNullOrEmpty(item2))
            {
                res.Append("." + item2);
            }
            return res.ToString();
        }

        string[] GetAllDirectory(string path)
        {
            string[] directories = System.IO.Directory.GetDirectories(path, "*", System.IO.SearchOption.AllDirectories);
            return directories;
        }

        string[] GetAllFiles(string path)
        {
            string[] files = System.IO.Directory.GetFiles(path, "*", System.IO.SearchOption.AllDirectories);
            return files;
        }

        async Task CreateDirectoryStructure(string newPath, string basePath, string from, string to)
        {
            Directory.CreateDirectory(newPath);

            await Task.Factory.StartNew(() =>
            {
                var directorieAddress = GetAllDirectory(basePath);
                Invoke(new Action(() =>
                {
                    progress.Maximum = directorieAddress.Length;
                    progress.Value = 0;
                }));

                foreach (var item in directorieAddress)
                {
                    Invoke(new Action(() => progress.Value += 1));
                    var newItem = item.Replace(basePath, newPath);
                    newItem = newItem.Replace(from, to);
                    Directory.CreateDirectory(newItem);
                }
            });


        }


        async Task CopyAndReplace(string newPath, string basePath, string from, string to)
        {
            await Task.Factory.StartNew(() =>
            {
                var files = GetAllFiles(basePath);

                var validFiles = files.Where(c => (new FileInfo(c)).Length < 20 * 1000 * 1000).ToList();

                Invoke(new Action(() =>
                {
                    progress.Maximum = validFiles.Count;
                    progress.Value = 0;
                }));
                foreach (var item in validFiles)
                {
                    Invoke(new Action(() => progress.Value += 1));
                    var newItem = item.Replace(basePath, newPath);
                    newItem = newItem.Replace(from, to);
                    String content = File.ReadAllText(item);
                    content = content.Replace(from, to);
                    File.WriteAllText(newItem, content);
                }
            });
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (String.IsNullOrEmpty(txtFrom.Text) || String.IsNullOrEmpty(txtTo.Text) || String.IsNullOrEmpty(txtPath.Text))
                {
                    MessageBox.Show(@"Please select folder and then Set From and To texts");
                }
                else
                {

                    string from = txtFrom.Text;
                    string to = txtTo.Text;
                    string selectedFolder = txtPath.Text;
                    string parent = Directory.GetParent(selectedFolder)?.ToString();
                    string newFolder = parent + (string.IsNullOrEmpty(txtSolutionTo.Text) ? $"\\New-For-{to}" : "\\" + txtSolutionTo.Text);
                    await CreateDirectoryStructure(newFolder, selectedFolder, from, to);

                    await CopyAndReplace(newFolder, selectedFolder, from, to);

                    var oldSolutionFile = newFolder + txtSolutionFrom + ".sln";
                    if (File.Exists(oldSolutionFile))
                    {
                        System.IO.File.Move(oldSolutionFile, newFolder + txtSolutionTo+ ".sln");
                    }

                    MessageBox.Show(@"All Done.");
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show("We have Error." + Environment.NewLine + ex.Message);
            }
        }
    }
}
