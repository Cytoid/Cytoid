require 'pathname'

Pod::Spec.new do |s|
  s.name             = 'cytoid_game_core'
  s.version          = '0.0.1'
  s.summary          = 'Cytoid game core host protocol and fullscreen runtime bridge.'
  s.description      = 'Flutter plugin that hosts the Cytoid game core through an engine-agnostic protocol.'
  s.homepage         = 'https://cytoid.io'
  s.license          = { :file => '../LICENSE' }
  s.author           = { 'Cytoid' => 'team@cytoid.io' }
  s.source           = { :path => '.' }
  s.source_files     = 'cytoid_game_core/Sources/cytoid_game_core/**/*.swift'
  s.dependency 'Flutter'
  s.platform = :ios, '13.0'
  s.swift_version = '5.0'

  unity_framework_candidates = [
    ENV['CYTOID_GAME_CORE_IOS_UNITY_FRAMEWORK'],
    '../.cytoid_game_core/artifacts/unity/ios/UnityFramework.xcframework',
    '../.cytoid_game_core/artifacts/unity/ios/UnityFramework.framework',
  ].compact.map do |path|
    absolute_path = Pathname.new(path).absolute? ? path : File.expand_path(path, __dir__)
    [path, absolute_path]
  end
  unity_framework = unity_framework_candidates.find { |_, absolute_path| File.exist?(absolute_path) }

  if unity_framework
    unity_framework_path, unity_framework_absolute_path = unity_framework
    s.vendored_frameworks = unity_framework_path
    s.pod_target_xcconfig = {
      'DEFINES_MODULE' => 'YES',
      'SWIFT_ACTIVE_COMPILATION_CONDITIONS' => '$(inherited) CYTOID_UNITY_FRAMEWORK_AVAILABLE',
      'FRAMEWORK_SEARCH_PATHS' => '$(inherited) "' + File.dirname(unity_framework_absolute_path) + '"',
      'OTHER_LDFLAGS' => '$(inherited) -ObjC'
    }
  else
    s.pod_target_xcconfig = {
      'DEFINES_MODULE' => 'YES'
    }
  end
end
