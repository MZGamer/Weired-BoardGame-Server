using UnityEngine;
using System.Collections.Generic;
public class DeckManager
{
    public const int MAXCARD = 100;

    public static void cardRandom(List<cardInfo> needRandom, ref Stack<int> newDeck) {
        System.Random random = new System.Random();
        List<int> indexList = new List<int>();
        for (int i = 0; i < needRandom.Count; i++) {

            for (int k = 0; k < needRandom[i].cardCount; k++) {
                indexList.Add(needRandom[i].ID);
            }
        }
        newDeck = new Stack<int>();
        while (indexList.Count > 0) {
            int index = random.Next(indexList.Count);
            newDeck.Push(indexList[index]);
            indexList.RemoveAt(index);

        }
    }
    public static void resetActionCard(ref gameInfo game) {
        List<cardInfo> needRandom = new List<cardInfo>();
        for (int i = 0; i < game.cardList.actionCardsList.Count; i++) {
            needRandom.Add(game.cardList.actionCardsList[i].card);
        }
        cardRandom(needRandom, ref game.actionCardDeck);
    }
    public static void resetFateCard(ref gameInfo game) {
        List<cardInfo> needRandom = new List<Card>();
        for (int i = 0; i < game.cardList.fateCardsList.Count; i++) {
            needRandom.Add(game.cardList.fateCardsList[i].card);
        }
        cardRandom(needRandom, ref game.FateCardDeck);
    }
    public static void resetEventCard(ref gameInfo game) {
        List<cardInfo> needRandom = new List<Card>();
        for (int i = 0; i < game.cardList.eventCardList.Count; i++) {
            needRandom.Add(game.cardList.eventCardList[i].card);
        }
        cardRandom(needRandom, ref game.EventCardDeck);
    }
    public static cardInfo? searchCard(ref gameInfo gameInfo, int cardID) {
        int c = cardID / MAXCARD;
        switch (c) {
            case 1:
                return gameInfo.cardList.actionCardsList[cardID % MAXCARD].card;
            case 3:
                return gameInfo.cardList.fateCardsList[cardID % MAXCARD].card;
            case 4:
                return gameInfo.cardList.eventCardList[cardID % MAXCARD].card;
        }
        return null;
    }
}
