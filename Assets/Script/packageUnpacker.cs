using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
 
 
public class packageUnpacker : MonoBehaviour
{
    const int MAXCARD = 100;

    public Card card = new Card();
    public CardList cardList;
    public Package package;
    public EventCard eventCard;
    public List<PlayerStatus> player = new List<PlayerStatus>();
    public List<Package> pkgQueueView = new List<Package>();
    public static Queue<Package> pkgQueue = new Queue<Package>();
    public int turn = 0;

    public bool askForCounter = false;
    public bool modifyEventCard = false;
    public Package waitForCounter; 
    public Package waitForRoll;

    public Stack<int> actionCardDeck = new Stack<int>();
    public Stack<int> FateCardDeck = new Stack<int>();
    public Stack<int> EventCardDeck = new Stack<int>();
    public List<Card> usedActionCardDeck = new List<Card>();

    public PlayerStatus DisconnectPlayer;

    void cardRandom(List<Card> needRandom,ref Stack<int> newDeck)
    {
        System.Random random = new System.Random();
        List<int> indexList = new List<int>();
        for (int i = 0; i < needRandom.Count; i++)
        {

            for(int k=0; k< needRandom[i].cardCount ;k++ ) {
                indexList.Add(needRandom[i].ID);
            }
        }
        newDeck = new Stack<int>();
        while(indexList.Count > 0) {
            int index = random.Next(indexList.Count);
            newDeck.Push( indexList[index]);
            indexList.RemoveAt(index);

        }
    }

    void resetActionCard() {
        List<Card> needRandom = new List<Card>();
        for (int i = 0;i < cardList.actionCardsList.Count;i++) {
            needRandom.Add(cardList.actionCardsList[i].card);
        }
        cardRandom(needRandom, ref actionCardDeck);
    }
    void resetFateCard() {
        List<Card> needRandom = new List<Card>();
        for (int i = 0; i < cardList.fateCardsList.Count; i++) {
            needRandom.Add(cardList.fateCardsList[i].card);
        }
        cardRandom(needRandom, ref FateCardDeck);
    }
    void resetEventCard() {
        List<Card> needRandom = new List<Card>();
        for (int i = 0; i < cardList.eventCardList.Count; i++) {
            needRandom.Add(cardList.eventCardList[i].card);
        }
        cardRandom(needRandom, ref EventCardDeck);
    }

    // Start is called before the first frame update
    void Start()
    {
        resetActionCard();
        resetFateCard();
        resetEventCard();
        /*
        int firstCard;
        firstCard = EventCardDeck.Pop();
        eventCard = cardList.eventCardList[firstCard % 100].card;*/
        //listen(4);
    }

