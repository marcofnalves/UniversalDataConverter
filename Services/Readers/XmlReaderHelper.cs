using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Windows.Forms; // Adicione referência ao System.Windows.Forms

namespace Universal_Data_Converter.Services.Readers
{
    public static class XmlReaderHelper
    {
        public static DataTable? ReadXmlSample(string filePath, int sampleSize, bool useHeaderRow = true)
        {
            try
            {
                // Validações iniciais
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {filePath}");

                var dt = new DataTable();

                // Usar XmlReader para arquivos grandes (mais eficiente)
                if (new FileInfo(filePath).Length > 10 * 1024 * 1024) // > 10MB
                {
                    return ReadLargeXmlSample(filePath, sampleSize);
                }

                var doc = XDocument.Load(filePath);
                var root = doc.Root;
                if (root == null) return dt;

                // Detectar elementos de dados de forma inteligente
                var dataElements = FindDataElements(root).ToList();
                if (!dataElements.Any())
                {
                    // Se não encontrar elementos repetidos, tentar elementos filhos diretos
                    dataElements = root.Elements().ToList();
                }

                if (!dataElements.Any()) return dt;

                // Determinar estrutura
                BuildDataTableStructure(dataElements, dt);

                // Adicionar dados (limitado pelo sampleSize)
                int count = 0;
                foreach (var element in dataElements.Take(sampleSize))
                {
                    var row = dt.NewRow();
                    PopulateRowFromElement(element, row, dt);
                    dt.Rows.Add(row);
                    count++;
                }

                return dt;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading XML file: {ex.Message}", ex);
            }
        }

        public static DataTable? ReadXmlFull(string filePath, bool useHeaderRow = true)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {filePath}");

                var dt = new DataTable();

                // Para arquivos grandes, usar leitura stream
                if (new FileInfo(filePath).Length > 50 * 1024 * 1024) // > 50MB
                {
                    return ReadLargeXmlFull(filePath);
                }

                var doc = XDocument.Load(filePath);
                var root = doc.Root;
                if (root == null) return dt;

                var dataElements = FindDataElements(root).ToList();
                if (!dataElements.Any())
                {
                    dataElements = root.Elements().ToList();
                }

                if (!dataElements.Any()) return dt;

                // Construir estrutura baseada em todos os elementos
                BuildCompleteDataTableStructure(dataElements, dt);

                // Adicionar todos os dados
                foreach (var element in dataElements)
                {
                    var row = dt.NewRow();
                    PopulateRowFromElement(element, row, dt);
                    dt.Rows.Add(row);
                }

