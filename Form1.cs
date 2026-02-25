using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Universal_Data_Converter.Models;
using Universal_Data_Converter.Services;
using Universal_Data_Converter.Utils;

namespace Universal_Data_Converter
{
    public partial class Form1 : Form
    {
        // Services
        private readonly FileReaderService _fileReaderService;
        private readonly FileWriterService _fileWriterService;
        private readonly TypeDetectionService _typeDetectionService;
        private readonly SeparatorDetector _separatorDetector;
        private readonly DataConverterService? _converterService;

        // Estado da UI
        private ConversionOptions _options = new();
        private bool _isProcessing = false;

        // Componentes UI
        private TextBox? txtInputFile;
        private TextBox? txtOutputFile;
        private Button? btnBrowseInput;
        private Button? btnBrowseOutput;
        private ComboBox? cmbSeparator;
        private ComboBox? cmbEncoding;
        private NumericUpDown? numMaxRows;
        private NumericUpDown? numSampleSize;
        private RichTextBox? txtPreview;
        private RichTextBox? txtTypes;
        private RichTextBox? txtLog;
        private ProgressBar? progressBar;
        private Button? btnConvert;
        private ToolStripStatusLabel? lblStatus;
        private Panel? pnlFormatOptions;
        private FlowLayoutPanel? flpFormats;
        private System.Windows.Forms.Timer? progressTimer;

        // Componentes dinâmicos
        private ComboBox? cmbSqlTable;
        private ComboBox? cmbSqlDialect;
        private CheckBox? chkSmartTypes;
        private ComboBox? cmbJsonOrient;
        private CheckBox? chkJsonIndent;
        private CheckBox? chkJsonPreserveTypes;
        private ComboBox? cmbCsvSepOut;
        private CheckBox? chkCsvIndex;
        private ComboBox? cmbParquetCompression;
        private TextBox? txtSheetName;
        private CheckBox? chkExcelIndex;
        private CheckBox? chkExcelInferTypes;
        private TextBox? txtHtmlTitle;
        private CheckBox? chkMdIndex;

        public Form1()
        {
            InitializeComponent();

            // Inicializar serviços
            _fileReaderService = new FileReaderService();
            _fileWriterService = new FileWriterService();
            _typeDetectionService = new TypeDetectionService();
            _separatorDetector = new SeparatorDetector();
            _converterService = new DataConverterService(); // Agora deve funcionar

            // Registrar eventos do conversor
            if (_converterService != null)
            {
                _converterService.ProgressReported += OnConversionProgress;
                _converterService.ConversionCompleted += OnConversionCompleted;
                _converterService.ConversionFailed += OnConversionFailed;
            }

            SetupUI();
            RegisterEventHandlers();
        }

        private void RegisterEventHandlers()
        {
            this.Load += (s, e) => Log("Application started. Ready to convert files.");
        }

        private void SetupUI()
        {
            var panelScroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(10),
                ColumnCount = 1
            };

            // Título
            mainPanel.Controls.Add(CreateTitleSection());

            // Input Section
            mainPanel.Controls.Add(CreateInputSection());

            // Preview Section
            mainPanel.Controls.Add(CreatePreviewSection());

            // Read Settings
            mainPanel.Controls.Add(CreateReadSettingsSection());

            // Output Format
            mainPanel.Controls.Add(CreateFormatSection());

            // Format Options
            pnlFormatOptions = new Panel { Height = 80 };
            mainPanel.Controls.Add(pnlFormatOptions);

            // ===== NOVO: Opções Gerais (deve vir ANTES do Output Section) =====
            var generalOptionsPanel = CreateGeneralOptionsSection();
            mainPanel.Controls.Add(generalOptionsPanel);

            // Output Section
            mainPanel.Controls.Add(CreateOutputSection());

            // Action Buttons
            mainPanel.Controls.Add(CreateActionSection());

            // Log Section
            mainPanel.Controls.Add(CreateLogSection());

            panelScroll.Controls.Add(mainPanel);
            this.Controls.Add(panelScroll);

