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
            try
            {
                if (String.IsNullOrEmpty(txtFrom.Text) || String.IsNullOrEmpty(txtTo.Text))
                {
                    MessageBox.Show("Please Set From and To Textes");
                }
                else
                {
                    FolderBrowserDialog fd = new FolderBrowserDialog();
                    if (fd.ShowDialog() == DialogResult.OK)
                    {
                        string from = txtFrom.Text;
                        string to = txtTo.Text;
                        string selectedFolder = fd.SelectedPath;
                        string parent = Directory.GetParent(selectedFolder).ToString();
                        string newFolder = parent + $"\\New-For-{to}";
                        await CreateDirectoryStructure(newFolder, selectedFolder, from, to);

                        await CopyAndReplace(newFolder, selectedFolder, from, to);
                        MessageBox.Show("All Done.");
                    }
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show("We have Error."+ Environment.NewLine + ex.Message);
            }
            

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

                Invoke(new Action(() =>
                {
                    progress.Maximum = files.Length;
                    progress.Value = 0;
                })) ;
                foreach (var item in files)
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
    }
}
