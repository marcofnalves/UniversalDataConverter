# Universal Data Converter

Aplicação desktop para detecção automática de tipos e conversão de ficheiros tabulares entre múltiplos formatos (Excel, CSV, JSON, HTML, Markdown, SQL, etc).

## Visão geral
`Universal_Data_Converter` lê ficheiros tabulares (CSV, TXT, Excel, ...) aplica deteção de tipos, análise da tabela e exporta para vários formatos e dialetos SQL. A interface principal é uma aplicação Windows Forms (`Form1`) e o núcleo está organizado em serviços reutilizáveis.

## Funcionalidades principais
- Leitura de ficheiros com deteção automática de encoding e separadores.
- Amostragem e análise de colunas (valores nulos, únicos, amostras).
- Deteção de tipos inteligentes (`UseSmartTypes`).
- Exportadores:
  - `CsvExporter`
  - `JsonExporter`
  - `HtmlExporter`
  - `MarkdownExporter`
  - `ExcelExporter` (usa `ClosedXML`)
  - Exportadores SQL: `SqliteExporter`, `MySqlExporter`, `PostgreSqlExporter`, `SqlServerExporter`
- Factory para selecionar dialeto SQL: `SqlDialectFactory`.
- Opções de conversão centralizadas em `Models/ConversionOptions.cs`.

## Estrutura do projeto
- `Program.cs` — ponto de entrada da aplicação.
- `Form1.cs`, `Form1.Designer.cs` — UI Windows Forms.
- `Models/`
  - `ConversionOptions.cs` — todas as opções configuráveis (ver abaixo).
  - `DataTypeInfo.cs` — metadados e resultados de deteção de tipo.
- `Services/`
  - `FileReaderService.cs` — leitura de ficheiros (encoding / separador).
  - `FileWriterService.cs` — escrita para disco.
  - `TypeDetectionService.cs` — lógica de deteção de tipos.
  - `DataConverterService.cs` — orquestração da conversão.
  - `Analysis/` — `TableAnalyzer`, `TableAnalysis`.
  - `TypeDetection/` — `SqlTypeDetector`.
  - `ValueFormatting/` — `SqlValueFormatter`.
  - `OtherFormats/` — exportadores (CSV, JSON, HTML, Markdown, Excel).
  - `SQL/` — implementações de exportadores SQL e `SqlDialectFactory`.
- `Utils/` — `EncodingHelper`, `SeparatorDetector`.

## Requisitos
- .NET 10
- C# 14
- Visual Studio 2022/2023 ou `dotnet` SDK compatível
- Pacote NuGet: `ClosedXML` (para `ExcelExporter`)

## Build & Execução
1. Abrir a solução no Visual Studio e compilar normalmente.
2. Ou via CLI:
   - Navegue até à pasta do projeto onde está o ficheiro `.csproj`.
   - `dotnet restore`
   - `dotnet build`
   - `dotnet run --project <CaminhoParaProjeto>` (se for um projecto executável)

> Nota: A UI é WinForms; execute a aplicação e abra ficheiros a partir da interface.

## Uso (GUI)
1. Abrir a aplicação.
2. Selecionar o ficheiro de entrada (`InputFile`).
3. Ajustar opções de conversão nas preferências (se necessário).
4. Selecionar formato de saída (`OutputFormat`) e caminho (`OutputFile`).
5. Executar conversão. Logs/erros surgirão na UI (ou consola se executado via CLI).

## `ConversionOptions` — opções principais
As opções estão definidas em `Models/ConversionOptions.cs`. Principais propriedades:

- Arquivos
  - `InputFile` — caminho do ficheiro de entrada.
  - `OutputFile` — caminho do ficheiro de saída.
  - `OutputFormat` — formato de saída (ex.: `xlsx`, `csv`, `json`, `sql`, `html`, `md`).
- Leitura
  - `MaxRows` — limite máximo de linhas a processar.
  - `SampleSize` — número de linhas usado para amostragem/detecção.
  - `Separator` — separador (`"AUTO"` para deteção automática).
  - `Encoding` — encoding do ficheiro (ex.: `utf-8`).
- Smart typing
  - `UseSmartTypes` — ativa conversão inteligente de tipos.
- SQL
  - `SqlTableName` — nome da tabela SQL de saída.
  - `SqlDialect` — `sqlite` (default), `mysql`, `postgresql`, `sqlserver`.
- JSON
  - `JsonOrient` — orientação (ex.: `records`).
  - `JsonIndent` — formatação legível.
  - `JsonPreserveTypes` — tenta preservar tipos nos objetos JSON.
- CSV
  - `CsvSeparator`, `CsvIncludeIndex`
- Excel
  - `ExcelSheetName`, `ExcelIncludeIndex`, `ExcelInferTypes`
- HTML / Markdown
  - `HtmlTitle`, `MarkdownIncludeIndex`

## Extensibilidade
- Para adicionar um novo exportador, criar uma classe em `Services/OtherFormats` ou `Services/SQL` implementando a interface adequada (padrão do projecto) e registar no fluxo principal (`DataConverterService` / `SqlDialectFactory`).
- Para suportar novo dialeto SQL, estender `SqlDialectFactory` e adicionar um `ISqlDialectExporter`.

## Erros comuns / Troubleshooting
- Problemas com Excel: verifique se `ClosedXML` está instalado e compatível com a versão do .NET.
- Encoding/Separador incorreto: force `ConversionOptions.Encoding` ou `ConversionOptions.Separator` para evitar deteção automática falha.
- Grande volume de dados: aumentar `MaxRows` ou usar exportadores orientados a streaming (se implementados).

## Testes
Nenhum projeto de testes incluído por padrão. Recomenda-se criar testes unitários para:
- `TypeDetectionService`
- `TableAnalyzer`
- Cada exportador (CSV, JSON, Excel, SQL)

## Contribuição
- Abrir issue descrevendo o problema/feature.
- Enviar pull request com mudanças pequenas e documentação atualizada.
- Seguir convenções de código existentes.

## Licença
Adicionar a licença do projecto (ex.: `MIT`) no ficheiro `LICENSE`.

## Contacto / Notas finais
Este `README` resume a arquitetura e utilização do projeto. Para informações detalhadas sobre um ficheiro/classe específica consulte os ficheiros em `Services/`, `Models/` e `Utils/`.
