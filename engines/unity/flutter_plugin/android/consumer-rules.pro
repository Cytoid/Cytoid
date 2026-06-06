# Unity/IL2CPP symbols are preserved by the Unity AAR artifacts.

# Unity calls this exact JVM class and method from C# via AndroidJavaClass:
# org.cytoid.gamecore.UnityHostCallback.onMessage(String).
# Keep the name stable in release builds so game.ready, game.pong, game.logs.batch,
# and game.play.result can be delivered back to Flutter after R8/ProGuard runs.
-keep class org.cytoid.gamecore.UnityHostCallback { *; }
