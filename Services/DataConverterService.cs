using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Universal_Data_Converter.Models;
using Universal_Data_Converter.Utils;

namespace Universal_Data_Converter.Services
{
    /// <summary>
    /// Serviço central que orquestra o processo completo de conversão de dados.
    /// Atua como uma fachada para os serviços especializados.
    /// </summary>
    public class DataConverterService
    {
        private readonly FileReaderService _fileReaderService;
        private readonly FileWriterService _fileWriterService;
        private readonly TypeDetectionService _typeDetectionService;
        private readonly DataCleanerService _dataCleanerService;
        private readonly IProgress<string>? _progressReporter;

        // Eventos para comunicação com a UI
        public event EventHandler<string>? ProgressReported;
        public event EventHandler<ConversionResult>? ConversionCompleted;
        public event EventHandler<Exception>? ConversionFailed;

        public DataConverterService()
        {
            _fileReaderService = new FileReaderService();
            _fileWriterService = new FileWriterService();
            _typeDetectionService = new TypeDetectionService();
            _dataCleanerService = new DataCleanerService();
        }

        public DataConverterService(IProgress<string> progressReporter) : this()
        {
            _progressReporter = progressReporter;
        }

        /// <summary>
        /// Executa a conversão completa de um arquivo
        /// </summary>
        public async Task<ConversionResult> ConvertAsync(ConversionOptions options)
        {
            var startTime = DateTime.Now;

            try
            {
                ReportProgress("Iniciando processo de conversão...");
                ValidateOptions(options);

                ReportProgress($"Carregando arquivo: {Path.GetFileName(options.InputFile)}");
                var dataTable = await LoadDataAsync(options);

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    throw new Exception("Nenhum dado foi carregado do arquivo de entrada.");
                }

                ReportProgress($"Arquivo carregado: {dataTable.Rows.Count} linhas, {dataTable.Columns.Count} colunas");

                // Analisar e remover colunas vazias (se ativado)
                if (options.RemoveEmptyColumns)
                {
                    dataTable = await RemoveEmptyColumnsAsync(dataTable, options);
                }

                // Aplicar smart typing (se ativado)
                if (options.UseSmartTypes)
                {
                    ReportProgress("Aplicando detecção inteligente de tipos...");
                    dataTable = await ApplySmartTypingAsync(dataTable, options);
                }

                // Aplicar amostragem se necessário
                if (dataTable.Rows.Count > options.SampleSize)
                {
                    ReportProgress($"Amostragem para {options.SampleSize} registros...");
                    dataTable = _typeDetectionService.SampleDataTable(dataTable, options.SampleSize);
                }

                ReportProgress($"Exportando para formato: {options.OutputFormat.ToUpper()}");
                await ExportDataAsync(dataTable, options);

                var duration = DateTime.Now - startTime;
                var result = CreateSuccessResult(dataTable, options, duration);

                ConversionCompleted?.Invoke(this, result);
                ReportProgress("✅ Conversão concluída com sucesso!");

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                var errorResult = CreateErrorResult(ex, options, duration);

                ConversionFailed?.Invoke(this, ex);
                ReportProgress($"❌ ERRO: {ex.Message}");

                return errorResult;
            }
        }

        /// <summary>
        /// Remove colunas completamente vazias do DataTable
        /// </summary>
        private async Task<DataTable> RemoveEmptyColumnsAsync(DataTable dataTable, ConversionOptions options)
        {
            return await Task.Run(() =>
            {
                var stats = _dataCleanerService.AnalyzeNullColumns(dataTable);
                var emptyColumns = stats.Values.Where(s => s.IsCompletelyEmpty).ToList();

                if (emptyColumns.Any())
                {
                    ReportProgress($"🔍 Análise de colunas vazias:");
                    foreach (var stat in emptyColumns)
                    {
                        ReportProgress($"   └─ {stat}");
                    }

                    var cleanedTable = _dataCleanerService.RemoveEmptyColumns(dataTable, options, out var removedColumns);

                    if (removedColumns.Any())
                    {
                        ReportProgress($"✅ Removidas {removedColumns.Count} colunas completamente vazias: {string.Join(", ", removedColumns)}");
                        ReportProgress($"   Nova estrutura: {cleanedTable.Columns.Count} colunas restantes");
                    }

                    return cleanedTable;
                }

                ReportProgress($"✅ Nenhuma coluna completamente vazia encontrada");
                return dataTable;
            });
        }

