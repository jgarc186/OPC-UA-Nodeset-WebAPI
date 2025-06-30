namespace OPC_UA_Nodeset_WebApi.Tests;

using Microsoft.AspNetCore.Mvc.Testing;
using System.Text;
using System.Text.Json;

/**
 * to run this set of tests paste the following command in the terminal:
 *
 * dotnet test --filter "FullyQualifiedName~ObjectControllerTest"
 */
public class ObjectControllerTest : TestBase
{
    public ObjectControllerTest(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task TestWhenUploadingXmlFromBase64_ShouldReturnSuccess()
    {
        await CreateProject();

        await LoadNodesetModel("opcfoundation.org.UA.NodeSet2.xml");
        await LoadNodesetModel("opcfoundation.org.UA.DI.NodeSet2.xml");

        var response = await UploadXmlFromBase64("opcfoundation.org.UA.Machinery.xml", "/api/v1/nodeset-model/upload-xml-from-base-64");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("http:opcfoundation.orgUAMachinery", await response.Content.ReadAsStringAsync());
    }
}
