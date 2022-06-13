using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "newCardList", menuName = "Create newList/CardList", order = 2)]
public class CardList : ScriptableObject {
    public List<Action_Card_Temp> actionCardsList;
    public List<Corp_Card_Temp> corpCardsList;
    public List<Fate_Card_Temp> fateCardsList;
    public List<Event_Card_Temp> eventCardList;
}