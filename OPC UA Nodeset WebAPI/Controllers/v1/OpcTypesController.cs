namespace OPC_UA_Nodeset_WebAPI.Controllers.v1;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

public class OpcTypesController : AbstractBaseController
{
    public OpcTypesController(IApplicationModelProvider applicationModelProvider) : base(applicationModelProvider)
    {
    }

    [HttpGet]
    [Route("v1/reference-type")]
    public IActionResult PostReferenceType([FromQuery] OpcTypesRequest request)
    {

        return Ok(response);
    }
}
