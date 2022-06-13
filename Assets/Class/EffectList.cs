using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 

[System.Serializable]
public class EffectDes {
    public EFFECT_ID effectID;
    public string effectName;
    [Multiline(5)]
    public string effectDescription;

}

[CreateAssetMenu(fileName = "newEffectList", menuName = "Create newList/newEffectList", order = 2)]
public class EffectList : ScriptableObject {
    public List<Effect_Des_Temp> effectDesList;

}
