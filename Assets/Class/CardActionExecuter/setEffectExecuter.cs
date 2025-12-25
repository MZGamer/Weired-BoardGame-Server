using UnityEngine;
using System.Collections.Generic;
public class setEffectExecuter:actionExecuter
{
    public Package execute(ref gameInfo gameInfo, int power = 1, List<int> target = null, int index = 0) {
        if (target[0] == 5) {
            foreach (playerStatus player in gameInfo.playerList){
                player.effect[(int)index] += power;
            }
        } else {
            gameInfo.playerList[target[0]].effect[(int)index] += power;
        }
        return new Package(-1, ACTION.DATA_UPDATE, 0, 0, false, gameInfo);
    }
}
