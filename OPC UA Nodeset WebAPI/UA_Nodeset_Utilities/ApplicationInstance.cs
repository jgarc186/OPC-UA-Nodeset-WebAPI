﻿using CESMII.OpcUa.NodeSetModel;
using Microsoft.AspNetCore.Mvc;
using Opc.Ua.Export.v1.Responses;
using Opc.Ua;
using OPC_UA_Nodeset_WebAPI.Model.v1.Responses;
using System.Collections.Concurrent;
using System.Reflection.PortableExecutable;
using System.Web;
using Microsoft.Extensions.Logging.Abstractions;

namespace OPC_UA_Nodeset_WebAPI.UA_Nodeset_Utilities
{
    public class ApplicationInstance : ControllerBase
    {
        public ConcurrentDictionary<string, NodeSetProjectInstance> NodeSetProjectInstances { get; } = new ConcurrentDictionary<string, NodeSetProjectInstance>();
        private ConcurrentDictionary<string, NodeSetInfoWithDependenciesResponse> _localNodesets { get; set; }
        public ConcurrentDictionary<string, NodeSetInfoWithDependenciesResponse> LocalNodesets
        {
            get
            {
                if (_localNodesets == null)
                {
                    ScanNodesetFiles();
                }
                return _localNodesets;
            }
            set
            {
                _localNodesets = value;
            }
        }

        public void ScanNodesetFiles()
        {
            _localNodesets = new ConcurrentDictionary<string, NodeSetInfoWithDependenciesResponse>();
            var allLocalNodesetFiles = Directory.GetFiles($"{AppContext.BaseDirectory}/NodeSets");
            foreach (var aFile in allLocalNodesetFiles)
            {
                //_localNodesets.Add(file, new List<string>());
                string aXmlString = System.IO.File.ReadAllText(aFile);
                var aNodeSet = UANodeSetFromString.Read(aXmlString);
                _localNodesets.TryAdd(Path.GetFileName(aFile), new NodeSetInfoWithDependenciesResponse(aNodeSet));
            }
        }

        public IActionResult GetNodeSetProjectInstance(string id)
        {
            NodeSetProjectInstance aNodesetProjectInstance;
            if (NodeSetProjectInstances.TryGetValue(id, out aNodesetProjectInstance))
            {
                return Ok(aNodesetProjectInstance);
            }
            else
            {
                return NotFound($"{id} - not a valid project id."); ; // because the project doesn't exist
            }
        }

        public IActionResult GetNodeSetModel(string id, string uri)
        {
            //var uriNoSlashes = HttpUtility.UrlDecode(uri); ;
            var uriNoSlashes = HttpUtility.UrlDecode(uri).Replace("/", "");
            NodeSetProjectInstance aNodesetProjectInstance;
            if (NodeSetProjectInstances.TryGetValue(id, out aNodesetProjectInstance))
            {
                NodeSetModel aNodesetModel;
                //if (aNodesetProjectInstance.NodeSetModels.Keys.Contains(uriNoSlashes))
                if (aNodesetProjectInstance.NodeSetModels.Keys.Select(x => x.Replace("/", "")).Contains(uriNoSlashes))
                {
                    //return Ok(aNodesetProjectInstance.NodeSetModels.First(x => x.Value.ModelUri == uriNoSlashes).Value);
                    return Ok(aNodesetProjectInstance.NodeSetModels.First(x => x.Value.ModelUri.Replace("/", "") == uriNoSlashes).Value);
                }
                return NotFound("The model does not exist.");
            }
            return NotFound("The project does not exist.");
        }

        public IActionResult GetNodeApiModelByNodeId(string id, string uri, string nodeId, string nodeModelTypeName = "")
        {

            var activeNodesetModelResult = GetNodeSetModel(id, uri) as ObjectResult;

            if (Microsoft.AspNetCore.Http.StatusCodes.Status200OK != activeNodesetModelResult.StatusCode)
            {
                return activeNodesetModelResult;
            }
            else
            {
                var activeNodesetModel = activeNodesetModelResult.Value as NodeSetModel;

                var aNodeModelCandidates = activeNodesetModel.AllNodesByNodeId.Where(x => x.Key.Replace("/", "") == nodeId);

                if (aNodeModelCandidates.Count() == 0)
                {
                    return NotFound("The node could not be found.");
                }
                else
                {
                    var aNodeModel = aNodeModelCandidates.First().Value;
                    UaNodeResponse returnObject = null;
                    switch (aNodeModel)
                    {
                        case DataTypeModel aModel:
                            returnObject = new DataTypeResponse(aModel as DataTypeModel);
                            break;
                        case DataVariableModel aModel:
                            returnObject = new DataVariableResponse(aModel as DataVariableModel);
                            break;
                        case ObjectModel aModel:
                            returnObject = new ObjectModelResponse(aModel as ObjectModel);
                            break;
                        case ObjectTypeModel aModel:
                            returnObject = new ObjectTypeResponse(aModel as ObjectTypeModel);
                            break;
                        case PropertyModel aModel:
                            returnObject = new PropertyResponse(aModel as PropertyModel);
                            break;
                        case VariableTypeModel aModel:
                            returnObject = new VariableTypeResponse(aModel as VariableTypeModel);
                            break;
                        default:
                            returnObject = null;
                            break;
                    }

                    if (returnObject == null)
                    {
                        return NotFound("The node type is not implemented.");
                    }
                    else if (nodeModelTypeName == "" || (aNodeModel.GetType().Name == nodeModelTypeName))
                    {
                        return Ok(returnObject);
                    }
                    else
                    {
                        return NotFound($"The node is not a {nodeModelTypeName.Replace("Model", "")}.");
                    }
                }
            }
        }

        public IActionResult GetNodeModelByNodeId(string id, string uri, string nodeId)
        {

            var activeNodesetModelResult = GetNodeSetModel(id, uri) as ObjectResult;

            if (Microsoft.AspNetCore.Http.StatusCodes.Status200OK != activeNodesetModelResult.StatusCode)
            {
                return activeNodesetModelResult;
            }
            else
            {
                var activeNodesetModel = activeNodesetModelResult.Value as NodeSetModel;

                var aNodeModelCandidates = activeNodesetModel.AllNodesByNodeId.Where(x => x.Key.Replace("/", "") == nodeId);

                if (aNodeModelCandidates.Count() == 0)
                {
                    return NotFound("The node could not be found.");
                }
                else
                {
                    var aNodeModel = aNodeModelCandidates.First().Value;

                    return Ok(aNodeModel);

                }
            }
        }

    }
}
