package org.cytoid.gamecore

import android.util.Log

object UnityHostCallback {
    private const val TAG = "UnityHostCallback"

    @JvmStatic
    fun onMessage(json: String) {
        val bridge = CytoidGameCoreBridge.instance
        if (bridge == null) {
            Log.w(TAG, "Dropping inbound message because the game core bridge is not registered.")
            return
        }
        bridge.onUnityMessage(json)
    }
}
