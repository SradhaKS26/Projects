allprojects {
    repositories {
        google()
        mavenCentral()
    }
}

val newBuildDir: Directory =
    rootProject.layout.buildDirectory
        .dir("../../build")
        .get()
rootProject.layout.buildDirectory.value(newBuildDir)

subprojects {
    val newSubprojectBuildDir: Directory = newBuildDir.dir(project.name)
    project.layout.buildDirectory.value(newSubprojectBuildDir)
}
subprojects {
    project.evaluationDependsOn(":app")
}

// file_picker still writes compileSdk 34 in its own android {} block. Setting
// 36 when the plugin is applied is overwritten; afterEvaluate runs after that.
fun Project.forceCompileSdk(version: Int) {
    val android = extensions.findByName("android") ?: return
    val setter =
        android.javaClass.methods.firstOrNull { method ->
            method.name in listOf("setCompileSdk", "setCompileSdkVersion") &&
                method.parameterCount == 1 &&
                (method.parameterTypes[0] == Int::class.javaPrimitiveType ||
                    method.parameterTypes[0] == Integer::class.java)
        }
    setter?.invoke(android, version)
}

subprojects {
    afterEvaluate {
        if (pluginManager.hasPlugin("com.android.library") ||
            pluginManager.hasPlugin("com.android.application")
        ) {
            forceCompileSdk(36)
        }
    }
}

tasks.register<Delete>("clean") {
    delete(rootProject.layout.buildDirectory)
}
