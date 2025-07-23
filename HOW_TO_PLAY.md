# 🎮 How to Play and Test Crowd Multiplier 3D

## 🚀 Quick Start Guide

### Option 1: Play in Unity Editor (Recommended for Testing)

1. **Open Unity Hub** and add the project:
   - Open Unity Hub
   - Click "Add" and navigate to: `c:\Users\tamir\Downloads\Kuppy 3D\crowd-multiplier-3d`
   - Select Unity 2023.3 LTS (or latest available)

2. **Open the Project:**
   - Unity will import all packages and scripts
   - Wait for compilation to complete

3. **Open the Game Scene:**
   - In Project window, navigate to `Assets/Scenes/`
   - Double-click `GameScene.unity`

4. **Press Play:**
   - Click the ▶️ Play button in Unity Editor
   - Use keyboard controls to test:
     - **WASD** or **Arrow Keys** for movement
     - **Mouse** for touch simulation

### Option 2: Build and Test on Mobile

#### Android Build:
```powershell
# Install Android SDK and NDK through Unity Hub
# Set Android build target in Unity:
# File > Build Settings > Android > Switch Platform
```

#### iOS Build:
```powershell
# Requires Xcode on macOS
# File > Build Settings > iOS > Switch Platform
```

### Option 3: WebGL Build for Browser Testing

1. **Build for WebGL:**
   - File > Build Settings
   - Select WebGL platform
   - Click "Switch Platform"
   - Click "Build and Run"

## 🎯 Game Controls

### Desktop Testing:
- **Movement:** WASD or Arrow Keys
- **Mouse:** Click and drag for touch simulation
- **Space:** Jump/interact with gates
- **ESC:** Pause menu

### Mobile Controls:
- **Touch and Drag:** Move player left/right
- **Tap:** Interact with gates and obstacles
- **Swipe:** Quick movements

## 🧪 Testing Features

### Core Gameplay:
1. **Player Movement:** Test smooth movement and responsiveness
2. **Gate Interactions:** Walk through gates to multiply crowd
3. **Obstacle Avoidance:** Navigate around obstacles
4. **Level Progression:** Complete levels to advance

### Enterprise Features Testing:
1. **Analytics Dashboard:** 
   - Check Console for analytics events
   - Monitor performance metrics in real-time

2. **ML Analytics:**
   - Play for 5+ minutes to trigger ML predictions
   - Check for difficulty adjustments

3. **A/B Testing:**
   - Multiple play sessions will show different variants
   - Analytics track conversion rates

4. **Performance Monitoring:**
   - Monitor FPS, memory usage in Console
   - Auto-optimization triggers on performance issues

## 📊 Development Testing Commands

### Terminal Testing (PowerShell):
```powershell
# Navigate to project
cd "c:\Users\tamir\Downloads\Kuppy 3D\crowd-multiplier-3d"

# Run Unity in batch mode for testing
"C:\Program Files\Unity\Hub\Editor\2023.3.0f1\Editor\Unity.exe" -batchmode -projectPath . -executeMethod BuildManager.BuildAll -quit

# Run automated tests
"C:\Program Files\Unity\Hub\Editor\2023.3.0f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testResults results.xml -quit

# Check build logs
Get-Content "Logs\build.log" | Select-Object -Last 50
```

### Unity Console Commands:
```csharp
// In Unity Console (Window > General > Console)
// Check for analytics events
[Analytics] Event tracked: player_movement
[Analytics] Performance: FPS=60, Memory=245MB
[ML] Churn risk prediction: 0.23 (Low risk)
[Production] Auto-optimization triggered: Quality reduced
```

## 🔧 Troubleshooting

### Common Issues:

1. **Compilation Errors:**
   - Unity version mismatch: Use Unity 2023.3 LTS or later
   - Missing packages: Window > Package Manager > Install required packages

2. **Performance Issues:**
   - Reduce crowd size in CrowdController settings
   - Lower graphics quality in Project Settings

3. **Analytics Not Working:**
   - Check internet connection for Unity Analytics
   - Verify Unity Services configuration

4. **Build Failures:**
   - Check Player Settings for target platform
   - Verify SDK installations for mobile builds

### Performance Targets:
- **Desktop:** 60+ FPS, <512MB RAM
- **Mobile:** 30+ FPS, <256MB RAM
- **WebGL:** 30+ FPS, smooth gameplay

## 📱 Mobile Testing Instructions

### Android Testing:
1. Enable Developer Options on Android device
2. Enable USB Debugging
3. Connect device via USB
4. Build and Run from Unity (File > Build Settings > Build and Run)

### iOS Testing:
1. Connect iPhone/iPad via USB
2. Build from Unity to Xcode project
3. Open in Xcode and deploy to device
4. Trust developer certificate on device

## 📈 Analytics & Monitoring

### Real-time Monitoring:
- Open Unity Console to see live analytics events
- Performance metrics update every 5 seconds
- ML predictions appear after 2-3 minutes of play

### Dashboard Features:
- Player behavior tracking
- Performance optimization alerts
- A/B test results
- Churn prediction scores

## 🎮 Gameplay Features to Test

### Core Mechanics:
1. **Crowd Multiplication:** Walk through green gates
2. **Obstacle Navigation:** Avoid red obstacles
3. **Level Completion:** Reach the end with maximum crowd
4. **Progressive Difficulty:** Levels get harder over time

### Advanced Features:
1. **Dynamic Difficulty:** Game adjusts based on performance
2. **Personalization:** UI adapts to player behavior
3. **Performance Optimization:** Auto-quality adjustment
4. **Analytics Integration:** Real-time behavior tracking

## 🚀 Production Deployment

Once testing is complete:
1. **Build for Production:** Use ProductionDeploymentManager settings
2. **Enable Security:** Full encryption and tamper protection
3. **Deploy to Stores:** Google Play Store, App Store
4. **Monitor Live:** Real-time analytics and performance

---

**Ready to Play!** 🎉

The game is fully functional with enterprise-grade features. Start with Unity Editor testing, then progress to mobile builds for full experience testing.

**Support:** Check FINAL_STATUS_REPORT.md for complete feature documentation.
