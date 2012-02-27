using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ShoopDoup.Models
{
    class DataObject
    {
        private int elementId;
        private int projectId;
        private String elementValue;
        private String dataType;
        private String url;

        public DataObject(JObject dataObject)
        {
            elementId = Convert.ToInt32((String)dataObject["elementID"]);
            projectId = Convert.ToInt32((String)dataObject["projectID"]);
            elementValue = (String)dataObject["elementValue"];
            dataType = (String)dataObject["dataType"];
            url = (String)dataObject["url"];
            Console.WriteLine("Adding Data Object: " + elementId + " " + projectId + " " + elementValue + " " + dataType + " " + url);
        }

        public int getElementId()
        {
            return elementId;
        }

        public int getProjectId()
        {
            return projectId;
        }

        public String getElementValue()
        {
            return elementValue;
        }

        public String getDataType()
        {
            return dataType;
        }

        private String getUrl()
        {
            return url;
        }

    }
}
