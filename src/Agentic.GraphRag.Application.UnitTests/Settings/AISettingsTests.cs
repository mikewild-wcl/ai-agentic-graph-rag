using Agentic.GraphRag.Shared.Configuration;

namespace Agentic.GraphRag.Application.UnitTests.Settings;

public class AISettingsTests
{
    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var provider = AIProvider.AzureOpenAI;
        var deploymentName = "deployment";
        var modelName = "model";
        var timeout = 99;

        // Act
        var options = new AISettings(provider, deploymentName, modelName, timeout);

        // Assert
        options.Provider.Should().Be(provider);
        options.DeploymentName.Should().Be(deploymentName);
        options.Model.Should().Be(modelName);
        options.EmbeddingDeploymentName.Should().BeNull();
        options.EmbeddingModel.Should().BeNull();
        options.Timeout.Should().Be(timeout);
    }

    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly_WithDefaultTimeout()
    {
        // Arrange
        var provider = AIProvider.AzureOpenAI;
        var deploymentName = "deployment";
        var modelName = "model";
        var defaultTimeout = 120;

        // Act
        var options = new AISettings(provider, deploymentName, modelName);

        // Assert
        options.Provider.Should().Be(provider);
        options.DeploymentName.Should().Be(deploymentName);
        options.Model.Should().Be(modelName);
        options.EmbeddingDeploymentName.Should().BeNull();
        options.EmbeddingModel.Should().BeNull();
        options.Timeout.Should().Be(defaultTimeout);
    }

    [Fact]
    public void With_ShouldSetPropertiesCorrectly()
    {
        //Arrange
        var provider = AIProvider.AzureOpenAI;
        var deploymentName = "deployment";
        var modelName = "model";
        var embeddingDeploymentName = "embedding";
        var embeddingModelName = "embedding";
        var timeout = 100;

        var options = new AISettings(
            AIProvider.AzureLocalFoundry,
            "DUMMY_DEPLOYMENT",
            "DUMMY_MODEL")
        {
            Timeout = 10
        };

        // Act
        options = options with
        {
            Provider = provider,
            DeploymentName = deploymentName,
            Model = modelName,
            EmbeddingDeploymentName = embeddingDeploymentName,
            EmbeddingModel = embeddingModelName,
            Timeout = timeout
        };

        //Assert
        options.Provider.Should().Be(provider);
        options.DeploymentName.Should().Be(deploymentName);
        options.Model.Should().Be(modelName);
        options.EmbeddingDeploymentName.Should().Be(embeddingDeploymentName);
        options.EmbeddingModel.Should().Be(embeddingModelName);
        options.Timeout.Should().Be(timeout);
    }

    [Fact]
    public void Timeout_CanBeSetViaInit()
    {
        // Arrange
        var options = new AISettings(AIProvider.GitHubModels, "a", "b") { Timeout = 99 };

        // Assert
        options.Timeout.Should().Be(99);
    }
}
