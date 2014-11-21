//Import various C# things.
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Timers;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
//Import Procon things.
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;
using PRoCon.Core.Maps;

namespace PRoConEvents
{
    public class pureWords2 : PRoConPluginAPI, IPRoConPluginInterface
    {

        //--------------------------------------
        //Class level variables.
        //--------------------------------------

        private bool pluginEnabled = false;
        private string debugLevelString = "1";
        private int debugLevel = 1;

        private string logName = "";

        //Bad words list stuff
        private string kickMessage = "";
        private string chatMessage = "";
        private string keywordListString = "";
        private string[] keywordArray;
        private int keywordArraySize = 0;

        private List<command> commandList = new List<command>();
        private List<trigger> triggerList = new List<trigger>();


        //Supported RCON commands
		private Timer rconTimer = new Timer();
		private Queue<String> rconQueue = new Queue<String>();
		
		private Queue<rconAndTarget> nextMap = new Queue<rconAndTarget>();
        private List<MaplistEntry> lstMaplist = new List<MaplistEntry>();
        private int mapIndexNext = 0;

        public void rconReset()
        {
            nextMap.Clear();
            rconQueue.Clear();
            lstMaplist.Clear();
            mapIndexNext = 0;
        }
		
		public bool rconSort (string message, string player){
			if(message.Contains("[nextMap]")){
                toConsole(2, "Message contains an RCON query " + "[nextMap]. Adding to queue...");
				rconQueue.Enqueue("[nextMap]");
				nextMap.Enqueue(new rconAndTarget("[nextMap]", message, player));
				return true;
            }
			return false;
		}

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
                if (this.broadcastLevel > 0 && message.Trim().Length > 1 && !String.IsNullOrEmpty(this.response) && !String.IsNullOrEmpty(this.commandWord) && this.i_prefixes.Count > 0)
                {
                    String cmdBody = message.Substring(1).ToLower();
                    foreach (char k in i_prefixes)
                    {
                        if (message[0] == k && cmdBody == this.commandWord)
                        {
                            switch (this.broadcastLevel)
                            {
                                case 1:
                                    pw2.toChat(this.response.Replace("[player]", playerName), playerName);
                                    return true;
                                default:
                                    pw2.toChat(this.response.Replace("[player]", playerName));
                                    return true;
                            }
                        }
                    }
                }
                return false;
            }
        }

        private class trigger
        {
            private string i_triggerWord = "";
            public string triggerWord
            {
                get { return i_triggerWord; }
                set { i_triggerWord = value.Trim().ToLower(); }
            }
            private string i_response = "";
            public string response
            {
                get { return i_response; }
                set { i_response = value.Replace("\r", "").Trim(); }
            }
            public int broadcastLevel
            {
                get;
                set;
            }
            private pureWords2 pw2 = null;
            public trigger()
            {
                triggerWord = "";
                response = "";
                pw2 = null;
                broadcastLevel = 1;
            }
            public trigger(pureWords2 instance, String triggerString, String responseString, int broadcast)
            {
                pw2 = instance;
                triggerWord = triggerString;
                response = responseString;
                broadcastLevel = broadcast;
            }
            public Boolean checkChatAndRespond(String playerName, String chatMsg)
            {
                String message = chatMsg;
                if (message.Trim().Length > 0 && this.broadcastLevel != 0 && !String.IsNullOrEmpty(this.response) && !String.IsNullOrEmpty(this.triggerWord) && Regex.IsMatch(message, "\\b" + this.triggerWord + "\\b", RegexOptions.IgnoreCase))
                {
                    switch (this.broadcastLevel)
                    {
                        case 1:
                            pw2.toChat(this.response.Replace("[player]", playerName), playerName);
                            return true;
                        default:
                            pw2.toChat(this.response.Replace("[player]", playerName));
                            return true;
                    }
                }
                return false;
            }
        }
		
		public class rconAndTarget
		{
			public string query { get; set; }
			public string message { get; set; }
			public string player { get; set; }
			public rconAndTarget(){
				this.query = "";
				this.message = "";
				this.player = "";
			}
			public rconAndTarget(string q, string m, string p){
				this.query = q;
				this.message = m;
				this.player = p;
			}
		}

        public void processRcon(object source, ElapsedEventArgs e)
        {
            if (pluginEnabled)
            {
                this.toConsole(4, "rconTimer Ticking... " + rconQueue.Count + " elements in queue.");
                if (rconQueue.Count > 0)
                {
                    switch (rconQueue.Dequeue())
                    {
                        case "[nextMap]":
                            this.toConsole(2, "Requesting next map info...");
                            this.ExecuteCommand("procon.protected.send", "mapList.list", "0");
                            this.ExecuteCommand("procon.protected.send", "mapList.getMapIndices");
                            break;
                        default:
                            break;
                    }
                }
            }
        }

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
                    if (!String.IsNullOrEmpty(chatMessage))
                    {
                        toChat(chatMessage.Replace("[player]", speaker));
                    }
                    kickPlayer(speaker);
                    toLog("[ACTION] " + speaker + " was kicked for saying \"" + message + "\"");
                }
                    //Ignore commands and triggers if the message contained a bad word.
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
                        toConsole(2, "Was not a command...");
                    Boolean wasTrigger = false;
                    foreach (trigger aTgr in triggerList)
                    {
                        if (aTgr.checkChatAndRespond(speaker, message)){
                            wasTrigger = true;
                            break;
                        }
                    }
                    if (!wasTrigger)
                        toConsole(2, "Was not a trigger...");
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
            return "2.2.3";
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
            return @"<p><b>pureWords2</b> is a superior
