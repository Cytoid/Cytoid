package org.cytoid.gamecore

import android.app.Activity
import io.flutter.embedding.engine.plugins.FlutterPlugin
import io.flutter.embedding.engine.plugins.activity.ActivityAware
import io.flutter.embedding.engine.plugins.activity.ActivityPluginBinding
import io.flutter.plugin.common.EventChannel
import io.flutter.plugin.common.MethodCall
import io.flutter.plugin.common.MethodChannel

class CytoidGameCorePlugin : FlutterPlugin, ActivityAware, MethodChannel.MethodCallHandler {
    private var methodChannel: MethodChannel? = null
    private var eventChannel: EventChannel? = null
    private var bridge: CytoidGameCoreBridge? = null
    private var activity: Activity? = null

    override fun onAttachedToEngine(binding: FlutterPlugin.FlutterPluginBinding) {
        methodChannel = MethodChannel(binding.binaryMessenger, METHOD_CHANNEL_NAME).also {
            it.setMethodCallHandler(this)
        }
        eventChannel = EventChannel(binding.binaryMessenger, EVENT_CHANNEL_NAME)
    }

    override fun onDetachedFromEngine(binding: FlutterPlugin.FlutterPluginBinding) {
        methodChannel?.setMethodCallHandler(null)
        methodChannel = null
        eventChannel?.setStreamHandler(null)
        eventChannel = null
        bridge?.dispose()
        bridge = null
    }

    override fun onAttachedToActivity(binding: ActivityPluginBinding) {
        activity = binding.activity
        attachBridgeIfReady()
    }

    override fun onDetachedFromActivityForConfigChanges() {
        activity = null
        bridge?.detachActivity()
    }

    override fun onReattachedToActivityForConfigChanges(binding: ActivityPluginBinding) {
        activity = binding.activity
        attachBridgeIfReady()
    }

    override fun onDetachedFromActivity() {
        activity = null
        bridge?.detachActivity()
    }

    override fun onMethodCall(call: MethodCall, result: MethodChannel.Result) {
        val currentBridge = bridge
        if (currentBridge == null) {
            result.error("no_activity", "Game core plugin is not attached to an Activity.", null)
            return
        }

        when (call.method) {
            "send" -> {
                val json = call.arguments as? String
                if (json == null) {
                    result.error("invalid_argument", "Expected envelope JSON string for send.", null)
                    return
                }
                currentBridge.onOutboundMessage(json)
                result.success(null)
            }
            "getEngineMode" -> result.success(currentBridge.engineMode)
            "queryRuntimeStatus" -> result.success(currentBridge.runtimeStatus())
            "ensureRuntimeStarted" -> {
                currentBridge.ensureRuntimeStarted()
                result.success(null)
            }
            "showGameSurface" -> currentBridge.showGameSurface(result)
            "hideGameSurface" -> {
                currentBridge.hideGameSurface()
                result.success(null)
            }
            else -> result.notImplemented()
        }
    }

    private fun attachBridgeIfReady() {
        val currentActivity = activity ?: return
        val currentEventChannel = eventChannel ?: return
        val currentBridge = CytoidGameCoreBridge.getOrCreate(currentActivity).also {
            it.attachActivity(currentActivity)
        }
        bridge = currentBridge
        currentEventChannel.setStreamHandler(currentBridge)
    }

    companion object {
        private const val METHOD_CHANNEL_NAME = "cytoid/game_core"
        private const val EVENT_CHANNEL_NAME = "cytoid/game_core/events"
    }
}
