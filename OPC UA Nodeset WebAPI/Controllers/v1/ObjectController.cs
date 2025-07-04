﻿using CESMII.OpcUa.NodeSetModel;
using Microsoft.AspNetCore.Mvc;
using OPC_UA_Nodeset_WebAPI.Model.v1.Responses;
using OPC_UA_Nodeset_WebAPI.Model.v1.Requests;
using OPC_UA_Nodeset_WebAPI.UA_Nodeset_Utilities;
using System.Web;
using System.Text.Json;

namespace OPC_UA_Nodeset_WebAPI.Controllers.v1
{
    [ApiController]
    [Route("api/v1/object")]
    public class ObjectController : AbstractBaseController
    {
        private readonly ILogger<ProjectController> _logger;

        private ApplicationInstance ApplicationInstance { get; set; }

        public ObjectController(ILogger<ProjectController> logger, ApplicationInstance applicationInstance)
        {
            _logger = logger;
            ApplicationInstance = applicationInstance;
        }

        [HttpGet("{id}/{uri}")]
        [ProducesResponseType(200, Type = typeof(Dictionary<string, ObjectModelResponse>))]
        public IActionResult Get(string id, string uri)
        {
            var activeNodesetModelResult = ApplicationInstance.GetNodeSetModel(id, uri) as ObjectResult;

            if (StatusCodes.Status200OK != activeNodesetModelResult.StatusCode)
            {
                return activeNodesetModelResult;
            }
            var activeNodesetModel = activeNodesetModelResult.Value as NodeSetModel;
            var returnObject = new List<ObjectModelResponse>();
            foreach (var aObject in activeNodesetModel.GetObjects())
            {
                returnObject.Add(new ObjectModelResponse(aObject));
            }
            return Ok(returnObject);
        }

        [HttpGet("{nodeId}")]
        [ProducesResponseType(200, Type = typeof(ObjectModelResponse))]
        [ProducesResponseType(404, Type = typeof(NotFoundResult))]
        public IActionResult GetByNodeId(string id, string uri, string nodeId)
        {

            return ApplicationInstance.GetNodeApiModelByNodeId(id, uri, nodeId, "ObjectModel");
        }

