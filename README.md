# FuriganaGlossing

A .NET 8 WPF application designed to process Japanese text from images or input, providing automatic furigana (readings) and translations, rendered in a professional HTML format.

## 🌟 Features

- **OCR Integration**: Extracts Japanese text from images via an external OCR service.
- **Automatic Furigana**: Utilizes `MeCab.DotNet` with the UniDic dictionary to analyze text and generate accurate hiragana readings for kanji.
- **Translation**: Integrates a translation service to provide meanings for the extracted Japanese text.
- **Rich Rendering**: Uses `Microsoft.Web.WebView2` to render results using HTML `<ruby>` tags, ensuring a clean and readable furigana display.
- **MVVM Architecture**: Built with `CommunityToolkit.Mvvm` for a scalable and maintainable codebase.

## 🛠 Tech Stack

- **Framework**: .NET 8.0 (WPF)
- **NLP**: MeCab (via `MeCab.DotNet`)
- **UI**: WPF, WebView2
- **Pattern**: MVVM (Community Toolkit)
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection

## 🚀 Getting Started

### Prerequisites

- **.NET 8 SDK**
- **MeCab Dictionary**: The application expects a MeCab dictionary (e.g., UniDic) to be located in a folder named `dic` within the application's base directory.

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/FuriganaGlossing.git
   ```
2. Open the solution in Visual Studio 2022.
3. Ensure the `dic` folder is placed in the output directory (`bin/Debug/net8.0-windows/dic`).
4. Build and run the application.

### Configuration

You can configure the OCR and Translation server endpoints through the application's settings window.
UniDic download link: https://cotonoha-dic.s3-ap-northeast-1.amazonaws.com/unidic-3.1.0.zip
Please extract the contents of the zip file into the `dic` folder in the output directory.

---

# FuriganaGlossing (中文)

这是一个基于 .NET 8 WPF 开发的日语文本处理工具。它能够从图片或输入中提取日语文本，自动标注振假名（Furigana）并提供翻译，最终以专业的 HTML 格式进行渲染。

## 🌟 功能特点

- **OCR 识别**: 通过外部 OCR 服务从图片中提取日语文本。
- **自动振假名**: 集成 `MeCab.DotNet` 并使用 UniDic 词典分析文本，为汉字生成准确的平假名读音。
- **文本翻译**: 集成翻译服务，为提取的日语文本提供中文/英文翻译。
- **富文本渲染**: 使用 `Microsoft.Web.WebView2` 控件，利用 HTML 的 `<ruby>` 标签实现标准的日语振假名显示效果。
- **MVVM 架构**: 采用 `CommunityToolkit.Mvvm` 构建，确保代码结构清晰且易于维护。

## 🛠 技术栈

- **框架**: .NET 8.0 (WPF)
- **自然语言处理**: MeCab (通过 `MeCab.DotNet`)
- **UI 界面**: WPF, WebView2
- **设计模式**: MVVM (Community Toolkit)
- **依赖注入**: Microsoft.Extensions.DependencyInjection

## 🚀 快速开始

### 前置条件

- **.NET 8 SDK**
- **MeCab 词典**: 程序运行需要 MeCab 词典（如 UniDic），请将其放置在程序根目录下的 `dic` 文件夹中。

### 安装步骤

1. 克隆仓库:
   ```bash
   git clone https://github.com/your-username/FuriganaGlossing.git
   ```
2. 使用 Visual Studio 2022 打开解决方案。
3. 确保 `dic` 文件夹位于输出目录中 (`bin/Debug/net8.0-windows/dic`)。
4. 编译并运行程序。

### 配置

您可以通过程序的设置窗口配置 OCR 和翻译服务器的 API 端点。
UniDic 下载链接: https://cotonoha-dic.s3-ap-northeast-1.amazonaws.com/unidic-3.1.0.zip
请将 zip 文件的内容解压到输出目录中的 `dic` 文件夹里。