    // Update is called once per frame
    void Update()
    {
        if (pkgQueue.Count > 0)
            package = pkgQueue.Dequeue();
        else
            return;
        switch (package.ACTION)
        {
            case ACTION.PLAYER_DISCONNECTED:
                if(!NetworkMenager.gameStart) {
                    Debug.Log(package.index);
                    player[package.index] = DisconnectPlayer;
                    package.playerStatuses = player;
                    NetworkMenager.sendingQueue.Enqueue(package);
                } else {
                    player[package.index] = DisconnectPlayer;
                    package.playerStatuses = player;
                    NetworkMenager.sendingQueue.Enqueue(package);
                    if(NetworkMenager.gameStart && turn % player.Count == package.index) {
                        pkgQueue.Enqueue(new Package(-1, ACTION.NEXT_PLAYER, 0, 0, false, player));
                    }
                }
                break;
            //暫時用來表示不使用反擊卡
            case ACTION.NULL:
                //重新處理卡片效果
                waitForCounter.askCounter = false;
                waitForCounter.playerStatuses = player;
                pkgQueue.Enqueue(waitForCounter);
                waitForCounter = null;
                break;
            case ACTION.PLAYER_JOIN:
                while(package.src >= player.Count) {
                    player.Add(null);
                }
                player[package.src] = package.playerStatuses[0];
                Package pkg = new Package(-1, ACTION.PLAYER_JOIN, 0, 0, false, player);
                NetworkMenager.sendingQueue.Enqueue(pkg);
                break;
            case ACTION.GAMESTART:
                pkg = new Package(-1, ACTION.GAMESTART, 0, 0, false, player);
                NetworkMenager.gameStart = true;
                NetworkMenager.sendingQueue.Enqueue(pkg);

                int nextCard = EventCardDeck.Pop();
                powerCard eventCard = searchCard(nextCard);
                pkg = new Package(-1, ACTION.CARD_ACTIVE, nextCard, 5, false, player);
                cardActionExecute(pkg, eventCard.Action, eventCard.actionPower, eventCard.effect);

                nextCard = FateCardDeck.Pop();
                pkg = new Package(-1, ACTION.ROLL_POINT, turn % player.Count, nextCard);
                NetworkMenager.sendingQueue.Enqueue(pkg);
                waitForRoll = pkg;

                break;
            case ACTION.NEXT_PLAYER:
                turn++;
                nextCard = 0;
                //如果turn數到達，開啟一個新Turn
                if (turn != 0 && turn % player.Count == 0)
                {
                    //將每人個別稅率放入Target回傳(因為我懶)
                    List<int> bill = new List<int>();
                    int disPlayerCount = 0;
                    for (int i = 0; i < player.Count; i++)
                    {
                        if (player[i].name == "Disconnected") {
                            disPlayerCount++;
                            bill.Add(0);
                            continue;
                        }
                            
                        //計算稅率
                        int billMoney = 2 + player[i].effect[(int)EFFECT_ID.BILL_RATIO];
                        bill.Add(billMoney);
                        player[i].money -= billMoney;

                        //reset effect array
                        player[i].effect = new int[Enum.GetNames(typeof(EFFECT_ID)).Length];

                        //作物生長
                        for (int k = 0; k < 4; k++)
                        {
                            if (player[i].farm[k].ID != 0)
                            {
                                bool overGrow = player[i].farm[k].grow();
                                if (overGrow)
                                    player[i].farm[k] = new corpCard();
                            }
                        }
                        
                    }
                    if (disPlayerCount == player.Count) {
                        NetworkMenager.gameOver = true;
                        return;
                    }
                        
                    if (EventCardDeck.Count == 0)
                    {
                        resetEventCard();
                    }

                    //通知clinet更新GUI(扣稅，農田增長)
                    pkg = new Package(-1, ACTION.NEW_TURN, 0, bill, false, player);
                    NetworkMenager.sendingQueue.Enqueue(pkg);
                    gameOverChk();

                    //發布新的EventCard
                    nextCard = EventCardDeck.Pop();
                    eventCard = searchCard(nextCard);
                    pkg = new Package(-1, ACTION.CARD_ACTIVE, nextCard, 5, false, player);
                    cardActionExecute(pkg, eventCard.Action, eventCard.actionPower, eventCard.effect);
                }

                //通知Clinet更新GUI提示輪到下個玩家
                pkg = new Package(-1, ACTION.NEXT_PLAYER, 0, 0, false, player);
                NetworkMenager.sendingQueue.Enqueue(pkg);

                if (player[turn % player.Count].name == "Disconnected") {
                    pkgQueue.Enqueue(new Package(-1, ACTION.NEXT_PLAYER, 0, 0, false, player));
                    return;
                }

                //發布新的FateCard 並等待Roll點
                if (FateCardDeck.Count == 0) {
                    resetFateCard();
                }
                nextCard = FateCardDeck.Pop();
                pkg = new Package(-1, ACTION.ROLL_POINT, turn % player.Count, nextCard);
                NetworkMenager.sendingQueue.Enqueue(pkg);
                waitForRoll = pkg;
                break;
            //收到玩家對FateCard的Roll點
            case ACTION.ROLL_POINT:
                FateCard fate = cardList.fateCardsList[package.target[0] % 100].card;
                roll rollSelect = fate.rools.Find(x => package.index >= x.min && package.index <= x.max);
                pkg = new Package(-1, ACTION.CARD_ACTIVE, waitForRoll.target[0],new List<int> { waitForRoll.index, package.index});
                cardActionExecute(pkg, rollSelect.Action, rollSelect.power, rollSelect.effectType);

                break;
            case ACTION.CARD_ACTIVE:
                if(package.index /MAXCARD == 1) {
                    usedActionCardDeck.Add(cardList.actionCardsList[package.index% MAXCARD].card);
                }
                switch (package.index / MAXCARD) {
                    case 2: // Corp Card
                        corpCard corp = cardList.corpCardsList[package.index % 100].card.seed();
                        corp.plant();
                        player[package.src].farm[package.target[0]] = corp;
                        player[package.src].money -= 1;
                        package.playerStatuses = player;
                        NetworkMenager.sendingQueue.Enqueue(package);
                        gameOverChk();
                        break;
                    default:

                        player[package.src].handCard.Remove(package.index);
                        powerCard card = searchCard(package.index);
                        cardActionExecute(package, card.Action, card.actionPower, card.effect);

                        break;

                }
                break;

            case ACTION.GET_NEW_CARD:
                turn = turn % player.Count;
                int actionCard = 0 ;
                if (turn == package.src)
                {
                    if (actionCardDeck.Count == 0)
                    {
                        cardRandom(usedActionCardDeck, ref actionCardDeck);
                    }
                    if(player[package.src].handCard.Count <= 5) {
                        actionCard = actionCardDeck.Pop();
                        player[package.src].handCard.Add(actionCard);
                        player[package.src].money -= 2;
                    }

                }
                pkg = new Package(-1, ACTION.GET_NEW_CARD, actionCard, package.src, false, player);
                NetworkMenager.sendingQueue.Enqueue(pkg);
                break;

            case ACTION.HARVEST:
                turn = turn % player.Count;
                if(turn == package.src)
                {
                    player[package.src].money += player[package.src].farm[package.target[0]].getReward() + player[package.src].effect[(int)EFFECT_ID.HARVEST_RATIO];
                    player[package.src].farm[package.target[0]] = new corpCard();
                }
                pkg = package;
                pkg.playerStatuses = player;
                NetworkMenager.sendingQueue.Enqueue(pkg);
                gameOverChk();
                break;
        }
    }