command,trigger,and filter plugin that can monitor and respond to
in-game chat
messages.<br>
</p>
<ul>
  <li>Bad Words: Set a list of bad words that players are kicked
for saying.</li>
  <ul>
    <li>Configurable kick message received by player</li>
  </ul>
  <ul>
    <li>Configurable server-wide chat response</li>
    <li>Words are matched just like triggers</li>
  </ul>
  <li>Command Words: Set commands like '!help' or '@rules' that
players can query through chat and get an automatic response to.</li>
  <ul>
    <li>Unlimited amount of prefixes, default '/!@#'</li>
    <li>Configurable response broadcast level (to original
speaker or all players)</li>
    <li>Must be an exact match (prefix + command word only)</li>
  </ul>
  <li>Trigger Words: Set trigger words like 'hacks' or 'aimbot'
that players
will get an automatic response to.</li>
  <ul>
    <li>No prefixes - trigger words are discovered by a regex
search within the entire message</li>
    <li>Configurable response broadcast level (to original
speaker or all players)</li>
    <li>Words are matched anywhere in a player chat message</li>
  </ul>
</ul>
<p>It is a massive overhaul and substantial upgrade to the
original
pureWords and is not a direct upgrade. 'Triggers' in the original
pureWords are now known as 'Command Words' in pureWords2. Timestamped
kick actions by
pureWords can be logged into a local text file.</p>
<p>This plugin was developed by Analytalica originally for PURE
Battlefield.</p>
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
<p>Bad Words are matched as&nbsp;whole words only
(ignoring
any punctuation),
e.g. a player will not be kicked for 'ass' if he says 'assassin'.
In the bad word list, leading and trailing spaces (as well as line
breaks) are automatically removed,
so it is fine to use <i>bathtub , porch ,bottle </i> in
place of <i>bathtub,porch,bottle</i>.</p>
<p><big><b>Command Words Setup:</b></big></p>
<ol>
  <li>Type a command word into 'Create a New Command Word'. Do
not include prefixes here, like the '!' in '!test'.<br>
    <i>rules</i></li>
  <li>Configure a response message. This is the message sent to
chat in response. If used, [player] gets replaced by the sender.<br>
    <i>Welcome [player]! No
cheating.</i></li>
  <li>Configure any of the command's prefixes. The defaults are
/!@#, split by individual characters.<br>
    <i>/rules, !rules, @rules,
and #rules</i></li>
  <li>Set the broadcast level to 0 (disabled), 1 (original player
only), or 2 (all players). The broadcast level dictates who receives
the response message when the command word is entered.<br>
  </li>
</ol>
<p>Command Words are matched by searching for any of the prefixes
and then the word itself immediately after. In the example above in
italics, a player named Joe who types '!rules' in squad, team, or
global chat will
receive 'Welcome Joe! No cheating.' in chat sent by the server.</p>
<p><big><b>Trigger Words Setup:</b></big></p>
<ol>
  <li>Type a trigger word into 'Create a New Command Word'.<br>
    <i>hacker</i></li>
  <li>Configure a response message. This is the message sent to
chat in response. If used, [player] gets replaced by the sender.<br>
    <i>Hey [player], please
report hackers on our website.</i></li>
  <li>Set the broadcast level to 0 (disabled), 1 (original player
only), or 2 (all players). The broadcast level dictates who receives
the response message when the command word is entered.<br>
  </li>
