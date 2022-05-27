using Sfs2X.Entities;
using Sfs2X.Entities.Data;
namespace bot
{
    public class GemBot : BaseBot
    {
        public List<HeroIdEnum> ap = new List<HeroIdEnum>() { HeroIdEnum.FIRE_SPIRIT, HeroIdEnum.DISPATER}; //1
        public List<HeroIdEnum> carry = new List<HeroIdEnum>() { HeroIdEnum.THUNDER_GOD, HeroIdEnum.MERMAID }; //2
        public List<HeroIdEnum> aoe = new List<HeroIdEnum>() { HeroIdEnum.AIR_SPIRIT, HeroIdEnum.CERBERUS }; //3
        public List<HeroIdEnum> buff = new List<HeroIdEnum>() { HeroIdEnum.MONK, HeroIdEnum.SEA_SPIRIT };//4
        public List<HeroIdEnum> tank = new List<HeroIdEnum>() { HeroIdEnum.ELIZAH, HeroIdEnum.SKELETON };//5
        public List<HeroIdEnum> imba = new List<HeroIdEnum>() { HeroIdEnum.CERBERUS, HeroIdEnum.SEA_GOD };//0
        internal void Load()
        {
            Console.WriteLine("Bot.Load()");
        }

        internal void Update(TimeSpan gameTime)
        {
            Console.WriteLine("Bot.Update()");
        }

        protected override void StartGame(ISFSObject gameSession, Room room)
        {
            // Assign Bot player & enemy player
            AssignPlayers(room);

            // Player & Heroes
            ISFSObject objBotPlayer = gameSession.GetSFSObject(botPlayer.displayName);
            ISFSObject objEnemyPlayer = gameSession.GetSFSObject(enemyPlayer.displayName);

            ISFSArray botPlayerHero = objBotPlayer.GetSFSArray("heroes");
            ISFSArray enemyPlayerHero = objEnemyPlayer.GetSFSArray("heroes");

            for (int i = 0; i < botPlayerHero.Size(); i++)
            {
                var hero = new Hero(botPlayerHero.GetSFSObject(i));
                botPlayer.heroes.Add(hero);
            }

            for (int i = 0; i < enemyPlayerHero.Size(); i++)
            {
                enemyPlayer.heroes.Add(new Hero(enemyPlayerHero.GetSFSObject(i)));
            }
            // Gems
            grid = new Grid(
                                gameSession.GetSFSArray("gems"), 
                                null,
                                botPlayer.heroes, 
                                enemyPlayer.heroes
                           );
            currentPlayerId = gameSession.GetInt("currentPlayerId");
            log("StartGame ");

            // SendFinishTurn(true);
            //taskScheduler.schedule(new FinishTurn(true), new Date(System.currentTimeMillis() + delaySwapGem));
            TaskSchedule(delaySwapGem, _ => SendFinishTurn(true));
        }

        protected override void SwapGem(ISFSObject paramz)
        {
            bool isValidSwap = paramz.GetBool("validSwap");
            if (!isValidSwap)
            {
                return;
            }

            HandleGems(paramz);
        }

        protected override void HandleGems(ISFSObject paramz)
        {
            ISFSObject gameSession = paramz.GetSFSObject("gameSession");
            currentPlayerId = gameSession.GetInt("currentPlayerId");
            //get last snapshot
            ISFSArray snapshotSfsArray = paramz.GetSFSArray("snapshots");
            ISFSObject lastSnapshot = snapshotSfsArray.GetSFSObject(snapshotSfsArray.Size() - 1);
            bool needRenewBoard = paramz.ContainsKey("renewBoard");
            // update information of hero
            HandleHeroes(lastSnapshot);
            if (needRenewBoard)
            {
                grid.updateGems(paramz.GetSFSArray("renewBoard"), null);
                TaskSchedule(delaySwapGem, _ => SendFinishTurn(false));
                return;
            }
            // update gem
            grid.gemTypes = botPlayer.getRecommendGemType();
            // grid.enemyGemTypes = enemyPlayer.getRecommendGemType();

            ISFSArray gemCodes = lastSnapshot.GetSFSArray("gems");
            ISFSArray gemModifiers = lastSnapshot.GetSFSArray("gemModifiers");

            if (gemModifiers != null) log("has gemModifiers");

            grid.updateGems(gemCodes, gemModifiers);

            TaskSchedule(delaySwapGem, _ => SendFinishTurn(false));
        }

        private void HandleHeroes(ISFSObject paramz)
        {
            ISFSArray heroesBotPlayer = paramz.GetSFSArray(botPlayer.displayName);
            for (int i = 0; i < botPlayer.heroes.Count; i++)
            {
                botPlayer.heroes[i].updateHero(heroesBotPlayer.GetSFSObject(i));
            }

            ISFSArray heroesEnemyPlayer = paramz.GetSFSArray(enemyPlayer.displayName);
            for (int i = 0; i < enemyPlayer.heroes.Count; i++)
            {
                enemyPlayer.heroes[i].updateHero(heroesEnemyPlayer.GetSFSObject(i));
            }
        }