                return dt;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading XML file: {ex.Message}", ex);
            }
        }

        private static DataTable? ReadLargeXmlSample(string filePath, int sampleSize)
        {
            var dt = new DataTable();
            var elements = new List<XElement>();

            using (var reader = XmlReader.Create(filePath))
            {
                while (reader.Read() && elements.Count < sampleSize * 2) // Ler um pouco mais para análise
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        var element = XNode.ReadFrom(reader) as XElement;
                        if (element != null)
                        {
                            elements.Add(element);
                        }
                    }
                }
            }

            if (!elements.Any()) return dt;

            // Detectar estrutura a partir dos elementos lidos
            BuildDataTableStructure(elements, dt);

            // Adicionar dados (limitado pelo sampleSize)
            foreach (var element in elements.Take(sampleSize))
            {
                var row = dt.NewRow();
                PopulateRowFromElement(element, row, dt);
                dt.Rows.Add(row);
            }

            return dt;
        }

        private static DataTable? ReadLargeXmlFull(string filePath)
        {
            // Para arquivos muito grandes, usar abordagem de streaming
            var dt = new DataTable();
            var structureBuilt = false;

            using (var reader = XmlReader.Create(filePath))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && !structureBuilt)
                    {
                        // Tentar construir estrutura baseada no primeiro elemento
                        var element = XNode.ReadFrom(reader) as XElement;
                        if (element != null)
                        {
                            var elements = new List<XElement> { element };
                            BuildDataTableStructure(elements, dt);
                            structureBuilt = true;

                            // Adicionar primeira linha
                            var row = dt.NewRow();
                            PopulateRowFromElement(element, row, dt);
                            dt.Rows.Add(row);
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Element && structureBuilt)
                    {
                        var element = XNode.ReadFrom(reader) as XElement;
                        if (element != null)
                        {
                            var row = dt.NewRow();
                            PopulateRowFromElement(element, row, dt);
                            dt.Rows.Add(row);
                        }
                    }
                }
            }

            return dt;
        }

        private static IEnumerable<XElement> FindDataElements(XElement root)
        {
            // Estratégia inteligente: encontrar elementos que se repetem
            var elementGroups = root.Descendants()
                .GroupBy(e => e.Name.LocalName)
                .Where(g => g.Count() > 1)
                .OrderByDescending(g => g.Count());

            if (elementGroups.Any())
            {
                return elementGroups.First();
            }

            return root.Elements();
        }

        private static void BuildDataTableStructure(List<XElement> elements, DataTable dt)
        {
            if (!elements.Any()) return;

            var columns = new Dictionary<string, HashSet<Type>>();
            var firstElement = elements.First();

            // Analisar elementos filhos
            foreach (var elem in firstElement.Elements())
            {
                var colName = elem.Name.LocalName;
                if (!columns.ContainsKey(colName))
                {
                    columns[colName] = new HashSet<Type>();
                }
            }

            // Analisar atributos
            foreach (var attr in firstElement.Attributes())
            {
                var colName = $"@{attr.Name.LocalName}";
                if (!columns.ContainsKey(colName))
                {
                    columns[colName] = new HashSet<Type>();
                }
            }

            // Analisar tipos em todos os elementos (limitado para performance)
            foreach (var element in elements.Take(100))
            {
                foreach (var colName in columns.Keys.ToList())
                {
                    if (colName.StartsWith("@"))
                    {
                        // É um atributo
                        var attrName = colName.Substring(1);
                        var attr = element.Attribute(attrName);
                        if (attr != null)
                        {
                            var type = DetectDataType(attr.Value);
                            columns[colName].Add(type);
                        }
                    }
                    else
                    {
                        // É um elemento
                        var childElem = element.Element(colName);
                        if (childElem != null)
                        {
                            var type = DetectDataType(childElem.Value);
                            columns[colName].Add(type);
                        }
                    }
                }
            }

            // Criar colunas com o tipo mais apropriado
            foreach (var column in columns)
            {
                var finalType = GetMostAppropriateType(column.Value);
                dt.Columns.Add(column.Key, finalType);
            }
        }

        private static void BuildCompleteDataTableStructure(List<XElement> elements, DataTable dt)
        {
            if (!elements.Any()) return;

            var allColumns = new Dictionary<string, HashSet<Type>>();

            // Mapear todos os campos possíveis de todos os elementos
            foreach (var element in elements)
            {
                // Elementos filhos
                foreach (var elem in element.Elements())
                {
                    var colName = elem.Name.LocalName;
                    if (!allColumns.ContainsKey(colName))
                    {
                        allColumns[colName] = new HashSet<Type>();
                    }
                    var type = DetectDataType(elem.Value);
                    allColumns[colName].Add(type);
                }

                // Atributos
                foreach (var attr in element.Attributes())
                {
                    var colName = $"@{attr.Name.LocalName}";
                    if (!allColumns.ContainsKey(colName))
                    {
                        allColumns[colName] = new HashSet<Type>();
                    }
                    var type = DetectDataType(attr.Value);
                    allColumns[colName].Add(type);
                }
            }

            // Criar colunas com o tipo mais apropriado
            foreach (var column in allColumns)
            {
                var finalType = GetMostAppropriateType(column.Value);
                dt.Columns.Add(column.Key, finalType);
            }
        }

        private static void PopulateRowFromElement(XElement element, DataRow row, DataTable dt)
        {
            // Preencher elementos
            foreach (DataColumn col in dt.Columns)
            {
                if (col.ColumnName.StartsWith("@"))
                {
                    // É um atributo
                    var attrName = col.ColumnName.Substring(1);
                    var attr = element.Attribute(attrName);
                    if (attr != null)
                    {
                        row[col.ColumnName] = ConvertValue(attr.Value, col.DataType);
                    }
                    else
                    {
                        row[col.ColumnName] = DBNull.Value;
                    }
                }
                else
                {
                    // É um elemento
                    var childElem = element.Element(col.ColumnName);
                    if (childElem != null)
                    {
                        row[col.ColumnName] = ConvertValue(childElem.Value, col.DataType);
                    }
                    else
                    {
                        row[col.ColumnName] = DBNull.Value;
                    }
                }
            }
        }

        private static Type DetectDataType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return typeof(string);

            if (int.TryParse(value, out _))
                return typeof(int);

            if (long.TryParse(value, out _))
                return typeof(long);

            if (decimal.TryParse(value, out _))
                return typeof(decimal);

            if (DateTime.TryParse(value, out _))
                return typeof(DateTime);

            if (bool.TryParse(value, out _))
                return typeof(bool);

            return typeof(string);
        }

        private static Type GetMostAppropriateType(HashSet<Type> types)
        {
            if (types.Count == 1)
                return types.First();

            if (types.Contains(typeof(string)) || types.Contains(typeof(DateTime)))
                return typeof(string);

            if (types.Contains(typeof(decimal)))
                return typeof(decimal);

            if (types.Contains(typeof(long)))
                return typeof(long);

            return typeof(string);
        }

        private static object ConvertValue(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
                return DBNull.Value;

            try
            {
                if (targetType == typeof(int))
                    return int.Parse(value);
                if (targetType == typeof(long))
                    return long.Parse(value);
                if (targetType == typeof(decimal))
                    return decimal.Parse(value);
                if (targetType == typeof(DateTime))
                    return DateTime.Parse(value);
                if (targetType == typeof(bool))
                    return bool.Parse(value);
            }
            catch
            {
                return value;
            }

            return value;
        }

        public static XmlMetadata ExtractMetadata(string filePath)
        {
            var metadata = new XmlMetadata();

            try
            {
                using (var reader = XmlReader.Create(filePath))
                {
                    int elementCount = 0;
                    var fieldSet = new HashSet<string>();
                    var attrSet = new HashSet<string>();

                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            elementCount++;

                            if (elementCount < 1000) // Limitar para performance
                            {
                                fieldSet.Add(reader.Name);

                                if (reader.HasAttributes)
                                {
                                    for (int i = 0; i < reader.AttributeCount; i++)
                                    {
                                        reader.MoveToAttribute(i);
                                        attrSet.Add(reader.Name);
                                    }
                                    reader.MoveToElement();
                                }
                            }
                        }
                    }

                    metadata.ElementCount = elementCount;
                    metadata.Fields = fieldSet.Take(50).ToList(); // Limitar a 50 campos
                    metadata.Attributes = attrSet.Take(20).ToList();
                }
            }
            catch (Exception ex)
            {
                metadata.ErrorMessage = ex.Message;
            }

            return metadata;
        }
    }

    public class XmlMetadata
    {
        public string RootElement { get; set; } = string.Empty;
        public int ElementCount { get; set; }
        public int RecordCount { get; set; }
        public List<string> Fields { get; set; } = new List<string>();
        public List<string> Attributes { get; set; } = new List<string>();
        public string ErrorMessage { get; set; } = string.Empty;
    }
}