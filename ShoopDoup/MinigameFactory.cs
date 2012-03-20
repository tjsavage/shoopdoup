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
        private Dictionary<MINIGAME_TYPE, List<Minigame>> minigameDictionary;
        private List<Minigame> minigames;
        private MINIGAME_TYPE[] types = (MINIGAME_TYPE[])Enum.GetValues(typeof(MINIGAME_TYPE));
        public MainWindow mainController;

        private Random randomGen = new Random();

        public MinigameFactory()
        {
            minigames = new List<Minigame>();
            minigameDictionary = new Dictionary<MINIGAME_TYPE, List<Minigame>>();
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
                JObject projectTypeResult = sc.makeRequest("projectType", types[i].ToString(), true, "");

                if (projectTypeResult == null) continue;

                Newtonsoft.Json.Linq.JToken[] jsonArray = projectTypeResult["response"].ToArray();

                for (int j = 0; j < jsonArray.Count(); j++)
                {
                    String projectId = (String)jsonArray[j]["projectId"];

                    JObject projectIdResult = sc.makeRequest("projectId", "", projectId);

                    addNewMinigame(projectIdResult, types[i], (String)jsonArray[j]["title"], (String)jsonArray[j]["description"]);
                }
                /*String projectId = (String)projectTypeResult["response"]["projectId"];

                JObject projectIdResult = sc.makeRequest("projectId", "", projectId);
                addNewMinigame(projectIdResult, types[i], (String)projectTypeResult["response"]["title"], (String)projectTypeResult["response"]["description"]);*/
            }

            Console.WriteLine(minigameDictionary.ToString());

        }

        private void addNewMinigame(JObject projectIdResult, MINIGAME_TYPE type, String title, String description)
        {
            Minigame mg = new Minigame(projectIdResult, type, title, description);

            if(!minigameDictionary.Keys.Contains(type))
            {
                minigameDictionary[type] = new List<Minigame>();
            }

            minigameDictionary[type].Add(mg);
        }

        /*public Minigame getDefaultMinigame()
        {
            Minigame defaultGame = new Minigame(null, MINIGAME_TYPE.Binary, "Catch the Object", "Catch the correct object");
            defaultGame.setController(new NetGameController(null, "", ""));
            defaultGame.getController().parentController = mainController;
            return defaultGame;
        }*/

        public Minigame getMinigameOfType(MINIGAME_TYPE type)
        {
            if (minigameDictionary[type] != null)
            {
                int numGames = minigameDictionary[type].Count;
                int randomNum = randomGen.Next(0, numGames);

                return minigameDictionary[type][randomNum];
            }

            return null;
        }

    }
}