    void gameOverChk() {
        for(int i=0;i<player.Count;i++) {
            if (player[i].name == "Disconnected") {
                continue;
            }
            if (player[i].money >= 40 || player[i].money < 0) {
                Package pkg = new Package(-1, ACTION.GAMEOVER, 0, 0);
                NetworkMenager.sendingQueue.Enqueue(pkg);
                NetworkMenager.gameOver = true;
                break;
            }
        }

    }

    void cardActionExecute(Package pkg, CARD_ACTION cardAction, int power, EFFECT_ID effect = EFFECT_ID.NONE) {
        switch (cardAction) {
            case CARD_ACTION.NONE:
                pkg.playerStatuses = player;
                NetworkMenager.sendingQueue.Enqueue(pkg);
                break;
            case CARD_ACTION.SKIP_TURN:
                //通知Clinet更新GUI提示輪到下個玩家
                pkg.playerStatuses = player;
                NetworkMenager.sendingQueue.Enqueue(pkg);
                pkg = new Package(-1, ACTION.NEXT_PLAYER, 0, 0, false, player); //還沒改
                packageUnpacker.pkgQueue.Enqueue(pkg);
                break;
            case CARD_ACTION.GROWUP
            :
                if (power < 0) {
                    if (GuardedChk(pkg, pkg.target[0]))
                        return;
                    if (canAskCounter(pkg)) {
                        return;
                    }

                }
                askForCounter = false;
                for (int i = 0; i < 4; i++) {
                    if (player[pkg.target[0]].farm[i].ID != 0) {
                        if (power > 0) {
                            bool overGrow = player[pkg.target[0]].farm[i].grow();
                            if (overGrow)
                                player[pkg.target[0]].farm[i] = new corpCard();
                        } else {
                            bool beforeSeed = player[pkg.target[0]].farm[i].timeBack();
                            if (beforeSeed) {
                                player[pkg.target[0]].farm[i] = new corpCard();
                            }
                        }

                    }
                }
                pkg.playerStatuses = player;
                NetworkMenager.sendingQueue.Enqueue(pkg);
                break;
            case CARD_ACTION.GIVE_MONEY:
                if (power < 0) {
                    if (GuardedChk(pkg, pkg.target[0]))
                        return;
                    if (canAskCounter(pkg)) {
                        return;
                    }
                }
                player[pkg.target[0]].money += power;
                pkg.playerStatuses = player;
                NetworkMenager.sendingQueue.Enqueue(pkg);
                gameOverChk();
                break;
            case CARD_ACTION.DESTROY:
                if (pkg.target[0] == 5) {
                    for (int i = 0; i < player.Count; i++) {
                        if (GuardedChk(pkg, i))
                            continue;
                        for (int j = 0; j < 4; j++) {
                            if (player[i].farm[j].ID != 0) {
                                player[i].farm[j] = new corpCard();
                                player[i].money += 1;
                            }
                        }
                    }
                    pkg.playerStatuses = player;
                    NetworkMenager.sendingQueue.Enqueue(pkg);
                } else {
                    if (GuardedChk(pkg, pkg.target[0]))
                        return;
                    if (canAskCounter(pkg)) {
                        return;
                    } else {
                        //執行卡片效果
                        askForCounter = false;
                        player[pkg.target[0]].farm[pkg.target[1]] = new corpCard();
                    }
                    pkg.playerStatuses = player;
                    NetworkMenager.sendingQueue.Enqueue(pkg);
                }
                break;
            case CARD_ACTION.DEFEND:
                if (askForCounter) {
                    askForCounter = false;
                    waitForCounter = null;
                    pkg.playerStatuses = player;
                    NetworkMenager.sendingQueue.Enqueue(pkg);
                }
                break;
            case CARD_ACTION.SPECIALEFFECT:
                if (pkg.target[0] == 5) {
                    for (int i = 0; i < player.Count; i++) {
                        player[i].effect[(int)effect] += power;
                    }
                } else {
                    player[pkg.target[0]].effect[(int)effect] += power;
                }
                pkg.playerStatuses = player;
                NetworkMenager.sendingQueue.Enqueue(pkg);
                break;
            case CARD_ACTION.MODIFY_EVENT:
                if (modifyEventCard) {
                    for (int i = pkg.target.Count - 1; i >= 0; i--) {
                        EventCardDeck.Push(pkg.target[i]);
                    }
                    modifyEventCard = false;
                } else {
                    List<int> card = new List<int>();
                    for (int i = 0; i < power; i++) {
                        if (EventCardDeck.Count == 0) {
                            resetEventCard();
                        }
                        card.Add(EventCardDeck.Pop());
                    }
                    pkg.target = card;
                    modifyEventCard = true;
                    pkg.playerStatuses = player;
                    NetworkMenager.sendingQueue.Enqueue(pkg);
                }
                break;
            case CARD_ACTION.STEAL_MONEY:
                if (pkg.target[0] == 5) {
                    int steal = 0;
                    for (int i = 0; i < player.Count; i++) {
                        if (i == pkg.src || GuardedChk(pkg, i)) {
                            continue;
                        }
                        if (player[i].money < power) {
                            steal += player[i].money;
                            player[i].money = 0;
                        } else {
                            steal += power;
                            player[i].money -= power;
                        }
                    }
                    player[pkg.src].money += steal;
                } else {
                    if (GuardedChk(pkg, pkg.target[0]))
                        return;
                    if (canAskCounter(pkg)) {
                        return;
                    } else {
                        int steal = 0;
                        if (player[pkg.target[0]].money < power) {
                            steal += player[pkg.target[0]].money;
                            player[pkg.target[0]].money = 0;
                        } else {
                            steal += power;
                            player[pkg.target[0]].money -= power;
                        }
                        player[pkg.src].money += steal;
                    }
                }
                pkg.playerStatuses = player;
                NetworkMenager.sendingQueue.Enqueue(pkg);
                gameOverChk();
                break;
        }
    }
    bool GuardedChk(Package pkg, int playerChk) {
        if (pkg.src != -1 && player[playerChk].effect[(int)EFFECT_ID.GUARD] >= 1) {
            if (pkg.target[0] != 5) {
                pkg.playerStatuses = player;
                NetworkMenager.sendingQueue.Enqueue(pkg);
            }
            return true;
        }
        return false;
    }

