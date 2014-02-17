//Import various C# things.
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
//Import Procon things.
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;

namespace PRoConEvents
{
    public class pureWords2 : PRoConPluginAPI, IPRoConPluginInterface
    {

        //--------------------------------------
        //Class level variables.
        //--------------------------------------

        private bool pluginEnabled = false;
        private string keywordListString = "";
        private string[] keywordArray;
        private int keywordArraySize = 0;

        private string kickMessage = "";
        private string chatMessage = "";

        private List<command> commandList = new List<command>();

        private class command
        {
            //private string cmd = "";
            private string i_commandWord = "";
            public string commandWord
            {
                get { return i_commandWord; }
                set { i_commandWord = value.Trim().ToLower(); }
            }

            private string i_response = "";
            public string response
            {
                get { return i_response; }
                set { i_response = value.Replace("\r", "").Trim(); }
            }

            //Default Prefixes
            private List<char> i_prefixes = new List<char>(new char[] { '/', '!', '@', '#' });
            public string prefixes
            {
                get
                {
                    String prefixesListString = "";
                    foreach (char c in i_prefixes)
                    {
                        prefixesListString += c.ToString();
                    }
                    return prefixesListString;
                }
                set
                {
                    i_prefixes.Clear();
                    if (!String.IsNullOrEmpty(value))
                    {
                        i_prefixes = new List<char>(value.ToCharArray());
                    }
                    else //Default Prefixes
                    {
                        pw2.toConsole(2, "Resetting prefixes...");
                        i_prefixes = new List<char>(new char[] { '/', '!', '@', '#' });
                    }
                }
            }
            public int broadcastLevel
            {
                get;
                set;
            }
            private pureWords2 pw2 = null;
            public command()
            {
                commandWord = "";
                response = "";
                pw2 = null;
                prefixes = "";
                broadcastLevel = 1;
            }
            public command(pureWords2 instance, String cmdString, String prefixesString, String responseString, int broadcast)
            {
                pw2 = instance;
                commandWord = cmdString;
                response = responseString;
                broadcastLevel = broadcast;
                prefixes = prefixesString;

            }
            public Boolean checkChatAndRespond(String playerName, String chatMsg)
            {
                String message = chatMsg;
                pw2.toConsole(3, "First character is " + message[0].ToString());
                if (message.Trim().Length > 1 && !String.IsNullOrEmpty(this.response) && !String.IsNullOrEmpty(this.commandWord) && this.i_prefixes.Count > 0)
                {
                    String cmdBody = message.Substring(1).ToLower();
                    foreach (char k in i_prefixes)
                    {
                        if (message[0] == k && cmdBody == this.commandWord && this.broadcastLevel != 0)
                        {
                            switch (this.broadcastLevel)
                            {
                                case 1:
                                    pw2.toChat(this.response.Replace("[player]", playerName), playerName);
                                    break;
                                default:
                                    pw2.toChat(this.response.Replace("[player]", playerName));
                                    break;
                            }
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        private string debugLevelString = "1";
        private int debugLevel = 1;

        private string logName = "";
        //private StreamWriter log = File.AppendText("pureWordsLog.txt");

        public pureWords2()
        {
        }

        public void processChat(string speaker, string message)
        {
            if (pluginEnabled && speaker != "Server")
            {
                toConsole(2, speaker + " just said: \"" + message + "\"");
                if (containsBadWords(message))
                {
                    toConsole(1, "Kicking " + speaker + " with message \"" + kickMessage + "\"");
                    if (!String.IsNullOrEmpty(chatMessage))
                    {
                        String chatThis = chatMessage.Replace("[player]", speaker);
                        toConsole(2, "Sent to chat: \"" + chatThis + "\"");
                        toChat(chatThis);
                    }
                    kickPlayer(speaker);
                    toLog("[ACTION] " + speaker + " was kicked for saying \"" + message + "\"");
                }
                else
                {
                    toConsole(2, "No bad words...");
                    Boolean wasCommand = false;
                    foreach (command aCmd in commandList)
                    {
                        if (aCmd.checkChatAndRespond(speaker, message))
                        {
                            wasCommand = true;
                            break;
                        }
                    }
                    if (!wasCommand)
                    {
                        toConsole(2, "Was not a command...");
                    }
                }
            }
        }


        //--------------------------------------
        //Description settings
        //--------------------------------------

        public string GetPluginName()
        {
            return "pureWords2";
        }

        public string GetPluginVersion()
        {
            return "0.7.8";
        }

        public string GetPluginAuthor()
        {
            return "Analytalica";
        }

        public string GetPluginWebsite()
        {
            return "purebattlefield.org";
        }
        #region Description
        public string GetPluginDescription()
        {
            return @"<p><b>pureWords2</b> is a superior command, trigger, and filter plugin that can monitor and respond to in-game chat messages.<br>
<ul>
  <li>Command Words: Set commands like '!help' or '@rules' that players can query through chat and get an automatic response to.</li>
  <ul>
    <li>Unlimited amount of prefixes, default '/!@#'</li>
    <li>Configurable response broadcast level (to original speaker or all players)</li>
    <li>Must be an exact match (prefix + command word only)</li>
  </ul>
  <li>Trigger Words: Set trigger words like 'hacks' that players will get an automatic response to.</li>
  <ul>
    <li>No prefixes - trigger words are discovered by a regex search</li>
    <li>Configurable response broadcast level (to original speaker or all players)</li>
    <li>Words are matched anywhere in a player chat message</li>
  </ul>
  <li>Bad Words: Set a list of bad words that players are kicked for saying.</li>
  <ul>
    <li>Configurable kick message received by player</li>
  </ul>
  <ul>
    <li>Configurable server-wide chat response</li>
    <li>Words are matched just like triggers</li>
  </ul>
</ul>
It is a massive overhaul and substantial upgrade to the original
pureWords and is not a direct upgrade, hence the new name. Timestamped
kick actions by
pureWords can be logged into a local text file.</p>
<p>This plugin was developed by Analytalica originally for PURE
Battlefield.</p>
<p><big><b>Bad Word List Setup (identical to
pureWords):</b></big><br>
</p>
<ol>
  <li>Set the bad word list by separating individual keywords by
commas. For example, if I wanted to filter out 'bathtub',
'porch', and 'bottle', I would enter:<br>
    <i>bathtub,porch,bottle</i></li>
  <li>Set a kick message. This is seen by the kicked player in
Battlelog.</li>
  <li>Set an in-game warning message. This is seen by all other
players in the server. To mention the player's name, type [player] and
it will be replaced. For example, if a player named 'Draeger' was
kicked by pureWords, setting the message to<br>
    <i>[player] was kicked by pureWords</i><br>
would show up as<br>
    <i>Draeger was kicked by pureWords</i><br>
in the in-game chat. This feature can be disabled by leaving the field
blank.</li>
  <li>To enable logging, configure a file name (preferrably one
that ends in .txt) and relative path. For example,<br>
    <i>Logs/pureWords.txt<br>
    </i>will
write to a file 'pureWords.txt' in the 'Logs' folder. The log timestamp
format is <b>MM/dd/yyyy HH:mm:ss</b>.<br>
If you wish to insert the daily timestamp into the log's filename, add
[date] in the path.</li>
</ol>
<p>pureWords is case insensitive and matches whole words only
(ignoring
any punctuation),
e.g. a player will not be kicked for 'ass' if he says 'assassin'.
In the bad word list, leading and trailing spaces (as well as line
breaks) are automatically removed,
so it is fine to use <i>bathtub , porch ,bottle </i> in
place of <i>bathtub,porch,bottle</i>.</p>
<p><big><b>Command Words Setup:</b></big></p>
<ol>
  <li>Insert the command word (and only the word) into a trigger
field.</li>
  <li>Give a response to the corresponding trigger. This message
is sent to the player who sent the command.</li>
</ol>
<p>pureWords will match chat messages that start with '!' or '/'
and immediately after the
trigger words and reply with the appropriate response. Do not use '!'
or '/' inside the trigger field, use only the words themselves. If a
trigger is set to 'help', pureWords will respond if a player asks
'!help' or '/help'. However, if the trigger is set to '!help',
pureWords will only respond if a player asks '!!help' or '/!help'.</p>
<p>Note that Battlefield has a hard restriction on the number of
characters (roughly 128) that you can send per chat. It is fine to make
a response one line, but it will be cut off
or may not display at all past the character limit. Instead, manually
split the response into multiple lines (click the dropdown button in
PRoCon next to the entry field) and they will be sent properly as
multiple chat responses
as opposed to one long continuous response.</p>


";
        }
        #endregion
        //--------------------------------------
        //Helper Functions
        //--------------------------------------
        #region Helper Functions
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
            //a message with msgLevel 1 is more important than 2
            if (debugLevel >= msgLevel)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "pureWords2: " + message);
            }
        }

        public void toLog(String logText)
        {
            if (!String.IsNullOrEmpty(logName))
            {
                String logNameTimestamped = logName.Replace("[date]", DateTime.Now.ToString("MMddyyyy"));
                bool logSuccess = true;
                try
                {
                    using (StreamWriter writeFile = new StreamWriter(logNameTimestamped, true))
                    {
                        writeFile.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + " " + logText);
                    }
                }
                catch (Exception e)
                {
                    this.toConsole(1, "WARNING: File write error! Try resetting the log path.");
                    this.toConsole(1, e.ToString());
                    logSuccess = false;
                }
                finally
                {
                    if (logSuccess)
                        this.toConsole(2, "An event has been logged to " + logNameTimestamped + ".");
                }
            }
        }

