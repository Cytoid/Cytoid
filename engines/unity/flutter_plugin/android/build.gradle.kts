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

android {
    namespace = "org.cytoid.gamecore"
    compileSdk = 36

    defaultConfig {
        // Unity 6 (6000.0.x) requires API 24+; the runtime probe in
        // CytoidGameCoreBridge fails fast if the AAR is missing.
        minSdk = 24
        consumerProguardFiles("consumer-rules.pro")
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    sourceSets {
        getByName("main") {
            java.srcDirs("src/main/kotlin")
        }
        getByName("test") {
            java.srcDirs("src/test/kotlin")
        }
    }

    testOptions {
        unitTests {
            isReturnDefaultValues = true
        }
    }

    packaging {
        jniLibs {
            if (unityCoreAar.exists()) {
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
    testImplementation("junit:junit:4.13.2")
    testImplementation("org.jetbrains.kotlin:kotlin-test")
    testImplementation("io.mockk:mockk:1.13.10")
    // Real org.json for JVM unit tests. android.jar ships stubbed org.json
    // classes that return null from put()/toString() under
    // isReturnDefaultValues=true, which breaks JSON-building assertions.
    testImplementation("org.json:json:20240303")
}

rootProject.subprojects {
    plugins.withId("com.android.application") {
        if (unityCoreAar.exists()) {
            dependencies.add("implementation", unityAars)
        }
    }
}
