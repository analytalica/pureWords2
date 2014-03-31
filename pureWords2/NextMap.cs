//Import various C# things.
using System;
using System.IO;
using System.Timers;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

//Import Procon things.
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;
using PRoCon.Core.Maps;

namespace PRoConEvents
{
    public class NextMap : PRoConPluginAPI, IPRoConPluginInterface
    {

        private String[] gameModes = { };
        
        //--------------------------------------
        //Class level variables.
        //--------------------------------------

        private bool pluginEnabled = false;
        private List<MaplistEntry> lstMaplist;
        private int mapIndex;
        private int gameModeToo = 0;
        private String debugLevelString = "1";
        private int debugLevel = 2;

        //--------------------------------------
        //Plugin constructor. Can be left blank.
        //--------------------------------------

        public NextMap()
        {

        }

        //--------------------------------------
        //Description settings for your plugin.
        //--------------------------------------

        public string GetPluginName()
        {
            return "Next Map Notifcation";
        }

        public string GetPluginVersion()
        {
            return "2.0.0";
        }

        public string GetPluginAuthor()
        {
            return "F0rceTen2112";
        }

        public string GetPluginWebsite()
        {
            return "purebattlefield.org";
        }

        public string GetPluginDescription()
        {
            return @"Turn it on and then off. Before shutting down it will print the next map name in the console." +
                   "<br><br>Will print game mode if \"Game Mode Too?\" equals 1." + 
                   "<br>WARNING: As of now, game modes will print weirdly (i.e. Conquest Large = ConquestLarge0)." +
                   "<br>I will research UMM further and try and figure out how to do this, but for now, you should probably set this to 0.";
        }

        //--------------------------------------
        //Helper Functions
        //--------------------------------------

        public void toChat(String message)
        {
            if (!message.Contains("\n"))
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", message, "all");
            }
            else
            {
                string[] multiMsg = message.Split(new string[] { "\n" }, StringSplitOptions.None);
                foreach (string send in multiMsg)
                {
                    toChat(send);
                }
            }
        }

        public void toChat(String message, String playerName)
        {
            if (!message.Contains("\n"))
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", message, "player", playerName);
            }
            else
            {
                string[] multiMsg = message.Split(new string[] { "\n" }, StringSplitOptions.None);
                foreach (string send in multiMsg)
                {
                    toChat(send, playerName);
                }
            }
        }

        public void toConsole(int msgLevel, String message)
        {
            if (debugLevel >= msgLevel)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", message);
            }
        }

        public String realName(String MapFileName)
        {
            foreach (CMap map in this.GetMapDefines())
            {
                if (map.FileName == MapFileName)
                {
                    return map.PublicLevelName;
                }
            }
            this.toConsole(1, "Something went wrong converting file name (" + MapFileName + ") to map name.");
            return "";
        }

        public String realMode(MaplistEntry MEMap)
        {
            foreach (CMap map in this.GetMapDefines())
            {
                if (MEMap.Gamemode == map.PlayList && MEMap.MapFileName == map.FileName)
                {
                    return map.GameMode;
                }
            }
            this.toConsole(1, "Something went wrong converting the GameMode name.");
            return "";
        }

        //--------------------------------------
        //These methods run when Procon does what's on the label.
        //--------------------------------------

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name, "OnPluginLoaded", "OnMaplistList", "OnMaplistGetMapIndices");
        }

        public void OnMaplistList(List<MaplistEntry> lstMaplist)
        {
            if (pluginEnabled)
            {
                this.toConsole(2, "OnMaplistList called.");
                this.lstMaplist = lstMaplist;
            }
        }

        public void OnMaplistGetMapIndices(int mapIndex, int nextIndex)
        {
            if (pluginEnabled)
            {
                this.toConsole(2, "OnMaplistGetMapIndices called.");
                this.mapIndex = nextIndex;
            }
        }

        public void OnPluginEnable()
        {
            this.pluginEnabled = true;
            this.toConsole(1, "Next Map Notification Enabled");
            this.ExecuteCommand("procon.protected.send", "mapList.list", "0");
            this.ExecuteCommand("procon.protected.send", "mapList.getMapIndices");
            if (pluginEnabled)
            {
                bool done = false;
                do
                {
                    String message = "";
                    try
                    {
                        if (gameModeToo == 1)
                            message = realName(lstMaplist[mapIndex].MapFileName) + " on " + realMode(lstMaplist[mapIndex]);
                        else
                            message = realName(lstMaplist[mapIndex].MapFileName);
                        this.toConsole(1, message);
                        done = true;
                    }
                    catch (Exception e) { }
                } while (!done);
            }
        }

        public void OnPluginDisable()
        {
            //String message = "";
            //try {
            //    if (gameModeToo == 1)
            //        message = realName(lstMaplist[mapIndex].MapFileName) + " on " + realMode(lstMaplist[mapIndex]);
            //    else
            //        message = realName(lstMaplist[mapIndex].MapFileName);
            //}
            //catch (Exception e) { this.toConsole(1, e.ToString()); }
            //this.toConsole(1, message);
            this.pluginEnabled = false;
            this.toConsole(1, "Next Map Notification Disabled");
        }

        //List plugin variables.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Settings|Game mode too?", typeof(int), gameModeToo));
            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            return GetDisplayPluginVariables();
        }

        //Set variables.
        public void SetPluginVariable(String strVariable, String strValue)
        {
            if (Regex.Match(strVariable, @"Game mode too?").Success)
            {
                try {
                    this.gameModeToo = Convert.ToInt32(strValue);
                    if (gameModeToo != 0 && gameModeToo != 1)
                    {
                        this.toConsole(1, "Must be 0 or 1.");
                        this.gameModeToo = 0;
                    }
                }
                catch (Exception e)
                {
                    this.toConsole(1, "Needs to be an integer.");
                    this.gameModeToo = 0;
                }
            }
        }
    }
}