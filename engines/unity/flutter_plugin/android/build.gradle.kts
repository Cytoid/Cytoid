group = "org.cytoid.gamecore"
version = "1.0"

buildscript {
    val kotlinVersion = "2.3.20"
    repositories {
        google()
        mavenCentral()
    }

    dependencies {
        classpath("com.android.tools.build:gradle:9.0.1")
        classpath("org.jetbrains.kotlin:kotlin-gradle-plugin:$kotlinVersion")
    }
}

allprojects {
    repositories {
        google()
        mavenCentral()
        flatDir {
            dirs("$projectDir/../.cytoid_game_core/artifacts/unity/android")
        }
    }
}

plugins {
    id("com.android.library")
}

val unityArtifactDir = file("$projectDir/../.cytoid_game_core/artifacts/unity/android")
val unityCoreAar = file("$unityArtifactDir/cytoid-unity-core.aar")
val unityAars =
    if (unityArtifactDir.exists()) {
        fileTree(mapOf("dir" to unityArtifactDir, "include" to listOf("*.aar")))
    } else {
        files()
    }
val unityArtifactAvailable = unityCoreAar.exists()

android {
    namespace = "org.cytoid.gamecore"
    compileSdk = 36

    defaultConfig {
        minSdk = if (unityArtifactAvailable) 24 else 21
        consumerProguardFiles("consumer-rules.pro")
        buildConfigField("boolean", "UNITY_ARTIFACT_AVAILABLE", unityArtifactAvailable.toString())
    }

    buildFeatures {
        buildConfig = true
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    sourceSets {
        getByName("main") {
            java.srcDirs("src/main/kotlin")
        }
    }

    packaging {
        jniLibs {
            if (unityArtifactAvailable) {
                useLegacyPackaging = true
            }
            pickFirsts += listOf("**/libunity.so", "**/libil2cpp.so", "**/libmain.so")
        }
    }
}

kotlin {
    compilerOptions {
        jvmTarget = org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17
    }
}

dependencies {
}

rootProject.subprojects {
    plugins.withId("com.android.application") {
        if (unityArtifactAvailable) {
            dependencies.add("implementation", unityAars)
        }
    }
}
