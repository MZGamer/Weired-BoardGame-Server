using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
 
 
public class packageUnpacker : MonoBehaviour
{
    const int MAXCARD = 100;
    public gameInfo gameData;
    public CardList cardList;
    public Package package;
    public EventCard eventCard;
    public List<Package> pkgQueueView = new List<Package>();
    public static Queue<Package> pkgQueue = new Queue<Package>();

    public const int BILL = 2;
    public bool askForCounter = false;
    public bool modifyEventCard = false;
    public Package waitForCounter; 
    public Package waitForRoll;

    public playerStatus DisconnectPlayer;

    int rollPoint;

    private static readonly Dictionary<CARD_ACTION, Type> actionMap = new Dictionary<CARD_ACTION, Type>
    {
        { CARD_ACTION.NONE, typeof(noActionExecuter)},
        { CARD_ACTION.SKIP_TURN, typeof(skipTurnExecuter) },
        { CARD_ACTION.GROWUP,    typeof(growUPExecuter) },
        { CARD_ACTION.GIVE_MONEY,    typeof(giveMoneyExecuter) },
        { CARD_ACTION.DESTROY,    typeof(destoryExecuter) },
        { CARD_ACTION.DEFEND,    typeof(noActionExecuter) },
        { CARD_ACTION.SPECIALEFFECT,    typeof(setEffectExecuter) },
        { CARD_ACTION.MODIFY_EVENT,    typeof(modifyEvent_PopExecuter) },
        { CARD_ACTION.STEAL_MONEY,    typeof(stealMoneyExecuter) }
    };

    // Start is called before the first frame update
    void Start()
    {
        gameInit();
    }
    void gameInit() {
        gameData.playerList = new playerStatus[0];
        gameData.turn = 0;
        gameData.cardList = cardList;
        gameData.eventCard = eventCard;
        gameData.actionCardDeck = new Stack<int>();
        gameData.FateCardDeck = new Stack<int>();
        gameData.EventCardDeck = new Stack<int>();
        gameData.usedActionCardDeck = new List<int>();
        DeckManager.resetFateCard(ref gameData);
        DeckManager.resetEventCard(ref gameData);
        DeckManager.resetActionCard(ref gameData);

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
                    gameData.playerList[package.index] = DisconnectPlayer;
                    package.playerData = gameData.playerList;
                    NetworkMenager.sendingQueue.Enqueue(package);
                } else {
                    gameData.playerList[package.index] = DisconnectPlayer;
                    package.playerData = gameData.playerList;
                    NetworkMenager.sendingQueue.Enqueue(package);
                    if(NetworkMenager.gameStart && gameData.turn % gameData.playerList.Length == package.index) {
                        pkgQueue.Enqueue(new Package(-1, ACTION.NEXT_PLAYER, 0, 0, false, gameData.playerList));
                    }
                }
                break;
            //暫時用來表示不使用反擊卡
            case ACTION.NULL:
                //重新處理卡片效果
                waitForCounter.askCounter = false;
                package.playerData = gameData.playerList;
                pkgQueue.Enqueue(waitForCounter);
                waitForCounter = null;
                break;
            case ACTION.PLAYER_JOIN:
                Array.Resize(ref gameData.playerList, package.src+1);
                gameData.playerList[package.src] = package.playerData[0];
                Package pkg = new Package(-1, ACTION.PLAYER_JOIN, 0, 0, false, gameData.playerList);
                NetworkMenager.sendingQueue.Enqueue(pkg);
                break;
            case ACTION.GAMESTART:
                pkg = new Package(-1, ACTION.GAMESTART, 0, 0, false, gameData.playerList);
                NetworkMenager.gameStart = true;
                NetworkMenager.sendingQueue.Enqueue(pkg);

                int nextCard = gameData.EventCardDeck.Pop();
                powerCard eventCard = (powerCard)DeckManager.searchCard(ref gameData, nextCard);
                pkg = new Package(-1, ACTION.CARD_ACTIVE, nextCard, 5, false, gameData.playerList);
                cardActionExecute(pkg, eventCard);