</ol>
<p>Trigger Words are matched as whole words only (ignoring any
punctuation), just like Bad Words. In the example above in
italics, a player named Charlie who types 'Joe is a hacker!' in squad,
team, or global chat will
receive 'Hey Charlie, please report hackers on our website.' in chat
sent by the server.</p>
<p><big><b>Special Response Strings</b></big></p>
<p>pureWords2 v2.2.0 has built the foundation for special
response strings. By inserting a special response string, it is
replaced by an always up-to-date dynamic response. Both Command and
Trigger word responses can include special response strings.</p>
<ul>
  <li><b>[nextMap]</b> : Replaced with the next map.</li>
  <li><b>[nextMode]</b> : Replaced with the next mode.</li>
</ul>
<p>Example: A 24/7 CQ Large Operation Locker server. Command word
'nextmap' with response 'The next map is [nextMap] and the mode is
[nextMode]' will respond to players with 'The next map is Operation
Locker and the mode is Conquest Large'.</p>
<p><big><b>General Notes</b></big></p>
<ul>
  <li>All words and messages matched are case insensitive. The
configuration page should prevent you from entering any capitalized
letters.</li>
  <li>Use the 'Copy
Existing Command' or 'Copy Existing Trigger' option by entering the
number for the command or trigger you'd like to duplicate and it will
appear underneath the original.</li>
  <li>Clearing out most of the configuration fields for a command
or trigger will automatically delete it.</li>
  <li><b>Battlefield has a hard restriction on the number
of
characters (roughly 128) that you can send per chat.</b> Manually
split responses into multiple lines (click the dropdown button in
PRoCon next to the entry field) and they will be sent properly as
multiple chat responses.</li>
  <li>The bad word functionality is nearly identical to how it
