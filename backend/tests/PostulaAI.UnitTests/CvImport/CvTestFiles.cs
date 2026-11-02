using System.IO.Compression;
using System.Text;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace PostulaAI.UnitTests.CvImport;

/// <summary>Builds small, fictitious CV files in memory for tests.</summary>
internal static class CvTestFiles
{
    public static readonly string[] Lines =
    [
        "Laura Gómez",
        "Desarrolladora backend",
        "Experiencia: Empresa Ficticia S.A., 2022 - actual. APIs con C# y PostgreSQL.",
        "Educación: Universidad Ejemplo, Ingeniería de Sistemas, 2015 - 2020.",
        "Idiomas: Inglés B2.",
    ];

    public static byte[] Docx(params string[] paragraphs)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("word/document.xml");
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>");
            foreach (var paragraph in paragraphs)
            {
                writer.Write($"<w:p><w:r><w:t xml:space=\"preserve\">{System.Security.SecurityElement.Escape(paragraph)}</w:t></w:r></w:p>");
            }

            writer.Write("</w:body></w:document>");
        }

        return output.ToArray();
    }

    public static byte[] Pdf(params string[] lines)
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(PageSize.A4);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var y = 780.0;
        foreach (var line in lines)
        {
            // Standard 14 fonts only cover WinAnsi; tests use plain ASCII text for PDFs.
            page.AddText(line, 11, new PdfPoint(40, y), font);
            y -= 18;
        }

        return builder.Build();
    }
}
