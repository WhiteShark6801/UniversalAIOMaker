using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace UniversalAIOMaker
{
    public class MainForm : Form
    {
        // UI Controls
        private TabControl tabControl;
        private TabPage tabSettings;
        private TabPage tabISOList;
        private TabPage tabProcess;
        
        // Settings tab controls
        private TextBox txtNT32Path;
        private TextBox txtNT64Path;
        private TextBox txt9xPath;
        private TextBox txtModernPath;
        private TextBox txtLonghornPath;
        private Button btnBrowseNT32;
        private Button btnBrowseNT64;
        private Button btnBrowse9x;
        private Button btnBrowseModern;
        private Button btnBrowseLonghorn;
        private CheckBox chkAutoGenerate;
        private CheckBox chkGraphicsMode;
        private CheckBox chkBackgroundImage;
        private TextBox txtBackgroundImage;
        private Button btnBrowseBackground;
        private Button btnScanISOs;
        
        // ISO List tab controls
        private DataGridView dgvISOList;
        private Button btnGenerateNames;
        private Button btnStartProcess;
        
        // Process tab controls
        private TextBox txtLog;
        private ProgressBar progressBar;
        
        // Data storage
        private List<ISOEntry> isoEntries = new List<ISOEntry>();
        public bool DebugMode { get; set; } = false;
        public string OutputISOPath { get; set; } = "";
        public bool PauseBeforeISO { get; set; } = false;
        public bool GraphicsMode { get; set; } = false;
        public bool UseBackgroundImage { get; set; } = false;
        public string BackgroundImagePath { get; set; } = "";
        
        public MainForm()
        {
            InitializeComponents();
            LoadSettings();
        }
        
        private void InitializeComponents()
        {
            // Form settings
            this.Text = "UniversalAIOMaker - GUI Edition";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            
            // Create tab control
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };
            
            // Create tabs
            CreateSettingsTab();
            CreateISOListTab();
            CreateProcessTab();
            
            tabControl.TabPages.Add(tabSettings);
            tabControl.TabPages.Add(tabISOList);
            tabControl.TabPages.Add(tabProcess);
            
            this.Controls.Add(tabControl);
        }
        
        private void CreateSettingsTab()
        {
            tabSettings = new TabPage("ISO Paths");
            
            // NT x86 Path
            Label lblNT32 = new Label { Text = "Windows NT x86 ISOs:", Location = new Point(20, 20), Size = new Size(150, 20) };
            txtNT32Path = new TextBox { Location = new Point(180, 18), Size = new Size(550, 23) };
            btnBrowseNT32 = new Button { Text = "Browse...", Location = new Point(740, 17), Size = new Size(100, 25) };
            btnBrowseNT32.Click += (s, e) => BrowseFolder(txtNT32Path);
            
            // NT amd64 Path
            Label lblNT64 = new Label { Text = "Windows NT amd64 ISOs:", Location = new Point(20, 60), Size = new Size(150, 20) };
            txtNT64Path = new TextBox { Location = new Point(180, 58), Size = new Size(550, 23) };
            btnBrowseNT64 = new Button { Text = "Browse...", Location = new Point(740, 57), Size = new Size(100, 25) };
            btnBrowseNT64.Click += (s, e) => BrowseFolder(txtNT64Path);
            
            // 9x Path
            Label lbl9x = new Label { Text = "Windows 9x ISOs:", Location = new Point(20, 100), Size = new Size(150, 20) };
            txt9xPath = new TextBox { Location = new Point(180, 98), Size = new Size(550, 23) };
            btnBrowse9x = new Button { Text = "Browse...", Location = new Point(740, 97), Size = new Size(100, 25) };
            btnBrowse9x.Click += (s, e) => BrowseFolder(txt9xPath);
            
            // Modern Path
            Label lblModern = new Label { Text = "Modern Windows ISOs:", Location = new Point(20, 140), Size = new Size(150, 20) };
            txtModernPath = new TextBox { Location = new Point(180, 138), Size = new Size(550, 23) };
            btnBrowseModern = new Button { Text = "Browse...", Location = new Point(740, 137), Size = new Size(100, 25) };
            btnBrowseModern.Click += (s, e) => BrowseFolder(txtModernPath);

            // Longhorn builds through 4093 only
            Label lblLonghorn = new Label { Text = "Longhorn (Only for builds up to 4093):", Location = new Point(20, 180), Size = new Size(250, 20) };
            txtLonghornPath = new TextBox { Location = new Point(280, 178), Size = new Size(450, 23) };
            btnBrowseLonghorn = new Button { Text = "Browse...", Location = new Point(740, 177), Size = new Size(100, 25) };
            btnBrowseLonghorn.Click += (s, e) => BrowseFolder(txtLonghornPath);
            
            // Output ISO Path
            Label lblOutput = new Label { Text = "Output ISO Location:", Location = new Point(20, 220), Size = new Size(150, 20) };
            TextBox txtOutputPath = new TextBox { Location = new Point(180, 218), Size = new Size(550, 23) };
            txtOutputPath.Tag = "OutputPath"; // Tag for settings
            Button btnBrowseOutput = new Button { Text = "Browse...", Location = new Point(740, 217), Size = new Size(100, 25) };
            btnBrowseOutput.Click += (s, e) => {
                using (var dialog = new SaveFileDialog())
                {
                    dialog.Filter = "ISO files (*.iso)|*.iso|All files (*.*)|*.*";
                    dialog.DefaultExt = "iso";
                    dialog.FileName = "UniversalAIOMaker_Media.iso";
                    dialog.Title = "Select Output ISO Location";
                    
                    if (!string.IsNullOrEmpty(txtOutputPath.Text))
                    {
                        try
                        {
                            dialog.InitialDirectory = Path.GetDirectoryName(txtOutputPath.Text);
                            dialog.FileName = Path.GetFileName(txtOutputPath.Text);
                        }
                        catch { }
                    }
                    
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        txtOutputPath.Text = dialog.FileName;
                        SaveSettings();
                    }
                }
            };
            
            // Auto-generate option
            chkAutoGenerate = new CheckBox 
            { 
                Text = "Auto-generate boot folder names and menu entries", 
                Location = new Point(20, 260), 
                Size = new Size(400, 20),
                Checked = true
            };
            
            // Debug mode option
            CheckBox chkDebugMode = new CheckBox
            {
                Text = "Enable debug mode (verbose command output)",
                Location = new Point(20, 290),
                Size = new Size(400, 20),
                Checked = false
            };
            chkDebugMode.CheckedChanged += (s, e) => {
                DebugMode = chkDebugMode.Checked;
            };
            
            // Pause before ISO creation option
            CheckBox chkPauseBeforeISO = new CheckBox
            {
                Text = "Pause before creating ISO (for manual modifications)",
                Location = new Point(20, 320),
                Size = new Size(400, 20),
                Checked = false
            };
            chkPauseBeforeISO.CheckedChanged += (s, e) => {
                PauseBeforeISO = chkPauseBeforeISO.Checked;
            };

            // Graphics mode option
            chkGraphicsMode = new CheckBox
            {
                Text = "Enable graphics mode (graphicsmode -1 800 600)",
                Location = new Point(20, 350),
                Size = new Size(400, 20),
                Checked = false
            };
            chkGraphicsMode.CheckedChanged += (s, e) => {
                GraphicsMode = chkGraphicsMode.Checked;
                UpdateBackgroundImageControls();
                SaveSettings();
            };

            // Background image option
            chkBackgroundImage = new CheckBox
            {
                Text = "Background image (BMP):",
                Location = new Point(20, 380),
                Size = new Size(160, 20),
                Checked = false
            };
            chkBackgroundImage.CheckedChanged += (s, e) => {
                UseBackgroundImage = chkBackgroundImage.Checked;
                UpdateBackgroundImageControls();
                SaveSettings();
            };

            txtBackgroundImage = new TextBox
            {
                Location = new Point(180, 378),
                Size = new Size(550, 23),
                Enabled = false
            };
            txtBackgroundImage.TextChanged += (s, e) => {
                BackgroundImagePath = txtBackgroundImage.Text;
            };

            btnBrowseBackground = new Button
            {
                Text = "Browse...",
                Location = new Point(740, 377),
                Size = new Size(100, 25),
                Enabled = false
            };
            btnBrowseBackground.Click += (s, e) => {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Filter = "Bitmap files (*.bmp)|*.bmp";
                    dialog.DefaultExt = "bmp";
                    dialog.CheckFileExists = true;
                    dialog.Title = "Select Background Image";
                    dialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

                    if (!string.IsNullOrEmpty(txtBackgroundImage.Text))
                    {
                        try
                        {
                            dialog.InitialDirectory = Path.GetDirectoryName(txtBackgroundImage.Text);
                            dialog.FileName = Path.GetFileName(txtBackgroundImage.Text);
                        }
                        catch { }
                    }

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        txtBackgroundImage.Text = dialog.FileName;
                        BackgroundImagePath = dialog.FileName;
                        SaveSettings();
                    }
                }
            };

            // Scan ISOs button
            btnScanISOs = new Button 
            { 
                Text = "Scan ISOs", 
                Location = new Point(370, 420), 
                Size = new Size(150, 35),
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold)
            };
            btnScanISOs.Click += BtnScanISOs_Click;
            
            // Info label
            Label lblInfo = new Label
            {
                Text = "Select directories containing ISO files and output location, then click 'Scan ISOs' to continue.",
                Location = new Point(20, 470),
                Size = new Size(820, 40),
                ForeColor = Color.DarkBlue
            };
            
            // Background image is only available with graphics mode enabled
            UpdateBackgroundImageControls();

            // Add controls to tab
            tabSettings.Controls.AddRange(new Control[] 
            { 
                lblNT32, txtNT32Path, btnBrowseNT32,
                lblNT64, txtNT64Path, btnBrowseNT64,
                lbl9x, txt9xPath, btnBrowse9x,
                lblModern, txtModernPath, btnBrowseModern,
                lblLonghorn, txtLonghornPath, btnBrowseLonghorn,
                lblOutput, txtOutputPath, btnBrowseOutput,
                chkAutoGenerate, chkDebugMode, chkPauseBeforeISO,
                chkGraphicsMode,
                chkBackgroundImage, txtBackgroundImage, btnBrowseBackground,
                btnScanISOs, lblInfo
            });
        }
        
        private void CreateISOListTab()
        {
            tabISOList = new TabPage("ISO Configuration");
            
            // DataGridView for ISO entries
            dgvISOList = new DataGridView
            {
                Location = new Point(10, 10),
                Size = new Size(800, 450),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                MultiSelect = false
            };
            
            // Define columns
            dgvISOList.Columns.Add(new DataGridViewTextBoxColumn { Name = "FileName", HeaderText = "ISO File Name", ReadOnly = true, FillWeight = 28 });
            dgvISOList.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type", ReadOnly = true, FillWeight = 22 });
            dgvISOList.Columns.Add(new DataGridViewTextBoxColumn { Name = "BootFolder", HeaderText = "Boot/WinPE Folder (4 chars)", FillWeight = 16 });
            dgvISOList.Columns.Add(new DataGridViewTextBoxColumn { Name = "SourcesFolder", HeaderText = "Sources Folder (Longhorn, 7 chars)", FillWeight = 22 });
            dgvISOList.Columns.Add(new DataGridViewTextBoxColumn { Name = "MenuEntry", HeaderText = "Menu Entry", FillWeight = 24 });
            
            // Ordering buttons
            Button btnMoveUp = new Button
            {
                Text = "Move Up ▲",
                Location = new Point(825, 10),
                Size = new Size(90, 30)
            };
            btnMoveUp.Click += BtnMoveUp_Click;
            
            Button btnMoveDown = new Button
            {
                Text = "Move Down ▼",
                Location = new Point(825, 50),
                Size = new Size(90, 30)
            };
            btnMoveDown.Click += BtnMoveDown_Click;
            
            // Auto-generate button
            btnGenerateNames = new Button
            {
                Text = "Auto-Generate Names",
                Location = new Point(10, 470),
                Size = new Size(150, 30)
            };
            btnGenerateNames.Click += BtnGenerateNames_Click;
            
            // Start processing button
            btnStartProcess = new Button
            {
                Text = "Start Processing",
                Location = new Point(805, 470),
                Size = new Size(160, 30),
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
                BackColor = Color.LightGreen
            };
            btnStartProcess.Click += BtnStartProcess_Click;
            
            // Info label
            Label lblInfo = new Label
            {
                Text = "Edit boot folders (4 chars), Longhorn sources folders (7 chars), and menu labels. Use Move Up/Down to reorder.",
                Location = new Point(170, 475),
                Size = new Size(520, 20),
                ForeColor = Color.DarkBlue
            };
            
            tabISOList.Controls.AddRange(new Control[] { dgvISOList, btnMoveUp, btnMoveDown, btnGenerateNames, btnStartProcess, lblInfo });
        }
        
        private void CreateProcessTab()
        {
            tabProcess = new TabPage("Processing");
            
            // Progress bar
            progressBar = new ProgressBar
            {
                Location = new Point(10, 10),
                Size = new Size(850, 30),
                Style = ProgressBarStyle.Continuous
            };
            
            // Log text box
            txtLog = new TextBox
            {
                Location = new Point(10, 50),
                Size = new Size(850, 470),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9)
            };
            
            tabProcess.Controls.AddRange(new Control[] { progressBar, txtLog });
        }
        
        private void UpdateBackgroundImageControls()
        {
            // Background image only makes sense when graphics mode is enabled
            bool graphicsEnabled = chkGraphicsMode.Checked;
            bool backgroundEnabled = graphicsEnabled && chkBackgroundImage.Checked;

            chkBackgroundImage.Enabled = graphicsEnabled;
            txtBackgroundImage.Enabled = backgroundEnabled;
            btnBrowseBackground.Enabled = backgroundEnabled;
        }

        private void BrowseFolder(TextBox textBox)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select folder containing ISO files";
                if (!string.IsNullOrEmpty(textBox.Text) && Directory.Exists(textBox.Text))
                {
                    dialog.SelectedPath = textBox.Text;
                }
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    textBox.Text = dialog.SelectedPath;
                    SaveSettings();
                }
            }
        }
        
        private void BtnScanISOs_Click(object sender, EventArgs e)
        {
            isoEntries.Clear();
            dgvISOList.Rows.Clear();
            
            int isoCount = 0;
            
            // Scan NT x86
            if (!string.IsNullOrEmpty(txtNT32Path.Text) && Directory.Exists(txtNT32Path.Text))
            {
                var files = Directory.GetFiles(txtNT32Path.Text, "*.iso");
                foreach (var file in files)
                {
                    isoEntries.Add(new ISOEntry
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        Type = ISOType.NT32,
                        TypeDisplay = "NT x86"
                    });
                    isoCount++;
                }
            }
            
            // Scan NT amd64
            if (!string.IsNullOrEmpty(txtNT64Path.Text) && Directory.Exists(txtNT64Path.Text))
            {
                var files = Directory.GetFiles(txtNT64Path.Text, "*.iso");
                foreach (var file in files)
                {
                    isoEntries.Add(new ISOEntry
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        Type = ISOType.NT64,
                        TypeDisplay = "NT amd64"
                    });
                    isoCount++;
                }
            }
            
            // Scan 9x
            if (!string.IsNullOrEmpty(txt9xPath.Text) && Directory.Exists(txt9xPath.Text))
            {
                var files = Directory.GetFiles(txt9xPath.Text, "*.iso");
                foreach (var file in files)
                {
                    isoEntries.Add(new ISOEntry
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        Type = ISOType.Win9x,
                        TypeDisplay = "Win 9x"
                    });
                    isoCount++;
                }
            }
            
            // Scan Modern
            if (!string.IsNullOrEmpty(txtModernPath.Text) && Directory.Exists(txtModernPath.Text))
            {
                var files = Directory.GetFiles(txtModernPath.Text, "*.iso");
                foreach (var file in files)
                {
                    isoEntries.Add(new ISOEntry
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        Type = ISOType.Modern,
                        TypeDisplay = "Vista/Later"
                    });
                    isoCount++;
                }
            }

            // Longhorn (supported builds through 4093)
            if (!string.IsNullOrEmpty(txtLonghornPath.Text) && Directory.Exists(txtLonghornPath.Text))
            {
                var files = Directory.GetFiles(txtLonghornPath.Text, "*.iso");
                foreach (var file in files)
                {
                    isoEntries.Add(new ISOEntry
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        Type = ISOType.Longhorn,
                        TypeDisplay = "Longhorn (Only for builds up to 4093)"
                    });
                    isoCount++;
                }
            }
            
            if (isoCount == 0)
            {
                MessageBox.Show("No ISO files found in the specified directories.", "No ISOs Found", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Auto-generate if enabled
            if (chkAutoGenerate.Checked)
            {
                AutoGenerateNames();
            }
            
            // Populate grid
            PopulateGrid();
            
            // Switch to ISO List tab
            tabControl.SelectedTab = tabISOList;
            
            MessageBox.Show($"Found {isoCount} ISO file(s).\n\nReview the configurations and click 'Start Processing' when ready.", 
                "ISOs Scanned", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void AutoGenerateNames()
        {
            var nt32Counter = 1;
            var nt64Counter = 1;
            var win9xCounter = 1;
            var longhornCounter = 1;
            
            foreach (var entry in isoEntries)
            {
                switch (entry.Type)
                {
                    case ISOType.NT32:
                        entry.BootFolder = $"NT{nt32Counter:D2}";
                        entry.MenuEntry = ExtractMenuName(entry.FileName, "Windows NT x86");
                        nt32Counter++;
                        break;
                        
                    case ISOType.NT64:
                        entry.BootFolder = $"N{nt64Counter:D3}";
                        entry.MenuEntry = ExtractMenuName(entry.FileName, "Windows NT amd64");
                        nt64Counter++;
                        break;
                        
                    case ISOType.Win9x:
                        entry.BootFolder = $"W9x{win9xCounter}";
                        entry.MenuEntry = ExtractMenuName(entry.FileName, "Windows 9x");
                        win9xCounter++;
                        break;
                        
                    case ISOType.Modern:
                        entry.BootFolder = "Modn";
                        entry.MenuEntry = ExtractMenuName(entry.FileName, "Windows Vista and later");
                        break;

                    case ISOType.Longhorn:
                        entry.BootFolder = $"LH{longhornCounter:D2}";
                        entry.SourcesFolder = $"LHSRC{longhornCounter:D2}";
                        entry.MenuEntry = ExtractMenuName(entry.FileName, "Windows Longhorn (build up to 4093)");
                        longhornCounter++;
                        break;
                }
            }
        }
        
        private string ExtractMenuName(string fileName, string defaultName)
        {
            // Remove extension
            string name = Path.GetFileNameWithoutExtension(fileName);
            
            // Try to extract meaningful name
            // Remove common patterns
            name = System.Text.RegularExpressions.Regex.Replace(name, @"[_-]", " ");
            name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ");
            name = name.Trim();
            
            // If name is too generic or empty, use default
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
            {
                return defaultName;
            }
            
            // Capitalize and limit length
            if (name.Length > 50)
            {
                name = name.Substring(0, 47) + "...";
            }
            
            return name;
        }
        
        private void PopulateGrid()
        {
            dgvISOList.Rows.Clear();
            
            foreach (var entry in isoEntries)
            {
                int rowIndex = dgvISOList.Rows.Add(
                    entry.FileName,
                    entry.TypeDisplay,
                    entry.BootFolder,
                    entry.SourcesFolder,
                    entry.MenuEntry
                );
                
                dgvISOList.Rows[rowIndex].Tag = entry;
            }
        }
        
        private void BtnGenerateNames_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("This will regenerate all boot folder names and menu entries.\n\nAny manual changes will be lost. Continue?",
                "Confirm Regeneration", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                AutoGenerateNames();
                PopulateGrid();
            }
        }
        
        private void BtnMoveUp_Click(object sender, EventArgs e)
        {
            if (dgvISOList.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a row to move up.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            int selectedIndex = dgvISOList.SelectedRows[0].Index;
            if (selectedIndex == 0)
            {
                MessageBox.Show("Cannot move the first item up.", "Already at Top", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            // Swap entries
            var temp = isoEntries[selectedIndex];
            isoEntries[selectedIndex] = isoEntries[selectedIndex - 1];
            isoEntries[selectedIndex - 1] = temp;
            
            // Refresh grid
            PopulateGrid();
            
            // Reselect the moved row
            dgvISOList.ClearSelection();
            dgvISOList.Rows[selectedIndex - 1].Selected = true;
        }
        
        private void BtnMoveDown_Click(object sender, EventArgs e)
        {
            if (dgvISOList.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a row to move down.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            int selectedIndex = dgvISOList.SelectedRows[0].Index;
            if (selectedIndex == isoEntries.Count - 1)
            {
                MessageBox.Show("Cannot move the last item down.", "Already at Bottom", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            // Swap entries
            var temp = isoEntries[selectedIndex];
            isoEntries[selectedIndex] = isoEntries[selectedIndex + 1];
            isoEntries[selectedIndex + 1] = temp;
            
            // Refresh grid
            PopulateGrid();
            
            // Reselect the moved row
            dgvISOList.ClearSelection();
            dgvISOList.Rows[selectedIndex + 1].Selected = true;
        }
        
        private void BtnStartProcess_Click(object sender, EventArgs e)
        {
            // Update entries from grid
            for (int i = 0; i < dgvISOList.Rows.Count; i++)
            {
                var row = dgvISOList.Rows[i];
                var entry = (ISOEntry)row.Tag;
                
                entry.BootFolder = row.Cells["BootFolder"].Value?.ToString() ?? "";
                entry.SourcesFolder = row.Cells["SourcesFolder"].Value?.ToString() ?? "";
                entry.MenuEntry = row.Cells["MenuEntry"].Value?.ToString() ?? "";
            }
            
            // Validate
            var errors = ValidateEntries();

            // Validate optional background image (only used with graphics mode)
            BackgroundImagePath = txtBackgroundImage.Text.Trim();
            if (chkGraphicsMode.Checked && chkBackgroundImage.Checked && !string.IsNullOrEmpty(BackgroundImagePath))
            {
                if (!File.Exists(BackgroundImagePath))
                {
                    errors.Add($"Background image not found: {BackgroundImagePath}");
                }
                else if (!BackgroundImagePath.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("Background image must be a BMP file (.bmp).");
                }
            }

            if (errors.Count > 0)
            {
                MessageBox.Show("Please fix the following errors:\n\n" + string.Join("\n", errors),
                    "Validation Errors", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // Check for duplicates
            var duplicateBootFolders = isoEntries
                .Where(e => e.Type != ISOType.Modern)
                .GroupBy(e => e.BootFolder)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
                
            if (duplicateBootFolders.Count > 0)
            {
                MessageBox.Show("Duplicate boot folder names found:\n\n" + string.Join(", ", duplicateBootFolders) +
                    "\n\nEach ISO (except Modern) must have a unique boot folder name.",
                    "Duplicate Names", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var duplicateSourcesFolders = isoEntries
                .Where(e => e.Type == ISOType.Longhorn)
                .GroupBy(e => e.SourcesFolder, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateSourcesFolders.Count > 0)
            {
                MessageBox.Show("Duplicate Longhorn sources folder names found:\n\n" + string.Join(", ", duplicateSourcesFolders) +
                    "\n\nEach Longhorn ISO must have a unique sources folder name.",
                    "Duplicate Names", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var longhornFolderNames = isoEntries.Where(e => e.Type == ISOType.Longhorn)
                .SelectMany(e => new[] { e.BootFolder, e.SourcesFolder });
            var otherBootFolders = isoEntries
                .Where(e => e.Type == ISOType.NT32 || e.Type == ISOType.NT64)
                .Select(e => e.BootFolder);
            var folderConflicts = longhornFolderNames.Concat(otherBootFolders)
                .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            if (folderConflicts.Count > 0)
            {
                MessageBox.Show("Longhorn boot/sources folders must not conflict with each other or NT boot folders:\n\n" +
                    string.Join(", ", folderConflicts), "Folder Name Conflict", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // Confirm and start
            if (MessageBox.Show($"Ready to process {isoEntries.Count} ISO(s).\n\nThis may take a while. Continue?",
                "Start Processing", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                tabControl.SelectedTab = tabProcess;
                StartProcessing();
            }
        }
        
        private List<string> ValidateEntries()
        {
            var errors = new List<string>();
            
            for (int i = 0; i < isoEntries.Count; i++)
            {
                var entry = isoEntries[i];
                
                // Validate boot folder
                if (string.IsNullOrWhiteSpace(entry.BootFolder))
                {
                    errors.Add($"Row {i + 1}: Boot folder name is empty");
                }
                else if (entry.BootFolder.Length != 4)
                {
                    errors.Add($"Row {i + 1}: Boot folder name must be exactly 4 characters (current: '{entry.BootFolder}')");
                }
                else if (!System.Text.RegularExpressions.Regex.IsMatch(entry.BootFolder, @"^[a-zA-Z0-9]+$"))
                {
                    errors.Add($"Row {i + 1}: Boot folder name must contain only letters and numbers");
                }

                if (entry.Type == ISOType.Longhorn)
                {
                    if (string.IsNullOrWhiteSpace(entry.SourcesFolder))
                    {
                        errors.Add($"Row {i + 1}: Longhorn sources folder name is empty");
                    }
                    else if (entry.SourcesFolder.Length != 7)
                    {
                        errors.Add($"Row {i + 1}: Longhorn sources folder name must be exactly 7 characters (current: '{entry.SourcesFolder}')");
                    }
                    else if (!System.Text.RegularExpressions.Regex.IsMatch(entry.SourcesFolder, @"^[a-zA-Z0-9]+$"))
                    {
                        errors.Add($"Row {i + 1}: Longhorn sources folder name must contain only letters and numbers");
                    }
                    else if (entry.SourcesFolder.Equals("sources", StringComparison.OrdinalIgnoreCase) ||
                        entry.SourcesFolder.Equals("windows", StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"Row {i + 1}: Longhorn sources folder name cannot be 'sources' or 'windows' because those paths are reserved in the generated media.");
                    }
                }
                
                // Validate menu entry
                if (string.IsNullOrWhiteSpace(entry.MenuEntry))
                {
                    errors.Add($"Row {i + 1}: Menu entry is empty");
                }
            }
            
            return errors;
        }
        
        private async void StartProcessing()
        {
            btnStartProcess.Enabled = false;
            progressBar.Value = 0;
            progressBar.Maximum = isoEntries.Count;
            txtLog.Clear();
            
            try
            {
                var processor = new ISOProcessor(this);
                await System.Threading.Tasks.Task.Run(() => processor.ProcessISOs(isoEntries));
                
                MessageBox.Show("Processing completed successfully!\n\nOutput: UniversalAIOMaker_Media.iso",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during processing:\n\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnStartProcess.Enabled = true;
            }
        }
        
        public void UpdateProgress(int value)
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke(new Action<int>(UpdateProgress), value);
            }
            else
            {
                progressBar.Value = value;
            }
        }
        
        public void LogMessage(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(LogMessage), message);
            }
            else
            {
                txtLog.AppendText(message + Environment.NewLine);
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
        }
        
        private bool isLoadingSettings = false;

        private void LoadSettings()
        {
            try
            {
                isLoadingSettings = true;
                string settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");
                if (File.Exists(settingsFile))
                {
                    var lines = File.ReadAllLines(settingsFile);
                    foreach (var line in lines)
                    {
                        int separatorIndex = line.IndexOf('=');
                        if (separatorIndex > 0)
                        {
                            string key = line.Substring(0, separatorIndex).Trim();
                            string value = line.Substring(separatorIndex + 1).Trim();
                            
                            switch (key)
                            {
                                case "NT32Path":
                                    txtNT32Path.Text = value;
                                    break;
                                case "NT64Path":
                                    txtNT64Path.Text = value;
                                    break;
                                case "Win9xPath":
                                    txt9xPath.Text = value;
                                    break;
                                case "ModernPath":
                                    txtModernPath.Text = value;
                                    break;
                                case "LonghornPath":
                                    txtLonghornPath.Text = value;
                                    break;
                                case "OutputPath":
                                    foreach (Control ctrl in tabSettings.Controls)
                                    {
                                        if (ctrl is TextBox && ctrl.Tag?.ToString() == "OutputPath")
                                        {
                                            ctrl.Text = value;
                                            OutputISOPath = value;
                                            break;
                                        }
                                    }
                                    break;
                                case "GraphicsMode":
                                    GraphicsMode = value == "1";
                                    chkGraphicsMode.Checked = GraphicsMode;
                                    break;
                                case "UseBackgroundImage":
                                    UseBackgroundImage = value == "1";
                                    chkBackgroundImage.Checked = UseBackgroundImage;
                                    break;
                                case "BackgroundImagePath":
                                    txtBackgroundImage.Text = value;
                                    BackgroundImagePath = value;
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    // Set default output path
                    OutputISOPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UniversalAIOMaker_Media.iso");
                    foreach (Control ctrl in tabSettings.Controls)
                    {
                        if (ctrl is TextBox && ctrl.Tag?.ToString() == "OutputPath")
                        {
                            ctrl.Text = OutputISOPath;
                            break;
                        }
                    }
                }

                // Make sure the background image controls reflect the loaded state
                chkGraphicsMode.Checked = GraphicsMode;
                chkBackgroundImage.Checked = UseBackgroundImage;
                UpdateBackgroundImageControls();
            }
            catch { }
            finally
            {
                isLoadingSettings = false;
            }
        }
        
        private void SaveSettings()
        {
            if (isLoadingSettings)
            {
                return;
            }

            try
            {
                string settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");
                
                // Get output path from textbox
                string outputPath = "";
                foreach (Control ctrl in tabSettings.Controls)
                {
                    if (ctrl is TextBox && ctrl.Tag?.ToString() == "OutputPath")
                    {
                        outputPath = ctrl.Text;
                        OutputISOPath = outputPath;
                        break;
                    }
                }
                
                var lines = new[]
                {
                    $"NT32Path={txtNT32Path.Text}",
                    $"NT64Path={txtNT64Path.Text}",
                    $"Win9xPath={txt9xPath.Text}",
                    $"ModernPath={txtModernPath.Text}",
                    $"LonghornPath={txtLonghornPath.Text}",
                    $"OutputPath={outputPath}",
                    $"GraphicsMode={(GraphicsMode ? "1" : "0")}",
                    $"UseBackgroundImage={(UseBackgroundImage ? "1" : "0")}",
                    $"BackgroundImagePath={txtBackgroundImage.Text}"
                };
                File.WriteAllLines(settingsFile, lines);
            }
            catch { }
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveSettings();
            base.OnFormClosing(e);
        }
    }
    
    public enum ISOType
    {
        NT32,
        NT64,
        Win9x,
        Modern,
        Longhorn
    }
    
    public class ISOEntry
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public ISOType Type { get; set; }
        public string TypeDisplay { get; set; }
        public string BootFolder { get; set; }
        public string SourcesFolder { get; set; }
        public string MenuEntry { get; set; }
    }
}
