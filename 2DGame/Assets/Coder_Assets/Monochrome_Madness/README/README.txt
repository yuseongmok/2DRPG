# Shadow Dash 2D Template
by [Your Name or Studio]

---

## 📦 What’s Included

- Smooth 2D player movement (run, jump, dash)
- Fully rigged and animated cutout ninja character (limb-based)
- Camera follow with lookahead & screen shake
- Multi-layer parallax background system
- Dash and jump particle effects
- Idle, Run, Jump, Dash blend animations
- Ghost trail on dash
- Ready-to-edit scene with prefab setup

---

## 🚀 How to Use

1. Open the included scene:  
   `Assets/Coder_Assets/Monochrome_Madness/ExampleScene.unity`

2. Hit Play and test the controller

3. You can find all key components here:
   - **Player Controller:** `Assets/Coder_Assets/Monochrome_Madness/PlayerController.cs`
   - **Camera Follow:** `Assets/Coder_Assets/Monochrome_Madness/CameraFollow2D.cs`
   - **Parallax System:** `Assets/Coder_Assets/Monochrome_Madness/ParallaxLayerController.cs`

4. To customize:
   - Swap out sprite images in `art/Ninja_Limbs`
   - Adjust animation timings in the Animator
   - Add more parallax layers in the `ParallaxScript`

---

## ⚙️ Requirements

- Unity 2021.3+ (or your target version)
- URP or Built-In Render Pipeline compatible
- 2D Animation Package (pre-installed in this project)

---

## 📚 Helpful Tips

- Make sure your Main Camera is tagged as `MainCamera`
- To tweak movement feel, adjust values in the PlayerController
- Use the Animator parameters: `Speed` (float), `Jump` (trigger), `Dash` (trigger)

---

## ❓ Need Help?

For questions or bug reports, contact:
📧 artbykirill@gmail.com

---

Thank you for using this asset! 🙏
