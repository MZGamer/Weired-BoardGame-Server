using UnityEngine;
using System.Collections.Generic;

public class modifyEvent_PopExecuter: actionExecuter
{
    public Package execute(ref gameInfo gameInfo, int power = 1, List<int> target = null, int index = 0) {
        List<int> card = new List<int>();
        for (int i = 0; i < power; i++) {
            if (gameInfo.EventCardDeck.Count == 0) {
                DeckManager.resetEventCard(ref gameInfo);
            }
            card.Add(gameInfo.EventCardDeck.Pop());
        }
        return new Package(-1, ACTION.DATA_UPDATE, 0, 0, false, gameInfo);
    }
}
