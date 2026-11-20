# 构建与发布

游戏由团结引擎 1.8.5 生成 Windows x64 播放器与 macOS 应用。GitHub Actions 使用 Windows 和 macOS 托管运行器分别生成安装程序和磁盘映像，并在两份产物都通过检查后发布 GitHub Release。

## 生成播放器

在已激活团结引擎、安装 Windows 与 Mac Mono Build Support 的 Windows 机器上运行以下命令。输出目录需为空。

```powershell
./scripts/build-player.ps1 -Editor 'C:\Program Files\Tuanjie\Hub\Editor\2022.3.62t7\Editor\Tuanjie.exe' -Platform Windows -OutputDirectory Builds/Windows
./scripts/build-player.ps1 -Editor 'C:\Program Files\Tuanjie\Hub\Editor\2022.3.62t7\Editor\Tuanjie.exe' -Platform macOS -OutputDirectory Builds/macOS
```

将 Windows 构建目录中的内容打包成 `player-windows.zip`。将 `PicoPark.app` 保留为顶层目录，打包成 `player-macos.zip`。从干净的已提交源码生成这两个输入，版本标签使用 `vMAJOR.MINOR.PATCH`。

## 运行 GitHub Actions

创建 Release 草稿，`--target` 指向生成播放器时的提交，并上传两个 ZIP。保存草稿的数字 ID，用它运行 `release.yml`。

```sh
gh release create v1.0.0 player-windows.zip player-macos.zip --draft --target "$(git rev-parse HEAD)" --title "1.0.0"
gh api repos/{owner}/{repo}/releases --jq '.[] | select(.draft and .tag_name == "v1.0.0") | .id'
gh workflow run release.yml -f release-id=<数字ID> -f tag=v1.0.0
```

发布会附上 Windows 安装程序、macOS DMG 和 SHA-256 校验文件。Windows 安装程序按用户安装。macOS 应用使用临时签名，适用于预览分发；Developer ID 签名和公证需另行配置。
