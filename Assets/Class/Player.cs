using System;
using System.Collections.Generic;
using Unity.Android.Gradle.Manifest;
using UnityEngine;

public class Player
{
    public playerStatus status;

    public Player() {
        status.farm = new Farm[4];
        status.handCard = new List<int>();
        status.money = 20;
        status.effect = new int[Enum.GetNames(typeof(EFFECT_ID)).Length];
    }
    public void Harvest(int farmIndex) {
        status.money += status.farm[farmIndex].getReward() + status.effect[(int)EFFECT_ID.HARVEST_RATIO];
        status.farm[farmIndex].resetFarm();
    }
}
