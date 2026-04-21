# Title screen shortcuts

The new buttons in reading order: Cyber Grind, Angry Level Loader, Plugin Config, Sandbox

<img alt="image" src="https://github.com/user-attachments/assets/41b9f8b1-3a63-4df5-975e-23101db47cd2" />

# Building

- Use `setup-libs.ps1`
    - You might have to specify the path of ULTRAKILL and r2modman profile manually via the `$UltrakillPath` and `$R2ModmanProfilePath` arguments, respectively
    - Example: `.\setup-libs.ps1 -UltrakillPath "D:\steam gamez\steamapps\common\ULTRAKILL" -R2ModmanProfilePath "E:\someFolder"`
- And then `make-package.ps1`
- You'll get a zip file. That's the package to be imported in r2modman