                nextCard = gameData.FateCardDeck.Pop();
                rollPoint = UnityEngine.Random.Range(1, 6);
                pkg = new Package(-1, ACTION.ROLL_POINT, rollPoint, new List<int> { gameData.turn % gameData.playerList.Length, nextCard });
                NetworkMenager.sendingQueue.Enqueue(pkg);
                waitForRoll = pkg;
                break;
            case ACTION.NEXT_PLAYER:
                gameData.turn++;
                nextCard = 0;
                //如果turn數到達，開啟一個新Turn
                if (gameData.turn != 0 && gameData.turn % gameData.playerList.Length == 0)
                {
                    //將每人個別稅率放入Target回傳(因為我懶)
                    List<int> bill = new List<int>();
                    int disPlayerCount = 0;
                    for (int i = 0; i < gameData.playerList.Length; i++)
                    {
                        if (gameData.playerList[i].name == "Disconnected") {
                            disPlayerCount++;
                            bill.Add(0);
                            continue;
                        }
                            
                        //計算稅率
                        int billMoney = BILL + gameData.playerList[i].effect[(int)EFFECT_ID.BILL_RATIO];
                        bill.Add(billMoney);
                        gameData.playerList[i].money -= billMoney;

                        //reset effect array
                        gameData.playerList[i].effect = new int[Enum.GetNames(typeof(EFFECT_ID)).Length];

                        //作物生長
                        for (int k = 0; k < 4; k++)
                        {
                            bool overGrow = FarmManager.grow(ref gameData.playerList[i].farm[k]);
                            if (overGrow)
                                FarmManager.resetFarm(ref gameData.playerList[i].farm[k]);
                        }
                        
                    }
                    if (disPlayerCount == gameData.playerList.Length) {
                        NetworkMenager.gameOver = true;
                        return;
                    }
                        
                    if (gameData.EventCardDeck.Count == 0)
                    {
                        DeckManager.resetEventCard(ref gameData);
                    }

                    //通知clinet更新GUI(扣稅，農田增長)
                    pkg = new Package(-1, ACTION.NEW_TURN, 0, bill, false, gameData.playerList);
                    NetworkMenager.sendingQueue.Enqueue(pkg);
                    gameOverChk();

                    //發布新的EventCard
                    nextCard = gameData.EventCardDeck.Pop();
                    eventCard = (powerCard)DeckManager.searchCard(ref gameData, nextCard);
                    pkg = new Package(-1, ACTION.CARD_ACTIVE, nextCard, 5, false, gameData.playerList);
                    cardActionExecute(pkg, eventCard);
                }

                //通知Clinet更新GUI提示輪到下個玩家
                pkg = new Package(-1, ACTION.NEXT_PLAYER, 0, 0, false, gameData.playerList);
                NetworkMenager.sendingQueue.Enqueue(pkg);

                if (gameData.playerList[gameData.turn % gameData.playerList.Length].name == "Disconnected") {
                    pkgQueue.Enqueue(new Package(-1, ACTION.NEXT_PLAYER, 0, 0, false, gameData.playerList));
                    return;
                }

                //發布新的FateCard 並等待Roll點
                if (gameData.FateCardDeck.Count == 0) {
                    DeckManager.resetFateCard(ref gameData);
                }
                nextCard = gameData.FateCardDeck.Pop();
                rollPoint = UnityEngine.Random.Range(1, 6);
                pkg = new Package(-1, ACTION.ROLL_POINT, rollPoint, new List<int> { gameData.turn % gameData.playerList.Length, nextCard });
                NetworkMenager.sendingQueue.Enqueue(pkg);
                waitForRoll = pkg;
                break;
            //收到玩家對FateCard的Roll點
            case ACTION.ROLL_POINT:
                FateCard fate = (FateCard)DeckManager.searchCard(ref gameData, package.target[1]);
                roll rollSelect = fate.fatecardinfo.rools.Find(x => package.index >= x.min && package.index <= x.max);
                pkg = new Package(-1, ACTION.CARD_ACTIVE, 0, waitForRoll.target[0]);
                cardActionExecute(pkg, fate, rollSelect);

                break;
            case ACTION.CARD_ACTIVE:
                int activatedCardID = package.index;
                int activatedSource = package.src;
                List<int> target = package.target;
                if (package.index /MAXCARD == 1) {
                    gameData.usedActionCardDeck.Add(activatedCardID);
                }
                switch (package.index / MAXCARD) {
                    case 2: // Corp Card
                        FarmManager.plant(ref gameData.playerList[activatedSource].farm[target[0]], activatedCardID);
                        gameData.playerList[activatedSource].money -= 1;
                        Package sendpkg = new Package(-1, ACTION.CARD_ACTIVE, 0, 0, false, gameData.playerList);
                        NetworkMenager.sendingQueue.Enqueue(sendpkg);
                        gameOverChk();
                        break;
                    default:

                        gameData.playerList[package.src].handCard.Remove(package.index);
                        powerCard card = (powerCard)DeckManager.searchCard(ref gameData, package.index);
                        cardActionExecute(package, card);

                        break;

                }
                break;

            case ACTION.GET_NEW_CARD:
                gameData.turn = gameData.turn % gameData.playerList.Length;
                int actionCard = 0 ;
                if (gameData.turn == package.src)
                {
                    if (gameData.actionCardDeck.Count == 0)
                    {
                        DeckManager.cardRandom(gameData.usedActionCardDeck, ref gameData.actionCardDeck);
                    }
                    if(gameData.playerList[package.src].handCard.Count <= 5) {
                        actionCard = gameData.actionCardDeck.Pop();
                        gameData.playerList[package.src].handCard.Add(actionCard);
                        gameData.playerList[package.src].money -= 2;
                    }

                }
                pkg = new Package(-1, ACTION.GET_NEW_CARD, actionCard, package.src, false, gameData.playerList);
                NetworkMenager.sendingQueue.Enqueue(pkg);
                break;

