using UnityEngine;
using System.Collections.Generic;

public class giveMoneyExecuter : actionExecuter
{
    public void execute(ref gameInfo gameInfo, ref Package pkg, int power = 1, List<int> target = null, int index = 0) {
        foreach(int id in target) {
            gameInfo.playerList[id].money += power;
        }

        pkg.playerData =   gameInfo.playerList;
        NetworkMenager.sendingQueue.Enqueue(pkg);
    }
    public bool isNegative(int power) {
        if(power <0)
            return true;
        else
            return false;
    }
}
