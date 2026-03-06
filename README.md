# 汉维互译翻译器 (Uyghur Translator)

一个支持汉语和维吾尔语互译的翻译软件，集成了维语语音合成功能。

## 功能特性

- ✅ **翻译功能**：支持汉语 ↔ 维吾尔语互译
- ✅ **语音合成**：维语朗读（基于 ONNX 和 MMS-TTS 模型）
- ✅ **离线词典**：常用词汇支持离线翻译
- ✅ **音量调节**：可调节语音播放音量
- ✅ **复制功能**：一键复制翻译结果
- ✅ **无需 Python**：纯 C# 实现，单文件运行

## 技术栈

- **框架**: .NET 8.0 (Windows)
- **翻译 API**: AppWorlds Translate API
- **语音合成**: ONNX Runtime + MMS-TTS
- **音频播放**: NAudio

## 使用方法

### 直接运行
1. 从 [Releases](https://github.com/YOUR_USERNAME/UyghurTranslator/releases) 下载最新版本
2. 解压到任意目录
3. 运行 `汉维互译翻译器.exe`

### 自行编译
```bash
# 克隆仓库
git clone https://github.com/YOUR_USERNAME/UyghurTranslator.git
cd UyghurTranslator

# 发布应用
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish

# 复制 ONNX 模型
xcopy /E /I onnx_model publish\onnx_model
```

## 项目结构

```
UyghurTranslator/
├── MainForm.cs           # 主窗口 UI
├── TranslatorEngine.cs   # 翻译引擎
├── TTSEngine.cs          # 语音合成引擎
├── UyghurTranslator.csproj
├── onnx_model/           # ONNX 模型文件
│   ├── mms_tts_uighur.onnx
│   ├── vocab.json
│   └── config.json
└── README.md
```

## API 说明

### 翻译 API
使用 [AppWorlds Translate API](https://appworlds.cn/translate/)
- 支持 100+ 种语言
- 免费额度：2 秒/次，1000 次/日

### 语音模型
使用 Facebook MMS-TTS 模型（维吾尔语）
- 模型来源：Hugging Face
- 导出格式：ONNX

## 注意事项

1. **系统要求**: Windows 10/11
2. **网络连接**: 翻译功能需要网络连接
3. **模型文件**: 首次使用需确保 `onnx_model` 文件夹完整

## 开发说明

### 添加新语言支持
修改 `TranslatorEngine.cs` 中的语言检测逻辑和 API 调用参数。

### 更换翻译 API
在 `TranslatorEngine.cs` 中实现新的翻译接口。

## 许可证

MIT License

## 致谢

- Facebook MMS-TTS 模型
- AppWorlds 翻译 API
- ONNX Runtime
- NAudio
