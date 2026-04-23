# Title screen shortcuts

The new buttons in reading order: Cyber Grind, Angry Level Loader, Plugin Config, Sandbox

<img alt="image" src="https://github.com/user-attachments/assets/95c453e1-0c2f-43f6-93ff-28e4e4e83884" />

# Known limitations

- Custom level and plugin config buttons show even when they're unavailable

# Building

- Use `setup-libs.ps1`
    - You might have to specify the path of ULTRAKILL and r2modman profile manually via the `$UltrakillPath` and `$R2ModmanProfilePath` arguments, respectively
    - Example: `.\setup-libs.ps1 -UltrakillPath "D:\steam gamez\steamapps\common\ULTRAKILL" -R2ModmanProfilePath "E:\someFolder"`
- And then `make-package.ps1`
- You'll get a zip file. That's the package to be imported in r2modman
