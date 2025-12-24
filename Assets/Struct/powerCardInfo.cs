using UnityEngine;

public struct powerCardInfo
{
    public CARD_ACTION Action;
    public int actionPower;
    public EFFECT_ID effect;
    [Header("-2 : 自己, -1 : 需指定 , 5 : 全場")]
    public int target;
    public select selectType;
}
