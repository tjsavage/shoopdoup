using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ShoopDoup.Models
{
    class Minigame
    {
        private List<DataObject> list;

        public Minigame(JObject dataModel)
        {
            list = new List<DataObject>();
            parseDataModel(dataModel);
        }

        private void parseDataModel(JObject dataModel)
        {
            for (int i = 0; i < (((JArray)dataModel["response"]).Count); i++)
            {
                DataObject ndo = new DataObject((JObject)((JArray)dataModel["response"])[i]);
                list.Add(ndo);
            }
        }

    }
}
