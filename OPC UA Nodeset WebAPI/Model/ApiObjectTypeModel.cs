﻿using CESMII.OpcUa.NodeSetModel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Opc.Ua;
using OPC_UA_Nodeset_WebAPI.UA_Nodeset_Utilities;
using System.Xml.Linq;

namespace OPC_UA_Nodeset_WebAPI.Model
{
    public class ApiObjectTypeModel : ApiUaNodeModel
    {
        public int PropertiesCount { get; set; }
        public int DataVariablesCount { get; set; }

        public string? SuperTypeNodeId { get; set; }
        internal ObjectTypeModel? ObjectTypeModel { get; set; }
        
        public ApiObjectTypeModel() { }

        public ApiObjectTypeModel(ObjectTypeModel aOjectTypeModel)
        {
            ObjectTypeModel = aOjectTypeModel;
            NodeId = aOjectTypeModel.NodeId;
            DisplayName = aOjectTypeModel.DisplayName.First().Text;
            Description = aOjectTypeModel.Description.First().Text;
            PropertiesCount = aOjectTypeModel.Properties.Count;
            DataVariablesCount = aOjectTypeModel.DataVariables.Count;
            SuperTypeNodeId = aOjectTypeModel.SuperType == null ? "" : aOjectTypeModel.SuperType.NodeId;
        }

    }
}