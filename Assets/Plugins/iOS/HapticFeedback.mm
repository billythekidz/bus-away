#import <UIKit/UIKit.h>

// Called from C# HapticFeedback.cs via DllImport("__Internal")
// style: 0 = Light, 1 = Medium, 2 = Heavy
extern "C" void _TriggerImpactFeedback(int style)
{
    if (@available(iOS 10.0, *))
    {
        UIImpactFeedbackStyle feedbackStyle;
        switch (style)
        {
            case 1:  feedbackStyle = UIImpactFeedbackStyleMedium; break;
            case 2:  feedbackStyle = UIImpactFeedbackStyleHeavy;  break;
            default: feedbackStyle = UIImpactFeedbackStyleLight;  break;
        }
        UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:feedbackStyle];
        [generator prepare];
        [generator impactOccurred];
    }
}
