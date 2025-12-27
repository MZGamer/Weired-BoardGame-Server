using UnityEngine;
using System.Collections.Generic;

public class stealMoneyExecuter:actionExecuter
{
    public void execute(ref gameInfo gameInfo, ref Package pkg, int power = 1, List<int> target = null, int index = 0) {
        int steal = 0;
        foreach(int id in target) {
            if (gameInfo.playerList[id].money < power) {
                steal += gameInfo.playerList[id].money;
                gameInfo.playerList[id].money = 0;
            } else {
                steal += power;
                gameInfo.playerList[id].money -= power;
            }
        }
        pkg.playerData = gameInfo.playerList;
        NetworkMenager.sendingQueue.Enqueue(pkg);
    }
    public bool isNegative(int power) {
        return true;
    }
}
