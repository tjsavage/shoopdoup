using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using ShoopDoup.ViewControllers;

namespace ShoopDoup.Models
{
    public enum MINIGAME_TYPE {Ratings, Binary, Association, Info};

    class Minigame
    {
        private List<ShoopDoup.Models.DataObject> list;
        private String title;
        private String description;
        private MINIGAME_TYPE type;
        private SceneController controller;

        public Minigame(JObject dataModel,MINIGAME_TYPE minigameType, String minigameTitle, String minigameDescription)
        {
            list = new List<ShoopDoup.Models.DataObject>();
            if (dataModel != null)
            {
                parseDataModel(dataModel);
            }
            title = minigameTitle;
            description = minigameDescription;
            type = minigameType;
        }

        public void start()
        {
            this.getController().start();
        }

        public SceneController getController()
        {
            if (this.controller == null)
            {
                switch (this.type)
                {
                    case MINIGAME_TYPE.Binary:
                        this.controller = new NetGameController(list, title, description);
                        break;
                    default:
                        this.controller = new NetGameController(list, title, description);
                        break;
                }
            }
            return this.controller;
        }

        public void setController(SceneController controller)
        {
            this.controller = controller;
        }

        private void parseDataModel(JObject dataModel)
        {
            for (int i = 0; i < (((JArray)dataModel["response"]).Count); i++)
            {
                DataObject ndo = new DataObject((JObject)((JArray)dataModel["response"])[i]);
                list.Add(ndo);
            }
            Console.WriteLine("what");
        }

        public void setTitle(String t)
        {
            title = t;
        }

        public void setDescription(String d)
        {
            description = d;
        }

        public void setType(MINIGAME_TYPE t)
        {
            type = t;
        }

        public String getTitle()
        {
            return title;
        }

        public String getDescription()
        {
            return description;
        }

        public MINIGAME_TYPE getType()
        {
            return type;
        }

        public List<DataObject> getData()
        {
            return list;
        }

    }
}
