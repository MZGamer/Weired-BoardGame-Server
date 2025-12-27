using UnityEngine;
using System.Collections.Generic;

public class growUPExecuter : actionExecuter {
    public void execute(ref gameInfo gameInfo, ref Package pkg, int power = 1, List<int> target = null, int index = 0) {
        foreach(int id in target) {
            var farmList = gameInfo.playerList[id].farm;
            for (int i = 0; i < farmList.Length; i++) {
                bool overflow = FarmManager.grow(ref farmList[i], power);
                if (overflow) {
                    FarmManager.resetFarm(ref farmList[i]);
                }
            }
        }
        pkg.playerData = gameInfo.playerList;
        NetworkMenager.sendingQueue.Enqueue(pkg); 
    }
    public bool isNegative(int power) {
        if (power < 0)
            return true;
        else
            return false;
    }
}