worked in the original pureWords.</li>
</ul>
";
        }
        #endregion
        //--------------------------------------
        //Helper Functions
        //--------------------------------------
        #region Helper Functions
        public void toChat(String message)
        {
            toChat(message, "all");
        }

        public void toChat(String message, String playerName)
        {
            if (!rconSort(message, playerName))
            {
                if (!message.Contains("\n") && !String.IsNullOrEmpty(message))
                {
                    if (playerName == "all")
                    {
                        this.toConsole(2, "Telling all players: '" + message + "'");
                        this.ExecuteCommand("procon.protected.send", "admin.say", message, "all");
                    }
                    else
                    {
                        this.toConsole(2, "Telling " + playerName + " '" + message + "'");
                        this.ExecuteCommand("procon.protected.send", "admin.say", message, "player", playerName);
                    }
                }
                else if (message != "\n")
                {
                    string[] multiMsg = message.Split(new string[] { "\n" }, StringSplitOptions.None);
                    foreach (string send in multiMsg)
                    {
                        if (!String.IsNullOrEmpty(message))
                            toChat(send, playerName);
                    }
                }
            }
        }

        public void exceeds128(string message)
        {
            if (message.Length > 128)
                toConsole(1, "WARNING: Message \"" + message + "\" exceeds 128 characters and may not display properly in chat! Split messages into new lines <128 characters.");
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
            toConsole(1, "Kicking " + playerName + " with message \"" + this.kickMessage + "\"");
            this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", playerName, this.kickMessage);
        }
        #endregion
        #region Events
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name, "OnPluginLoaded", "OnGlobalChat", "OnTeamChat", "OnSquadChat", "OnMaplistList", "OnMaplistGetMapIndices", "OnRoundOver", "OnEndRound", "OnRunNextLevel");
            this.rconTimer.Stop();
            this.rconTimer = new Timer();
            this.rconTimer.Elapsed += new ElapsedEventHandler(this.processRcon);
            this.rconTimer.Interval = 500;
            this.rconTimer.Start();
            this.toConsole(2, "rconTimer enabled!");
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

        public override void OnMaplistList(List<MaplistEntry> lstMaplist)
        {
            if (pluginEnabled)
            {
                this.toConsole(3, "OnMaplistList called.");
                this.lstMaplist = lstMaplist;
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
            return MapFileName;
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
            return "Dinosaur Survival";
        }

        public override void OnMaplistGetMapIndices(int mapIndex, int nextIndex)
        {
            if (pluginEnabled)
            {
                this.toConsole(3, "OnMaplistGetMapIndices called.");
                this.toConsole(3, "There are " + this.nextMap.Count + " people requesting the next map.");
                this.mapIndexNext = nextIndex;
                if (this.lstMaplist.Count < 1)
                {
                    this.toConsole(3, "Recalling [nextMap] commands, current map list is empty.");
                    rconQueue.Enqueue("[nextMap]");
                }
                else
                {
                    this.toConsole(3, "Chatting to players requesting [nextMap]...");
                    while (this.nextMap.Count > 0)
                    {
                        rconAndTarget wantNextMap = this.nextMap.Dequeue();
                        string player = wantNextMap.player;
                        string completeResponse = wantNextMap.message.Replace("[nextMap]", realName(lstMaplist[this.mapIndexNext].MapFileName)).Replace("[nextMode]", realMode(lstMaplist[this.mapIndexNext]));
                        toChat(completeResponse, player);
                    }
                    this.toConsole(3, "[nextMap] chat queue cleared.");
                }
            }
        }
        #region Round End Events
        public override void OnRoundOver(int winningTeamId) 
        {
            rconReset();
        }

        public override void OnEndRound(int iWinningTeamID) 
        {
            rconReset();
        }

        public override void OnRunNextLevel() 
        {
            rconReset();
        }
        #endregion

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
            this.toConsole(2, "Bad Words List (" + keywordArraySize + " words): " + stringKeywordList);
            this.rconTimer.Stop();
            this.rconTimer = new Timer();
            this.rconTimer.Elapsed += new ElapsedEventHandler(this.processRcon);
            this.rconTimer.Interval = 500;
            this.rconTimer.Start();
            this.toConsole(2, "rconTimer enabled!");
            
            toLog("[STATUS] pureWords2 Enabled");
        }

        public void OnPluginDisable()
        {
            this.pluginEnabled = false;
            rconReset();
            this.rconTimer.Stop();
            this.toConsole(2, "rconTimer stopped!");
            toLog("[STATUS] pureWords2 Disabled");
            this.toConsole(1, "pureWords2 Disabled!");
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            try
            {
                lstReturn.Add(new CPluginVariable("1) Bad Word Settings|Bad Word List", typeof(string), keywordListString));
                lstReturn.Add(new CPluginVariable("1) Bad Word Settings|Kick Reason", typeof(string), kickMessage));
                lstReturn.Add(new CPluginVariable("1) Bad Word Settings|Response Chat Message", typeof(string), chatMessage));

                lstReturn.Add(new CPluginVariable("2) Command Settings|Create a New Command Word", typeof(string), ""));
				
				if(commandList.Count > 0){
                    lstReturn.Add(new CPluginVariable("2) Command Settings|Copy Existing Command", typeof(string), ""));
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
                        //lstReturn.Add(new CPluginVariable("Command Settings|________________________________________________________________________________________________________________________________", typeof(string), "________________________________________________________________________________________________________________________________"));
                        lstReturn.Add(new CPluginVariable("2) Command Settings|" + i.ToString() + ". Command Word       -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------", typeof(string), commandWordAdd));
                        lstReturn.Add(new CPluginVariable("2) Command Settings|" + i.ToString() + ". Command Prefixes", typeof(string), prefixesAdd));
                        lstReturn.Add(new CPluginVariable("2) Command Settings|" + i.ToString() + ". Command Response", typeof(string), responseAdd));
                        lstReturn.Add(new CPluginVariable("2) Command Settings|" + i.ToString() + ". Command Broadcast Level", typeof(string), broadcastAdd));
                    }
                }

                lstReturn.Add(new CPluginVariable("3) Trigger Settings|Create a New Trigger Word", typeof(string), ""));

                if (triggerList.Count > 0)
                {
                    lstReturn.Add(new CPluginVariable("3) Trigger Settings|Copy Existing Trigger", typeof(string), ""));
                }

                for (int i = 0; i < triggerList.Count; i++)
                {
                    trigger thisTrigger = triggerList[i];
                    String triggerWordAdd = thisTrigger.triggerWord;
                    String responseAdd = thisTrigger.response;
                    String broadcastAdd = thisTrigger.broadcastLevel.ToString();
                    //Default prefixes
                    if (String.IsNullOrEmpty(triggerWordAdd) && String.IsNullOrEmpty(responseAdd))
                    {
                        triggerList.Remove(thisTrigger);
                        i--;
                    }
                    else
                    {
                        //lstReturn.Add(new CPluginVariable("Trigger Settings|________________________________________________________________________________________________________________________________", typeof(string), "________________________________________________________________________________________________________________________________"));
                        lstReturn.Add(new CPluginVariable("3) Trigger Settings|" + i.ToString() + ". Trigger Word       -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------", typeof(string), triggerWordAdd));
                        lstReturn.Add(new CPluginVariable("3) Trigger Settings|" + i.ToString() + ". Trigger Response", typeof(string), responseAdd));
                        lstReturn.Add(new CPluginVariable("3) Trigger Settings|" + i.ToString() + ". Trigger Broadcast Level", typeof(string), broadcastAdd));
                    }
                }

                lstReturn.Add(new CPluginVariable("4) Main Settings|Log Path", typeof(string), logName));
                lstReturn.Add(new CPluginVariable("4) Main Settings|Debug Level", typeof(string), debugLevelString));

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
            else if (strVariable.Contains("Kick Reason"))
            {
                kickMessage = removeCReturn(strValue.Trim());
            }
            else if (strVariable.Contains("Response Chat Message"))
            {
                chatMessage = removeCReturn(strValue.Trim());
            }
            else if (strVariable.Contains("Log Path"))
            {
                string originalStrValue = strValue;
                if (originalStrValue.StartsWith("\\") || originalStrValue.StartsWith("/"))
                    originalStrValue = originalStrValue.Substring(1);
                logName = originalStrValue.Trim().Replace('\\', '/');
                if(pluginEnabled)
                    toLog("[STATUS] Log path set to " + logName);
            }
            else if (strVariable.Contains("New Command Word"))
            {
                commandList.Add(new command(this, strValue, "", "", 1));
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
            else if (strVariable.Contains("New Trigger Word"))
            {
                triggerList.Add(new trigger(this, strValue, "", 1));
                //StreamWriter log = File.AppendText(logName);
            }
            else if (strVariable.Contains("Copy Existing Trigger"))
            {
                int tgrToCopy = 0;
                try
                {
                    tgrToCopy = Int32.Parse(strValue);
                    if (tgrToCopy + 1 > triggerList.Count)
                    {
                        toConsole(1, "ERROR: That trigger doesn't exist!");
                    }
                    else
                    {
                        String newTgr = "" + triggerList[tgrToCopy].triggerWord;
                        String newResponse = "" + triggerList[tgrToCopy].response;
                        triggerList.Insert(tgrToCopy + 1, new trigger(this, newTgr, newResponse, 1));
                    }

                }
                catch (Exception z)
                {
                    toConsole(1, "ERROR: Invalid trigger ID! Use integer values only.");
                    toConsole(2, z.ToString());
                }
            }
            else if (strVariable.Contains("Trigger Word"))
            {
                int n = getConfigIndex(strVariable);
                try
                {
                    if (triggerList[n].GetType() != typeof(trigger) || triggerList[n] == null)
                        triggerList[n] = new trigger(this, strValue, "", 1);
                    triggerList[n].triggerWord = strValue;
                }
                catch (ArgumentOutOfRangeException e)
                {
                    toConsole(3, e.ToString());
                    triggerList.Add(new trigger(this, strValue, "", 1));
                }
            }
            else if (strVariable.Contains("Trigger Response"))
            {
                int n = getConfigIndex(strVariable);
                try
                {
                    if (triggerList[n].GetType() != typeof(trigger) || triggerList[n] == null)
                        triggerList[n] = new trigger(this, "", strValue, 1);
                    triggerList[n].response = strValue;
                }
                catch (ArgumentOutOfRangeException e)
                {
                    toConsole(3, e.ToString());
                    triggerList.Add(new trigger(this, "", strValue, 1));
                }
            }
            else if (strVariable.Contains("Trigger Broadcast Level"))
            {
                int n = getConfigIndex(strVariable);
                try
                {
                    int bc = Int32.Parse(strValue);
                    /*if (triggerList[n].GetType() != typeof(trigger) || triggerList[n] == null)
                        triggerList[n] = new trigger(this, "", "", strValue, 1);*/
                    triggerList[n].broadcastLevel = bc;
                }
                catch (ArgumentOutOfRangeException e)
                {
                    toConsole(3, e.ToString());
                    toConsole(2, "ERROR: You cannot set a broadcast level for a nonexistent trigger.");
                    //triggerList.Add(new trigger(this, "", "", strValue, 1));
                }
                catch (Exception e)
                {
                    toConsole(1, "ERROR: Invalid broadcast level! Use integer values only.");
                    triggerList[n].broadcastLevel = 1;
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