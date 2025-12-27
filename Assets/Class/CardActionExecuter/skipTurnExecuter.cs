using UnityEngine;
using System.Collections.Generic;

public class skipTurnExecuter : actionExecuter
{
    public void execute(ref gameInfo gameInfo, ref Package pkg, int power = 1, List<int> target = null, int index = 0) {
        //廣播卡片發動, 並跳至下個玩家
        pkg.playerData = gameInfo.playerList;
        NetworkMenager.sendingQueue.Enqueue(pkg);
        pkg = new Package(-1, ACTION.NEXT_PLAYER, 0, 0, false, gameInfo.playerList); 
        packageUnpacker.pkgQueue.Enqueue(pkg);
    }
    public bool isNegative(int power) {
        return false;
    }
}