    bool canAskCounter(Package pkg) {
        if(pkg.src != -1 && pkg.target[0] != 5) {
            if(!askForCounter) {
                askCounter(pkg);
                return true;
            }
        }
        return false;
    }

    void askCounter(Package pkg) {

        askForCounter = true;
        if (pkg.target[0] != 5) {
            List<int> cardChk = player[pkg.target[0]].handCard;
            for (int i = 0; i < cardChk.Count; i++) {
                if (cardList.actionCardsList[cardChk[i] % MAXCARD ].card.Action == CARD_ACTION.DEFEND || cardList.actionCardsList[cardChk[i] % MAXCARD].card.effect == EFFECT_ID.GUARD) {
                    waitForCounter = pkg;
                    pkg.askCounter = true;
                    pkg.playerStatuses = player;
                    NetworkMenager.sendingQueue.Enqueue(pkg);
                    return;
                }
            }
        }
        pkgQueue.Enqueue(pkg);

    }

    powerCard searchCard(int cardID) {
        int c = cardID / MAXCARD;
        switch (c) {
            case 1:
                return cardList.actionCardsList[cardID % MAXCARD].card;
            case 3:
                return cardList.fateCardsList[cardID % MAXCARD].card;
            case 4:
                return cardList.eventCardList[cardID % MAXCARD].card;
        }
        return null;
    }
}
