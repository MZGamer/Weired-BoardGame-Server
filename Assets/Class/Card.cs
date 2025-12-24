//using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public enum CARD_ACTION {
    NONE,
    SKIP_TURN,
    GROWUP,
    GIVE_MONEY,
    DESTROY,
    DEFEND,
    SPECIALEFFECT,
    MODIFY_EVENT,
    STEAL_MONEY
}

public enum EFFECT_ID {
    NONE,
    BILL_RATIO,
    HARVEST_RATIO,
    CLOSE_SHOP,
    GUARD
}


public enum SELECT_TYPE {
    NONE,
    PLAYER,
    FARM,
    CARD
}

[System.Serializable]
public struct select {
    public SELECT_TYPE type;
    public int targetCount;
}


[System.Serializable]
public struct roll {
    public CARD_ACTION Action;
    public EFFECT_ID effectType;

    public int min;
    public int max;
    public int power;
}

public class Card {
    public string Name;
    [Multiline(5)]
    public string description;
    public Sprite cardImg;

    [Header("1xx:機會 2xx:作物 3xx:命運 4xx:特效")]
    public int ID;
    public int cardCount;
}

[System.Serializable]
public class corpCard : Card {
    public List<int> reward;
    public int turn;

    //收成
    public int getReward() {
        return reward[turn];
    }
    public void plant() {
        turn = 0;
    }
    public bool grow() {
        turn++;
        if (turn >=5) {
            return true;
        }
        return false;
    }

    public bool timeBack() {
        turn--;
        if(turn < 0) {
            return true;
        }
        return false;
    }
    public int getTurn() {
        return turn;
    }

    public corpCard seed() {
        corpCard seed = new corpCard();
        seed.ID = this.ID;
        seed.Name = this.Name;
        seed.cardImg = this.cardImg;
        seed.description = this.description;
        seed.reward = this.reward;

        return seed;
    }
}

public class powerCard : Card {
    public CARD_ACTION Action;
    public int actionPower;
    public EFFECT_ID effect;
    [Header("-2 : 自己, -1 : 需指定 , 5 : 全場")]
    public int target;
    public select selectType;

}

[System.Serializable]
public class actionCard : powerCard {


}

[System.Serializable]
public class FateCard : powerCard {
    public List<roll> rools;
}



[System.Serializable]
public class EventCard : powerCard {

}
