using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct playerStatus
{
    public string name;
    public farmInfo[] farm;
    public List<int> handCard;
    public int money;
    public int[] effect;
}