        //--------------------------------------
        //Handy pureWords2 Methods
        //--------------------------------------

        public Boolean containsBadWords(String chatMessage)
        {
            if (!(keywordArray == null || keywordArray.Length < 1))
            {
                int cbwCount = 1;
                foreach (string kw in keywordArray)
                {
                    toConsole(2, "Testing keyword (" + cbwCount + "/" + keywordArraySize + "): " + kw);
                    cbwCount++;
                    if (Regex.IsMatch(chatMessage, "\\b" + kw + "\\b", RegexOptions.IgnoreCase))
                    {
                        toConsole(2, "Match for " + kw + " found.");
                        return true;
                    }
                }
            }
            return false;
        }

        public String removeCReturn(String original)
        {
            return original.Replace("\r", "");
        }

        public void kickPlayer(String playerName)
        {
            this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", playerName, this.kickMessage);
        }
        #endregion
        #region Events
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name, "OnPluginLoaded", "OnGlobalChat", "OnTeamChat", "OnSquadChat");
        }

        public override void OnGlobalChat(string speaker, string message)
        {
            processChat(speaker, message.ToLower());
        }

        public override void OnTeamChat(string speaker, string message, int teamId)
        {
            processChat(speaker, message.ToLower());
        }

        public override void OnSquadChat(string speaker, string message, int teamId, int squadId)
        {
            processChat(speaker, message.ToLower());
        }
        #endregion
        public void OnPluginEnable()
        {
            this.pluginEnabled = true;
            this.toConsole(1, "pureWords2 Enabled!");
            string stringKeywordList = "";
            foreach (string keyword in keywordArray)
            {
                stringKeywordList += (keyword + ", ");
            }
            keywordArraySize = keywordArray.Length;
            this.toConsole(2, "Keyword List (" + keywordArraySize + " words): " + stringKeywordList);
            toLog("[STATUS] pureWords2 Enabled");
        }

