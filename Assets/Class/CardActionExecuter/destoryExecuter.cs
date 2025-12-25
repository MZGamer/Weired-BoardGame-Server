using UnityEngine;
using System.Collections.Generic;

public class destoryExecuter : actionExecuter {
    public Package execute(ref gameInfo gameInfo, int power = 0, List<int> target = null, int index = -1) {
        int targetPlayer = target[0];
        if (target == null)
            return new Package(-1, ACTION.DATA_UPDATE, 0, 0, false, gameInfo);

        if (targetPlayer == 5) {
            foreach(playerStatus player in gameInfo.playerList){
                foreach (Farm farm in player.farm) {
                    farm.resetFarm();
                }
            }

        } else {
            int targetFarm = target[1];
            gameInfo.playerList[targetPlayer].farm[targetFarm].resetFarm();
        }
        return new Package(-1, ACTION.DATA_UPDATE, 0, 0, false, gameInfo);
    }
}
