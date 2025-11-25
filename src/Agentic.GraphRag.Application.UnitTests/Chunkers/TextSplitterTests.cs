using Agentic.GraphRag.Application.Chunkers;

namespace Agentic.GraphRag.Application.UnitTests.Chunkers;

public class TextSplitterTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void SplitTextByTitles_EmptyInput_ReturnsEmptyArray()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = TextSplitter.SplitTextByTitles(input);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void SplitTextByTitles_NoTitles_ReturnsSingleSection()
    {
        // Arrange
        var input = "This is a sample text without any titles.";

        // Act
        var result = TextSplitter.SplitTextByTitles(input);

        // Assert
        Assert.Single(result);
        Assert.Equal(input, result[0]);
    }

    [Fact]
    public void SplitTextByTitles_WithTitles_ReturnsSectionsWithTitles()
    {
        // Arrange
        var input =
            """
            1. Introduction
            This is the introduction section.

            2. Background
            This is the background section.

            3. Conclusion
            This is the conclusion section.
            """;

        // Act
        var result = TextSplitter.SplitTextByTitles(input);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("1. Introduction\nThis is the introduction section.", result[0]);
        Assert.Equal("2. Background\nThis is the background section.", result[1]);
        Assert.Equal("3. Conclusion\nThis is the conclusion section.", result[2]);
    }

    [Fact]
    public void SplitTextByTitles_TitleWithoutContent_ReturnsEmptySection()
    {
        // Arrange
        var input =
            """
              1. Title Only            
            """;

        // Act
        var result = TextSplitter.SplitTextByTitles(input);

        // Assert
        Assert.Single(result);
        Assert.Equal("1. Title Only", result[0]);
    }

    [Fact]
    public void SplitTextByTitles_MultipleSpacesInTitle_HandlesCorrectly()
    {
        // Arrange
        var input =
            """
            1.   Title With Extra Spaces
            This is the content.     
            """;

        // Act
        var result = TextSplitter.SplitTextByTitles(input);

        // Assert
        Assert.Single(result);
        Assert.Equal("1.   Title With Extra Spaces\nThis is the content.", result[0]);
    }

    [Fact]
    public void SplitTextByTitles_WithTitlesandLeadingText_ReturnsSectionsWithTitles()
    {
        // Arrange
        var input =
            """
            Preamble text that is not part of any section.

            1. Introduction
               This is the introduction section.

            2. Background
               This is the background section.

                2a. Subsection
                   This is a subsection.

            3. Conclusion
               This is the conclusion section.
            """;

        // Act
        var result = TextSplitter.SplitTextByTitles(input);

        _output.WriteLine("Results:");
        foreach (var section in result)
        {
            _output.WriteLine(section);
        }

        // Assert
        Assert.Equal(5, result.Length);
        Assert.Equal("Preamble text that is not part of any section.", result[0]);
        Assert.Equal("1. Introduction\nThis is the introduction section.", result[1]);
        Assert.Equal("2. Background\nThis is the background section.", result[2]);
        Assert.Equal("2a. Subsection\nThis is a subsection.", result[3]);
        Assert.Equal("3. Conclusion\nThis is the conclusion section.", result[4]);
    }
}
