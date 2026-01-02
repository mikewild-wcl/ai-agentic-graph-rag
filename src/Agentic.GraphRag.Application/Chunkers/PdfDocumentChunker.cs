using Agentic.GraphRag.Application.Chunkers.Interfaces;
using System.Runtime.CompilerServices;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;

namespace Agentic.GraphRag.Application.Chunkers;

public class PdfDocumentChunker : IDocumentChunker
{
    public async IAsyncEnumerable<string> StreamTextChunks(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            yield break;
        }

        /*
         * Improvement: track the page number for each chunk, possibly by having a dictionary with the index 
         * in documentText (.Length before the text is appended?) of the text , 
         * and return the details as metadata in a tuple with the text:
         *  IAsyncEnumerable<(int PageNumber, int IndexOnPage, string Text)>
         */

        var (documentText, _) = ExtractDocument(filePath);

        foreach (var chunk in TextChunker.ChunkTextOnWhitespaceOnly(documentText.ToString()))
        {
            yield return chunk;
        }
    }

    public async IAsyncEnumerable<(int SectionId, string SectionText, IReadOnlyList<string> ChildChunks)> StreamSections(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            yield break;
        }

        var (documentText, _) = ExtractDocument(filePath);
        var sections = TextSplitter.SplitTextByTitles(documentText.ToString());

        var sectionId = 0;
        foreach (var section in sections)
        {
            sectionId++;
            var childChunks = new List<string>();

            foreach (var chunk in TextChunker.ChunkTextOnWhitespaceOnly(section))
            {
                childChunks.Add(chunk);
            }

            yield return (sectionId, section, childChunks);
        }
    }

    private static (StringBuilder, List<(int, IPdfImage)>) ExtractDocument(string filePath)
    {
        var documentText = new StringBuilder();
        List<(int, IPdfImage)> imageList = [];

        using (var document = PdfDocument.Open(filePath))
        {
            foreach (var page in document.GetPages())
            {
                var letters = page.Letters;
                var words = NearestNeighbourWordExtractor.Instance.GetWords(letters);
                var textBlocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);
                var pageText = string.Join(
                    string.Empty,
                    textBlocks.Select(t => t.Text.ReplaceLineEndings(" ")).ToArray());

                documentText.Append(pageText);

                var images = page.GetImages();
                images?.ToList().ForEach(img => imageList.Add((page.Number, img)));
            }
        }

        return (documentText, imageList);
    }
}
