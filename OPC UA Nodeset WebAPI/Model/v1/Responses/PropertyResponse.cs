﻿using CESMII.OpcUa.NodeSetModel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using OPC_UA_Nodeset_WebAPI.UA_Nodeset_Utilities;
using System.Xml.Linq;

namespace OPC_UA_Nodeset_WebAPI.Model.v1.Responses
{
    public class PropertyResponse : UaNodeResponse
    {
        public string DataTypeNodeId { get; set; }
        public string TypeDefinitionNodeId { get; set; }
        public string? Value { get; set; }
        public List<NodeAndReferenceResponse> AllReferencedNodes { get; set; }
        public List<NodeAndReferenceResponse> OtherReferencedNodes { get; set; }
        public List<NodeAndReferenceResponse> OtherReferencingNodes { get; set; }


        internal NodeModel ParentModel { get; set; }
        internal PropertyModel PropertyModel { get; set; }
        public PropertyResponse() { }


        public PropertyResponse(VariableModel aVariableModel)
        {
            PropertyModel = aVariableModel as PropertyModel;
            NodeId = aVariableModel.NodeId;
            DisplayName = aVariableModel.DisplayName.First().Text;
            BrowseName = aVariableModel.BrowseName;
            Description = aVariableModel.Description.Count == 0 ? "" : aVariableModel.Description.First().Text;
            ParentModel = aVariableModel.Parent;
            ParentNodeId = aVariableModel.Parent?.NodeId ?? "";
            DataTypeNodeId = aVariableModel.DataType == null ? "" : aVariableModel.DataType.NodeId;
            TypeDefinitionNodeId = aVariableModel.TypeDefinition == null ? "" : aVariableModel.TypeDefinition.NodeId;

            AllReferencedNodes = new List<NodeAndReferenceResponse>();
            if (aVariableModel.AllReferencedNodes.Count() > 0)
            {
                foreach (var aReference in aVariableModel.AllReferencedNodes)
                {
                    AllReferencedNodes.Add(new NodeAndReferenceResponse(aReference));
                }
            }

            OtherReferencedNodes = new List<NodeAndReferenceResponse>();
            foreach (var aReference in aVariableModel.OtherReferencedNodes)
            {
                OtherReferencedNodes.Add(new NodeAndReferenceResponse(aReference));
            }

            OtherReferencingNodes = new List<NodeAndReferenceResponse>();
            foreach (var aReference in aVariableModel.OtherReferencingNodes)
            {
                OtherReferencingNodes.Add(new NodeAndReferenceResponse(aReference));
            }


            if (aVariableModel.Value != null)
            {
                var aPropertyModelValue = JsonConvert.DeserializeObject<JObject>(aVariableModel.Value);
                var valueTypeId = aPropertyModelValue["Type"].Value<int>();

                //switch (aPropertyModelValue["Value"]["Body"].Type.ToString())
                //{
                //    case "Array":
                //    case "Object":
                Value = aPropertyModelValue["Body"].ToString();
                //        break;
                //    default:
                //        Value = aPropertyModelValue["Value"]["Body"].Value<string>();
                //        break;
                //}

            }
            else
            {
                Value = null;
            }
        }

    }
}
