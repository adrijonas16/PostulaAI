using System.IO.Compression;
using System.Text;
using System.Xml;
using PostulaAI.Api.Shared.Errors;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PostulaAI.Api.Modules.ProfessionalProfiles.Import;

/// <summary>
/// Extracts plain text from a CV file (PDF or DOCX) in memory. The file type is detected from its content, not its
/// name. Files are never written to disk or stored. Limits guard against oversized and malicious documents.
/// </summary>
public static class CvTextExtractor
{
    public const int MaxFileBytes = 5 * 1024 * 1024;
    public const int MinTextCharacters = 80;
    private const int MaxPdfPages = 15;
    private const long MaxDocxXmlBytes = 20 * 1024 * 1024;

    public static Result<string> Extract(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.Length > MaxFileBytes)
        {
            return new AppError(ErrorCodes.CvTooLarge, StatusCodes.Status413PayloadTooLarge, "El archivo supera el máximo de 5 MB.");
        }

        string? text;
        try
        {
            text = IsPdf(content) ? ExtractPdf(content) : IsZip(content) ? ExtractDocx(content) : null;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Malformed, encrypted or hostile files surface as many exception types across the PDF, ZIP and XML parsers.
            // The content is never logged; the user gets a clear, generic message.
            return Unreadable();
        }

        if (text is null)
        {
            return new AppError(ErrorCodes.CvUnsupportedFormat, StatusCodes.Status415UnsupportedMediaType,
                "Formato no soportado. Sube tu CV en PDF o Word (.docx).");
        }

        text = Normalize(text);
        if (text.Length < MinTextCharacters)
        {
            return new AppError(ErrorCodes.CvNoText, StatusCodes.Status422UnprocessableEntity,
                "No encontramos texto en el archivo. Si es un PDF escaneado (una imagen), exporta tu CV desde Word o Google Docs como PDF con texto.");
        }

        return text;
    }

    private static AppError Unreadable() => new(ErrorCodes.CvUnreadable, StatusCodes.Status422UnprocessableEntity,
        "No pudimos leer el archivo. Puede estar dañado o protegido con contraseña.");

    private static bool IsPdf(byte[] content) =>
        content.Length > 5 && content[0] == '%' && content[1] == 'P' && content[2] == 'D' && content[3] == 'F' && content[4] == '-';

    private static bool IsZip(byte[] content) =>
        content.Length > 4 && content[0] == 'P' && content[1] == 'K' && content[2] == 3 && content[3] == 4;

    private static string ExtractPdf(byte[] content)
    {
        var builder = new StringBuilder();
        using var document = PdfDocument.Open(content);
        foreach (var page in document.GetPages().Take(MaxPdfPages))
        {
            AppendPageLines(builder, page);
            builder.Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>Rebuilds lines from word positions (top to bottom, left to right) so the model sees readable text.</summary>
    private static void AppendPageLines(StringBuilder builder, Page page)
    {
        const double lineTolerance = 3.0;
        var words = page.GetWords()
            .OrderByDescending(word => Math.Round(word.BoundingBox.Bottom / lineTolerance))
            .ThenBy(word => word.BoundingBox.Left)
            .ToList();

        double? currentLine = null;
        foreach (var word in words)
        {
            var line = Math.Round(word.BoundingBox.Bottom / lineTolerance);
            if (currentLine is not null && Math.Abs(line - currentLine.Value) > 0)
            {
                builder.Append('\n');
            }
            else if (currentLine is not null)
            {
                builder.Append(' ');
            }

            builder.Append(word.Text);
            currentLine = line;
        }
    }

    private static string? ExtractDocx(byte[] content)
    {
        using var archive = new ZipArchive(new MemoryStream(content), ZipArchiveMode.Read);
        var entry = archive.GetEntry("word/document.xml");
        if (entry is null)
        {
            return null;
        }

        if (entry.Length > MaxDocxXmlBytes)
        {
            throw new InvalidDataException("document.xml is too large.");
        }

        const string wordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var builder = new StringBuilder();
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        while (reader.Read())
        {
            if (reader.NamespaceURI != wordNamespace)
            {
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.LocalName)
                {
                    case "t":
                        builder.Append(reader.ReadElementContentAsString());
                        break;
                    case "tab":
                        builder.Append('\t');
                        break;
                    case "br":
                        builder.Append('\n');
                        break;
                    default:
                        break;
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "p")
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    private static string Normalize(string text)
    {
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            .Select(line => string.Join(' ', line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)));
        var collapsed = new StringBuilder();
        var blank = 0;
        foreach (var line in lines)
        {
            if (line.Length == 0)
            {
                if (++blank > 1)
                {
                    continue;
                }
            }
            else
            {
                blank = 0;
            }

            collapsed.Append(line).Append('\n');
        }

        return collapsed.ToString().Trim();
    }
}
