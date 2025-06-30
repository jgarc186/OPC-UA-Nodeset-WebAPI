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

    public async Task<HttpResponseMessage> UploadXmlFromBase64(string xmlFileName, string endpoint)
    {
        EnsureNodeSetsDirectoryExists();
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "TestData", xmlFileName);
        var fileBytes = await File.ReadAllBytesAsync(xmlPath);
        var base64Xml = Convert.ToBase64String(fileBytes);
        var requestBody = new
        {
            projectId = _projectId,
            xmlBase64 = base64Xml
        };
        var json = JsonSerializer.Serialize(requestBody);
        var body = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PostAsync(endpoint, body);
    }
}
