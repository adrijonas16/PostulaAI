using System.Text;
using PostulaAI.Api.Modules.ProfessionalProfiles.Import;

namespace PostulaAI.UnitTests.CvImport;

public sealed class CvTextExtractorTests
{
    [Fact]
    public void Docx_ExtractsParagraphsAsLines()
    {
        var result = CvTextExtractor.Extract(CvTestFiles.Docx(CvTestFiles.Lines));

        Assert.True(result.IsSuccess, result.Error?.Title);
        var lines = result.Value.Split('\n');
        Assert.Equal("Laura Gómez", lines[0]);
        Assert.Contains("Idiomas: Inglés B2.", lines);
    }

    [Fact]
    public void Pdf_ExtractsTextInReadingOrder()
    {
        var result = CvTextExtractor.Extract(CvTestFiles.Pdf(
            "Laura Gomez",
            "Desarrolladora backend",
            "Experiencia: Empresa Ficticia S.A., 2022 - actual. APIs con C# y PostgreSQL.",
            "Idiomas: Ingles B2."));

        Assert.True(result.IsSuccess, result.Error?.Title);
        Assert.StartsWith("Laura Gomez\nDesarrolladora backend\n", result.Value, StringComparison.Ordinal);
        Assert.Contains("APIs con C# y PostgreSQL.", result.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownFormat_IsRejectedByContent_NotByName() =>
        Assert.Equal("cv_unsupported_format", CvTextExtractor.Extract(Encoding.UTF8.GetBytes("Soy un archivo de texto con un CV dentro, pero no es PDF ni DOCX.")).Error?.Code);

    [Fact]
    public void ZipWithoutWordDocument_IsUnsupported()
    {
        using var output = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(output, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            archive.CreateEntry("otro.txt");
        }

        Assert.Equal("cv_unsupported_format", CvTextExtractor.Extract(output.ToArray()).Error?.Code);
    }

    [Fact]
    public void CorruptPdf_IsRejectedWithAClearError() =>
        Assert.Contains(
            CvTextExtractor.Extract(Encoding.ASCII.GetBytes("%PDF-1.7 esto no es un pdf valido")).Error?.Code,
            new string?[] { "cv_unreadable", "cv_no_text" });

    [Fact]
    public void AlmostEmptyDocument_IsReportedAsNoText() =>
        Assert.Equal("cv_no_text", CvTextExtractor.Extract(CvTestFiles.Docx("Hola")).Error?.Code);

    [Fact]
    public void OversizedFile_IsRejected() =>
        Assert.Equal("cv_too_large", CvTextExtractor.Extract(new byte[CvTextExtractor.MaxFileBytes + 1]).Error?.Code);
}
