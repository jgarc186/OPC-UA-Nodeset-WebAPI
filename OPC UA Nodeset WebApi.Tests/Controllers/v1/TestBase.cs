namespace OPC_UA_Nodeset_WebApi.Tests;

using Microsoft.AspNetCore.Mvc.Testing;
using System.Text;
using System.Text.Json;

public class TestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> _factory;

    protected HttpClient _client = null!;

    protected string _projectId = string.Empty;

    public TestBase(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    protected async Task CreateProject()
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

    protected async Task<HttpResponseMessage> LoadNodesetModel(string uri)
    {
        var body = new StringContent(
            $"{{\"projectId\":\"{_projectId}\",\"uri\":\"{uri}\"}}",
            Encoding.UTF8,
            "application/json"
        );
        return await _client.PostAsync("/api/v1/nodeset-model/load-xml-from-server-async", body);
    }

    public void EnsureNodeSetsDirectoryExists()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "NodeSets");
        if (!Directory.Exists(path))
        {

            Directory.CreateDirectory(path);
        }
    }
}
