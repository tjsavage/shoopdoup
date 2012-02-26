using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using ShoopDoup.Models;

namespace ShoopDoup
{
    class MinigameFactory
    {
        private ServerConnector sc;
        private List<Minigame> minigames;
        private MINIGAME_TYPE[] types = (MINIGAME_TYPE[])Enum.GetValues(typeof(MINIGAME_TYPE));

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
                String projectId = (String)projectTypeResult["response"]["projectId"];
                Console.WriteLine("Requesting Project: " + projectId);
                JObject projectIdResult = sc.makeRequest("projectId", "", projectId);
                addNewMinigame(projectIdResult, types[i], (String)projectTypeResult["response"]["title"], (String)projectTypeResult["response"]["description"]);
            }

        }

        private void addNewMinigame(JObject projectIdResult, MINIGAME_TYPE type, String title, String description)
        {
            Minigame mg = new Minigame(projectIdResult, type, title, description);
            minigames.Add(mg);
        }

    }
}
