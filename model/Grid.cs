
using Sfs2X.Entities.Data;
using System.Linq;

namespace bot {
    public class Grid
    {
        private List<Gem> gems = new List<Gem>();
        private ISFSArray gemsCode;
        public HashSet<GemType> gemTypes = new HashSet<GemType>();
        public List<Hero> myheroes;
        public List<Hero> enemyheroes;
        public List<GemModifier> listGemModifier = new List<GemModifier>() 
        { 
            GemModifier.MANA,
            GemModifier.HIT_POINT,
            GemModifier.BUFF_ATTACK,
            GemModifier.EXTRA_TURN, 
            GemModifier.EXPLODE_HORIZONTAL,
            GemModifier.EXPLODE_VERTICAL,
            GemModifier.EXPLODE_SQUARE,
        };

        public Grid(ISFSArray gemsCode,ISFSArray gemModifiers, List<Hero> myheroes, List<Hero> enemyheroes)
        {
            updateGems(gemsCode, gemModifiers);
            this.myheroes = myheroes;
            this.enemyheroes = enemyheroes;
        }

        public void updateGems(ISFSArray gemsCode, ISFSArray gemModifiers) {
            gems.Clear();
            gemTypes.Clear();
            for (int i = 0; i < gemsCode.Size(); i++) {
                Gem gem = new Gem(i, (GemType)gemsCode.GetByte(i) , gemModifiers != null ? (GemModifier)gemModifiers.GetByte(i) : GemModifier.NONE);
                gems.Add(gem);
                gemTypes.Add(gem.type);
            }
        }

        public Pair<int> recommendSwapGem() {
            List<GemSwapInfo> listMatchGem = suggestMatch();
            //get gemtype myheros
            HashSet<GemType> myHeroGemType = new HashSet<GemType>();
            var myHeroAlive = myheroes.Where(x => x.isAlive() && !x.isFullMana());//.Reverse();
            foreach (var hero in myHeroAlive)
            {
                foreach (var gt in hero.gemTypes)
                {
                    myHeroGemType.Add((GemType)gt);
                }
            }
            //get gemtype enemyheros
            HashSet<GemType> enemyHeroGemType = new HashSet<GemType>();
            var enemyHeroAlive = enemyheroes.Where(x => x.isAlive()).OrderBy(x => x.isFullMana());
            foreach (var hero in enemyHeroAlive)
            {
                foreach (var gt in hero.gemTypes)
                {
                    enemyHeroGemType.Add((GemType)gt);
                }
            }

            if (listMatchGem.Count == 0) {
                return new Pair<int>(-1, -1);
            }

            GemSwapInfo haveFiveGemType = listMatchGem.Where(gemMatch => myHeroGemType.Contains(gemMatch.type) && gemMatch.sizeMatch > 4).OrderByDescending(x => x.hasGemModifier).FirstOrDefault();
            GemSwapInfo gemModifier = listMatchGem.Where(gemMatch => myHeroGemType.Contains(gemMatch.type) && gemMatch.hasGemModifier).OrderByDescending(x => x.sizeMatch).FirstOrDefault();

            GemSwapInfo maxGemType = listMatchGem.Where(gemMatch => myHeroGemType.Contains(gemMatch.type)).OrderByDescending(x => x.sizeMatch).FirstOrDefault();

            Console.WriteLine("maxGemType.sizeMatch" + maxGemType?.sizeMatch);
            Console.WriteLine("gemModifier" + gemModifier?.type);

            GemSwapInfo maxSword = listMatchGem.Where(gemMatch => gemMatch.type == GemType.SWORD).OrderByDescending(x => x.sizeMatch).FirstOrDefault();

            GemSwapInfo maxEnemyGemType = listMatchGem.Where(gemMatch => enemyHeroGemType.Contains(gemMatch.type)).OrderByDescending(x => x.sizeMatch).ThenByDescending(x => x.hasGemModifier).FirstOrDefault();

            if (maxSword?.sizeMatch > 4)
            {
                return maxSword.getIndexSwapGem();
            }
            if (haveFiveGemType != null)
            {
                return haveFiveGemType.getIndexSwapGem();
            }
            if (myheroes.Where(x => x.isAlive()).FirstOrDefault()?.attack >= enemyheroes.Where(x => x.isAlive()).FirstOrDefault()?.hp | 
                myheroes.Where(x => x.isAlive()).FirstOrDefault()?.hp <= enemyheroes.Where(x => x.isAlive()).FirstOrDefault()?.attack)
            {
                if (maxSword != null)
                {
                    return maxSword.getIndexSwapGem();
                }
            }
            if (gemModifier != null)
            {
                return gemModifier.getIndexSwapGem();
            }
            if (maxSword != null && maxSword?.sizeMatch > 3)
            {
                return maxSword.getIndexSwapGem();
            }
            if (maxGemType != null)
            {
                return maxGemType.getIndexSwapGem();
            }
            if (maxEnemyGemType != null)
            {
                return maxEnemyGemType.getIndexSwapGem();
            }
            if (maxSword != null)
            {
                return maxSword.getIndexSwapGem();
            }
            return listMatchGem[0].getIndexSwapGem();
        }

