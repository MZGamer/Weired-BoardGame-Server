using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ACTION {
    NULL,
    GAMESTART,
    PLAYER_JOIN,
    NEW_TURN,
    NEXT_PLAYER,
    CARD_ACTIVE,
    DATA_UPDATE,
    ROLL_POINT,
    ASSIGN_PLAYER_ID,
    GET_NEW_CARD,
    HARVEST,
    PLAYER_DISCONNECTED,
    GAMEOVER
}
public class Package {
    public int src;
    public ACTION ACTION;
    public int index;
    public List<int> target;
    public playerStatus[] playerData;
    public bool askCounter;

    public Package(int src, ACTION ACTION = ACTION.NULL, int index = -1, List<int> target = null, bool askCounter = false, playerStatus[] playerData = null) {
        this.src = src;
        this.ACTION = ACTION;
        this.playerData = playerData;
        this.index = index;
        this.target = target;
        this.askCounter = askCounter;
    }
    public Package(int src, ACTION ACTION = ACTION.NULL, int index = -1, int target = 0, bool askCounter = false, playerStatus[] playerData = null) {
        this.src = src;
        this.ACTION = ACTION;
        this.playerData = playerData;
        this.index = index;
        this.target = new List<int>();
        this.target.Add(target);
        this.askCounter = askCounter;
    }
}
