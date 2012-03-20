using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ShoopDoup
{
    class ServerConnector
    {
        private String url;

        public ServerConnector()
        {
            url = "http://www.tomhschmidt.com/cs247/api.php";
        }

        public JObject makeRequest(String requestType, String projectType, String projectId)
        {
            return makeRequest(requestType, projectType, false, projectId);
        }


        public JObject makeRequest(String requestType, String projectType, bool fetchAll, String projectId)
        {
            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];

            HttpWebRequest request;

            if (requestType == "projectType")
            {
                if (fetchAll)
                {
                    request = (HttpWebRequest)WebRequest.Create(url + "?projectType=" + projectType + "&fetchAll=true");
                }
                else
                {
                    request = (HttpWebRequest)WebRequest.Create(url + "?projectType=" + projectType);
                }
            }
            else if (requestType == "projectId")
            {
                request = (HttpWebRequest)WebRequest.Create(url + "?projectId=" + projectId);
            }
            else
            {
                return null;
            }

            HttpWebResponse response = (HttpWebResponse)
                request.GetResponse();

            Stream resStream = response.GetResponseStream();

            string tempString = null;
            int count = 0;

            do
            {
                count = resStream.Read(buf, 0, buf.Length);

                if (count != 0)
                {
                    tempString = Encoding.ASCII.GetString(buf, 0, count);
                    sb.Append(tempString);
                }
            }
            while (count > 0);

            Console.WriteLine(sb.ToString());

            //return null;
            if (sb.ToString().Contains("false"))
            {
                return null;
            }
            else
            {
                return JObject.Parse(sb.ToString());
            }


        }

    }
}
