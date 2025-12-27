using UnityEngine;
using System.Collections.Generic;

public class destoryExecuter : actionExecuter {
    public void execute(ref gameInfo gameInfo, ref Package pkg, int power = 0, List<int> target = null, int index = -1) {
        if(target.Count == 2) {
            FarmManager.resetFarm(ref gameInfo.playerList[target[0]].farm[target[1]]);
        } else {
            foreach (int id in target) {
                if (index == -1) {
                    FarmManager.resetFarm(ref gameInfo.playerList[id].farm[index]);
                } else {
                    var farmList = gameInfo.playerList[id].farm;
                    for (int i = 0; i < farmList.Length; i++) {
                        bool overflow = FarmManager.grow(ref farmList[i], power);
                        if (overflow) {
                            FarmManager.resetFarm(ref farmList[i]);
                        }
                    }
                }
            }
        }
        pkg.playerData = gameInfo.playerList;
        NetworkMenager.sendingQueue.Enqueue(pkg);
    }

    public bool isNegative(int power) {
        return true;
    }
}