            // Status Bar
            var statusBar = new StatusStrip
            {
                BackColor = Color.FromArgb(224, 224, 224),
                ForeColor = Color.Black
            };
            lblStatus = new ToolStripStatusLabel("Ready. Select a file to begin.");
            statusBar.Items.Add(lblStatus);
            this.Controls.Add(statusBar);

            // Timer
            progressTimer = new System.Windows.Forms.Timer { Interval = 100 };
            progressTimer.Tick += (s, e) =>
            {
                if (_isProcessing && progressBar != null)
                    progressBar.Value = (progressBar.Value + 10) % 100;
            };

            SetupFormatOptions();
        }

        private Panel CreateTitleSection()
        {
            var panel = new Panel { Height = 80 };

            var lblTitle = new Label
            {
                Text = "UNIVERSAL DATA CONVERTER",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 40,
                Dock = DockStyle.Top
            };

            var lblSubtitle = new Label
            {
                Text = "CSV • Excel • SQL • JSON • Parquet • HTML • Markdown",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Gray,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 25,
                Dock = DockStyle.Top
            };

            panel.Controls.Add(lblSubtitle);
            panel.Controls.Add(lblTitle);
            return panel;
        }

        private GroupBox CreateInputSection()
        {
            var group = new GroupBox { Text = " INPUT FILE ", Dock = DockStyle.Fill, Height = 60 };

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(5)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));

            txtInputFile = new TextBox { Font = new Font("Consolas", 9), Dock = DockStyle.Fill };
            btnBrowseInput = new Button { Text = "Browse...", Dock = DockStyle.Right };
            btnBrowseInput.Click += BtnBrowseInput_Click;

            panel.Controls.Add(txtInputFile, 0, 0);
            panel.Controls.Add(btnBrowseInput, 1, 0);

            group.Controls.Add(panel);
            return group;
        }

        /// <summary>
        /// Cria a seção de opções gerais com checkbox para remover colunas vazias
        /// </summary>
        private Panel CreateGeneralOptionsSection()
        {
            var panel = new Panel { Height = 60 }; // AUMENTADO de 40 para 60

            var groupBox = new GroupBox
            {
                Text = " GENERAL OPTIONS ",
                Dock = DockStyle.Fill,  // ✅ Já está correto - preenche todo o espaço
                                        // Width = 500,          // ❌ REMOVA esta linha!
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            var flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 8, 10, 8),
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight
            };

            // CheckBox para remover colunas vazias
            var chkRemoveEmptyColumns = new CheckBox
            {
                Text = "Remove completely empty columns",
                Checked = _options.RemoveEmptyColumns,
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(64, 64, 64),
                Margin = new Padding(0, 2, 15, 2)
            };

            // ToolTip para explicar a funcionalidade
            var toolTip = new ToolTip();
            toolTip.SetToolTip(chkRemoveEmptyColumns,
                "Remove colunas onde TODOS os valores são NULL ou vazios.\n" +
                "Se uma coluna tiver pelo menos 1 valor válido, ela NÃO será removida.");

            chkRemoveEmptyColumns.CheckedChanged += (s, e) =>
            {
                if (s is CheckBox cb)
                {
                    _options.RemoveEmptyColumns = cb.Checked;
                    Log($"Remove empty columns: {(cb.Checked ? "ON" : "OFF")}");

                    // Atualizar o indicador visual no SetupFormatOptions
                    SetupFormatOptions();
                }
            };

            flowPanel.Controls.Add(chkRemoveEmptyColumns);

            // Opção extra: Trim spaces (pode adicionar futuramente)
            var chkTrimSpaces = new CheckBox
            {
                Text = "Trim string values",
                Checked = false,
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(64, 64, 64),
                Enabled = false,
                Margin = new Padding(0, 2, 0, 2)
            };

            var toolTipTrim = new ToolTip();
            toolTipTrim.SetToolTip(chkTrimSpaces, "Remove espaços extras no início e fim das strings (futuro)");

            flowPanel.Controls.Add(chkTrimSpaces);

            groupBox.Controls.Add(flowPanel);
            panel.Controls.Add(groupBox);

            return panel;
        }

        private GroupBox CreatePreviewSection()
        {
            var group = new GroupBox { Text = " PREVIEW & DATA TYPES ", Dock = DockStyle.Fill, Height = 200 };

            var tabControl = new TabControl { Dock = DockStyle.Fill };

            // Preview Tab
            var previewTab = new TabPage("Preview");
            txtPreview = new RichTextBox
            {
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Dock = DockStyle.Fill
            };
            previewTab.Controls.Add(txtPreview);

            // Types Tab
            var typesTab = new TabPage("Detected Types");
            txtTypes = new RichTextBox
            {
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Dock = DockStyle.Fill
            };
            typesTab.Controls.Add(txtTypes);

            tabControl.TabPages.Add(previewTab);
            tabControl.TabPages.Add(typesTab);

            group.Controls.Add(tabControl);
            return group;
        }

        private GroupBox CreateReadSettingsSection()
        {
            var group = new GroupBox { Text = " READ SETTINGS ", Dock = DockStyle.Fill, Height = 120 };

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 2,
                Padding = new Padding(5)
            };

            // Linha 0
            panel.Controls.Add(new Label { Text = "Separator:", TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            cmbSeparator = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Items = { "AUTO", "Tab", ",", ";", "|", "::" }
            };
            cmbSeparator.SelectedIndex = 0;
            cmbSeparator.SelectedIndexChanged += (s, e) => UpdatePreview();
            panel.Controls.Add(cmbSeparator, 1, 0);

            panel.Controls.Add(new Label { Text = "Encoding:", TextAlign = ContentAlignment.MiddleLeft }, 2, 0);
            cmbEncoding = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Items = { "utf-8", "latin-1", "iso-8859-1", "cp1252" }
            };
            cmbEncoding.SelectedIndex = 0;
            cmbEncoding.SelectedIndexChanged += (s, e) => UpdatePreview();
            panel.Controls.Add(cmbEncoding, 3, 0);

            var btnAutoDetect = new Button { Text = "Auto-Detect", Width = 80 };
            btnAutoDetect.Click += BtnAutoDetect_Click;
            panel.Controls.Add(btnAutoDetect, 4, 0);

            // Linha 1
            panel.Controls.Add(new Label { Text = "Max Rows:", TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
            numMaxRows = new NumericUpDown { Minimum = 100, Maximum = 5000000, Value = 200000, Width = 100 };
            panel.Controls.Add(numMaxRows, 1, 1);

            panel.Controls.Add(new Label { Text = "Sample:", TextAlign = ContentAlignment.MiddleLeft }, 2, 1);
            numSampleSize = new NumericUpDown { Minimum = 10, Maximum = 50000, Value = 500, Width = 100 };
            panel.Controls.Add(numSampleSize, 3, 1);

            var btnSmartTypes = new Button { Text = "Smart Types", Width = 80 };
            btnSmartTypes.Click += BtnSmartTypes_Click;
            panel.Controls.Add(btnSmartTypes, 4, 1);

            group.Controls.Add(panel);
            return group;
        }

        private GroupBox CreateFormatSection()
        {
            var group = new GroupBox { Text = " OUTPUT FORMAT ", Dock = DockStyle.Fill, Height = 100 };

            flpFormats = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = true,
                Padding = new Padding(5)
            };

            var formats = new (string text, string value, string? dialect)[]
            {
                ("Excel (.xlsx)", "xlsx", null),

                ("MySQL (.sql)", "sql", "mysql"),
                ("PostgreSQL (.sql)", "sql", "postgresql"),
                ("SQL Server (.sql)", "sql", "sqlserver"),
                ("SQLite (.sql)", "sql", "sqlite"),
                ("Oracle (.sql)", "sql", "oracle"),
                ("MariaDB (.sql)", "sql", "mariadb"),

                ("JSON (.json)", "json", null),
                ("CSV (.csv)", "csv", null),
                ("Parquet (.parquet)", "parquet", null),
                ("HTML (.html)", "html", null),
                ("Markdown (.md)", "md", null),
                ("Excel Legacy (.xls)", "xls", null)
            };

            foreach (var (text, value, dialect) in formats)
            {
                var radio = new RadioButton
                {
                    Text = text,
                    Tag = new { Format = value, Dialect = dialect },
                    AutoSize = true,
                    Padding = new Padding(5, 2, 5, 2),
                    BackColor = Color.White
                };

                if (text.Contains("Excel (.xlsx)"))
                    radio.Checked = true;

                radio.CheckedChanged += (s, e) =>
                {
                    if (s is RadioButton rb && rb.Checked)
                    {
                        dynamic tag = rb.Tag;

                        _options.OutputFormat = tag.Format;

                        if (tag.Dialect != null)
                            _options.SqlDialect = tag.Dialect;

                        SetupFormatOptions();
                        UpdateOutputExtension();
                    }
                };

                flpFormats.Controls.Add(radio);
            }

            group.Controls.Add(flpFormats);
            return group;
        }

        private GroupBox CreateOutputSection()
        {
            var group = new GroupBox { Text = " OUTPUT FILE ", Dock = DockStyle.Fill, Height = 60 };

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(5)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));

            txtOutputFile = new TextBox { Font = new Font("Consolas", 9), Dock = DockStyle.Fill };
            btnBrowseOutput = new Button { Text = "Save As...", Dock = DockStyle.Right };
            btnBrowseOutput.Click += BtnBrowseOutput_Click;

            panel.Controls.Add(txtOutputFile, 0, 0);
            panel.Controls.Add(btnBrowseOutput, 1, 0);

            group.Controls.Add(panel);
            return group;
        }

        private Panel CreateActionSection()
        {
            var panel = new Panel { Height = 80 };

            progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                Visible = false,
                Height = 20,
                Width = 300,
                Anchor = AnchorStyles.None
            };

            btnConvert = new Button
            {
                Text = "CONVERT",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Size = new Size(150, 40),
                Anchor = AnchorStyles.None
            };
            btnConvert.Click += BtnConvert_Click;

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true
            };
            flow.Controls.Add(progressBar);
            flow.Controls.Add(btnConvert);

            panel.Controls.Add(flow);
            return panel;
        }

        private GroupBox CreateLogSection()
        {
            var group = new GroupBox { Text = " OPERATION LOG ", Dock = DockStyle.Fill, Height = 200 };

            txtLog = new RichTextBox
            {
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Dock = DockStyle.Fill
            };

            group.Controls.Add(txtLog);
            return group;
        }

        private void SetupFormatOptions()
        {
    if (pnlFormatOptions == null) return;

    pnlFormatOptions.Controls.Clear();

    var panel = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        ColumnCount = 4,
        RowCount = 2,
        Padding = new Padding(5)
    };

    // Indicador de colunas vazias (opcional)
    if (_options.RemoveEmptyColumns)
    {
        var lblEmptyWarning = new Label
        {
            Text = "🧹 Empty columns will be removed",
            ForeColor = Color.Green,
            Font = new Font("Segoe UI", 8, FontStyle.Italic),
            AutoSize = true
        };
        panel.Controls.Add(lblEmptyWarning, 3, 1);
    }

            switch (_options.OutputFormat)
            {
                case "sql":

                    panel.Controls.Add(new Label
                    {
                        Text = "Table Name:",
                        TextAlign = ContentAlignment.MiddleLeft
                    }, 0, 0);

                    cmbSqlTable = new ComboBox
                    {
                        Width = 200,
                        Text = string.IsNullOrWhiteSpace(_options.SqlTableName)
                            ? "my_table"
                            : _options.SqlTableName
                    };

                    cmbSqlTable.TextChanged += (s, e) =>
                    {
                        if (cmbSqlTable != null)
                            _options.SqlTableName = cmbSqlTable.Text;
                    };

                    panel.Controls.Add(cmbSqlTable, 1, 0);


                    var lblDialect = new Label
                    {
                        Text = $"Dialect: {_options.SqlDialect?.ToUpper()}",
                        ForeColor = Color.Gray,
                        AutoSize = true
                    };

                    panel.Controls.Add(lblDialect, 2, 0);


                    chkSmartTypes = new CheckBox
                    {
                        Text = "Smart Typing",
                        Checked = _options.UseSmartTypes,
                        AutoSize = true
                    };

                    chkSmartTypes.CheckedChanged += (s, e) =>
                    {
                        if (chkSmartTypes != null)
                            _options.UseSmartTypes = chkSmartTypes.Checked;
                    };

                    panel.Controls.Add(chkSmartTypes, 0, 1);
                    panel.SetColumnSpan(chkSmartTypes, 2);

                    break;

                case "json":
                    panel.Controls.Add(new Label { Text = "Orientation:", TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
                    cmbJsonOrient = new ComboBox
                    {
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Items = { "records", "index", "columns", "split" }
                    };
                    cmbJsonOrient.SelectedItem = _options.JsonOrient;
                    cmbJsonOrient.SelectedIndexChanged += (s, e) => { if (cmbJsonOrient?.SelectedItem != null) _options.JsonOrient = cmbJsonOrient.SelectedItem.ToString()!; };
                    panel.Controls.Add(cmbJsonOrient, 1, 0);

                    chkJsonIndent = new CheckBox { Text = "Indent", Checked = _options.JsonIndent, AutoSize = true };
                    chkJsonIndent.CheckedChanged += (s, e) => { if (chkJsonIndent != null) _options.JsonIndent = chkJsonIndent.Checked; };
                    panel.Controls.Add(chkJsonIndent, 2, 0);

                    chkJsonPreserveTypes = new CheckBox { Text = "Preserve Types", Checked = _options.JsonPreserveTypes, AutoSize = true };
                    chkJsonPreserveTypes.CheckedChanged += (s, e) => { if (chkJsonPreserveTypes != null) _options.JsonPreserveTypes = chkJsonPreserveTypes.Checked; };
                    panel.Controls.Add(chkJsonPreserveTypes, 3, 0);
                    break;

                case "csv":
                    panel.Controls.Add(new Label { Text = "Output Separator:", TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
                    cmbCsvSepOut = new ComboBox
                    {
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Items = { ";", ",", "\t", "|" }
                    };
                    cmbCsvSepOut.SelectedItem = _options.CsvSeparator;
                    cmbCsvSepOut.SelectedIndexChanged += (s, e) => { if (cmbCsvSepOut?.SelectedItem != null) _options.CsvSeparator = cmbCsvSepOut.SelectedItem.ToString()!; };
                    panel.Controls.Add(cmbCsvSepOut, 1, 0);

                    chkCsvIndex = new CheckBox { Text = "Include Index", Checked = _options.CsvIncludeIndex, AutoSize = true };
                    chkCsvIndex.CheckedChanged += (s, e) => { if (chkCsvIndex != null) _options.CsvIncludeIndex = chkCsvIndex.Checked; };
                    panel.Controls.Add(chkCsvIndex, 2, 0);
                    break;

                case "parquet":
                    panel.Controls.Add(new Label { Text = "Compression:", TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
                    cmbParquetCompression = new ComboBox
                    {
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Items = { "snappy", "gzip", "brotli", "none" }
                    };
                    cmbParquetCompression.SelectedItem = _options.ParquetCompression;
                    cmbParquetCompression.SelectedIndexChanged += (s, e) => { if (cmbParquetCompression?.SelectedItem != null) _options.ParquetCompression = cmbParquetCompression.SelectedItem.ToString()!; };
                    panel.Controls.Add(cmbParquetCompression, 1, 0);
                    break;

                case "xlsx":
                case "xls":
                    panel.Controls.Add(new Label { Text = "Sheet Name:", TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
                    txtSheetName = new TextBox { Text = _options.ExcelSheetName, Width = 150 };
                    txtSheetName.TextChanged += (s, e) => { if (txtSheetName != null) _options.ExcelSheetName = txtSheetName.Text; };
                    panel.Controls.Add(txtSheetName, 1, 0);

                    chkExcelIndex = new CheckBox { Text = "Include Index", Checked = _options.ExcelIncludeIndex, AutoSize = true };
                    chkExcelIndex.CheckedChanged += (s, e) => { if (chkExcelIndex != null) _options.ExcelIncludeIndex = chkExcelIndex.Checked; };
                    panel.Controls.Add(chkExcelIndex, 2, 0);

                    chkExcelInferTypes = new CheckBox { Text = "Infer Types", Checked = _options.ExcelInferTypes, AutoSize = true };
                    chkExcelInferTypes.CheckedChanged += (s, e) => { if (chkExcelInferTypes != null) _options.ExcelInferTypes = chkExcelInferTypes.Checked; };
                    panel.Controls.Add(chkExcelInferTypes, 3, 0);
                    break;

                case "html":
                    panel.Controls.Add(new Label { Text = "Title:", TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
                    txtHtmlTitle = new TextBox { Text = _options.HtmlTitle, Width = 200 };
                    txtHtmlTitle.TextChanged += (s, e) => { if (txtHtmlTitle != null) _options.HtmlTitle = txtHtmlTitle.Text; };
                    panel.Controls.Add(txtHtmlTitle, 1, 0);
                    break;

                case "md":
                    chkMdIndex = new CheckBox { Text = "Include Index", Checked = _options.MarkdownIncludeIndex, AutoSize = true };
                    chkMdIndex.CheckedChanged += (s, e) => { if (chkMdIndex != null) _options.MarkdownIncludeIndex = chkMdIndex.Checked; };
                    panel.Controls.Add(chkMdIndex, 0, 0);
                    break;
            }

            pnlFormatOptions.Controls.Add(panel);
            pnlFormatOptions.Height = panel.PreferredSize.Height + 20;
        }

        // ========== EVENT HANDLERS ==========

        private void OnConversionProgress(object? sender, string message)
        {
            if (this.IsHandleCreated)
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    Log(message);
                    if (lblStatus != null) lblStatus.Text = message;
                }));
            }
        }

        private void OnConversionCompleted(object? sender, ConversionResult result)
        {
            if (this.IsHandleCreated)
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    Log("CONVERSÃO CONCLUÍDA!", "success");
                    Log($"Arquivo: {result.OutputFile}");
                    Log($"Tamanho: {result.FileSizeKB:F1} KB");

                    MessageBox.Show(this,
                        $"Conversão concluída com sucesso!\n\n" +
                        $"Formato: {result.Format?.ToUpper()}\n" +
                        $"Registros: {result.RecordCount}\n" +
                        $"Colunas: {result.ColumnCount}\n" +
                        $"Tamanho: {result.FileSizeKB:F1} KB\n" +
                        $"Smart Typing: {(result.SmartTypingApplied ? "Sim" : "Não")}",
                        "Sucesso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }));
            }
        }

        private void OnConversionFailed(object? sender, Exception ex)
        {
            if (this.IsHandleCreated)
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    Log($"ERRO: {ex.Message}", "error");
                    MessageBox.Show(this, $"Falha na conversão:\n\n{ex.Message}", "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
        }

        private void BtnBrowseInput_Click(object? sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select input file";
                dialog.Filter = "All Supported|*.csv;*.tsv;*.txt;*.json;*.xlsx;*.xls;*.parquet|" +
                               "CSV|*.csv;*.tsv|Excel|*.xlsx;*.xls|JSON|*.json|Parquet|*.parquet|All Files|*.*";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _options.InputFile = dialog.FileName;
                    if (txtInputFile != null) txtInputFile.Text = _options.InputFile;
                    UpdateOutputExtension();
                    Log($"Input file: {Path.GetFileName(_options.InputFile)}");

                    Task.Run(() => DetectSeparator());
                    Task.Run(() => UpdatePreview());
                }
            }
        }

        private void BtnBrowseOutput_Click(object? sender, EventArgs e)
        {
            var extensions = new Dictionary<string, string>
            {
                ["xlsx"] = "Excel|*.xlsx",
                ["xls"] = "Excel 97-2003|*.xls",
                ["sql"] = "SQL|*.sql",
                ["json"] = "JSON|*.json",
                ["csv"] = "CSV|*.csv",
                ["parquet"] = "Parquet|*.parquet",
                ["html"] = "HTML|*.html",
                ["md"] = "Markdown|*.md"
            };

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = $"Save as {_options.OutputFormat.ToUpper()}";
                dialog.DefaultExt = $".{_options.OutputFormat}";
                dialog.Filter = extensions.ContainsKey(_options.OutputFormat) ? extensions[_options.OutputFormat] : "All Files|*.*";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _options.OutputFile = dialog.FileName;
                    if (txtOutputFile != null) txtOutputFile.Text = _options.OutputFile;
                    Log($"Output file: {Path.GetFileName(_options.OutputFile)}");
                }
            }
        }

        private void BtnAutoDetect_Click(object? sender, EventArgs e)
        {
            DetectSeparator();
        }

        private void BtnSmartTypes_Click(object? sender, EventArgs e)
        {
            AnalyzeDataTypes();
        }

        private async void BtnConvert_Click(object? sender, EventArgs e)
        {
            await StartConversionAsync();
        }

        // ========== MÉTODOS DE NEGÓCIO ==========

        private void UpdateOutputExtension()
        {
            if (!string.IsNullOrEmpty(_options.OutputFile))
            {
                var dir = Path.GetDirectoryName(_options.OutputFile);
                var name = Path.GetFileNameWithoutExtension(_options.OutputFile);
                if (dir != null && name != null)
                {
                    _options.OutputFile = Path.Combine(dir, $"{name}.{_options.OutputFormat}");
                    if (txtOutputFile != null) txtOutputFile.Text = _options.OutputFile;
                }
            }
            else if (!string.IsNullOrEmpty(_options.InputFile))
            {
                var dir = Path.GetDirectoryName(_options.InputFile);
                var name = Path.GetFileNameWithoutExtension(_options.InputFile);
                if (dir != null && name != null)
                {
                    _options.OutputFile = Path.Combine(dir, $"{name}.{_options.OutputFormat}");
                    if (txtOutputFile != null) txtOutputFile.Text = _options.OutputFile;
                }
            }
        }

        private void DetectSeparator()
        {
            if (string.IsNullOrEmpty(_options.InputFile) ||
                !(_options.InputFile.EndsWith(".csv") || _options.InputFile.EndsWith(".tsv") || _options.InputFile.EndsWith(".txt")))
                return;

            try
            {
                var encoding = EncodingHelper.GetEncoding(_options.Encoding);
                var detectedSep = _separatorDetector.DetectFromFile(_options.InputFile, encoding);

                if (!string.IsNullOrEmpty(detectedSep))
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        _options.Separator = detectedSep;
                        if (cmbSeparator != null) cmbSeparator.SelectedItem = detectedSep;
                        Log($"Detected separator: '{detectedSep}'");
                    }));
                }
                else
                {
                    Log("Could not detect separator automatically");
                }
            }
            catch (Exception ex)
            {
                Log($"Error detecting separator: {ex.Message}", "error");
            }
        }

        private void UpdatePreview()
        {
            if (string.IsNullOrEmpty(_options.InputFile) ||
                cmbSeparator == null || cmbEncoding == null ||
                numMaxRows == null || numSampleSize == null ||
                txtPreview == null) return;

            try
            {
                _options.MaxRows = (int)numMaxRows.Value;
                _options.SampleSize = (int)numSampleSize.Value;
                _options.Separator = cmbSeparator.SelectedItem?.ToString() ?? "AUTO";
                _options.Encoding = cmbEncoding.SelectedItem?.ToString() ?? "utf-8";

                var dt = _fileReaderService.LoadSample(_options);

                if (dt == null) return;

                this.Invoke((MethodInvoker)(() =>
                {
                    txtPreview.Clear();
                    txtPreview.AppendText($"Columns: {dt.Columns.Count} | Rows: {dt.Rows.Count}\n");

                    var columns = dt.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName).ToList();
                    var previewColumns = string.Join(", ", columns.Take(10));
                    if (columns.Count > 10) previewColumns += "...";
                    txtPreview.AppendText($"Fields: {previewColumns}\n\n");

                    for (int i = 0; i < Math.Min(5, dt.Rows.Count); i++)
                    {
                        var row = dt.Rows[i];
                        var values = new List<string>();
                        foreach (System.Data.DataColumn col in dt.Columns)
                        {
                            values.Add(row[col]?.ToString() ?? "");
                        }
                        txtPreview.AppendText(string.Join("\t", values) + "\n");
                    }
                }));

                AnalyzeDataTypes();
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    if (txtPreview != null)
                    {
                        txtPreview.Clear();
                        txtPreview.AppendText($"Preview error:\n{ex.Message}");
                    }
                }));
            }
        }

        private void AnalyzeDataTypes()
        {
            if (string.IsNullOrEmpty(_options.InputFile) ||
                txtTypes == null || lblStatus == null) return;

            try
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    lblStatus.Text = "Analyzing data types...";
                    Log("Analyzing data types...");
                }));

                var dt = _fileReaderService.LoadSample(_options);
                if (dt == null) return;

                var typeInfos = _typeDetectionService.AnalyzeDataTypes(dt);

                this.Invoke((MethodInvoker)(() =>
                {
                    txtTypes.Clear();
                    txtTypes.SelectionFont = new Font("Consolas", 9, FontStyle.Bold);
                    txtTypes.AppendText("COLUMN TYPE ANALYSIS\n");
                    txtTypes.AppendText(new string('=', 60) + "\n\n");

                    foreach (var info in typeInfos)
                    {
                        txtTypes.SelectionFont = new Font("Consolas", 9, FontStyle.Bold);
                        txtTypes.AppendText($"📊 {info.ColumnName}\n");
                        txtTypes.SelectionFont = new Font("Consolas", 9);
                        txtTypes.AppendText($"   ├─ Current type: {info.CurrentType}\n");
                        txtTypes.AppendText($"   ├─ Detected type: {info.DetectedType}\n");
                        txtTypes.AppendText($"   ├─ Non-null: {info.NonNullCount}\n");
                        txtTypes.AppendText($"   ├─ Nulls: {info.NullCount}\n");
                        txtTypes.AppendText($"   └─ Unique: {info.UniqueCount}\n\n");
                    }

                    Log($"✅ Type analysis complete for {dt.Columns.Count} columns");
                    lblStatus.Text = "Type analysis complete";
                }));
            }
            catch (Exception ex)
            {
                Log($"Error analyzing types: {ex.Message}", "error");
            }
        }

        private async Task StartConversionAsync()
        {
            if (_isProcessing || _converterService == null) return;

            if (string.IsNullOrEmpty(_options.InputFile))
            {
                MessageBox.Show("Select an input file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(_options.OutputFile))
            {
                MessageBox.Show("Select an output file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Atualizar opções da UI
            if (cmbSeparator != null) _options.Separator = cmbSeparator.SelectedItem?.ToString() ?? "AUTO";
            if (cmbEncoding != null) _options.Encoding = cmbEncoding.SelectedItem?.ToString() ?? "utf-8";
            if (numMaxRows != null) _options.MaxRows = (int)numMaxRows.Value;
            if (numSampleSize != null) _options.SampleSize = (int)numSampleSize.Value;

            _isProcessing = true;
            if (btnConvert != null)
            {
                btnConvert.Enabled = false;
                btnConvert.Text = "PROCESSING...";
                btnConvert.BackColor = Color.Gray;
            }
            if (progressBar != null) progressBar.Visible = true;
            if (progressTimer != null) progressTimer.Start();

            try
            {
                await _converterService.ConvertAsync(_options);
            }
            catch (Exception ex)
            {
                Log($"ERRO INESPERADO: {ex.Message}", "error");
            }
            finally
            {
                _isProcessing = false;
                if (btnConvert != null)
                {
                    btnConvert.Enabled = true;
                    btnConvert.Text = "CONVERT";
                    btnConvert.BackColor = Color.FromArgb(64, 64, 64);
                }
                if (progressBar != null) progressBar.Visible = false;
                if (progressTimer != null) progressTimer.Stop();
            }
        }

        private void Log(string message, string tag = "info")
        {
            if (txtLog == null) return;

            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke((MethodInvoker)(() => Log(message, tag)));
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLog.AppendText($"[{timestamp}] ");

            if (tag == "error")
            {
                txtLog.SelectionColor = Color.Red;
                txtLog.AppendText($"{message}\n");
                txtLog.SelectionColor = txtLog.ForeColor;
            }
            else if (tag == "success")
            {
                txtLog.SelectionColor = Color.Green;
                txtLog.AppendText($"{message}\n");
                txtLog.SelectionColor = txtLog.ForeColor;
            }
            else
            {
                txtLog.AppendText($"{message}\n");
            }

            txtLog.ScrollToCaret();
        }
    }
}