using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using ShoopDoup.Models;
using ShoopDoup.ViewControllers;

namespace ShoopDoup
{
    class MinigameFactory
    {
        private ServerConnector sc;
        private List<Minigame> minigames;
        private MINIGAME_TYPE[] types = (MINIGAME_TYPE[])Enum.GetValues(typeof(MINIGAME_TYPE));
        public MainWindow mainController;

        public MinigameFactory()
        {
            minigames = new List<Minigame>();
            sc = new ServerConnector();
            preloadGames();
        }

        private int getCount()
        {
            return minigames.Count;
        }

        private Minigame getGame(int index)
        {
            return minigames[index];
        }

        private void preloadGames()
        {
            for (int i = 0; i < types.Length; i++)
            {
                JObject projectTypeResult = sc.makeRequest("projectType", types[i].ToString(), "");

                if (projectTypeResult == null) continue;
              
                String projectId = (String)projectTypeResult["response"]["projectId"];

                JObject projectIdResult = sc.makeRequest("projectId", "", projectId);
                addNewMinigame(projectIdResult, types[i], (String)projectTypeResult["response"]["title"], (String)projectTypeResult["response"]["description"]);
            }

        }

        private void addNewMinigame(JObject projectIdResult, MINIGAME_TYPE type, String title, String description)
        {
            Minigame mg = new Minigame(projectIdResult, type, title, description);
            mg.getController().parentController = mainController;
            minigames.Add(mg);
        }

        public Minigame getDefaultMinigame()
        {
            Minigame defaultGame = new Minigame(null, MINIGAME_TYPE.Binary, "Catch the Object", "Catch the correct object");
            defaultGame.setController(new NetGameController(null, "", ""));
            defaultGame.getController().parentController = mainController;
            return defaultGame;
        }

        public Minigame getMinigameOfType(MINIGAME_TYPE type)
        {
            for(int i = 0; i < minigames.Count; i++)
            {
                if(minigames[i].getType() == type)
                {
                    return minigames[i];
                }
            }

            return null;
        }

    }
}
