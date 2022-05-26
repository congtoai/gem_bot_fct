namespace bot
{
    public enum BattleMode
    {
        NORMAL
    }

    public enum GemType
    {
        NONE = -1,
        SWORD = 0,
        GREEN = 1,
        YELLOW = 2,
        RED = 3,
        PURPLE = 4,
        BLUE = 5,
        BROWN = 6
    }

    public enum HeroIdEnum
    {
        THUNDER_GOD = 0,
        MONK = 1,
        AIR_SPIRIT = 2,
        SEA_GOD = 3,
        MERMAID = 4,
        SEA_SPIRIT = 5,
        FIRE_SPIRIT = 6,
        CERBERUS = 7,
        DISPATER = 8,
        ELIZAH = 9,
        TALOS = 10,
        MONKEY = 11,
        GUTS = 12,

        SKELETON = 100,
        SPIDER = 101,
        WOLF = 102,
        BAT = 103,
        BERSERKER = 104,
        SNAKE = 105,
        GIANT_SNAKE = 106
    }

    public enum GemModifier
    {
        NONE = 0,
        MANA = 1,
        HIT_POINT = 2,
        BUFF_ATTACK = 3,
        //POINT = 4,
        EXTRA_TURN = 5,
        EXPLODE_HORIZONTAL = 6,
        EXPLODE_VERTICAL = 7,
        EXPLODE_SQUARE = 8
    }
}