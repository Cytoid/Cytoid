package org.cytoid.gamecore

import android.app.Activity
import android.os.Build
import android.util.Log
import android.view.Display
import android.view.Surface
import android.view.SurfaceHolder
import android.view.SurfaceView
import android.view.View
import android.view.ViewGroup

object DisplayRefreshRateHelper {
    private const val TAG = "DisplayRefreshRate"
    private const val SURFACE_CALLBACK_TAG = 0x4359544F

    data class RefreshRateState(
        val currentHz: Float,
        val maxSupportedHz: Float,
        val appliedModeId: Int?,
        val appliedPreferredHz: Float?,
    )

    fun applyGameplayRefreshRate(activity: Activity, unityRootView: View?) {
        if (unityRootView == null) {
            return
        }

        val state = applyWindowRefreshRate(activity)
        bindUnitySurfaceRefreshRate(unityRootView, state.maxSupportedHz)
        scheduleDelayedSurfaceBinding(activity, unityRootView, state.maxSupportedHz)

        Log.i(
            TAG,
            "Gameplay refresh rate: current=${state.currentHz}Hz, " +
                "maxSupported=${state.maxSupportedHz}Hz, " +
                "appliedModeId=${state.appliedModeId}, " +
                "appliedPreferredHz=${state.appliedPreferredHz}",
        )
    }

    fun restoreDefaultRefreshRate(activity: Activity) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.M) {
            return
        }

        val window = activity.window ?: return
        val params = window.attributes
        params.preferredDisplayModeId = 0
        params.preferredRefreshRate = 0f
        window.attributes = params
    }

    private fun applyWindowRefreshRate(activity: Activity): RefreshRateState {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.M) {
            return RefreshRateState(60f, 60f, null, null)
        }

        val display = activity.display ?: activity.windowManager.defaultDisplay
        val currentMode = display.mode
        val bestMode = selectBestDisplayMode(display)
        val window = activity.window
        val params = window.attributes

        params.preferredDisplayModeId = bestMode.modeId
        params.preferredRefreshRate = bestMode.refreshRate
        window.attributes = params

        return RefreshRateState(
            currentHz = currentMode.refreshRate,
            maxSupportedHz = bestMode.refreshRate,
            appliedModeId = bestMode.modeId,
            appliedPreferredHz = bestMode.refreshRate,
        )
    }

    private fun selectBestDisplayMode(display: Display): Display.Mode {
        val modes = display.supportedModes
        if (modes.isEmpty()) {
            return display.mode
        }

        val currentMode = display.mode
        val sameResolutionModes =
            modes.filter { mode ->
                mode.physicalWidth == currentMode.physicalWidth &&
                    mode.physicalHeight == currentMode.physicalHeight
            }

        val candidates = if (sameResolutionModes.isNotEmpty()) sameResolutionModes else modes.toList()
        return candidates.maxBy { it.refreshRate }
    }

    private fun bindUnitySurfaceRefreshRate(unityRootView: View?, targetHz: Float) {
        if (unityRootView == null || targetHz <= 0f) {
            return
        }

        for (surfaceView in findSurfaceViews(unityRootView)) {
            attachSurfaceRefreshRateCallback(surfaceView, targetHz)
            surfaceView.holder.surface?.let { surface ->
                applySurfaceFrameRate(surface, targetHz)
            }
        }
    }

    private fun attachSurfaceRefreshRateCallback(surfaceView: SurfaceView, targetHz: Float) {
        if (surfaceView.getTag(SURFACE_CALLBACK_TAG) == true) {
            applySurfaceFrameRate(surfaceView.holder.surface, targetHz)
            return
        }
        surfaceView.setTag(SURFACE_CALLBACK_TAG, true)

        surfaceView.holder.addCallback(
            object : SurfaceHolder.Callback {
                override fun surfaceCreated(holder: SurfaceHolder) {
                    applySurfaceFrameRate(holder.surface, targetHz)
                }

                override fun surfaceChanged(holder: SurfaceHolder, format: Int, width: Int, height: Int) {
                    applySurfaceFrameRate(holder.surface, targetHz)
                }

                override fun surfaceDestroyed(holder: SurfaceHolder) = Unit
            },
        )
    }

    private fun applySurfaceFrameRate(surface: Surface?, targetHz: Float) {
        if (surface == null || !surface.isValid || targetHz <= 0f) {
            return
        }
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.R) {
            return
        }

        runCatching {
            surface.setFrameRate(targetHz, Surface.FRAME_RATE_COMPATIBILITY_DEFAULT)
        }.onFailure { error ->
            Log.w(TAG, "Unable to set surface frame rate", error)
        }
    }

    private fun scheduleDelayedSurfaceBinding(activity: Activity, unityRootView: View?, targetHz: Float) {
        if (unityRootView == null) {
            return
        }

        val handler = android.os.Handler(android.os.Looper.getMainLooper())
        val delaysMs = longArrayOf(100L, 500L, 1_000L)
        for (delay in delaysMs) {
            handler.postDelayed({
                if (activity.isFinishing || activity.isDestroyed) {
                    return@postDelayed
                }
                applyWindowRefreshRate(activity)
                bindUnitySurfaceRefreshRate(unityRootView, targetHz)
            }, delay)
        }
    }

    private fun findSurfaceViews(root: View): List<SurfaceView> {
        if (root is SurfaceView) {
            return listOf(root)
        }
        if (root !is ViewGroup) {
            return emptyList()
        }

        val surfaceViews = mutableListOf<SurfaceView>()
        for (index in 0 until root.childCount) {
            surfaceViews.addAll(findSurfaceViews(root.getChildAt(index)))
        }
        return surfaceViews
    }
}
