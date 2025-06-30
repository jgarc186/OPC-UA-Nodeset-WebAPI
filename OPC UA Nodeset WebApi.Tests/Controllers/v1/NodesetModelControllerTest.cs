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
    public async Task TestWhenLoadingNodesetWithInvalidUri_ShouldReturnNotFound()
    {
        await createProject();

        var response = await loadNodesetModel("invalid-uri.xml");

        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
