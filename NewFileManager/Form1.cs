using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace NewFileManager
{
    public partial class Form1 : Form
    {
        private string _currentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private DataGridView _dataGridView;

        public Form1()
        {
            InitializeComponent();
            SetupControls();
            LoadFiles();
        }

        private void SetupControls()
        {
            this.Text = "Файловый менеджер";
            this.Size = new System.Drawing.Size(800, 600);

            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = System.Drawing.Color.LightGray };

            Button btnRefresh = new Button { Text = "Обновить", Location = new System.Drawing.Point(10, 5), AutoSize = true };
            btnRefresh.Click += (s, e) => LoadFiles();

            Button btnSave = new Button { Text = "Сохранить список", Location = new System.Drawing.Point(90, 5), AutoSize = true };
            btnSave.Click += (s, e) => SaveFileList();

            Button btnLoad = new Button { Text = "Загрузить список", Location = new System.Drawing.Point(220, 5), AutoSize = true };
            btnLoad.Click += (s, e) => LoadFileList();

            topPanel.Controls.Add(btnRefresh);
            topPanel.Controls.Add(btnSave);
            topPanel.Controls.Add(btnLoad);

            this.Controls.Add(topPanel);

            _dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false
            };

            _dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColName", HeaderText = "Имя", Width = 200 });
            _dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColType", HeaderText = "Тип", Width = 100 });
            _dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColDate", HeaderText = "Дата изменения", Width = 150 });
            _dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColSize", HeaderText = "Размер", Width = 100 });

            _dataGridView.CellDoubleClick += (s, e) => OpenItem(e.RowIndex);
            this.Controls.Add(_dataGridView);
        }


        private void LoadFiles()
        {
            try
            {
                _dataGridView.Rows.Clear();
                var files = Directory.GetFiles(_currentPath);
                var dirs = Directory.GetDirectories(_currentPath);

                foreach (var path in files)
                {
                    var fi = new FileInfo(path);
                    _dataGridView.Rows.Add(fi.Name, "Файл", fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm"), FormatSize(fi.Length));
                }

                foreach (var path in dirs)
                {
                    var di = new DirectoryInfo(path);
                    _dataGridView.Rows.Add(di.Name, "Папка", di.LastWriteTime.ToString("dd.MM.yyyy HH:mm"), "-");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenItem(int rowIndex)
        {
            if (rowIndex < 0) return;
            string name = _dataGridView.Rows[rowIndex].Cells["ColName"].Value.ToString();
            string type = _dataGridView.Rows[rowIndex].Cells["ColType"].Value.ToString();
            string fullPath = Path.Combine(_currentPath, name);

            try
            {
                if (type == "Папка")
                {
                    _currentPath = fullPath;
                    LoadFiles();
                }
                else
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fullPath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть: " + ex.Message);
            }
        }

        private void SaveFileList()
        {
            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "JSON files (*.json)|*.json" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var items = new List<FileItem>();
                    foreach (DataGridViewRow row in _dataGridView.Rows)
                    {
                        if (row.IsNewRow) continue;
                        items.Add(new FileItem
                        {
                            Name = row.Cells["ColName"].Value.ToString(),
                            Type = row.Cells["ColType"].Value.ToString(),
                            ModifiedDate = row.Cells["ColDate"].Value.ToString(),
                            Size = row.Cells["ColSize"].Value.ToString()
                        });
                    }

                    string json = JsonConvert.SerializeObject(items, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(sfd.FileName, json);
                    MessageBox.Show("Список сохранен!");
                }
            }
        }

        private void LoadFileList()
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "JSON files (*.json)|*.json" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string json = File.ReadAllText(ofd.FileName);
                        var items = JsonConvert.DeserializeObject<List<FileItem>>(json);

                        _dataGridView.Rows.Clear();
                        foreach (var item in items)
                        {
                            _dataGridView.Rows.Add(item.Name, item.Type, item.ModifiedDate, item.Size);
                        }
                        MessageBox.Show("Список загружен!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка чтения файла: " + ex.Message);
                    }
                }
            }
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:F2} {sizes[order]}";
        }


    }

    public class FileItem
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string ModifiedDate { get; set; } = "";
        public string Size { get; set; } = "";
    }
}