        [HttpGet("ByDisplayName/{displayName}")]
        [ProducesResponseType(200, Type = typeof(List<ObjectModelResponse>))]
        [ProducesResponseType(404, Type = typeof(NotFoundResult))]
        public IActionResult GetByDisplayName(string id, string uri, string displayName)
        {
            var objectsListResult = Get(id, uri) as ObjectResult;

            if (StatusCodes.Status200OK != objectsListResult.StatusCode)
            {
                return objectsListResult;
            }
            else
            {
                var objectsList = objectsListResult.Value as List<ObjectModelResponse>;
                var returnObject = objectsList.Where(x => x.DisplayName == displayName).ToList();
                return Ok(returnObject);
            }
        }

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(ObjectModelResponse))]
        [ProducesResponseType(404, Type = typeof(NotFoundResult))]
        public async Task<IActionResult> HttpPost([FromBody] ObjectRequest request)
        {
            try
            {
                var id = request.ProjectId;
                var uri = request.Uri;
                var objectsListResult = Get(id, uri) as ObjectResult;

                if (StatusCodes.Status200OK != objectsListResult.StatusCode)
                {
                    return objectsListResult;
                }

                var objects = objectsListResult.Value as List<ObjectModelResponse>;
                FindOpcType<ObjectModelResponse>(objects, request);

                // add new object
                var projectInstanceResult = ApplicationInstance.GetNodeSetProjectInstance(id) as ObjectResult;
                var activeProjectInstance = projectInstanceResult.Value as NodeSetProjectInstance;

                var activeNodesetModelResult = ApplicationInstance.GetNodeSetModel(id, uri) as ObjectResult;
                var activeNodesetModel = activeNodesetModelResult.Value as NodeSetModel;

                // look up parent object
                var aParentModel = activeProjectInstance.NodeSetModels.FirstOrDefault(x => x.Value.ModelUri == UaNodeResponse.GetNameSpaceFromNodeId(request.ParentNodeId)).Value;
                var parentNode = aParentModel.AllNodesByNodeId[request.ParentNodeId];

                // look up type definition
                var aObjectTypeModel = activeProjectInstance.NodeSetModels.FirstOrDefault(x => x.Value.ModelUri == UaNodeResponse.GetNameSpaceFromNodeId(request.TypeDefinitionNodeId)).Value;
                var aObjectTypeDefinition = aObjectTypeModel.ObjectTypes.FirstOrDefault(ot => ot.NodeId == request.TypeDefinitionNodeId);

                var newObjectModel = new ObjectModel
                {
                    NodeSet = activeNodesetModel,
                    NodeId = UaNodeResponse.GetNodeIdFromIdAndNameSpace((activeProjectInstance.NextNodeIds[activeNodesetModel.ModelUri]++).ToString(), activeNodesetModel.ModelUri),
                    Parent = parentNode,
                    TypeDefinition = aObjectTypeDefinition,
                    DisplayName = new List<NodeModel.LocalizedText> { request.DisplayName },
                    BrowseName = request.BrowseName,
                    Description = new List<NodeModel.LocalizedText> { request.Description == null ? "" : request.Description },
                    Properties = new List<VariableModel>(),
                    DataVariables = new List<DataVariableModel>()
                };

                if (request.GenerateChildren.HasValue)
                {
                    if (request.GenerateChildren.Value)
                    {
                        aObjectTypeDefinition.Properties.ForEach(aProperty =>
                        {
                            newObjectModel.Properties.Add(new PropertyModel
                            {
                                NodeSet = activeNodesetModel,
                                NodeId = UaNodeResponse.GetNodeIdFromIdAndNameSpace((activeProjectInstance.NextNodeIds[activeNodesetModel.ModelUri]++).ToString(), activeNodesetModel.ModelUri),
                                Parent = newObjectModel,
                                DisplayName = aProperty.DisplayName,
                                BrowseName = aProperty.BrowseName,
                                Description = aProperty.Description,
                                DataType = aProperty.DataType,
                                Value = aProperty.Value,
                                EngineeringUnit = aProperty.EngineeringUnit,
                            });
                        });
                        aObjectTypeDefinition.DataVariables.ForEach(aDataVariable =>
                        {
                            newObjectModel.DataVariables.Add(new DataVariableModel
                            {
                                NodeSet = activeNodesetModel,
                                NodeId = UaNodeResponse.GetNodeIdFromIdAndNameSpace((activeProjectInstance.NextNodeIds[activeNodesetModel.ModelUri]++).ToString(), activeNodesetModel.ModelUri),
                                Parent = newObjectModel,
                                DisplayName = aDataVariable.DisplayName,
                                BrowseName = aDataVariable.BrowseName,
                                Description = aDataVariable.Description,
                                DataType = aDataVariable.DataType,
                                Value = aDataVariable.Value,
                                EngineeringUnit = aDataVariable.EngineeringUnit,
                            });
                        });
                    }
                }

                activeNodesetModel.Objects.Add(newObjectModel);
                activeNodesetModel.UpdateIndices();
                return Ok(new ObjectModelResponse(newObjectModel));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new object");
                return BadRequest("Error creating new object: " + ex.Message);
            }
        }

        [HttpPost("bulk-processing")]
        [ProducesResponseType(200, Type = typeof(List<ObjectModelResponse>))]
        [ProducesResponseType(404, Type = typeof(NotFoundResult))]
        public async Task<IActionResult> BulkProcessing([FromBody] BulkObjectRequest request)
        {
            try
            {
                var id = request.ProjectId;
                var uri = request.Uri;
                var parentNodeId = request.ParentNodeId;
                var objectsListResult = Get(id, uri) as ObjectResult;
                var objectInstancesCreated = new List<ObjectModelResponse>();

                if (StatusCodes.Status200OK != objectsListResult.StatusCode)
                {
                    throw new Exception($"Error retrieving objects for project {id} and URI {uri}");
                }

                foreach (var type in request.Types)
                {
                    var objects = objectsListResult.Value as List<ObjectModelResponse>;
                    FindOpcType<ObjectModelResponse>(objects, type);

                    // add new object
                    var projectInstanceResult = ApplicationInstance.GetNodeSetProjectInstance(id) as ObjectResult;
                    var activeProjectInstance = projectInstanceResult.Value as NodeSetProjectInstance;
                    var activeNodesetModelResult = ApplicationInstance.GetNodeSetModel(id, uri) as ObjectResult;
                    var activeNodesetModel = activeNodesetModelResult.Value as NodeSetModel;

                    // look up parent object
                    var aParentModel = activeProjectInstance.NodeSetModels.FirstOrDefault(x => x.Value.ModelUri == UaNodeResponse.GetNameSpaceFromNodeId(type.ParentNodeId)).Value;
                    var parentNode = aParentModel.AllNodesByNodeId[type.ParentNodeId];

                    // look up type definition
                    var aObjectTypeModel = activeProjectInstance.NodeSetModels.FirstOrDefault(x => x.Value.ModelUri == UaNodeResponse.GetNameSpaceFromNodeId(type.TypeDefinitionNodeId)).Value;
                    var aObjectTypeDefinition = aObjectTypeModel.ObjectTypes.FirstOrDefault(ot => ot.NodeId == type.TypeDefinitionNodeId);

                    var newObjectModel = new ObjectModel
                    {
                        NodeSet = activeNodesetModel,
                        NodeId = UaNodeResponse.GetNodeIdFromIdAndNameSpace((activeProjectInstance.NextNodeIds[activeNodesetModel.ModelUri]++).ToString(), activeNodesetModel.ModelUri),
                        Parent = parentNode,
                        TypeDefinition = aObjectTypeDefinition,
                        DisplayName = new List<NodeModel.LocalizedText> { type.DisplayName },
                        BrowseName = type.BrowseName,
                        Description = new List<NodeModel.LocalizedText> { type.Description == null ? "" : type.Description },
                        Properties = new List<VariableModel>(),
                        DataVariables = new List<DataVariableModel>()
                    };

                    if (type.GenerateChildren.HasValue)
                    {
                        if (type.GenerateChildren.Value)
                        {
                            aObjectTypeDefinition.Properties.ForEach(aProperty =>
                            {
                                newObjectModel.Properties.Add(new PropertyModel
                                {
                                    NodeSet = activeNodesetModel,
                                    NodeId = UaNodeResponse.GetNodeIdFromIdAndNameSpace((activeProjectInstance.NextNodeIds[activeNodesetModel.ModelUri]++).ToString(), activeNodesetModel.ModelUri),
                                    Parent = newObjectModel,
                                    DisplayName = aProperty.DisplayName,
                                    BrowseName = aProperty.BrowseName,
                                    Description = aProperty.Description,
                                    DataType = aProperty.DataType,
                                    Value = aProperty.Value,
                                    EngineeringUnit = aProperty.EngineeringUnit,
                                });
                            });
                            aObjectTypeDefinition.DataVariables.ForEach(aDataVariable =>
                            {
                                newObjectModel.DataVariables.Add(new DataVariableModel
                                {
                                    NodeSet = activeNodesetModel,
                                    NodeId = UaNodeResponse.GetNodeIdFromIdAndNameSpace((activeProjectInstance.NextNodeIds[activeNodesetModel.ModelUri]++).ToString(), activeNodesetModel.ModelUri),
                                    Parent = newObjectModel,
                                    DisplayName = aDataVariable.DisplayName,
                                    BrowseName = aDataVariable.BrowseName,
                                    Description = aDataVariable.Description,
                                    DataType = aDataVariable.DataType,
                                    Value = aDataVariable.Value,
                                    EngineeringUnit = aDataVariable.EngineeringUnit,
                                });
                            });
                        }
                    }

                    activeNodesetModel.Objects.Add(newObjectModel);
                    activeNodesetModel.UpdateIndices();
                    objectInstancesCreated.Add(new ObjectModelResponse(newObjectModel));
                }

                return Ok(objectInstancesCreated);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing bulk request: " + ex.Message);
                _logger.LogError(ex, "Error processing bulk request");
                return BadRequest("Error processing bulk request: " + ex.Message);
            }
        }
    }
}
