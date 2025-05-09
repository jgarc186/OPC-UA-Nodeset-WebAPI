using CESMII.OpcUa.NodeSetModel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Opc.Ua;
using OPC_UA_Nodeset_WebAPI.UA_Nodeset_Utilities;
using System.Xml.Linq;

namespace OPC_UA_Nodeset_WebAPI.Model.v1.Requests
{
    public class BulkPropertyRequest
    {
        public string ProjectId { get; set; }

        public string Uri { get; set; }

        public string ParentNodeId { get; set; }

        public List<Property> Types { get; set; }

        public BulkPropertyRequest() { }
    }

    public class Property
    {
        public string DisplayName { get; set; }

        public string DataTypeNodeId { get; set; }

        public string? Value { get; set; }

        public string? BrowseName { get; set; }

        public string? Description { get; set; }
    }
}
