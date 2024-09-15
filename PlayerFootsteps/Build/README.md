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


# Making own sound packs:  
Download the mod to see which sounds are available.  
For example, there is Concrete.wav sound.  
If You want to replace it, simply create Your own mod that has sounds with correct naming:  
PlayerFootsteps_Concrete.wav  
PlayerFootsteps_Concrete2.wav  
PlayerFootsteps_Concrete3.wav  
PlayerFootsteps_Concrete4.wav  
PlayerFootsteps_Concrete5.wav  
This will result in concrete sound to be replaced with 5 custom sounds that will be played randomly.  

# Changelog
### 1.1.0  
- Multiple sounds per type support.
- It's now possible to make replace packs. Tutorial above.