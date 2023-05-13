# Player Footsteps

Adds footstep sounds for the player depending on the surface he walks through.  
Sosigs will react to these sounds.

Detection range is larger if the player is running.  
You can sneak by physically lowering Your head or enabling slow walking option in the config.  


[Footsteps Preview](https://www.youtube.com/watch?v=3_2KRXZhUFA)  
[AI detection Preview](https://www.youtube.com/watch?v=7unTFsOa2D8)

Configs are in: 
r2modman -> Config Editor   
BepInEx\config\h3vr.kodeman.playerfootsteps.cfg

You can customize:
- Does AI react to Your footsteps
- How far AI can hear You
- Footsteps volume
- Player height (having head above that height is considered flying)
- Crouch height
- Replace all footstep with meat
- Walk slowly without crouching
- Speed considered slow walk


# Changelog
### 1.0.3  
- The footsteps will now work if You're using Navblock as a walking surface in Your map.  

### 1.0.2  
- Quiet walking option, allows You to sneak without the need to crouch
- You can customize the speed that counts as quiet walking

### 1.0.1
- Rock footstep sound effect
- Meaty Feet option, replaces every sound with meat
- There are no PMats in Northest Dakota, so I hardcoded new default snow sound for it