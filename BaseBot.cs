using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Util;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using Sfs2X.Entities;
using Timer = System.Threading.Timer;
using Player = bot.Player;

namespace bot
{
    public abstract class BaseBot
    {
        private SmartFox sfs;
        private const string IP = "172.16.100.112";
        // private const string IP = "10.10.10.18";
        private const string username = "bot_fct";

        //private const string token = "eyJhbGciOiJIUzUxMiJ9.eyJzdWIiOiJhbmgubmd1eWVudGhpaG9uZyIsImF1dGgiOiJST0xFX1VTRVIiLCJMQVNUX0xPR0lOX1RJTUUiOjE2NTI4NTc4MjY1MzAsImV4cCI6MTY1Mjk0NDIyNn0.APDcJTDo7RrnIxL8ChHS_HyUXDxDRyBSj68txmkWtFpC3ISUFgqBESmKiW203hShaDF6wLKBwrlb0tekiJbKQg";
        private const string token = "eyJhbGciOiJIUzUxMiJ9.eyJzdWIiOiJ0b2FpLm5ndXllbmNvbmciLCJhdXRoIjoiUk9MRV9VU0VSIiwiTEFTVF9MT0dJTl9USU1FIjoxNjUzNTYxMTI0MTI4LCJleHAiOjE2NTUzNjExMjR9.duP5120vTRC56sWcvQQonNn46vv3097_IRfkRynKsvjB5LiTBORnz5kEis8V2UJsWHdo7RssUjBdZbWwuG17aQ";
        //private const string token = "bot";
        private const int TIME_INTERVAL_IN_MILLISECONDS = 1000;
        private const int ENEMY_PLAYER_ID = 0;
        private const int BOT_PLAYER_ID = 2;

        protected int delaySwapGem = 3500;
        protected int delayFindGame = 5000;
        private Timer _timer = null;
        private bool isJoinGameRoom = false;
        private bool isLogin = false;

        private Room room;
        protected Player botPlayer;
        protected Player enemyPlayer;
        protected int currentPlayerId;
        protected Grid grid;

        public void Start()
        {
            Console.WriteLine("Connecting to Game-Server at " + IP);

            _timer = new Timer(Tick, null, TIME_INTERVAL_IN_MILLISECONDS, Timeout.Infinite);

            // Connect to SFS
            ConfigData cfg = new ConfigData();
            cfg.Host = IP;
            cfg.Port = 9933;
            cfg.Zone = "gmm";
            cfg.Debug = false;
            cfg.BlueBox.IsActive = true;

            sfs.Connect(cfg);
        }

        public BaseBot()
        {
            sfs = new SmartFox();
            sfs.ThreadSafeMode = false;
            sfs.AddEventListener(SFSEvent.CONNECTION, OnConnection);
            sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
            sfs.AddEventListener(SFSEvent.LOGIN, onLoginSuccess);
            sfs.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);

