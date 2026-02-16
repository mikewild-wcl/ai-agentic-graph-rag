using Agentic.GraphRag.Application.Settings;

namespace Agentic.GraphRag.Application.UnitTests.Settings;

public class EinsteinQuerySettingsTests
{
    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var documentFileName = "Test.pdf";
        var documentUri = new Uri("http://localhost/test");

        // Act
        var options = new EinsteinQuerySettings()
        {
            DocumentFileName = documentFileName,
            DocumentUri = documentUri
        };

        // Assert
        options.DocumentFileName.ShouldBe(documentFileName);
        options.DocumentUri.ShouldBe(documentUri);
    }

    [Fact]
    public void With_ShouldSetPropertiesCorrectly()
    {
        //Arrange
        var documentFileName = "Test.pdf";
        var documentUri = new Uri("http://localhost/test");

        var options = new EinsteinQuerySettings()
        {
            DocumentFileName = "dummydoc.pdf",
            DocumentUri = new Uri("http://localhost/dummy")
        };

        // Act
        options = options with
        {
            DocumentFileName = documentFileName,
            DocumentUri = documentUri
        };

        //Assert
        options.DocumentFileName.ShouldBe(documentFileName);
        options.DocumentUri.ShouldBe(documentUri);
    }

    [Fact]
    public void NonConstructorProperties_CanBeSetViaInit()
    {
        // Arrange
        var options = new EinsteinQuerySettings()
        {
            DocumentFileName = "mydoc.pdf",
            DocumentUri = new Uri("http://localhost/test"),
        };

        // Assert
        options.DocumentFileName.ShouldBe("mydoc.pdf");
        options.DocumentUri.AbsoluteUri.ShouldBe("http://localhost/test");
    }
}
