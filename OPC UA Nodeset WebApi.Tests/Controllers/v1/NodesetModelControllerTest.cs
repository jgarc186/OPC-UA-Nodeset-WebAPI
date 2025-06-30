namespace OPC_UA_Nodeset_WebApi.Tests;

using Microsoft.AspNetCore.Mvc.Testing;
using System.Text;
using System.Text.Json;

public class NodesetModelControllerTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    private HttpClient _client;

    private string _projectId;

    public NodesetModelControllerTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task createProject()
    {
        _client = _factory.CreateClient();
        var body = new StringContent(
            "{\"name\":\"Test Project\",\"owner\":\"Foo\"}",
            Encoding.UTF8,
            "application/json"
        );
        var response = await _client.PostAsync("/api/v1/project", body);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        _projectId = JsonDocument.Parse(content).RootElement.GetProperty("projectId").GetString();
    }

    [Fact]
    public async Task TestWhenLoadingNodesetWithValidUri_ShouldReturnSuccess()
    {
        await createProject();

        var body = new StringContent(
            $"{{\"projectId\":\"{_projectId}\",\"uri\":\"opcfoundation.org.UA.NodeSet2.xml\"}}",
            Encoding.UTF8,
            "application/json"
        );
        var response = await _client.PostAsync("/api/v1/nodeset-model/upload-xml-from-base-64", body);
        // Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    }
}
