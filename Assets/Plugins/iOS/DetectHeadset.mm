#import <AVFoundation/AVFoundation.h>

extern "C" {

	bool DetectHeadset() {
		
        AVAudioSessionRouteDescription* route = [[AVAudioSession sharedInstance] currentRoute];
        
        for (AVAudioSessionPortDescription* desc in [route outputs]) {
            if ([[desc portType] isEqualToString:AVAudioSessionPortHeadphones])
                return true;
            if ([[desc portType] isEqualToString:AVAudioSessionPortBluetoothHFP])
                return true;
        }
        
        return false;
        
	}
	
}