        /// <summary>
        /// Valida as opções antes de iniciar a conversão
        /// </summary>
        private void ValidateOptions(ConversionOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options), "Opções de conversão não podem ser nulas");

            if (string.IsNullOrWhiteSpace(options.InputFile))
                throw new ArgumentException("Arquivo de entrada não especificado");

            if (!File.Exists(options.InputFile))
                throw new FileNotFoundException($"Arquivo não encontrado: {options.InputFile}");

            if (string.IsNullOrWhiteSpace(options.OutputFile))
                throw new ArgumentException("Arquivo de saída não especificado");

            var outputDir = Path.GetDirectoryName(options.OutputFile);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);
        }

        /// <summary>
        /// Carrega os dados do arquivo de entrada
        /// </summary>
        private async Task<DataTable> LoadDataAsync(ConversionOptions options)
        {
            return await Task.Run(() => _fileReaderService.LoadFullData(options) ?? new DataTable());
        }

        /// <summary>
        /// Aplica smart typing aos dados
        /// </summary>
        private async Task<DataTable> ApplySmartTypingAsync(DataTable dataTable, ConversionOptions options)
        {
            return await Task.Run(() => _typeDetectionService.ApplySmartTyping(dataTable));
        }

        /// <summary>
        /// Exporta os dados para o formato de saída
        /// </summary>
        private async Task ExportDataAsync(DataTable dataTable, ConversionOptions options)
        {
            await _fileWriterService.ExportData(dataTable, options, options.InputFile);
        }

        /// <summary>
        /// Cria um resultado de sucesso
        /// </summary>
        private ConversionResult CreateSuccessResult(DataTable dataTable, ConversionOptions options, TimeSpan duration)
        {
            var fileInfo = new FileInfo(options.OutputFile);

            return new ConversionResult
            {
                Success = true,
                OutputFile = options.OutputFile,
                FileSizeKB = fileInfo.Length / 1024.0,
                RecordCount = dataTable.Rows.Count,
                ColumnCount = dataTable.Columns.Count,
                Format = options.OutputFormat,
                SmartTypingApplied = options.UseSmartTypes,
                EmptyColumnsRemoved = options.RemoveEmptyColumns,
                Duration = duration,
                Message = "Conversão concluída com sucesso"
            };
        }

        /// <summary>
        /// Cria um resultado de erro
        /// </summary>
        private ConversionResult CreateErrorResult(Exception ex, ConversionOptions options, TimeSpan duration)
        {
            return new ConversionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Format = options.OutputFormat,
                SmartTypingApplied = options.UseSmartTypes,
                EmptyColumnsRemoved = options.RemoveEmptyColumns,
                Duration = duration
            };
        }

        /// <summary>
        /// Reporta progresso via evento e IProgress
        /// </summary>
        private void ReportProgress(string message)
        {
            ProgressReported?.Invoke(this, message);
            _progressReporter?.Report(message);
        }

        /// <summary>
        /// Obtém informações detalhadas sobre o arquivo de entrada
        /// </summary>
        public async Task<FileInfoResult> GetFileInfoAsync(string filePath, ConversionOptions options)
        {
            var result = new FileInfoResult
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSizeKB = new FileInfo(filePath).Length / 1024.0,
                Extension = Path.GetExtension(filePath).ToLower()
            };

            try
            {
                var analysisOptions = new ConversionOptions
                {
                    InputFile = filePath,
                    Encoding = options.Encoding,
                    Separator = options.Separator,
                    SampleSize = Math.Min(1000, options.SampleSize),
                    RemoveEmptyColumns = options.RemoveEmptyColumns
                };

                var sample = await Task.Run(() => _fileReaderService.LoadSample(analysisOptions));

                if (sample != null && sample.Rows.Count > 0)
                {
                    if (options.RemoveEmptyColumns)
                    {
                        sample = _dataCleanerService.RemoveEmptyColumns(sample, analysisOptions);
                    }

                    result.ColumnCount = sample.Columns.Count;
                    result.RowCount = sample.Rows.Count;
                    result.ColumnNames = sample.Columns.Cast<DataColumn>()
                        .Select(c => c.ColumnName)
                        .ToList();
                    result.DataTypes = _typeDetectionService.AnalyzeDataTypes(sample);
                    result.NullAnalysis = _dataCleanerService.AnalyzeNullColumns(sample);
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Cancela uma conversão em andamento
        /// </summary>
        public void Cancel()
        {
            ReportProgress("⏹️ Operação cancelada pelo usuário");
        }
    }

    /// <summary>
    /// Resultado da conversão
    /// </summary>
    public class ConversionResult
    {
        public bool Success { get; set; }
        public string? OutputFile { get; set; }
        public double FileSizeKB { get; set; }
        public int RecordCount { get; set; }
        public int ColumnCount { get; set; }
        public string? Format { get; set; }
        public bool SmartTypingApplied { get; set; }
        public bool EmptyColumnsRemoved { get; set; }
        public TimeSpan Duration { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }

        public override string ToString()
        {
            if (Success)
            {
                var features = new List<string>();
                if (SmartTypingApplied) features.Add("Smart Typing");
                if (EmptyColumnsRemoved) features.Add("Limpeza de Colunas");

                var featuresStr = features.Any() ? $" [{string.Join(", ", features)}]" : "";

                return $"✅ {Format?.ToUpper()}{featuresStr} | {RecordCount} registros | {FileSizeKB:F1} KB | {Duration.TotalSeconds:F1}s";
            }
            return $"❌ Erro: {ErrorMessage}";
        }
    }

    /// <summary>
    /// Informações detalhadas sobre um arquivo
    /// </summary>
    public class FileInfoResult
    {
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public double FileSizeKB { get; set; }
        public string? Extension { get; set; }
        public int ColumnCount { get; set; }
        public int RowCount { get; set; }
        public List<string> ColumnNames { get; set; } = new();
        public List<DataTypeInfo> DataTypes { get; set; } = new();
        public Dictionary<string, NullStats>? NullAnalysis { get; set; }
        public string? ErrorMessage { get; set; }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    }
}