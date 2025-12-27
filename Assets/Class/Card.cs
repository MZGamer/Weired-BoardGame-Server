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

public class cardData {
    public cardInfo cardinfo;
}

[System.Serializable]
public class corpCard : cardData {
    public corpCardInfo corpInfo;
}

public class powerCard : cardData {
    public powerCardInfo powerInfo;

}

[System.Serializable]
public class actionCard : powerCard {

}

[System.Serializable]
public class FateCard : powerCard {
    public fateCardInfo fatecardinfo;
}



[System.Serializable]
public class EventCard : powerCard {
}
