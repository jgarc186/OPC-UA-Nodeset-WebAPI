using CESMII.OpcUa.NodeSetModel;
using Microsoft.AspNetCore.Mvc;
using OPC_UA_Nodeset_WebAPI.Model.v1.Requests;
using OPC_UA_Nodeset_WebAPI.Model.v1.Responses;
using OPC_UA_Nodeset_WebAPI.UA_Nodeset_Utilities;

namespace OPC_UA_Nodeset_WebAPI.Services
{
    public class ObjectModelService
    {
        private ApplicationInstance ApplicationInstance { get; set; }

        public ObjectModelService(ApplicationInstance applicationInstance)
        {
            ApplicationInstance = applicationInstance;
        }

        public ObjectModel CreateObjectModel(string id, string uri, UaObject type)
        {
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

            return newObjectModel;
        }
    }
}
