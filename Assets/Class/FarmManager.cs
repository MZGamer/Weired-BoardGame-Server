using UnityEngine;

public class FarmManager
{
    public static int getReward(ref gameInfo gameInfo, ref farmInfo farm) {
        corpCard corp = (corpCard)DeckManager.searchCard(ref gameInfo, farm.corpCardID);
        if(corp != null)
            return corp.corpInfo.reward[farm.turn];
        return 0;
    }
    public static void plant(ref farmInfo farm, int corpCardID) {
        farm.turn = 0;
        farm.corpCardID = corpCardID;
    }
    public static bool grow(ref farmInfo farm,  int power = 1) {
        if (farm.corpCardID == -1) {
            return false;
        }
        farm.turn += power;
        if (farm.turn >= 5 || farm.turn < 0) {
            return true;
        }
        return false;
    }
    public static int getTurn(ref farmInfo farm) {
        return farm.turn;
    }
    public static void resetFarm(ref farmInfo farm) {
        farm.corpCardID = -1;
        farm.turn = 0;
    }
}
