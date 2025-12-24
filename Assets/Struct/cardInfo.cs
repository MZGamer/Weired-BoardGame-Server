using UnityEngine;

public struct cardInfo
{
    public string Name;
    [Multiline(5)]
    public string description;
    public Sprite cardImg;

    [Header("1xx:機會 2xx:作物 3xx:命運 4xx:特效")]
    public int ID;
    public int cardCount;
}