        public void OnPluginDisable()
        {
            this.pluginEnabled = false;
            toLog("[STATUS] pureWords2 Disabled");
            this.toConsole(1, "pureWords2 Disabled!");
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            try
            {
                lstReturn.Add(new CPluginVariable("Main Settings|Log Path", typeof(string), logName));
                lstReturn.Add(new CPluginVariable("Main Settings|Debug Level", typeof(string), debugLevelString));
                lstReturn.Add(new CPluginVariable("Filter Settings|Bad Word List", typeof(string), keywordListString));
                lstReturn.Add(new CPluginVariable("Filter Settings|Kick Message", typeof(string), kickMessage));
                lstReturn.Add(new CPluginVariable("Filter Settings|Admin Chat Message", typeof(string), chatMessage));
                lstReturn.Add(new CPluginVariable("Command Settings|Create a New Command Word", typeof(string), ""));
				
				if(commandList.Count > 0){
					lstReturn.Add(new CPluginVariable("Command Settings|Copy Existing Command", typeof(string), ""));
				}

                for (int i = 0; i < commandList.Count; i++)
                {
                    command thisCommand = commandList[i];
                    String commandWordAdd = thisCommand.commandWord;
                    String prefixesAdd = thisCommand.prefixes;
                    String responseAdd = thisCommand.response;
                    String broadcastAdd = thisCommand.broadcastLevel.ToString();
                    //Default prefixes
                    if (String.IsNullOrEmpty(commandWordAdd) && String.IsNullOrEmpty(responseAdd) && (String.IsNullOrEmpty(prefixesAdd) || prefixesAdd == "/!@#"))
                    {
                        commandList.Remove(thisCommand);
                        i--;
                    }
                    else
                    {
                        lstReturn.Add(new CPluginVariable("Command Settings|~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~", typeof(string), "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"));
                        lstReturn.Add(new CPluginVariable("Command Settings|" + i.ToString() + ". Command Word", typeof(string), commandWordAdd));
                        lstReturn.Add(new CPluginVariable("Command Settings|" + i.ToString() + ". Command Prefixes", typeof(string), prefixesAdd));
                        lstReturn.Add(new CPluginVariable("Command Settings|" + i.ToString() + ". Command Response", typeof(string), responseAdd));
                        lstReturn.Add(new CPluginVariable("Command Settings|" + i.ToString() + ". Command Broadcast Level", typeof(string), broadcastAdd));
                    }
                }

            }
            catch (Exception e)
            {
                toConsole(1, e.ToString());
            }

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            return GetDisplayPluginVariables();
        }

