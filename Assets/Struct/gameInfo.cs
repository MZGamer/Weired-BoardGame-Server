using System.Collections.Generic;
using UnityEngine;
using System;

public struct gameInfo {
    public playerStatus[] playerList;
    public CardList cardList;
    public EventCard eventCard;
    public Stack<int> actionCardDeck;
    public Stack<int> FateCardDeck;
    public Stack<int> EventCardDeck;
    public List<Card> usedActionCardDeck;
}
