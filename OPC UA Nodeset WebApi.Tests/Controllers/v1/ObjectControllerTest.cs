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
    public async Task TestWhenCreatingAnInstanceObject()
    {
        await CreateProject();

        await LoadNodesetModel("opcfoundation.org.UA.NodeSet2.xml");
        await LoadNodesetModel("opcfoundation.org.UA.DI.NodeSet2.xml");
        await UploadXmlFromBase64("opcfoundation.org.UA.Machinery.xml");

        var response = await PostAsync("/api/v1/object", new
        {
            projectId = _projectId,
            uri = "http:opcfoundation.orgUAMachinery",
            parentNodeId = "nsu=http://opcfoundation.org/UA/;i=24",
            typeDefinitionNodeId = "nsu=http://opcfoundation.org/UA/;i=1",
            nodeClass = "Object",
            browseName = "manager",
            displayName = "Manager",
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Manager", JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("displayName").GetString());
    }
}
