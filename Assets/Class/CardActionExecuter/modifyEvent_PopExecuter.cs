using UnityEngine;
using System.Collections.Generic;

public class modifyEvent_PopExecuter: actionExecuter
{
    public void execute(ref gameInfo gameInfo, ref Package pkg, int power = 1, List<int> target = null, int index = 0) {
        List<int> card = new List<int>();
        for (int i = 0; i < power; i++) {
            if (gameInfo.EventCardDeck.Count == 0) {
                DeckManager.resetEventCard(ref gameInfo);
            }
            card.Add(gameInfo.EventCardDeck.Pop());
        }
        pkg.target = card;
        pkg.playerData = gameInfo.playerList;
        NetworkMenager.sendingQueue.Enqueue(pkg);
    }
    public bool isNegative(int power) {
            return false;
    }
}
