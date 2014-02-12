//Import various C# things.
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

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
        private Boolean initialSet = false;

        private List<command> commandList = new List<command>();

        private class command
        {
            private string cmd = "";
            private string response = "";
            private pureWords2 pw2 = null;
            //Default Prefixes
            private List<char> prefixesList = new List<char>(new char[] { '!', '/' });

            public command()
            {
                cmd = "";
                response = "";
                pw2 = null;
                //Default Prefixes
                prefixesList = new List<char>(new char[] { '!','/' });
            }
            public command(pureWords2 instance, String cmdString, String prefixesString, String responseString)
            {
                pw2 = instance;
                setCommand(cmdString);
                setResponse(responseString);
                if (!String.IsNullOrEmpty(prefixesString))
                {
                    setPrefixes(prefixesString);
                }
                else //Default Prefixes
                {
                    setPrefixes("!/");
                }
            }
            public void setCommand(String cmdString)
            {
                cmd = cmdString.Trim().ToLower();
            }
            public String getCommand()
            {
                return cmd;
            }
            public void setPrefixes(String prefixString)
            {
                pw2.toConsole(3, "DEBUG: Setting prefixes to " + prefixString);
                prefixesList.Clear();
                prefixesList = new List<char>(prefixString.ToCharArray());
            }
            public String getPrefixes()
            {
                String prefixesListString = "";
                foreach (char c in prefixesList)
                {
                    prefixesListString += c.ToString();
                }
                return prefixesListString;
            }
            public void setResponse(String responseString)
            {
                response = responseString.Replace("\r", "").Trim();
            }
            public String getResponse()
            {
                return response;
            }
            public Boolean checkChatAndRespond(String playerName, String chatMsg)
            {
                String message = chatMsg;
                pw2.toConsole(3, "First character is " + message[0].ToString());
                if (message.Trim().Length > 1 && !String.IsNullOrEmpty(this.getResponse()) && !String.IsNullOrEmpty(this.getCommand()) && prefixesList.Count > 0)
                {
                    String cmdBody = message.Substring(1).ToLower();
                    foreach (char k in prefixesList)
                    {
                        if (message[0] == k && cmdBody == this.getCommand())
                        {
                            pw2.toChat(this.getResponse(), playerName);
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
            return "0.5.3";
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
            return @"<p><b>This version of pureWords that contains a special
'lag' trigger. Open the settings tab for configuration options.
</b> It can be disabled by clearing the 'Lag Response' field.
Special notes:</p>
<ul>
  <li><b>New in 1.7+: More information is contained in log entries.
See below for details.</b></li>
  <li>Add the timestamp to a log name by inserting [date] into
the path. It will be replaced with MMddyyyy.</li>
  <li>It is like any other trigger, but it also accepts messages.
(By default) !lag, /lag, !lag message, /lag message will all work. </li>
  <li>It logs like this:<br>
<i>11/26/2013 19:58:57 [LAG REPORT] MP_Abandoned | ConquestLarge0 | Round
Time : 153 | Server Uptime : 320 | Players : 1 | Analytalica : ''I had a
dream that the PURE server would not lag... -Martin Luther King Jr''</i><br>
in the time format 'MM/dd/yyyy HH:mm:ss</li>
  <li>You can only report lag once per round.</li>
</ul>
<p>pureWords is a word filter plugin that monitors server chat.
It features a configurable 'bad word' detector that kicks players for
saying certain words in the in-game chat (whether it be global, team,
or squad), and customizable chat triggers that respond to player
inquiries such as '!help' or '/info'. Timestamped kick actions by
pureWords can be logged into a local text file.
</p>
<p>This plugin was developed by analytalica for PURE Battlefield.</p>
<p><big><b>Bad Word List Setup:</b></big><br>
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
<p><big><b>Trigger/Response
Command Setup:</b></big></p>
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
characters (roughly 126) that you can send per chat. It is fine to make
a response one line, but it will be cut off
or may not display at all past the character limit. Instead, manually
split the response into multiple lines (click the dropdown button in
PRoCon next to the entry field) and they will be sent properly as
multiple chat responses
as opposed to one long continuous response.</p>
<p>
A future version of pureWords will automatically remove strings that
begin with '!' and '/' in the trigger field to avoid confusion, and may
include customizable starting characters like '#' or '@'.</p>

";
        }
        #endregion
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
            string replaceWith = "";
            string noCReturn = original.Replace("\r", replaceWith);
            return noCReturn;
        }

        public void kickPlayer(String playerName)
        {
            this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", playerName, this.kickMessage);
        }
        #region Chat Events
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name, "OnPluginLoaded", "OnGlobalChat", "OnTeamChat", "OnSquadChat");
        }

        public override void OnGlobalChat(string speaker, string message)
        {
            processChat(speaker, message);
        }

        public override void OnTeamChat(string speaker, string message, int teamId)
        {
            processChat(speaker, message);
        }

        public override void OnSquadChat(string speaker, string message, int teamId, int squadId)
        {
            processChat(speaker, message);
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
                lstReturn.Add(new CPluginVariable("Command Settings|New Command Word", typeof(string), ""));
				
				if(commandList.Count > 0){
					lstReturn.Add(new CPluginVariable("Command Settings|Copy Existing Command", typeof(string), ""));
				}

                for (int i = 0; i < commandList.Count; i++)
                {
                    command thisCommand = commandList[i];
                    String commandWordAdd = thisCommand.getCommand();
                    String prefixesAdd = thisCommand.getPrefixes();
                    String responseAdd = thisCommand.getResponse();
                    //Default prefixes
                    if (String.IsNullOrEmpty(commandWordAdd) && String.IsNullOrEmpty(responseAdd) && (String.IsNullOrEmpty(prefixesAdd) || prefixesAdd == "!/") && initialSet == true)
                    {
                        commandList.Remove(thisCommand);
                        i--;
                    }
                    else
                    {
                        lstReturn.Add(new CPluginVariable("Command Settings|Command Word " + i.ToString(), typeof(string), commandWordAdd));
                        lstReturn.Add(new CPluginVariable("Command Settings|Command Prefixes " + i.ToString(), typeof(string), prefixesAdd));
                        lstReturn.Add(new CPluginVariable("Command Settings|Command Response " + i.ToString(), typeof(string), responseAdd));
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

        public void SetPluginVariable(String strVariable, String strValue)
        {
            initialSet = true;
            toConsole(3, "DEBUG: Setting '" + strVariable + "' with value '" + strValue + "'");
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
                    string replaceWith = "";
                    keywordListString = strValue.Replace("\r\n", replaceWith).Replace("\n", replaceWith).Replace("\r", replaceWith);
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
                commandList.Add(new command(this, strValue, "", ""));
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
						toConsole(1, "ERROR: That command doesn't exist!")
					}else{
						//Need a better cloning function
						commandList.Insert(cmdToCopy + 1, new command(this, commandList[cmdToCopy].getCommand(), commandList[cmdToCopy].getPrefixes(), commandList[cmdToCopy].getResponse());
					}
                }
                catch (Exception z)
                {
                    toConsole(1, "ERROR: Invalid command ID! Use integer values only.");
                }
			}
            else if (Regex.Match(strVariable, @"Command Word").Success)
            {
                String[] strVariableArray = strVariable.Split(' ');
                int cmdNum = Convert.ToInt32(strVariableArray[strVariableArray.Length - 1]);
                try
                {
                    if (commandList[cmdNum].GetType() != typeof(command) || commandList[cmdNum] == null)
                        commandList[cmdNum] = new command(this, strValue, "", "");
                    commandList[cmdNum].setCommand(strValue);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    toConsole(3, e.ToString());
                    commandList.Add(new command(this, strValue, "", ""));
                }
            }
            else if (Regex.Match(strVariable, @"Command Prefixes").Success)
            {
                String[] strVariableArray = strVariable.Split(' ');
                int cmdNum = Convert.ToInt32(strVariableArray[strVariableArray.Length - 1]);
                try
                {
                    if (commandList[cmdNum].GetType() != typeof(command) || commandList[cmdNum] == null)
                        commandList[cmdNum] = new command(this, "", strValue, "");
                    commandList[cmdNum].setPrefixes(strValue);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    toConsole(3, e.ToString());
                    commandList.Add(new command(this, "", strValue, ""));
                    //commandList[cmdNum].setPrefixes(strValue);
                }
            }
            else if (Regex.Match(strVariable, @"Command Response").Success)
            {
                String[] strVariableArray = strVariable.Split(' ');
                int cmdNum = Convert.ToInt32(strVariableArray[strVariableArray.Length - 1]);
                try
                {
                    if (commandList[cmdNum].GetType() != typeof(command) || commandList[cmdNum] == null)
                        commandList[cmdNum] = new command(this, "", "", strValue);
                    commandList[cmdNum].setResponse(strValue);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    toConsole(3, e.ToString());
                    commandList.Add(new command(this, "", "", strValue));
                }
            }
            else if (Regex.Match(strVariable, @"Debug Level").Success)
            {
                debugLevelString = strValue;
                try
                {
                    debugLevel = Int32.Parse(debugLevelString);
                }
                catch (Exception z)
                {
                    toConsole(1, "Invalid debug level! Use integer values only.");
                    debugLevel = 1;
                    debugLevelString = "1";
                }
            }
        }
    }
}