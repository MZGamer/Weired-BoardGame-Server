using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PackageUnpackerTests
{
    private GameObject _go;
    private packageUnpacker _sut;
    private DeckManager _deckManager;

    [SetUp]
    public void SetUp()
    {
        // 清掉 static queues，避免測試互相污染
        packageUnpacker.pkgQueue = new Queue<Package>();
        NetworkMenager.sendingQueue = new Queue<Package>();
        NetworkMenager.gameStart = false;
        NetworkMenager.gameOver = false;

        _go = new GameObject("packageUnpacker_tests");
        _sut = _go.AddComponent<packageUnpacker>();

        // 基礎 player 初始化（避免 NRE）
        _sut.gameData.playerList = new playerStatus[2]
        {
            MakePlayer("P0", money: 10),
            MakePlayer("P1", money: 10),
        };

        // DisconnectPlayer
        _sut.DisconnectPlayer = MakePlayer("Disconnected", money: 0);

        // cardList 最小可用初始化
        _sut.cardList = ScriptableObject.CreateInstance<CardList>();
        _sut.cardList.actionCardsList = new List<Action_Card_Temp>();
        _sut.cardList.corpCardsList = new List<Corp_Card_Temp>();
        _sut.cardList.fateCardsList = new List<Fate_Card_Temp>();
        _sut.cardList.eventCardList = new List<Event_Card_Temp>();

        // 放入至少 1 張 Action / Fate / Event，避免 Start() reset 時 deck 為空造成後續 Pop 風險
        _sut.cardList.actionCardsList.Add(MakeActionTemp(id: 100, count: 1, action: CARD_ACTION.NONE,0, EFFECT_ID.NONE));
        _sut.cardList.actionCardsList.Add(MakeActionTemp(id: 101, count: 1, action: CARD_ACTION.DEFEND,0, EFFECT_ID.NONE));
        _sut.cardList.fateCardsList.Add(MakeFateTemp(id: 300, count: 1, null));
        _sut.cardList.eventCardList.Add(MakeEventTemp(id: 400, count: 1, action: CARD_ACTION.NONE,0, EFFECT_ID.NONE));

        _sut.gameData.cardList = _sut.cardList;

        _sut.gameData.actionCardDeck = new Stack<int>();
        _sut.gameData.FateCardDeck = new Stack<int>();
        _sut.gameData.EventCardDeck = new Stack<int>();
        _sut.gameData.usedActionCardDeck = new List<int>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_sut != null) UnityEngine.Object.DestroyImmediate(_sut);
        if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
    }

    // -----------------------------
    // Start()
    // -----------------------------
    [Test]
    public void Start_ShouldResetAllDecks()
    {
        InvokePrivate(_sut, "Start");

        Assert.That(_sut.gameData.actionCardDeck.Count, Is.EqualTo(2));
        Assert.That(_sut.gameData.FateCardDeck.Count, Is.EqualTo(1));
        Assert.That(_sut.gameData.EventCardDeck.Count, Is.EqualTo(1));
    }
    // Package處理

    // -----------------------------
    // 空 queue 直接 return
    // -----------------------------
    [Test]
    public void Update_WhenPkgQueueEmpty_ShouldReturnWithoutSending()
    {
        InvokePrivate(_sut, "Update");
        Assert.That(NetworkMenager.sendingQueue.Count, Is.EqualTo(0));
    }

    // -----------------------------
    // 玩家加入
    // -----------------------------
    [Test]
    public void A_PlayerJoin_ShouldAddPlayerAndBroadcast() {
        playerStatus newcomer = MakePlayer("P3", money: 10);
        Package pkg = new Package(src: 2, ACTION: ACTION.PLAYER_JOIN, index: 0, target: 0, askCounter: false, playerData: new playerStatus[1] { newcomer });
        packageUnpacker.pkgQueue.Enqueue(pkg);

        InvokePrivate(_sut, "Update");
        Assert.That(_sut.gameData.playerList.Count, Is.EqualTo(3));
        Assert.That(_sut.gameData.playerList[2].name, Is.EqualTo("P3"));
        Assert.That(NetworkMenager.sendingQueue.Count, Is.EqualTo(1));
        Package broadcastCheck = NetworkMenager.sendingQueue.Dequeue();
        Assert.That(broadcastCheck.ACTION, Is.EqualTo(ACTION.PLAYER_JOIN));
        Assert.That(broadcastCheck.playerData, Is.EqualTo(_sut.gameData.playerList));
        
    }
    // -----------------------------
    // 玩家登出遊戲（遊戲未開始前）
    // -----------------------------
    [Test]
    public void A_PlayerDisconnectedBeforeGameStart() {
        Package pkg = new Package(src: -1, ACTION: ACTION.PLAYER_DISCONNECTED, index: 0, target: 0);
        packageUnpacker.pkgQueue.Enqueue(pkg);
        InvokePrivate(_sut, "Update");
        Assert.That(_sut.gameData.playerList.Count, Is.EqualTo(2));
        Assert.That(_sut.gameData.playerList[0], Is.EqualTo(_sut.DisconnectPlayer));
        Package broadcastCheck = NetworkMenager.sendingQueue.Dequeue();
        Assert.That(broadcastCheck.ACTION, Is.EqualTo(ACTION.PLAYER_DISCONNECTED));
        Assert.That(broadcastCheck.playerData, Is.EqualTo(_sut.gameData.playerList));
    }

    [Test]
    public void ALL_PlayerDisconnectedBeforeGameStart() {
        Package pkg = new Package(src: -1, ACTION: ACTION.PLAYER_DISCONNECTED, index: 0, target: 0);
        packageUnpacker.pkgQueue.Enqueue(pkg);
        Package pkg2 = new Package(src: -1, ACTION: ACTION.PLAYER_DISCONNECTED, index: 1, target: 0);
        packageUnpacker.pkgQueue.Enqueue(pkg2);
        InvokePrivate(_sut, "Update");
        InvokePrivate(_sut, "Update");
        Debug.Log(_sut.gameData.playerList[1].name);
        Assert.That(_sut.gameData.playerList.Count, Is.EqualTo(2));
        Assert.That(_sut.gameData.playerList[0], Is.EqualTo(_sut.DisconnectPlayer));
        Assert.That(_sut.gameData.playerList[1], Is.EqualTo(_sut.DisconnectPlayer));
        
        Assert.That(NetworkMenager.sendingQueue.Count, Is.EqualTo(2));
    }
    // -----------------------------
    // 玩家登出遊戲（遊戲已開始）
    // -----------------------------
    [Test]
    public void A_PlayerDisconnectedInHisTurn() {
        NetworkMenager.gameStart = true;
        _sut.gameData.turn = 0;
        Package pkg = new Package(src: -1, ACTION: ACTION.PLAYER_DISCONNECTED, index: 0, target: 0);
        packageUnpacker.pkgQueue.Enqueue(pkg);
        InvokePrivate(_sut, "Update");
        Package pkgCheck = packageUnpacker.pkgQueue.Dequeue();

        Assert.That(_sut.gameData.playerList.Count, Is.EqualTo(2));
        Assert.That(_sut.gameData.playerList[0], Is.EqualTo(_sut.DisconnectPlayer));
        Assert.That(pkgCheck.ACTION, Is.EqualTo(ACTION.NEXT_PLAYER));

    }

    // -----------------------------
    // 玩家不使用防禦卡
    // -----------------------------
    [Test]
    public void A_AskCounter_NoUseDefendCard() {
        Package waitForcounter = new Package(src: 0, ACTION: ACTION.CARD_ACTIVE, index: 101, target: new List<int> {1});
        _sut.askForCounter = true;
        waitForcounter.askCounter = true;
        _sut.waitForCounter = waitForcounter;
        Package pkg = new Package(src: 0, ACTION: ACTION.NULL, index: 0, null);
        packageUnpacker.pkgQueue.Enqueue(pkg);
        InvokePrivate(_sut, "Update");
        Package pkgcheck = packageUnpacker.pkgQueue.Dequeue();
        Assert.AreEqual(null, _sut.waitForCounter);
        Assert.AreEqual(true, _sut.askForCounter);
        Assert.AreEqual(false, pkgcheck.askCounter);
        Assert.AreEqual(0, pkgcheck.src);
        Assert.AreEqual(ACTION.CARD_ACTIVE, pkgcheck.ACTION);
        Assert.AreEqual(101, pkgcheck.index);
        Assert.AreEqual(new List<int> { 1 }, pkgcheck.target);
    }
    // -----------------------------
    // 遊戲開始
    // -----------------------------
    [Test]
    public void GameStart() {
        Package pkg = new Package(src: 0, ACTION: ACTION.GAMESTART, index: 0, target: null);
        packageUnpacker.pkgQueue.Enqueue(pkg);

        _sut.gameData.FateCardDeck.Push(300); // 確保有牌可發
        _sut.gameData.EventCardDeck.Push(400); // 確保有牌可發
        InvokePrivate(_sut, "Update");

        Package pkgcheck = NetworkMenager.sendingQueue.Dequeue();
        Assert.AreEqual(ACTION.GAMESTART, pkgcheck.ACTION);
        Assert.AreEqual(_sut.gameData.playerList,pkgcheck.playerData);

        pkgcheck = NetworkMenager.sendingQueue.Dequeue();
        Assert.AreEqual(ACTION.CARD_ACTIVE, pkgcheck.ACTION);
        Assert.AreEqual(400, pkgcheck.index);

        pkgcheck = NetworkMenager.sendingQueue.Dequeue();
        Assert.AreEqual(ACTION.ROLL_POINT, pkgcheck.ACTION);

    }

    // -----------------------------
    // 下個回合(未過一輪)
    // -----------------------------
    [Test]
    public void NextPlayerNotRound() {
        Package pkg = new Package(src: 0, ACTION: ACTION.NEXT_PLAYER, index: 0, target: null);
        packageUnpacker.pkgQueue.Enqueue(pkg);

        _sut.gameData.FateCardDeck.Push(300); // 確保有牌可發
        _sut.gameData.EventCardDeck.Push(400); // 確保有牌可發
        InvokePrivate(_sut, "Update");

        Assert.AreEqual(_sut.gameData.turn, 1);

        Package pkgToTest;
        pkgToTest = NetworkMenager.sendingQueue.Dequeue();
        Assert.AreEqual(ACTION.NEXT_PLAYER, pkgToTest.ACTION);

        pkgToTest = NetworkMenager.sendingQueue.Dequeue();
        Assert.AreEqual(ACTION.ROLL_POINT, pkgToTest.ACTION);
        Assert.AreEqual(new List<int> {1,300}, pkgToTest.target);

    }

    // -----------------------------
    // 下個回合(過了一輪)
    // -----------------------------
    [Test]
    public void NextPlayerAndNewTurn() {
        _sut.gameData.playerList = new playerStatus[2]
        {
                MakePlayer("P0", money: 10),
                MakePlayer("P1", money: 10),
        };

        _sut.gameData.playerList[0].farm[0] = new farmInfo(200, 0);
        _sut.gameData.playerList[0].farm[1] = new farmInfo(200, 4);
        _sut.gameData.playerList[1].effect[(int)EFFECT_ID.BILL_RATIO] = 2 ;
        _sut.gameData.turn = 1;


        int[] expectedArray = new int[Enum.GetNames(typeof(EFFECT_ID)).Length];

        Package pkg = new Package(src: 1, ACTION: ACTION.NEXT_PLAYER, index: 0, target: null);
        packageUnpacker.pkgQueue.Enqueue(pkg);

        _sut.gameData.FateCardDeck.Push(300); // 確保有牌可發
        _sut.gameData.EventCardDeck.Push(400); // 確保有牌可發
        InvokePrivate(_sut, "Update");
        Assert.AreEqual(2, _sut.gameData.turn);
        Assert.AreEqual(8, _sut.gameData.playerList[0].money);
        Assert.AreEqual(6, _sut.gameData.playerList[1].money);
        Assert.That(_sut.gameData.playerList.All(p => p.effect.SequenceEqual(expectedArray)), Is.True);

        Assert.AreEqual(1, FarmManager.getTurn(ref _sut.gameData.playerList[0].farm[0]));
        Assert.AreEqual(0, FarmManager.getTurn(ref _sut.gameData.playerList[0].farm[1]));

        Package pkgToTest;
        pkgToTest = NetworkMenager.sendingQueue.Dequeue();
        Assert.AreEqual(ACTION.NEW_TURN, pkgToTest.ACTION);
        Assert.AreEqual(new List<int> { 2, 4 }, pkgToTest.target);

        pkgToTest = NetworkMenager.sendingQueue.Dequeue();
        Assert.AreEqual(ACTION.CARD_ACTIVE, pkgToTest.ACTION);

        pkgToTest = NetworkMenager.sendingQueue.Dequeue();
        Assert.AreEqual(ACTION.NEXT_PLAYER, pkgToTest.ACTION);

        pkgToTest = NetworkMenager.sendingQueue.Dequeue();
        Assert.AreEqual(ACTION.ROLL_POINT, pkgToTest.ACTION);
        Assert.AreEqual(new List<int> { 0, 300 }, pkgToTest.target);

    }

