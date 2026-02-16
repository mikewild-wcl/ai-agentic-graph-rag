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
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public void SplitTextByTitles_NoTitles_ReturnsSingleSection()
    {
        // Arrange
        var input = "This is a sample text without any titles.";

        // Act
        var result = TextSplitter.SplitTextByTitles(input);

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBe(input);
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
        result.Length.ShouldBe(3);
        result[0].ShouldBe("1. Introduction\nThis is the introduction section.");
        result[1].ShouldBe("2. Background\nThis is the background section.");
        result[2].ShouldBe("3. Conclusion\nThis is the conclusion section.");
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
        result.ShouldHaveSingleItem();
        result[0].ShouldBe("1. Title Only");
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
        result.ShouldHaveSingleItem();
        result[0].ShouldBe("1.   Title With Extra Spaces\nThis is the content.");
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
        result.Length.ShouldBe(5);
        result[0].ShouldBe("Preamble text that is not part of any section.");
        result[1].ShouldBe("1. Introduction\nThis is the introduction section.");
        result[2].ShouldBe("2. Background\nThis is the background section.");
        result[3].ShouldBe("2a. Subsection\nThis is a subsection.");
        result[4].ShouldBe("3. Conclusion\nThis is the conclusion section.");
    }
}