        public int? getIndexGem(HashSet<GemType> gemTypes)
        {
            var tempGems = new List<Gem>(gems);

            var ls = new List<int>() { -1, 1, 7, -7, 8, -8, 9, -9 };
            var lsObjs = new List<GemWithIndex>();

            var x = 8;
            var max = 64;
            for (int i = 0; i < max; i++)
            {
                if (i <= x | i % x == 0 | (i + 1) % x == 0 | i + x >= max)
                {
                    continue;
                }

                var countSword = tempGems[i].type == GemType.SWORD ? 1 : 0;
                var gemModifier = listGemModifier.Contains(tempGems[i].modifier) ? 1 : 0;
                var lsType = new List<GemType>() { };
                foreach (var item in ls)
                {
                    lsType.Add((GemType)tempGems[i + item].type);
                    if (tempGems[i + item].type == GemType.SWORD)
                    {
                        countSword++;
                    }
                    if (listGemModifier.Contains(tempGems[i + item].modifier))
                    {
                        gemModifier++;
                    }
                }
                var isBonusTurn = lsType.GroupBy(x => x).Select(x => new { key = x.Key, value = x.Count() }).Any(x => gemTypes.Contains(x.key) && x.value >= 5);
                var obj = new GemWithIndex
                {
                    index = i,
                    countSword = countSword,
                    gemModifier = gemModifier,
                    isBonusTurn = isBonusTurn
                };
                lsObjs.Add(obj);
            }

            var maxSword = lsObjs.Max(x => x.countSword);
            var index = lsObjs.Where(x => x.countSword >= 3 && x.countSword == maxSword).OrderByDescending(x => x.gemModifier).FirstOrDefault()?.index;
            if (index == null)
            {
                index = lsObjs.Where(x => x.gemModifier > 0).OrderByDescending(x => x.gemModifier).ThenBy(x => x.countSword).FirstOrDefault()?.index;
            }
            if (index == null)
            {
                index = lsObjs.Where(x => x.isBonusTurn).FirstOrDefault()?.index;
            }

            Console.WriteLine("index gems:------------------------------------------------------------------- " + index);
            return index;
        }

