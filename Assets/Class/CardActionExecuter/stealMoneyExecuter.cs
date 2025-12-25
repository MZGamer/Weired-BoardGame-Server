using UnityEngine;
using System.Collections.Generic;

public class stealMoneyExecuter:actionExecuter
{
    public Package execute(ref gameInfo gameInfo, int power = 1, List<int> target = null, int index = 0) {
        int steal = 0;
        List<int> targetList = new List<int>();
        if (target[0] == 5) {
            for(int i=0;i< gameInfo.playerList.Length; i++) {
                if (gameInfo.playerList[i].name != "Disconnected") {
                    targetList.Add(i);
                }
            }
        } else {
            targetList.Add(target[0]);
        }
        for (int i = 0; i < gameInfo.playerList.Length; i++) {
            if (i == index || GuardedChk(pkg, i) || gameInfo.playerList[i]) {
                continue;
            }
            if (gameInfo.playerList[i].money < power) {
                steal += gameInfo.playerList[i].money;
                gameInfo.playerList[i].money = 0;
            } else {
                steal += power;
                gameInfo.playerList[i].money -= power;
            }
        }
        return new Package(-1, ACTION.DATA_UPDATE, 0, 0, false, gameInfo);
    }
}
