using UnityEngine;
using System.Collections.Generic;

public class noActionExecuter : actionExecuter
{
    public void execute(ref gameInfo gameInfo, ref Package pkg, int power = 0, List<int> target = null, int index = -1) {
        pkg.playerData = gameInfo.playerList;
        NetworkMenager.sendingQueue.Enqueue(pkg);
    }
    public bool isNegative(int power) {
        return false;
    }
}
