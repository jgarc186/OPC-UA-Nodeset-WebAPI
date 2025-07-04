﻿using CESMII.OpcUa.NodeSetModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Opc.Ua;
using OPC_UA_Nodeset_WebAPI.Model.v1.Responses;
using OPC_UA_Nodeset_WebAPI.Model.v1.Requests;
using OPC_UA_Nodeset_WebAPI.UA_Nodeset_Utilities;
using System.Web;
using StatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace OPC_UA_Nodeset_WebAPI.Controllers.v1
{
    [ApiController]
    [Route("api/v1/data-type")]
    public class DataTypeController : AbstractBaseController
    {
        private readonly ILogger<ProjectController> _logger;

        private ApplicationInstance ApplicationInstance { get; set; }

        public DataTypeController(ILogger<ProjectController> logger, ApplicationInstance applicationInstance)
        {
            _logger = logger;
            ApplicationInstance = applicationInstance;
        }

        [HttpGet("{id}/{uri}")]
        [ProducesResponseType(200, Type = typeof(Dictionary<string, DataTypeResponse>))]
        public IActionResult Get(string id, string uri, [FromQuery] Dictionary<string, string> filters = null)
        {
            var activeNodesetModelResult = ApplicationInstance.GetNodeSetModel(id, uri) as ObjectResult;

            if (StatusCodes.Status200OK != activeNodesetModelResult.StatusCode)
            {
                return activeNodesetModelResult;
            }
            else
            {
                var activeNodesetModel = activeNodesetModelResult.Value as NodeSetModel;
                var returnObject = new List<DataTypeResponse>();
                foreach (var aDataType in activeNodesetModel.DataTypes)
                {
                    returnObject.Add(new DataTypeResponse(aDataType));
                }

                if (filters == null)
                {
                    return Ok(returnObject);
                }

                returnObject = returnObject.Where(x =>
                    (!filters.ContainsKey("displayName") || x.DisplayName == filters["displayName"]) &&
                    (!filters.ContainsKey("browseName") || x.BrowseName == filters["browseName"]) &&
                    (!filters.ContainsKey("description") || x.Description == filters["description"]) &&
                    (!filters.ContainsKey("superTypeNodeId") || x.SuperTypeNodeId == filters["superTypeNodeId"])
                ).ToList();

                return Ok(returnObject);
            }
        }

        [HttpGet("{nodeId}")]
        [ProducesResponseType(200, Type = typeof(DataTypeResponse))]
        [ProducesResponseType(404, Type = typeof(NotFoundResult))]
        public IActionResult GetByNodeId(string id, string uri, string nodeId)
        {
            return ApplicationInstance.GetNodeApiModelByNodeId(id, uri, nodeId, "DataTypeModel");
        }

        [HttpGet("ByDisplayName/{displayName}")]
        [ProducesResponseType(200, Type = typeof(List<DataTypeResponse>))]
        [ProducesResponseType(404, Type = typeof(NotFoundResult))]
        public IActionResult GetByDisplayName(string id, string uri, string displayName)
        {
            var dataTypesListResult = Get(id, uri) as ObjectResult;

            if (StatusCodes.Status200OK != dataTypesListResult.StatusCode)
            {
                return dataTypesListResult;
            }
            else
            {
                var dataTypes = dataTypesListResult.Value as List<DataTypeResponse>;
                var returnObject = dataTypes.Where(x => x.DisplayName == displayName).ToList();
                return Ok(returnObject);
            }
        }

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(DataTypeResponse))]
        [ProducesResponseType(404, Type = typeof(NotFoundResult))]
        public IActionResult HttpPost([FromBody] DataTypeRequest request)
        {
            try
            {
                var id = request.ProjectId;
                var uri = request.Uri;
                var dataTypesListResult = Get(id, uri) as ObjectResult;

                if (StatusCodes.Status200OK != dataTypesListResult.StatusCode)
                {
                    return dataTypesListResult;
                }

                var dataTypes = dataTypesListResult.Value as List<DataTypeResponse>;
                FindOpcType<DataTypeResponse>(dataTypes, request);

                // add new data type
                var projectInstanceResult = ApplicationInstance.GetNodeSetProjectInstance(id) as ObjectResult;
                var activeProjectInstance = projectInstanceResult.Value as NodeSetProjectInstance;
                var activeNodesetModelResult = ApplicationInstance.GetNodeSetModel(id, uri) as ObjectResult;
                var activeNodesetModel = activeNodesetModelResult.Value as NodeSetModel;

                var newDataTypeModel = new DataTypeModel
                {
                    NodeSet = activeNodesetModel,
                    NodeId = UaNodeResponse.GetNodeIdFromIdAndNameSpace((activeProjectInstance.NextNodeIds[activeNodesetModel.ModelUri]++).ToString(), activeNodesetModel.ModelUri),
                    SuperType = activeProjectInstance.GetNodeModelByNodeId(request.SuperTypeNodeId) as DataTypeModel,
                    DisplayName = new List<NodeModel.LocalizedText> { request.DisplayName == null ? "" : request.DisplayName },
                    BrowseName = request.BrowseName,
                    Description = new List<NodeModel.LocalizedText> { request.Description == null ? "" : request.Description },
                    EnumFields = request.EnumFields.Select(x => new DataTypeModel.UaEnumField
                    {
                        Name = x.Name,
                        Value = x.Value,
                        Description = x.Description == null ? new List<NodeModel.LocalizedText>() : new List<NodeModel.LocalizedText> { x.Description },
                        DisplayName = x.DisplayName == null ? new List<NodeModel.LocalizedText>() : new List<NodeModel.LocalizedText> { x.DisplayName }
                    }).ToList()
                };

                activeNodesetModel.DataTypes.Add(newDataTypeModel);
                activeNodesetModel.UpdateIndices();
                return Ok(new DataTypeResponse(newDataTypeModel));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating DataType");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
