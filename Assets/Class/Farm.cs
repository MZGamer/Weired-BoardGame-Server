using System.Collections.Generic;
using UnityEngine;

public class Farm {
    private int corpCardID;
    private corpCardInfo corpInfo;
    private List<int> reward;
    private int turn;

    public Farm() {
        corpCardID = -1;
        corpInfo = null;
        turn = 0;
    }

    public int getReward() {
        return corpInfo.reward[turn];
    }
    public void plant(int corpCardID, corpCardInfo plantedCorp) {
        turn = 0;
        this.corpCardID = corpCardID;
        corpInfo = plantedCorp;
    }
    public bool grow() {
        turn++;
        if (turn >= 5) {
            return true;
        }
        return false;
    }

    public bool timeBack() {
        turn--;
        if (turn < 0) {
            return true;
        }
        return false;
    }
    public int getTurn() {
        return turn;
    }
    public void resetFarm() {
        corpCardID = -1;
        corpInfo = null;
        turn = 0;
    }
}