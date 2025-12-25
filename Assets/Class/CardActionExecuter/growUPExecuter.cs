using UnityEngine;
using System.Collections.Generic;

public class growUPExecuter : actionExecuter {
    public Package execute(ref gameInfo gameInfo, int power = 1, List<int> target = null, int index = 0) {
        int targetPlayer = target[0];
        foreach(Farm farm in gameInfo.playerList[targetPlayer].farm) {
            bool overflow =  farm.grow(power);
            if (overflow) {
                farm.resetFarm();
            }
        }
        return new Package(-1, ACTION.DATA_UPDATE,0,0,false,gameInfo);
    }
}
