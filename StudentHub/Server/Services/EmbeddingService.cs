using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Xceed.Words.NET;

namespace StudentHub.Server.Services;

public class EmbeddingService
{
    public List<string>? Paragraphs;

    public EmbeddingService(Stream stream, string fileType)
    {
        string? fileToEmbed = ParseFile(stream, fileType);
        List<string> lines = SplitPlainTextLines(fileToEmbed, 1500);
        Paragraphs = SplitPlainTextParagraphs(lines, 1500);
    }

    public string? ParseFile(Stream stream, string fileName)
    {
        string fileType = Path.GetExtension(fileName);
        if (fileType.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            using PdfReader reader = new(stream);
            using PdfDocument pdfDoc = new(reader);
            StringBuilder text = new();

            for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
                text.AppendLine(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page)));

            string? textContent = text.ToString();
            return textContent;
        }

        if (fileType.Equals(".txt", StringComparison.OrdinalIgnoreCase))
        {
            using StreamReader reader = new(stream);
            string? textContent = reader.ReadToEnd();
            return textContent;
        }

        if (fileType.Equals(".docx", StringComparison.OrdinalIgnoreCase))
        {
            using DocX? reader = DocX.Load(stream);
            string? textContent = reader.Text;
            return textContent;
        }

        return "file cannot be parsed";
    }


    /// <summary>
    ///     Split plain text into lines.
    /// </summary>
    /// <param name="text">Text to split</param>
    /// <param name="maxTokensPerLine">Maximum number of tokens per line.</param>
    /// <returns>List of lines.</returns>
    public List<string> SplitPlainTextLines(string text, int maxTokensPerLine)
    {
        return InternalSplitPlaintextLines(text, maxTokensPerLine, true);
    }

    /// <summary>
    ///     Split markdown text into lines.
    /// </summary>
    /// <param name="text">Text to split</param>
    /// <param name="maxTokensPerLine">Maximum number of tokens per line.</param>
    /// <returns>List of lines.</returns>
    public static List<string> SplitMarkDownLines(string text, int maxTokensPerLine)
    {
        return InternalSplitMarkdownLines(text, maxTokensPerLine, true);
    }

    /// <summary>
    ///     Split plain text into paragraphs.
    /// </summary>
    /// <param name="lines">Lines of text.</param>
    /// <param name="maxTokensPerParagraph">Maximum number of tokens per paragraph.</param>
    /// <returns>List of paragraphs.</returns>
    public List<string> SplitPlainTextParagraphs(List<string> lines, int maxTokensPerParagraph)
    {
        return InternalSplitTextParagraphs(lines, maxTokensPerParagraph,
            text => InternalSplitPlaintextLines(text, maxTokensPerParagraph, false));
    }

    /// <summary>
    ///     Split markdown text into paragraphs.
    /// </summary>
    /// <param name="lines">Lines of text.</param>
    /// <param name="maxTokensPerParagraph">Maximum number of tokens per paragraph.</param>
    /// <returns>List of paragraphs.</returns>
    public static List<string> SplitMarkdownParagraphs(List<string> lines, int maxTokensPerParagraph)
    {
        return InternalSplitTextParagraphs(lines, maxTokensPerParagraph,
            text => InternalSplitMarkdownLines(text, maxTokensPerParagraph, false));
    }

    private static List<string> InternalSplitTextParagraphs(List<string> lines, int maxTokensPerParagraph,
        Func<string, List<string>> longLinesSplitter)
    {
        if (lines.Count == 0) return new List<string>();

        // Split long lines first
        List<string> truncatedLines = new();
        foreach (string line in lines) truncatedLines.AddRange(longLinesSplitter(line));

        lines = truncatedLines;

        // Group lines in paragraphs
        List<string> paragraphs = new();
        StringBuilder currentParagraph = new();
        foreach (string line in lines)
        {
            // "+1" to account for the "new line" added by AppendLine()
            if (TokenCount(currentParagraph.ToString()) + TokenCount(line) + 1 >= maxTokensPerParagraph &&
                currentParagraph.Length > 0)
            {
                paragraphs.Add(currentParagraph.ToString().Trim());
                currentParagraph.Clear();
            }

            currentParagraph.AppendLine(line);
        }

        if (currentParagraph.Length > 0)
        {
            paragraphs.Add(currentParagraph.ToString().Trim());
            currentParagraph.Clear();
        }

        // distribute text more evenly in the last paragraphs when the last paragraph is too short.
        if (paragraphs.Count > 1)
        {
            string lastParagraph = paragraphs[^1];
            string secondLastParagraph = paragraphs[^2];

            if (TokenCount(lastParagraph) < maxTokensPerParagraph / 4)
            {
                string[] lastParagraphTokens = lastParagraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string[] secondLastParagraphTokens =
                    secondLastParagraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                int lastParagraphTokensCount = lastParagraphTokens.Length;
                int secondLastParagraphTokensCount = secondLastParagraphTokens.Length;

                if (lastParagraphTokensCount + secondLastParagraphTokensCount <= maxTokensPerParagraph)
                {
                    StringBuilder newSecondLastParagraph = new();
                    for (int i = 0; i < secondLastParagraphTokensCount; i++)
                        newSecondLastParagraph.Append(secondLastParagraphTokens[i])
                            .Append(' ');

                    for (int i = 0; i < lastParagraphTokensCount; i++)
                        newSecondLastParagraph.Append(lastParagraphTokens[i])
                            .Append(' ');

                    paragraphs[^2] = newSecondLastParagraph.ToString().Trim();
                    paragraphs.RemoveAt(paragraphs.Count - 1);
                }
            }
        }

        return paragraphs;
    }

    private static List<string> InternalSplitPlaintextLines(string text, int maxTokensPerLine, bool trim)
    {
        text = text.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase);

        List<List<char>?> splitOptions = new()
        {
            new() { '\n', '\r' },
            new() { '.' },
            new() { '?', '!' },
            new() { ';' },
            new() { ':' },
            new() { ',' },
            new() { ')', ']', '}' },
            new() { ' ' },
            new() { '-' },
            null
        };

        List<string>? result = null;
        bool inputWasSplit;
        foreach (List<char>? splitOption in splitOptions)
        {
            if (result is null)
                result = Split(text, maxTokensPerLine, splitOption, trim, out inputWasSplit);
            else
                result = Split(result, maxTokensPerLine, splitOption, trim, out inputWasSplit);

            if (!inputWasSplit) break;
        }

        return result ?? new List<string>();
    }

    private static List<string> InternalSplitMarkdownLines(string text, int maxTokensPerLine, bool trim)
    {
        text = text.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase);

        List<List<char>?> splitOptions = new()
        {
            new() { '.' },
            new() { '?', '!' },
            new() { ';' },
            new() { ':' },
            new() { ',' },
            new() { ')', ']', '}' },
            new() { ' ' },
            new() { '-' },
            new() { '\n', '\r' },
            null
        };

        List<string>? result = null;
        bool inputWasSplit;
        foreach (List<char>? splitOption in splitOptions)
        {
            if (result is null)
                result = Split(text, maxTokensPerLine, splitOption, trim, out inputWasSplit);
            else
                result = Split(result, maxTokensPerLine, splitOption, trim, out inputWasSplit);

            if (!inputWasSplit) break;
        }

        return result ?? new List<string>();
    }

    private static List<string> Split(IEnumerable<string> input, int maxTokens, List<char>? separators, bool trim,
        out bool inputWasSplit)
    {
        inputWasSplit = false;
        List<string> result = new();
        foreach (string text in input)
        {
            result.AddRange(Split(text, maxTokens, separators, trim, out bool split));
            inputWasSplit = inputWasSplit || split;
        }

        return result;
    }

    private static List<string> Split(string input, int maxTokens, List<char>? separators, bool trim,
        out bool inputWasSplit)
    {
        inputWasSplit = false;
        List<string> asIs = new() { trim ? input.Trim() : input };
        if (TokenCount(input) <= maxTokens) return asIs;

        inputWasSplit = true;
        List<string> result = new();

        int half = input.Length / 2;
        int cutPoint = -1;

        if (separators == null || separators.Count == 0)
            cutPoint = half;
        else if (input.Any(separators.Contains) && input.Length > 2)
            for (int index = 0; index < input.Length - 1; index++)
            {
                if (!separators.Contains(input[index])) continue;

                if (Math.Abs(half - index) < Math.Abs(half - cutPoint)) cutPoint = index + 1;
            }

        if (cutPoint > 0)
        {
            string firstHalf = input[..cutPoint];
            string secondHalf = input[cutPoint..];
            if (trim)
            {
                firstHalf = firstHalf.Trim();
                secondHalf = secondHalf.Trim();
            }

            // Recursion
            result.AddRange(Split(firstHalf, maxTokens, separators, trim, out bool split1));
            result.AddRange(Split(secondHalf, maxTokens, separators, trim, out bool split2));

            inputWasSplit = split1 || split2;

            return result;
        }

        return asIs;
    }

    private static int TokenCount(string input)
    {
        // TODO: partitioning methods should be configurable to allow for different tokenization strategies
        //       depending on the model to be called. For now, we use an extremely rough estimate.
        return input.Length / 4;
    }
}