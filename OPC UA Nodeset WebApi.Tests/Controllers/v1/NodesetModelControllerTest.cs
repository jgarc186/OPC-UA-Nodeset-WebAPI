namespace OPC_UA_Nodeset_WebApi.Tests;

using Microsoft.AspNetCore.Mvc.Testing;
using System.Text;
using System.Text.Json;

public class NodesetModelControllerTest : TestBase
{
    public NodesetModelControllerTest(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task TestWhenLoadingNodesetWithValidUri_ShouldReturnSuccess()
    {
        await createProject();

        var response = await loadNodesetModel("opcfoundation.org.UA.NodeSet2.xml");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("http:opcfoundation.orgUA", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TestWhenLoadingDINodesetWithValidUri_ShouldReturnSuccess()
    {
        await createProject();

        // We are loading the Base Nodeset first because this is a dependency for the DI Nodeset
        await loadNodesetModel("opcfoundation.org.UA.NodeSet2.xml");

        // Then we load the DI Nodeset
        var response = await loadNodesetModel("opcfoundation.org.UA.DI.NodeSet2.xml");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("http:opcfoundation.orgUADI", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TestWhenLoadingNodesetWithInvalidUri_ShouldReturnInternalServerError()
    {
        await createProject();

        var response = await loadNodesetModel("invalid-uri.xml");

        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task TestWhenLoadingNodesetWithEmptyUri_ShouldReturnInternalServerError()
    {
        await createProject();

        var body = new StringContent(
            $"{{\"projectId\":\"{_projectId}\",\"uri\":\"\"}}",
            Encoding.UTF8,
            "application/json"
        );
        var response = await _client.PostAsync("/api/v1/nodeset-model/load-xml-from-server-async", body);

        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
