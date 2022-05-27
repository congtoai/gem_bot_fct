
namespace bot {
    public class Player
    {
        public int playerId;
        public string displayName;
        public List<Hero> heroes;
        public HashSet<GemType> heroGemType;

        public Player(int playerId, string name)
        {
            this.playerId = playerId;
            this.displayName = name;

            heroes = new List<Hero>();
            heroGemType = new HashSet<GemType>();
        }

        public Hero anyHeroFullMana() {
            foreach(var hero in heroes){
                if (hero.isAlive() && hero.isFullMana()) return hero;
            }

            return null;
        }

        public List<Hero> getHeroesAlive()
        {
            var rs = heroes.Where(hero =>
                                hero.isAlive()
                             ).ToList();
            return rs;
        }

        public List<Hero> getHeroesFullMana()
        {
            var heroesPassiveSkill = new List<HeroIdEnum>() { HeroIdEnum.ELIZAH };
            var rs = heroes.Where(hero => 
                                hero.isAlive() && 
                                hero.isFullMana() && 
                                !heroesPassiveSkill.Contains(hero.id)
                             ).ToList();
            return rs;
        }

        public List<Hero> getHeroesNotFullMana()
        {
            var rs = heroes.Where(hero =>
                                hero.isAlive() &&
                                !hero.isFullMana()
                             ).ToList();
            return rs;
        }

        public Hero getHeroMaxHp()
        {
            var rs  = heroes.OrderByDescending(x => x.hp).FirstOrDefault();

            return rs;
        }

        public int getTotalHp()
        {
            var rs = heroes.Where(hero =>
                    hero.isAlive()
                 ).Select(x => x.hp).Sum();

            return rs;
        }


        public Hero firstHeroAlive() {
            foreach(var hero in heroes){
                if (hero.isAlive()) return hero;
            }

            return null;
        }

        public HashSet<GemType> getRecommendGemType() {
            heroGemType.Clear();
            foreach(var hero in heroes){
                if (!hero.isAlive()) continue;
                if (hero.isFullMana()) continue;
                foreach(var gt in hero.gemTypes){
                    heroGemType.Add((GemType)gt);
                }
            }

            return heroGemType;
        }
    }
}