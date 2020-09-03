# AnimationSet: Sayaka
VAR IsBeginner = false
你好！# Position: Bottom # Speaker: Sayaka # Animation: Sayaka/Default
欢迎来到 Cytoid！# Animation: Sayaka/Happy
Cytoid 是一个由社区驱动、由玩家运营的，^/完全免费的音乐游戏。^^/让我先简单介绍一下游戏的不同模式吧！# Animation: Sayaka/Default
†自由模式†！^^/这是 Cytoid 最经典也是最广为人知的模式。请†点进去†看一下吧！# Highlight: FreePlayCard # WaitForHighlightOnClick
[N/A] # OverlayOpacity: 0/0.4/0 # Duration: 0.4
在自由模式中，^你可以任意游玩 Cytoid 中的内置关卡、^/在†社区†下载的用户关卡、^/在其他模式中获得的独占关卡、^/以及在外部导入的关卡。# OverlayOpacity: 0.7/0.4/0.4
包括刚才玩到的†教程关卡†，^也可以在这里玩到哦！# Animation: null # Highlight: @LevelSelectionScreen/HighlightedLevelCard
那么，我们†按下返回键†返回到主菜单，/来看一看其他地方吧！# Highlight: LevelSelectionScreen/Back # WaitForHighlightOnClick # Animation: Sayaka/Default
[N/A] # Duration: 0.8
{ IsBeginner == true:
    -> community_beginner
- else:
    -> community
}

=== community_beginner ===
Cytoid 内置的关卡并不多，^/那你可能会想，要怎样才可以玩到更多的关卡呢？# Animation: Sayaka/Surprised
答案就是.^.^.^^ †社区†啦！# Animation: Sayaka/Happy
-> cont

=== community ===
既然你是老玩家了，那你一定已经知道†社区†是什么了！# Animation: Sayaka/Happy
-> cont

=== cont ===
点进†社区†，就可以查看并下载由玩家们创作的 4000+ 个用户关卡了！^/每天都会有新的关卡，所以记得随时都回来看看。# Position: Top # Highlight: CommunityCard
熟悉游戏后，你甚至还可以自己制作关卡并上传到这里喔！# Highlight: CommunityCard
除了自由模式之外，Cytoid 还有其他三种模式：†活动模式†、†训练模式†和†段位模式†。
首先，†活动模式†。^^/无论是季节性的活动^，还是与独立创作者的合作，^/甚至是与其他同人游戏的联动，^/都会以活动关卡的形式展现在这里。# Highlight: EventsCard
然后是†训练模式†！^^/如果你是新手，^又想快速上手游戏的话，^/建议先从这里的关卡玩起，^循序渐进地熟悉游戏的基本玩法。# Highlight: TrainingCard
{ IsBeginner == false:
    就算是 Cytoid 的老玩家，^也务必尝试一下这里的关卡哦！# Highlight: TrainingCard
}
最后是†段位模式†。^^/段位系统是对玩家 Cytoid 实力的分级评定，^/要是充满信心又想考验自己的水平，^就来挑战一下吧～？# Highlight: TiersCard
那么，对 Cytoid 玩法的介绍就大致到这里啦！^^/祝你玩得愉快！# Position: Bottom # Animation: Sayaka/Happy
-> DONE