            sfs.AddEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
            sfs.AddEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);
            sfs.AddEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);
        }

        private void FindGame()
        {
            var data = new SFSObject();
            data.PutUtfString("type", "");
            data.PutUtfString("adventureId", "");
            sfs.Send(new ExtensionRequest(ConstantCommand.LOBBY_FIND_GAME, data));
        }

        private void OnRoomJoin(BaseEvent evt)
        {
            Console.WriteLine("Joined room " + this.sfs.LastJoinedRoom.Name);
            this.room = (Room)evt.Params["room"];
            if (room.IsGame)
            {
                isJoinGameRoom = true;
                return;
            }


            //taskScheduler.schedule(new FindRoomGame(), new Date(System.currentTimeMillis() + delayFindGame));
        }

        private void OnRoomJoinError(BaseEvent evt)
        {
            Console.WriteLine("Join-room ERROR");
        }

        private void OnExtensionResponse(BaseEvent evt)
        {
            var evtParam = (ISFSObject)evt.Params["params"];
            var cmd = (string)evt.Params["cmd"];
            Console.WriteLine("OnExtensionResponse " + cmd);

            switch (cmd)
            {
                case ConstantCommand.START_GAME:
                    ISFSObject gameSession = evtParam.GetSFSObject("gameSession");

                    StartGame(gameSession, room);
                    break;
                case ConstantCommand.END_GAME:
                    endGame();
                    break;
                case ConstantCommand.START_TURN:
                    StartTurn(evtParam);
                    break;
                case ConstantCommand.ON_SWAP_GEM:
                    SwapGem(evtParam);
                    break;
                case ConstantCommand.ON_PLAYER_USE_SKILL:
                    HandleGems(evtParam);
                    break;
                case ConstantCommand.PLAYER_JOINED_GAME:
                    this.sfs.Send(new ExtensionRequest(ConstantCommand.I_AM_READY, new SFSObject(), room));
                    break;
                case "SEND_ALERT":
                    ShowError(evtParam);
                    break;
            }
        }

        private void ShowError(ISFSObject param)
        {
            String error = param.GetUtfString("message");
            log(error);
        }

        private void onLoginSuccess(BaseEvent evt)
        {
            var user = evt.Params["user"] as User;
            Console.WriteLine("Login Success! You are: " + user?.Name);

            isLogin = true;
        }

        private void OnLoginError(BaseEvent evt)
        {
            Console.WriteLine("Login error");
        }

        private void Tick(Object state)
        {
            //if (sfs != null) sfs.ProcessEvents();

            _timer.Change(TIME_INTERVAL_IN_MILLISECONDS, Timeout.Infinite);

            //Console.WriteLine("Tick " + sfs.IsConnected);

            // If bot is at Lobby then FindGame
            if (isLogin && !isJoinGameRoom)
            {
                FindGame();
            }
        }

        private void OnConnection(BaseEvent evt)
        {
            Console.WriteLine("Smartfox connection state: " + (bool)evt.Params["success"]);
            SFSObject parameters = new SFSObject();
            parameters.PutUtfString("BATTLE_MODE", "NORMAL");
            parameters.PutUtfString("ID_TOKEN", token);
            parameters.PutUtfString("NICK_NAME", username);
            sfs.Send(new LoginRequest(username, "", "gmm", parameters));
        }

        private void OnConnectionLost(BaseEvent evt)
        {
            Console.WriteLine("Smartfox OnConnectionLost");
        }

        private void endGame()
        {
            isJoinGameRoom = false;
        }

        protected void AssignPlayers(Room room)
        {

            List<User> users = room.PlayerList;

            User user1 = users[0];

            log("id user1: " + user1.PlayerId + " name:" + user1.Name);

            if (users.Count == 1)
            {
                if (user1.IsItMe)
                {
                    botPlayer = new Player(user1.PlayerId, "player1");
                    enemyPlayer = new Player(ENEMY_PLAYER_ID, "player2");
                }
                else
                {
                    botPlayer = new Player(BOT_PLAYER_ID, "player2");
                    enemyPlayer = new Player(ENEMY_PLAYER_ID, "player1");
                }

                return;
            }

            User user2 = users[1];

            log("id user2: " + user2.PlayerId + " name:" + user2.Name);

            log("id user1: " + user1.PlayerId + " name:" + user1.Name);

            if (user1.IsItMe)
            {
                botPlayer = new Player(user1.PlayerId, "player" + user1.PlayerId);
                enemyPlayer = new Player(user2.PlayerId, "player" + user2.PlayerId);
            }
            else
            {
                botPlayer = new Player(user2.PlayerId, "player" + user2.PlayerId);
                enemyPlayer = new Player(user1.PlayerId, "player" + user1.PlayerId);
            }
        }

        protected void logStatus(String status, String logMsg)
        {
            Console.WriteLine(status + "|" + logMsg + "\n");
        }

        protected void log(String msg)
        {
            Console.WriteLine(username + "|" + msg);
        }

        protected abstract void StartGame(ISFSObject gameSession, Room room);

        protected abstract void SwapGem(ISFSObject paramz);

        protected abstract void HandleGems(ISFSObject paramz);

        protected abstract void StartTurn(ISFSObject paramz);

        public void SendFinishTurn(bool isFirstTurn)
        {
            SFSObject data = new SFSObject();
            data.PutBool("isFirstTurn", isFirstTurn);
            log("sendExtensionRequest()|room:" + room.Name + "|extCmd:" + ConstantCommand.FINISH_TURN + " first turn " + isFirstTurn);
            sendExtensionRequest(ConstantCommand.FINISH_TURN, data);
        }

        public void SendCastSkill(Hero heroCastSkill, Hero heroTarget = null, int? index = null)
        {
            log("SendCastSkill => heroTarget: " + heroTarget?.id);
            log("SendCastSkill => Index: " + index);
            var data = new SFSObject();

            data.PutUtfString("casterId", heroCastSkill.id.ToString());

            if (heroTarget != null)
            {
                data.PutUtfString("targetId", heroTarget.id.ToString());
            }
            else
            {
                if (heroCastSkill.isHeroSelfSkill())
                {
                    data.PutUtfString("targetId", botPlayer.firstHeroAlive().id.ToString());
                }
                else
                {
                    data.PutUtfString("targetId", enemyPlayer.firstHeroAlive().id.ToString());
                }
            }

            data.PutUtfString("selectedGem", selectGem().ToString());

            if (index != null)
            {
                data.PutUtfString("gemIndex", index.ToString());
            }
            else
            {
                data.PutUtfString("gemIndex", new Random().Next(64).ToString());
            }

            data.PutBool("isTargetAllyOrNot", false);
            log("sendExtensionRequest()|room:" + room.Name + "|extCmd:" + ConstantCommand.USE_SKILL + "|Hero cast skill: " + heroCastSkill.id);
            sendExtensionRequest(ConstantCommand.USE_SKILL, data);
        }

        public void SendSwapGem()
        {
            Pair<int> indexSwap = grid.recommendSwapGem();

            var data = new SFSObject();
            data.PutInt("index1", indexSwap.param1);
            data.PutInt("index2", indexSwap.param2);
            log("sendExtensionRequest()|room:" + room.Name + "|extCmd:" + ConstantCommand.SWAP_GEM + "|index1: " + indexSwap.param1 + " index2: " + indexSwap.param2);
            sendExtensionRequest(ConstantCommand.SWAP_GEM, data);
        }

        public void sendExtensionRequest(String extCmd, ISFSObject paramz)
        {
            this.sfs.Send(new ExtensionRequest(extCmd, paramz, room));
        }

        protected GemType selectGem()
        {
            var recommendGemType = botPlayer.getRecommendGemType();

            return recommendGemType.Where(gemType => grid.gemTypes.Contains(gemType)).FirstOrDefault();
            //return botPlayer.getRecommendGemType().stream().filter(gemType -> grid.getGemTypes().contains(gemType)).findFirst().orElseGet(null);
        }

        protected void TaskSchedule(int milliseconds, Action<Task> action)
        {
            System.Threading.Tasks.Task.Delay(milliseconds).ContinueWith(action);
        }
    }
}