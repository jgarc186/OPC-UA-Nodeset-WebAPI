namespace OPC_UA_Nodeset_WebAPI.Services;

using Microsoft.AspNetCore.Mvc;
using CESMII.OpcUa.NodeSetModel;
using OPC_UA_Nodeset_WebAPI.Model.v1.Requests;
using OPC_UA_Nodeset_WebAPI.Model.v1.Responses;
using OPC_UA_Nodeset_WebAPI.UA_Nodeset_Utilities;

public class ObjectModelService
{
    private ApplicationInstance ApplicationInstance { get; set; }

    public ObjectModelService(ApplicationInstance applicationInstance)
    {
        ApplicationInstance = applicationInstance;
    }

    private NodeSetProjectInstance _GetActiveProjectInstance(string id)
    {
        var projectInstanceResult = ApplicationInstance.GetNodeSetProjectInstance(id) as ObjectResult;
        if (projectInstanceResult.StatusCode != StatusCodes.Status200OK)
        {
            throw new KeyNotFoundException($"Project with ID {id} not found.");
        }
        return projectInstanceResult.Value as NodeSetProjectInstance;
    }

    private NodeSetModel _GetActiveNodeSetModel(string id, string uri, NodeSetProjectInstance activeProjectInstance)
    {
        var activeNodesetModelResult = ApplicationInstance.GetNodeSetModel(id, uri) as ObjectResult;
        return activeNodesetModelResult.Value as NodeSetModel;
    }

    private NodeModel _GetParentModel(NodeSetProjectInstance activeProjectInstance, UaObject type)
    {
        var parentModel = activeProjectInstance.NodeSetModels.FirstOrDefault(x => x.Value.ModelUri == UaNodeResponse.GetNameSpaceFromNodeId(type.ParentNodeId)).Value;
        return parentModel.AllNodesByNodeId[type.ParentNodeId] as NodeModel;
    }

    private ObjectTypeModel _GetObjectTypeDefinition(NodeSetProjectInstance activeProjectInstance, NodeSetModel activeNodesetModel, UaObject type)
    {
        var activeNamespace = UaNodeResponse.GetNameSpaceFromNodeId(type.TypeDefinitionNodeId);
        var objectTypeModel = activeProjectInstance.NodeSetModels.FirstOrDefault(x => x.Value.ModelUri == activeNamespace).Value;
        if (String.IsNullOrEmpty(activeNamespace) || objectTypeModel == null)
        {
            // this can potentially happen if the type definition is in the active nodeset model but not in the project instance's model
            // we  have to account for the active nodeset model that can have this type definition 
            if (activeNodesetModel.ModelUri == activeNamespace)
            {
                objectTypeModel = activeNodesetModel;
            }
            else
            {
                throw new KeyNotFoundException($"Object type definition for {type.TypeDefinitionNodeId} not found in namespace {activeNamespace}.");
            }
        }
        return objectTypeModel.ObjectTypes.FirstOrDefault(ot => ot.NodeId == type.TypeDefinitionNodeId) as ObjectTypeModel;
    }

    public ObjectModel CreateObjectModel(string id, string uri, UaObject type)
    {
        var activeProjectInstance = _GetActiveProjectInstance(id);
        var activeNodesetModel = _GetActiveNodeSetModel(id, uri, activeProjectInstance);
        var aObjectTypeDefinition = _GetObjectTypeDefinition(activeProjectInstance, activeNodesetModel, type);

        var newObjectModel = new ObjectModel
        {
            NodeSet = activeNodesetModel,
            NodeId = UaNodeResponse.GetNodeIdFromIdAndNameSpace((activeProjectInstance.NextNodeIds[activeNodesetModel.ModelUri]++).ToString(), activeNodesetModel.ModelUri),
            Parent = _GetParentModel(activeProjectInstance, type),
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

        return newObjectModel;
    }
}