            case ACTION.HARVEST:
                gameData.turn = gameData.turn % gameData.playerList.Length;
                if(gameData.turn == package.src)
                {
                    gameData.playerList[package.src].money += FarmManager.getReward(ref gameData, ref gameData.playerList[package.src].farm[package.target[0]]) + gameData.playerList[package.src].effect[(int)EFFECT_ID.HARVEST_RATIO];
                    FarmManager.resetFarm(ref gameData.playerList[package.src].farm[package.target[0]]);
                }
                pkg = package;
                pkg.playerData = gameData.playerList;
                NetworkMenager.sendingQueue.Enqueue(pkg);
                gameOverChk();
                break;
        }
    }

    void gameOverChk() {
        foreach(playerStatus player in gameData.playerList) {
            if (player.name == "Disconnected") {
                continue;
            }
            if (player.money >= 40 || player.money < 0) {
                Package pkg = new Package(-1, ACTION.GAMEOVER, 0, 0);
                NetworkMenager.sendingQueue.Enqueue(pkg);
                NetworkMenager.gameOver = true;
                break;
            }
        }

    }

    void cardActionExecute(Package pkg, powerCard activatedCard, roll? roll = null) {
        //被詢問是否要打防禦卡時，如果有打出防禦卡，將等待是否防禦的卡片消除(不發動)
        if (activatedCard.powerInfo.Action ==  CARD_ACTION.DEFEND) {
            askForCounter = false;
            waitForCounter = null;
            pkg.playerData = gameData.playerList;
            NetworkMenager.sendingQueue.Enqueue(pkg);
            return;
        }

        //處理target
        List<int> targetlistPrepare = new List<int>();
        List<int> targetlist= new List<int>();
        if (pkg.target[0] == 5) {
            for(int i = 0; i < gameData.playerList.Length; i++) {
                targetlistPrepare.Add(i);
            }
        } else {
            targetlistPrepare.Add(pkg.target[0]);
        }

        //拿取要使用的action，如果是Event卡依照roll決定發動效果，否則直接讀卡片資料
        CARD_ACTION act;
        if(roll == null) {
            act = activatedCard.powerInfo.Action;
        } else {
            act = roll.Value.Action;
        }
        if (actionMap.TryGetValue(act, out Type type)) {
            actionExecuter action = (actionExecuter)Activator.CreateInstance(type);
            //特定的情況下，檢查是否有無敵或是要詢問是否要打防禦卡
            if (action.isNegative(activatedCard.powerInfo.actionPower)) {
                foreach (int index in targetlistPrepare) {
                    if (gameData.playerList[index].name == "DisconnectPlayer")
                        continue;
                    if (GuardedChk(pkg, index))
                        continue;
                    //防禦卡只會在被指定時才能打出，所以多人時canAskCounter(pkg)必定為False
                    //當canAskCounter(pkg)為True時，本次執行必定空過，會將該package佔存，確定沒有防禦再執行
                    if (canAskCounter(pkg)) {
                        continue;
                    }
                    targetlist.Add(index);
                }
            } else {
                targetlist = targetlistPrepare;
            }

            //特定卡片效果會使用兩個target欄
            if (pkg.target.Count == 2) {
                targetlist.Add(pkg.target[1]);
            }
            askForCounter = false;
            //請確認executer執行完後有將pkg廣播以播放動畫
            action.execute(ref gameData, ref pkg, activatedCard.powerInfo.actionPower, targetlist, (int)activatedCard.powerInfo.effect);
            gameOverChk();
        }
    }
    /*
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
    }*/
    bool GuardedChk(Package pkg, int playerChk) {
        if (pkg.src != -1 && gameData.playerList[playerChk].effect[(int)EFFECT_ID.GUARD] >= 1) {
            /*if (pkg.target[0] != 5) {
                pkg.playerData = gameData.playerList;
                NetworkMenager.sendingQueue.Enqueue(pkg);
            }*/
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
            List<int> cardChk = gameData.playerList[pkg.target[0]].handCard;
            //檢查玩家的手牌，有可以加無敵效果或是防禦的卡，就先將預計要發動的卡先佔存，詢問是否要發動防禦卡
            for (int i = 0; i < cardChk.Count; i++) {
                powerCard card = (powerCard)DeckManager.searchCard(ref gameData, cardChk[i]);
                if (card.powerInfo.Action == CARD_ACTION.DEFEND || card.powerInfo.effect == EFFECT_ID.GUARD) {
                    waitForCounter = pkg;
                    pkg.askCounter = true;
                    pkg.playerData = gameData.playerList;
                    NetworkMenager.sendingQueue.Enqueue(pkg);
                    return;
                }
            }
        }
        pkgQueue.Enqueue(pkg);

    }
}
