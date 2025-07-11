namespace OPC_UA_Nodeset_WebApi.Tests;

using Microsoft.AspNetCore.Mvc.Testing;
using System.Text;
using System.Text.Json;

/**
 * to run this set of tests paste the following command in the terminal:
 *
 * dotnet test --filter "FullyQualifiedName~NodesetModelControllerTest"
 */
public class NodesetModelControllerTest : TestBase
{
    public NodesetModelControllerTest(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task TestWhenLoadingNodesetWithValidUri_ShouldReturnSuccess()
    {
        await CreateProject();

        var response = await LoadNodesetModel("opcfoundation.org.UA.NodeSet2.xml");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("http:opcfoundation.orgUA", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TestWhenLoadingDINodesetWithValidUri_ShouldReturnSuccess()
    {
        await CreateProject();

        // We are loading the Base Nodeset first because this is a dependency for the DI Nodeset
        await LoadNodesetModel("opcfoundation.org.UA.NodeSet2.xml");

        // Then we load the DI Nodeset
        var response = await LoadNodesetModel("opcfoundation.org.UA.DI.NodeSet2.xml");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("http:opcfoundation.orgUADI", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TestWhenLoadingNodesetWithInvalidUri_ShouldReturnInternalServerError()
    {
        await CreateProject();

        var response = await LoadNodesetModel("invalid-uri.xml");

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TestWhenLoadingNodesetWithEmptyUri_ShouldReturnInternalServerError()
    {
        await CreateProject();

        var body = new StringContent(
            $"{{\"projectId\":\"{_projectId}\",\"uri\":\"\"}}",
            Encoding.UTF8,
            "application/json"
        );
        var response = await _client.PostAsync("/api/v1/nodeset-model/load-xml-from-server-async", body);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TestWhenUploadingXmlFromBase64_ShouldReturnSuccess()
    {
        await CreateProject();

        await LoadNodesetModel("opcfoundation.org.UA.NodeSet2.xml");
        await LoadNodesetModel("opcfoundation.org.UA.DI.NodeSet2.xml");

        var response = await UploadXmlFromBase64("opcfoundation.org.UA.Machinery.xml");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("http:opcfoundation.orgUAMachinery", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TestWhenUploadingXmlFromBase64WithImcompatableNodesetDependencyVersion_ShouldReturnInternalServerError()
    {
        await CreateProject();

        // older version of the Base UA `1.04.7` and DI `1.03.0` Nodesets
        await LoadNodesetModel("opcfoundation.org.UA.NodeSet2.xml");
        await LoadNodesetModel("opcfoundation.org.UA.DI.NodeSet2.xml");

        // Upload version of IA Nodeset `1.01.2`
        await UploadXmlFromBase64("opcfoundation.org.IA.v.1.01.2.xml");

        // Now load the latests version of the Machinery Nodeset `1.04.0`
        var response = await UploadXmlFromBase64("opcfoundation.org.UA.Machinery.v.1.04.0.xml");

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Error: NodeSet can not be imported: Referenced node ns=1;i=480 not found for [ObjectType]MachineryOperationCounterType", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TestWhenUploadingXmlFromBase64WithCompatableNodesetDependencyVersion_ShouldReturnOKStatus()
    {
        await CreateProject();

        // Proper dependency versions: Base UA `1.05.03`, DI `1.04.0` & IA `1.01.2` Nodesets
        await UploadXmlFromBase64("opcfoundation.org.UA.v.1.05.03.xml");
        await UploadXmlFromBase64("opcfoundation.org.UA.DI.v.1.04.0.xml");
        await UploadXmlFromBase64("opcfoundation.org.IA.v.1.01.2.xml");

        // Now load the latests version of the Machinery Nodeset `1.04.0`
        var response = await UploadXmlFromBase64("opcfoundation.org.UA.Machinery.v.1.04.0.xml");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
