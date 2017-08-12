<img src="http://i.imgur.com/DrI3FkN.png" width="200">

# Cytoid <a href='https://play.google.com/store/apps/details?id=me.tigerhix.cytoid&hl=en'><img alt='Get it on Google Play' src='https://play.google.com/intl/en_us/badges/images/generic/en_badge_web_generic.png' width="100"/></a><a href='https://itunes.apple.com/us/app/cytoid/id1266582726?ls=1&mt=8'><img alt='Download on the App Store' src='https://devimages-cdn.apple.com/app-store/marketing/guidelines/images/badge-download-on-the-app-store.svg' width="100"/></a>

> 一个由社区驱动的 Cytus。

**Cytoid** 是一个参照 [Cytus](https://www.rayark.com/g/cytus/) 玩法的开源节奏游戏。

*用户关卡分享 & 下载：[CytoidIO](http://cytoid.io/browse)*

# 基本信息

Cytus 可谓是移动平台上数一数二的节奏游戏了吧。虽然游戏在视觉和音响上都堪称盛宴，遗憾的是它并没有开放类似 [osu!](https://osu.ppy.sh) 的自制谱面功能。从很久以前开始，Cytus 社区就在不断地贡献玩家原创内容：[自制章节](https://www.youtube.com/watch?v=84Gefg5YdYg&t=453s)、[自制谱面](https://www.youtube.com/results?search_query=Cytus+fanmade)，甚至有 [Cytunity](http://cytus-fanon.wikia.com/wiki/User_blog:JCEXE/List_of_Cytus_simulation_programs:_2017_edition#Cytunity)（制谱工具）和 [GLCytus](https://github.com/Dewott/GLCytus)（谱面模拟器）这样的专用工具。但是前者常常都只能供观赏用途，而后者又有一定的技术门槛，即使[效果十分完美](https://www.youtube.com/watch?v=2sopAxd8MZ0)，普通用户仍然会望而生畏。虽说还有将自制谱面导入到游戏里的方法，不过还是相当麻烦，也大概不是十分合法的事情。

在[续作](https://www.youtube.com/watch?v=rAStr9pjq_A)即将到来，且 Rayark 已有将近一年未有更新 Cytus 的这个时刻，Cytus 的 modding 社区是否可以光明正大地走出来，通过鼓励自制内容的创作、分享和试玩，来延伸这个游戏的可玩性呢？ *“可是光明正大搞 mod 会被官方和谐~~点艹~~的啊！”* 这就是此项目的由来。Cytoid 是一个基于 Unity 引擎的跨平台节奏游戏，特性如下：

* 复刻 Cytus 的游戏玩法
* 原创~~并且很丑~~的视觉效果
* 轻松导入自制关卡
* 触屏上也可以用的谱面编辑器 **（制作中）**

代表着：

* （对于玩家）不用再呆望着别人家的自制谱面，向谱师拿一份谱子就可以开玩！*（内心os：其实这才是我做 Cytoid 的原因...）*
* （对于开发者 *其实就是我*）解决版权问题！
* （对于制谱者）快速在实机上测试你的谱面！实话说，一玩就会感受到很多问题，比如遮手...
* （对于所有玩家）自己也可以写谱啦！**（制作中）**

<img src="http://i.imgur.com/QkCV1IW.png" width="800">
<img src="http://i.imgur.com/ueIS1Eo.png" width="800">
<img src="http://i.imgur.com/zJ7rp2D.png" width="800">
<img src="http://i.imgur.com/oFquEC5.png" width="800">
<img src="http://i.imgur.com/uqvBSf5.png" width="800">
<img src="http://i.imgur.com/UoqjWit.png" width="800">

## 玩家入门

Cytoid 是由社区驱动的节奏游戏；尽管其内置了[教程关卡](cytoid.io/browse/io.cytoid.glow_dance)，Cytoid 仍然依赖于玩家们（或者你）来创作和分享游戏内容。如果你在寻找新的关卡，不妨到 [CytoidDB](cytoid.io/browse) 看看。

如果你是谱师的话，请到 [wiki](https://github.com/TigerHix/Cytoid/wiki/a.-Creating-a-level) 查看如何创建 Cytoid 关卡，或者将现有的 Cytus 自制谱面转换为 Cytoid 关卡（剧透：几乎不需要改动）。

## 开发入门

此项目使用 [Unity](https://unity3d.com/) 引擎开发。说起来是个艰难的决定，但是相比用来制作 [Pulmusic](https://github.com/TigerHix/Pulmusic) 的 [libgdx](https://libgdx.badlogicgames.com/)，Unity 里开发迭代实在[快得令人无法相信](https://gamedev.stackexchange.com/a/8133)。要说缺点就是 Unity 对团队协作和版本控制并不友好，希望 [Github for Unity](https://unity.github.com/) 能解决这个问题吧。

```
源代码开放的时候会更新此部分。
```

## 所以，源代码呢？

好问题！...其实我还在清理项目代码，重构是一方面，最主要还是尽量将我用的 [Asset Store](https://www.assetstore.unity3d.com/) 资源替换成开源友好的方案。因为实在太方便了，结果醒悟过来的时候项目里全部都是别人的代码了...（逃

有兴趣加入开发的请敲我邮箱！（拜谢）

## 更新历史

* Beta 2
    * 导入 `.cytoidlevel` 文件
    * 修复若干 bugs
* Beta 1
    * 一个至少能跑的版本

## 开源协议

```
源代码开放的时候会更新此部分。
```

## Meta

Proudly presented by [Tiger Tang](https://github.com/tigerhix/).
* Twitter: [@tigerhixtang](https://twitter.com/tigerhixtang)
* Email: [tigerhix@gmail.com](mailto://tigerhix@gmail.com)

最后当然是，
```
Long Live the Rayark
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMM;``````````````````OMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMMMM8```````````````MM;``````````````NMMMMMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMM``````````````````MMMM``````````````````MMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMM`````````````````````8MMMMM;```````````````````'MMMMMMMMMMMMMMMM
MMMMMMMMMMMMMM``````````````````````MMMMMMMM``````````````````````MMMMMMMMMMMMMM
MMMMMMMMMMMM```````````````````````;MMMMMMMMM'``````````````````````MMMMMMMMMMMM
MMMMMMMMMM````````````````````````MMMMMMMMMMMM````````````````````````MMMMMMMMMM
MMMMMMMM'````````````````````````OMMMMMMMM`'MMM;```````````````````````OMMMMMMMM
MMMMMMM8````````````````````````8MMMMMMMM```;MMM````````````````````````MMMMMMMM
MMMMMM;````````````````````````'MMMMMMMM``````MMM'MMMMMMMMMN`````````````8MMMMMM
MMMMMM````````````````````````NMMMMMMM`NO;`````NMM`OMMMMMMMMMMM```````````MMMMMM
MMMMM8``````````````'8MMO;````MMMMMMM```````````MMM'```OMMMMMMM```````````8MMMMM
MMMMM'``````````````````````OMMMMMMM``````````````MM```'MMMMM8````````````;MMMMM
MMMMM'`````````````````````'MMMMMM`````````````````MM'MMMMM8``````````````;MMMMM
MMMMM8````````````````````'MMMMMM```````````````````MMMMM;````````````````8MMMMM
MMMMMM````````````````````MMMMMN`````````````````8MMMM`;``````````````````MMMMMM
MMMMMMO`````````````````;MMMMM````````````````MMMMM````M`````````````````NMMMMMM
MMMMMMM;````````````````MMMMM``````````````MMMMN````````M'``````````````8MMMMMMM
MMMMMMMM;`````````````'MMMMO```````````MMMM8`````````````8`````````````8MMMMMMMM
MMMMMMMMMM````````````MMMM``````````````````MMMN``````````;'``````````MMMMMMMMMM
MMMMMMMMMMMM`````````MMMM```````````````````````;MM'````````````````MMMMMMMMMMMM
MMMMMMMMMMMMMM``````MMM;``````````````````````````````M;`````'````MMMMMMMMMMMMMM
MMMMMMMMMMMMMMMM;``MMM`````````````````````````````````````````;MMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMN```````````````````````````````````````OMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMMMMM````````````````````````````````MMMMMMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMM8'````````````````;NMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
```