        public int getConfigIndex(string configString)
        {
            int lineLocation = configString.IndexOf('|');
            return Int32.Parse(configString.Substring(lineLocation + 1, configString.IndexOf('.') - lineLocation - 1));
        }

        public void SetPluginVariable(String strVariable, String strValue)
        {
            toConsole(3, "Setting '" + strVariable + "' with value '" + strValue + "'");
            if (strVariable.Contains("Bad Word List"))
            {
                //keywordListString = strValue;
                if (String.IsNullOrEmpty(strValue))
                {
                    keywordListString = "";
                    keywordArray = null;
                }
                else
                {
                    keywordListString = strValue.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
                    keywordArray = keywordListString.Split(',');
                    keywordArraySize = keywordArray.Length;
                    for (int i = 0; i < keywordArraySize; i++)
                    {
                        keywordArray[i] = keywordArray[i].Trim();
                    }
                    string stringKeywordList = "";
                    foreach (string keyword in keywordArray)
                    {
                        stringKeywordList += (keyword + ", ");
                    }
                    this.toConsole(1, "Keyword List Updated (" + keywordArraySize + " words): " + stringKeywordList);
                }
            }
            else if (strVariable.Contains("Kick Message"))
            {
                kickMessage = removeCReturn(strValue);
            }
            else if (strVariable.Contains("Admin Chat Message"))
            {
                chatMessage = removeCReturn(strValue);
            }
            else if (strVariable.Contains("Log Path"))
            {
                logName = strValue.Trim();
                if(pluginEnabled)
                    toLog("[STATUS] Log path set to " + logName);
                //StreamWriter log = File.AppendText(logName);
            }
            else if (strVariable.Contains("New Command Word"))
            {
                commandList.Add(new command(this, strValue, "", "", 1));
                //StreamWriter log = File.AppendText(logName);
            }
			else if (strVariable.Contains("Copy Existing Command"))
			{
				int cmdToCopy = 0;
				try
                {
                    cmdToCopy = Int32.Parse(strValue);
					if(cmdToCopy + 1 > commandList.Count)
					{
						toConsole(1, "ERROR: That command doesn't exist!");
					}else{
                        String newCmd = "" + commandList[cmdToCopy].commandWord;
                        String newPrefixes = "" + commandList[cmdToCopy].prefixes;
                        String newResponse = "" + commandList[cmdToCopy].response;
						commandList.Insert(cmdToCopy + 1, new command(this, newCmd, newPrefixes, newResponse, 1));
                    }

                }
                catch (Exception z)
                {
                    toConsole(1, "ERROR: Invalid command ID! Use integer values only.");
                    toConsole(2, z.ToString());
                }
			}
            else if (strVariable.Contains("Command Word"))
            {
                int n = getConfigIndex(strVariable);
                try
                {
                    if (commandList[n].GetType() != typeof(command) || commandList[n] == null)
                        commandList[n] = new command(this, strValue, "", "", 1);
                    commandList[n].commandWord = strValue;
                }
                catch (ArgumentOutOfRangeException e)
                {
                    toConsole(3, e.ToString());
                    commandList.Add(new command(this, strValue, "", "", 1));
                }
            }
            else if (strVariable.Contains("Command Prefixes"))
            {
                int n = getConfigIndex(strVariable);
                try
                {
                    if (commandList[n].GetType() != typeof(command) || commandList[n] == null)
                        commandList[n] = new command(this, "", strValue, "", 1);
                    commandList[n].prefixes = strValue;
                }
                catch (ArgumentOutOfRangeException e)
                {
                    toConsole(3, e.ToString());
                    commandList.Add(new command(this, "", strValue, "", 1));
                    //commandList[n].setPrefixes(strValue);
                }
            }
            else if (strVariable.Contains("Command Response"))
            {
                int n = getConfigIndex(strVariable);
                try
                {
                    if (commandList[n].GetType() != typeof(command) || commandList[n] == null)
                        commandList[n] = new command(this, "", "", strValue, 1);
                    commandList[n].response = strValue;
                }
                catch (ArgumentOutOfRangeException e)
                {
                    toConsole(3, e.ToString());
                    commandList.Add(new command(this, "", "", strValue, 1));
                }
            }
            else if (strVariable.Contains("Command Broadcast Level"))
            {
                int n = getConfigIndex(strVariable);
                try
                {
                    int bc = Int32.Parse(strValue);
                    /*if (commandList[n].GetType() != typeof(command) || commandList[n] == null)
                        commandList[n] = new command(this, "", "", strValue, 1);*/
                    commandList[n].broadcastLevel = bc;
                }
                catch (ArgumentOutOfRangeException e)
                {
                    toConsole(3, e.ToString());
                    toConsole(2, "ERROR: You cannot set a broadcast level for a nonexistent command.");
                    //commandList.Add(new command(this, "", "", strValue, 1));
                }
                catch (Exception e)
                {
                    toConsole(1, "ERROR: Invalid broadcast level! Use integer values only.");
                    commandList[n].broadcastLevel = 1;
                }
            }
            else if (strVariable.Contains("Debug Level"))
            {
                debugLevelString = strValue;
                try
                {
                    debugLevel = Int32.Parse(debugLevelString);
                }
                catch (Exception z)
                {
                    toConsole(1, "ERROR: Invalid debug level! Use integer values only.");
                    debugLevel = 1;
                    debugLevelString = "1";
                }
            }
        }
    }
}