        protected override void StartTurn(ISFSObject paramz)
        {
            currentPlayerId = paramz.GetInt("currentPlayerId");
            if (!isBotTurn())
            {
                return;
            }

            var enemyheroesAlive = enemyPlayer.getHeroesAlive();
            var myheroesAlive = botPlayer.getHeroesAlive();
            var anyHeroCarry = enemyheroesAlive.Any(x => carry.Contains(x.id));
            var anyHeroBuff = enemyheroesAlive.Any(x => buff.Contains(x.id));
            var heroAP = enemyheroesAlive.Where(x => ap.Contains(x.id)).FirstOrDefault();
            var heroImba = enemyheroesAlive.Where(x => imba.Contains(x.id)).FirstOrDefault();
            var enemyHeroMaxAttack = enemyPlayer.heroes.Where(x =>
                        ( carry.Contains(x.id) | aoe.Contains(x.id) ) &&
                        x.isAlive()
                     ).OrderByDescending(x => x.attack).FirstOrDefault();

            var enemyTotalHp = enemyPlayer.getTotalHp();
            
            var heroesFullMana = botPlayer.getHeroesFullMana();

            var myAIRSPIRIT = myheroesAlive.FirstOrDefault(x => x.id == HeroIdEnum.AIR_SPIRIT);

            //var anyAIRSPIRIT = myheroesAlive.Any(x => x.id == HeroIdEnum.AIR_SPIRIT && !x.isFullMana() && x.attack >= 11);
            //var anyEnemyFireHero = enemyheroesAlive.Any(x => x.id == HeroIdEnum.FIRE_SPIRIT);
            try
            {
                if (heroesFullMana.Any(x => buff.Contains(x.id)))
                {
                    if (myAIRSPIRIT == null)
                    {
                        TaskSchedule(delaySwapGem, _ => SendCastSkill(heroesFullMana.FirstOrDefault()));
                        return;
                    }
                    if (myAIRSPIRIT?.isFullMana() == true | heroesFullMana.FirstOrDefault()?.hp < 10)
                    {
                        var myheroCarryOrAoe = myheroesAlive.FirstOrDefault(x => carry.Contains(x.id) | aoe.Contains(x.id));
                        TaskSchedule(delaySwapGem, _ => SendCastSkill(heroesFullMana.FirstOrDefault(), myheroCarryOrAoe));
                        return;
                    }
                }
                foreach (var heroFullMana in heroesFullMana)
                {
                    log("heroFullMana Id: " + heroFullMana.id);
                    if (buff.Contains(heroFullMana.id))
                    {
                        continue;
                    }
                    if (heroFullMana.attack >= enemyTotalHp)
                    {
                        TaskSchedule(delaySwapGem, _ => SendCastSkill(heroFullMana));
                        return;
                    }
                    if (ap.Contains(heroFullMana.id))
                    {
                        if (heroImba != null && heroImba?.hp >= 22)
                        {
                            TaskSchedule(delaySwapGem, _ => SendCastSkill(heroFullMana, heroImba));
                            return;
                        }
                        if (heroAP != null && heroAP?.hp >= 22)
                        {
                            TaskSchedule(delaySwapGem, _ => SendCastSkill(heroFullMana, heroAP));
                            return;
                        }
                        if (enemyheroesAlive.Count() == 1 | enemyHeroMaxAttack.attack > 10 | !anyHeroBuff | heroFullMana.hp < 10)
                        {
                            TaskSchedule(delaySwapGem, _ => SendCastSkill(heroFullMana, enemyHeroMaxAttack));
                            return;
                        }
                        continue;
                    }
                    if (heroFullMana.id == HeroIdEnum.AIR_SPIRIT)
                    {
                        log("HeroIdEnum.AIR_SPIRIT");
                        var gemTypes = botPlayer.getRecommendGemType();

                        var index = grid.getIndexGem(gemTypes);
                        if (index != null)
                        {
                            TaskSchedule(delaySwapGem, _ => SendCastSkill(heroFullMana, null, index));
                            return;
                        }
                        continue;
                    }
                    TaskSchedule(delaySwapGem, _ => SendCastSkill(heroFullMana, null));
                    return;
                }

                TaskSchedule(delaySwapGem, _ => SendSwapGem());
            }
            catch
            {
                TaskSchedule(delaySwapGem, _ => SendSwapGem());
            }
        }

        protected bool isBotTurn()
        {
            return botPlayer.playerId == currentPlayerId;
        }
    }
}