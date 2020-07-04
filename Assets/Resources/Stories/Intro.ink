# SpriteSet: Tira
# SpriteSet: Nut
你好！# Speaker: Tira # Sprite: Tira/Default
这里是一句对话！
上面测试 # Speaker: null # Sprite: null # Position: Top
下面测试 # Position: Bottom
Cytoid 是一个由社区驱动、由玩家运营的，/完全非商业化的音乐游戏。^/那么，我想先问你一个问题。# Position: Bottom # Speaker: Tira # Sprite: Tira/Default
你是大爸爸吗？
* [是的]
    哇！你好强！
    -> cont
* [不是]
    没事！/我找来一位高手为你传授秘籍！
    -> cont
    
=== cont ===
...^^^ # Speaker: 坚果 # Sprite: Nut/Default
多练可破。
哇！^说得真有道理！# Speaker: Tira # Sprite: Tira/Default
-> DONE