// -----------------------------
// resetActionCard/resetFateCard/resetEventCard (間接覆蓋 cardRandom)
// -----------------------------
[Test]
    public void ResetActionCard_ShouldFillDeckWithAllCounts()
    {
        _sut.cardList.actionCardsList = new List<Action_Card_Temp>
        {
            MakeActionTemp(101, 2, CARD_ACTION.NONE,0, EFFECT_ID.NONE),
            MakeActionTemp(102, 3, CARD_ACTION.NONE,0, EFFECT_ID.NONE),
        };
        DeckManager.resetActionCard(ref _sut.gameData);

        Assert.That(_sut.gameData.actionCardDeck.Count, Is.EqualTo(5));
        var ids = _sut.gameData.actionCardDeck.ToArray();
        Assert.That(ids.Count(x => x == 101), Is.EqualTo(2));
        Assert.That(ids.Count(x => x == 102), Is.EqualTo(3));
    }

    [Test]
    public void ResetFateCard_ShouldFillDeckWithAllCounts()
    {
        _sut.cardList.fateCardsList = new List<Fate_Card_Temp>
        {
            MakeFateTemp(301, 2, null),
            MakeFateTemp(302, 1, null),
        };

        DeckManager.resetFateCard(ref _sut.gameData);

        Assert.That(_sut.gameData.FateCardDeck.Count, Is.EqualTo(3));
    }

    [Test]
    public void ResetEventCard_ShouldFillDeckWithAllCounts()
    {
        _sut.cardList.eventCardList = new List<Event_Card_Temp>
        {
            MakeEventTemp(400, 2, CARD_ACTION.NONE,0, EFFECT_ID.NONE),
            MakeEventTemp(401, 2, CARD_ACTION.NONE,0, EFFECT_ID.NONE),
            MakeEventTemp(402, 2, CARD_ACTION.NONE, 0, EFFECT_ID.NONE),
        };

        DeckManager.resetEventCard(ref _sut.gameData);

        Assert.That(_sut.gameData.EventCardDeck.Count, Is.EqualTo(6));
    }

    // -----------------------------
    // gameOverChk()
    // -----------------------------
    [Test]
    public void GameOverChk_WhenAnyPlayerMoneyReached40_ShouldEnqueueGameOverAndSetFlag()
    {
        _sut.gameData.playerList[0].money = 40;

        InvokePrivate(_sut, "gameOverChk");

        Assert.That(NetworkMenager.gameOver, Is.True);
        Assert.That(NetworkMenager.sendingQueue.Count, Is.EqualTo(1));
        Assert.That(NetworkMenager.sendingQueue.Dequeue().ACTION, Is.EqualTo(ACTION.GAMEOVER));
    }

    [Test]
    public void GameOverChk_ShouldIgnoreDisconnectedPlayers()
    {
        _sut.gameData.playerList[0] = MakePlayer("Disconnected", money: 999);
        _sut.gameData.playerList[1].money = 39;

        InvokePrivate(_sut, "gameOverChk");

        Assert.That(NetworkMenager.gameOver, Is.False);
        Assert.That(NetworkMenager.sendingQueue.Count, Is.EqualTo(0));
    }


    [Test]
    public void SearchCard_UnknownPrefix_ShouldReturnNull() {
        
        var result = DeckManager.searchCard(ref _sut.gameData, 999);
        Assert.That(result, Is.Null);
    }

    // -----------------------------
    // GuardedChk()
    // -----------------------------
    [Test]
    public void GuardedChk_WhenGuardEffectActive_ShouldReturnTrue()
    {
        _sut.gameData.playerList[1].effect[(int)EFFECT_ID.GUARD] = 1;

        var pkg = new Package(src: 0, ACTION: ACTION.CARD_ACTIVE, index: 101, target: new List<int> {1});

        var guarded = (bool)InvokePrivate(_sut, "GuardedChk", pkg, 1);

        Assert.That(guarded, Is.True);
    }

    [Test]
    public void GuardedChk_WhenNoGuard_ShouldReturnFalse()
    {
        _sut.gameData.playerList[1].effect[(int)EFFECT_ID.GUARD] = 0;

        var pkg = new Package(src: 0, ACTION: ACTION.CARD_ACTIVE, index: 101, target: new List<int> { 1, 0 });

        var guarded = (bool)InvokePrivate(_sut, "GuardedChk", pkg, 1);

        Assert.That(guarded, Is.False);
    }

    // -----------------------------
    // canAskCounter() / askCounter()
    // -----------------------------
    [Test]
    public void CanAskCounter_WhenFirstTimeAndTargetHasDefendCard_ShouldSendAskCounterAndReturnTrue()
    {
        // 讓 target 玩家手牌含 DEFEND
        _sut.cardList.actionCardsList = new List<Action_Card_Temp>
        {
            MakeActionTemp(id: 100, count: 1, action: CARD_ACTION.DEFEND, 1, EFFECT_ID.NONE),
        };
        _sut.gameData.playerList[1].handCard = new List<int> { 100 };

        var pkg = new Package(src: 0, ACTION: ACTION.CARD_ACTIVE, index: 100, target: new List<int> { 1, 0 });

        var result = (bool)InvokePrivate(_sut, "canAskCounter", pkg);

        Assert.That(result, Is.True);
        Assert.That(_sut.waitForCounter, Is.EqualTo(pkg));
        Assert.That(NetworkMenager.sendingQueue.Count, Is.EqualTo(1));
        var sent = NetworkMenager.sendingQueue.Dequeue();
        Assert.That(sent.askCounter, Is.True);
    }

    [Test]
    public void AskCounter_WhenNoCounterCard_ShouldEnqueueToPkgQueue()
    {
        _sut.cardList.actionCardsList = new List<Action_Card_Temp>
        {
            MakeActionTemp(id: 100, count: 1, action: CARD_ACTION.NONE, 0, EFFECT_ID.NONE),
        };
        _sut.gameData.playerList[1].handCard = new List<int> { 100 };

        var pkg = new Package(src: 0, ACTION: ACTION.CARD_ACTIVE, index: 100, target: new List<int> {1});

        InvokePrivate(_sut, "askCounter", pkg);

        Assert.That(packageUnpacker.pkgQueue.Count, Is.EqualTo(1));
        Assert.That(NetworkMenager.sendingQueue.Count, Is.EqualTo(0));
    }

    // -----------------------------
    // cardActionExecute(): 覆蓋各 case
    // -----------------------------
    [Test]
    public void CardActionExecute_NONE_ShouldEnqueueSending()
    {
        var pkg = new Package(src: 0, ACTION: ACTION.CARD_ACTIVE, index: 100, target: new List<int> {0});

        _sut.cardList.actionCardsList = new List<Action_Card_Temp>
{
            MakeActionTemp(100, 2, CARD_ACTION.NONE,5, EFFECT_ID.NONE)
        };
        powerCard P = MakeActionTemp(100, 2, CARD_ACTION.NONE, 5, EFFECT_ID.NONE).card;
        InvokePrivate(_sut, "cardActionExecute", pkg, P, null);

        Assert.That(NetworkMenager.sendingQueue.Count, Is.EqualTo(1));
        Assert.That(NetworkMenager.sendingQueue.Dequeue().ACTION, Is.EqualTo(ACTION.CARD_ACTIVE));
    }

    [Test]
    public void CardActionExecute_SKIP_TURN_ShouldSendAndEnqueueNextPlayer()
    {
        var pkg = new Package(src: 0, ACTION: ACTION.CARD_ACTIVE, index: 101, target: new List<int> { 1, 0 });

        _sut.cardList.actionCardsList = new List<Action_Card_Temp>
{
            MakeActionTemp(100, 2, CARD_ACTION.SKIP_TURN,5,EFFECT_ID.NONE)
        };
        powerCard P = MakeActionTemp(100, 2, CARD_ACTION.SKIP_TURN, 5, EFFECT_ID.NONE).card;
        InvokePrivate(_sut, "cardActionExecute", pkg, P, null);

        Assert.That(NetworkMenager.sendingQueue.Count, Is.EqualTo(1));
        Assert.That(packageUnpacker.pkgQueue.Count, Is.EqualTo(1));
        Assert.That(packageUnpacker.pkgQueue.Dequeue().ACTION, Is.EqualTo(ACTION.NEXT_PLAYER));
    }

    [Test]
    public void CardActionExecute_GIVE_MONEY_Positive_ShouldAddMoneyAndSend()
    {
        var pkg = new Package(src: -1, ACTION: ACTION.CARD_ACTIVE, index: 100, target: new List<int> {1});

        var before = _sut.gameData.playerList[1].money;
        _sut.cardList.actionCardsList = new List<Action_Card_Temp>
{
            MakeActionTemp(100, 2, CARD_ACTION.GIVE_MONEY,5,EFFECT_ID.NONE),
        };
        powerCard P = MakeActionTemp(100, 2, CARD_ACTION.GIVE_MONEY, 5, EFFECT_ID.NONE).card;
        InvokePrivate(_sut, "cardActionExecute", pkg, P, null);

        Assert.That(_sut.gameData.playerList[1].money, Is.EqualTo(before + 5));
        Assert.That(NetworkMenager.sendingQueue.Count, Is.EqualTo(1));
    }

    [Test]
    public void CardActionExecute_SPECIALEFFECT_GlobalTarget5_ShouldApplyToAllPlayers()
    {
        var pkg = new Package(src: -1, ACTION: ACTION.CARD_ACTIVE, index: 401, target: new List<int> { 5 });

        _sut.cardList.actionCardsList = new List<Action_Card_Temp>
{
            MakeActionTemp(100, 2, CARD_ACTION.SPECIALEFFECT,5, EFFECT_ID.BILL_RATIO)
        };
        powerCard P = MakeActionTemp(100, 2, CARD_ACTION.SPECIALEFFECT, 2, EFFECT_ID.BILL_RATIO).card;
        InvokePrivate(_sut, "cardActionExecute", pkg, P, null);

        Assert.That(_sut.gameData.playerList.All(p => p.effect[(int)EFFECT_ID.BILL_RATIO] == 2), Is.True);
        Assert.That(NetworkMenager.sendingQueue.Count, Is.EqualTo(1));
    }

    [Test]
    public void CardActionExecute_MODIFY_EVENT_FirstCall_ShouldPopCardsToTargetAndSend()
    {

        _sut.cardList.actionCardsList = new List<Action_Card_Temp>
{
            MakeActionTemp(100, 1, CARD_ACTION.MODIFY_EVENT,2, EFFECT_ID.NONE)
        };
        _sut.cardList.eventCardList = new List<Event_Card_Temp>
{
            MakeEventTemp(100, 1, CARD_ACTION.MODIFY_EVENT,2, EFFECT_ID.NONE),
            MakeEventTemp(101, 1, CARD_ACTION.NONE,5, EFFECT_ID.NONE)
        };
        powerCard P = MakeActionTemp(100, 2, CARD_ACTION.MODIFY_EVENT, 2, EFFECT_ID.NONE).card;
        var pkg = new Package(src: 0, ACTION: ACTION.CARD_ACTIVE, index: 100, target: new List<int> { 0 });

        InvokePrivate(_sut, "cardActionExecute", pkg, P, null);

        Assert.That(NetworkMenager.sendingQueue.Count, Is.EqualTo(1));
        var sent = NetworkMenager.sendingQueue.Dequeue();
        Assert.That(sent.target.Count, Is.EqualTo(2));
    }

    // -----------------------------
    // Update(): 覆蓋幾個主要 ACTION 分支（其餘可用相同模式擴充）
    // -----------------------------
    [Test]
    public void Update_PLAYER_JOIN_ShouldUpdatePlayerListAndSendJoinBroadcast()
    {
        var newcomer = MakePlayer("New", money: 10);
        var pkg = new Package(src: 1, ACTION: ACTION.PLAYER_JOIN, index: 0, target: 0, askCounter: false, playerData: new playerStatus[1] { newcomer });

        packageUnpacker.pkgQueue.Enqueue(pkg);

        InvokePrivate(_sut, "Update");

        Assert.That(_sut.gameData.playerList[1].name, Is.EqualTo("New"));
        Assert.That(NetworkMenager.sendingQueue.Count, Is.EqualTo(1));
        Assert.That(NetworkMenager.sendingQueue.Dequeue().ACTION, Is.EqualTo(ACTION.PLAYER_JOIN));
    }

    [Test]
    public void Update_PLAYER_DISCONNECTED_WhenNotGameStart_ShouldReplacePlayerAndSend()
    {
        NetworkMenager.gameStart = false;

        var pkg = new Package(src: -1, ACTION: ACTION.PLAYER_DISCONNECTED, index: 0, target: 0);

        packageUnpacker.pkgQueue.Enqueue(pkg);

        InvokePrivate(_sut, "Update");

        Assert.That(_sut.gameData.playerList[0].name, Is.EqualTo("Disconnected"));
        Assert.That(NetworkMenager.sendingQueue.Count, Is.EqualTo(1));
        Assert.That(NetworkMenager.sendingQueue.Dequeue().ACTION, Is.EqualTo(ACTION.PLAYER_DISCONNECTED));
    }

    // -----------------------------
    // helpers
    // -----------------------------
    private static playerStatus MakePlayer(string name, int money)
    {
        var p = new playerStatus();
        p.name = name;
        p.money = money;
        p.handCard = new List<int>();
        p.farm = new farmInfo[4] {new farmInfo(), new farmInfo(), new farmInfo(), new farmInfo() };
        p.effect = new int[Enum.GetNames(typeof(EFFECT_ID)).Length];
        return p;
    }

    private static Action_Card_Temp MakeActionTemp(int id, int count, CARD_ACTION action, int power, EFFECT_ID effect)
    {
        var temp = ScriptableObject.CreateInstance<Action_Card_Temp>();
        temp.card = new actionCard();
        temp.card.cardinfo = new cardInfo();
        temp.card.powerInfo = new powerCardInfo();
        temp.card.cardinfo.Name = id.ToString();
        temp.card.cardinfo.ID = id;
        temp.card.cardinfo.cardCount = count;
        temp.card.powerInfo.Action = action;
        temp.card.powerInfo.actionPower = power;
        temp.card.powerInfo.effect = effect;
        return temp;
    }

    private static Action_Card_Temp MakeActionTemp(actionCard card)
    {
        var temp = ScriptableObject.CreateInstance<Action_Card_Temp>();
        temp.card = card;
        return temp;
    }

    private static Fate_Card_Temp MakeFateTemp(int id, int count, List<roll> roll = null) {
        var temp = ScriptableObject.CreateInstance<Fate_Card_Temp>();
        temp.card = new FateCard();
        temp.card.cardinfo = new cardInfo();
        temp.card.fatecardinfo = new fateCardInfo();
        temp.card.cardinfo.Name = id.ToString();
        temp.card.cardinfo.ID = id;
        temp.card.cardinfo.cardCount = count;
        temp.card.fatecardinfo.rools = roll;

        return temp;
    }

    private static Fate_Card_Temp MakeFateTemp(FateCard card)
    {
        var temp = ScriptableObject.CreateInstance<Fate_Card_Temp>();
        temp.card = card;
        return temp;
    }

    private static Event_Card_Temp MakeEventTemp(int id, int count, CARD_ACTION action, int power, EFFECT_ID effect) {
        var temp = ScriptableObject.CreateInstance<Event_Card_Temp>();
        temp.card = new EventCard();
        temp.card.cardinfo = new cardInfo();
        temp.card.powerInfo = new powerCardInfo();
        temp.card.cardinfo.Name = id.ToString();
        temp.card.cardinfo.ID = id;
        temp.card.cardinfo.cardCount = count;
        temp.card.powerInfo.Action = action;
        temp.card.powerInfo.actionPower = power;
        temp.card.powerInfo.effect = effect;
        return temp;
    }

        private static Event_Card_Temp MakeEventTemp(EventCard card)
    {
        var temp = ScriptableObject.CreateInstance<Event_Card_Temp>();
        temp.card = card;
        return temp;
    }

    private static object InvokePrivate(object instance, string methodName, params object[] args)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var type = instance.GetType();

        // 依名稱找同名方法（此類別沒有多載衝突才安全；有衝突再改為依參數型別匹配）
        var method = type.GetMethods(flags).FirstOrDefault(m => m.Name == methodName);
        if (method == null)
            throw new MissingMethodException(type.FullName, methodName);

        return method.Invoke(instance, args);
    }
}