        private List<GemSwapInfo> suggestMatch() {
            var listMatchGem = new List<GemSwapInfo>();

            var tempGems = new List<Gem>(gems);
            foreach (Gem currentGem in tempGems) {
                Gem swapGem = null;
                // If x > 0 => swap left & check
                if (currentGem.x > 0) {
                    swapGem = gems[getGemIndexAt(currentGem.x - 1, currentGem.y)];
                    checkMatchSwapGem(listMatchGem, currentGem, swapGem);
                }
                // If x < 7 => swap right & check
                if (currentGem.x < 7) {
                    swapGem = gems[getGemIndexAt(currentGem.x + 1, currentGem.y)];
                    checkMatchSwapGem(listMatchGem, currentGem, swapGem);
                }
                // If y < 7 => swap up & check
                if (currentGem.y < 7) {
                    swapGem = gems[getGemIndexAt(currentGem.x, currentGem.y + 1)];
                    checkMatchSwapGem(listMatchGem, currentGem, swapGem);
                }
                // If y > 0 => swap down & check
                if (currentGem.y > 0) {
                    swapGem = gems[getGemIndexAt(currentGem.x, currentGem.y - 1)];
                    checkMatchSwapGem(listMatchGem, currentGem, swapGem);
                }
            }
            return listMatchGem;
        }

        private void checkMatchSwapGem(List<GemSwapInfo> listMatchGem, Gem currentGem, Gem swapGem) {
            swap(currentGem, swapGem);
            HashSet<Gem> matchGems = matchesAt(currentGem.x, currentGem.y);

            swap(currentGem, swapGem);
            if (matchGems.Count > 0) {
                listMatchGem.Add(new GemSwapInfo(currentGem.index, swapGem.index, matchGems.Count, currentGem.type, 
                    (listGemModifier.Contains(currentGem.modifier) | listGemModifier.Contains(swapGem.modifier))
                    ));
            }
        }

        private int getGemIndexAt(int x, int y) {
            return x + y * 8;
        }

        private void swap(Gem a, Gem b) {
            int tempIndex = a.index;
            int tempX = a.x;
            int tempY = a.y;

            // update reference
            gems[a.index] = b;
            gems[b.index] = a;

            // update data of element
            a.index = b.index;
            a.x = b.x;
            a.y = b.y;

            b.index = tempIndex;
            b.x = tempX;
            b.y = tempY;
        }

        private HashSet<Gem> matchesAt(int x, int y) {
            HashSet<Gem> res = new HashSet<Gem>();
            Gem center = gemAt(x, y);
            if (center == null) {
                return res;
            }

            // check horizontally
            List<Gem> hor = new List<Gem>();
            hor.Add(center);
            int xLeft = x - 1, xRight = x + 1;
            while (xLeft >= 0) {
                Gem gemLeft = gemAt(xLeft, y);
                if (gemLeft != null) {
                    if (!gemLeft.sameType(center)) {
                        break;
                    }
                    hor.Add(gemLeft);
                }
                xLeft--;
            }
            while (xRight < 8) {
                Gem gemRight = gemAt(xRight, y);
                if (gemRight != null) {
                    if (!gemRight.sameType(center)) {
                        break;
                    }
                    hor.Add(gemRight);
                }
                xRight++;
            }
            if (hor.Count >= 3) res.UnionWith(hor);

            // check vertically
            List<Gem> ver = new List<Gem>();
            ver.Add(center);
            int yBelow = y - 1, yAbove = y + 1;
            while (yBelow >= 0) {
                Gem gemBelow = gemAt(x, yBelow);
                if (gemBelow != null) {
                    if (!gemBelow.sameType(center)) {
                        break;
                    }
                    ver.Add(gemBelow);
                }
                yBelow--;
            }
            while (yAbove < 8) {
                Gem gemAbove = gemAt(x, yAbove);
                if (gemAbove != null) {
                    if (!gemAbove.sameType(center)) {
                        break;
                    }
                    ver.Add(gemAbove);
                }
                yAbove++;
            }
            if (ver.Count >= 3) res.UnionWith(ver);

            return res;
        }

        // Find Gem at Position (x, y)
        private Gem gemAt(int x, int y) {
            foreach (Gem g in gems) {
                if (g != null && g.x == x && g.y == y) {
                    return g;
                }
            }
            return null;
        }
    }

    public class GemWithIndex
    {
        public int? index { get; set; }
        public int countSword { get; set; }
        public int gemModifier { get; set; }
        public bool isBonusTurn { get; set; }
    }
}

