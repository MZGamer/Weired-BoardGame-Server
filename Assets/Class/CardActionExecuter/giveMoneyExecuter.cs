using UnityEngine;
using System.Collections.Generic;

public class giveMoneyExecuter : actionExecuter
{
    public Package execute(ref gameInfo gameInfo, int power = 1, List<int> target = null, int index = 0) {
        int targetPlayer = target[0];
        gameInfo.playerList[targetPlayer].money += power;
        return new Package(-1, ACTION.DATA_UPDATE, 0, 0, false, gameInfo);
    }
}
