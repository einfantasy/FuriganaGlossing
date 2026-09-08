# Project: FuriganaGlossing

An OCR-based Japanese text glossing and translation software developed with WPF.

## 1. Core Functionality
- **Image Input**: Receive images via `Ctrl+V` from the clipboard.
- **OCR**: Send images to a remote LLM Server to recognize Japanese text and their bounding box coordinates.
- **Furigana (Glossing)**: Use MeCab + UniDic to provide readings (hiragana) for Kanji characters.
- **Translation**: Use a remote LLM Server to translate the recognized Japanese text into Chinese.
- **Visual Rendering**: Display the original image with furigana positioned precisely above the kanji on a Canvas overlay.
- **Configuration**: A settings window to configure and persist the remote LLM Server address.

## 2. Technical Specification
- **Framework**: .NET 6/8 + WPF
- **Architecture**: MVVM (Model-View-ViewModel)
- **Morphological Analysis**: MeCab.NET + UniDic dictionary
- **Communication**: `HttpClient` for remote OCR and LLM Server APIs (JSON/REST)
- **Configuration**: JSON-based local storage (`AppData`)
- **UI Components**:
    - `Image` control for the source picture.
    - `Canvas` overlay for dynamic text rendering.
    - `TextBlock` pairs for Ruby-style (Furigana + Kanji) display.

## 3. Development Roadmap

### Phase 1: Foundation
- [ ] Initialize WPF project with MVVM (CommunityToolkit.Mvvm).
- [ ] Implement `AppConfig` and `ConfigService` for JSON persistence.
- [ ] Create the Configuration Window for server settings.

### Phase 2: OCR Pipeline
- [ ] Implement `Ctrl+V` clipboard image capture.
- [ ] Develop `OcrService` for remote API communication (Image $\rightarrow$ Text + Coords).
- [ ] Create basic image display and text output UI.

### Phase 3: Linguistic Processing
- [ ] Integrate `MeCab.NET` and configure UniDic dictionary paths.
- [ ] Implement `FuriganaService` to map Kanji $\rightarrow$ Hiragana.
- [ ] Develop `TranslationService` for remote LLM translation (Japanese $\rightarrow$ Chinese).

### Phase 4: Advanced UI Rendering
- [ ] Implement the `Canvas` overlay on top of the `Image` control.
- [ ] Develop the dynamic rendering logic for Ruby-style text based on OCR coordinates.
- [ ] Create the translation results display panel.

### Phase 5: Polish & QA
- [ ] Implement async loading states and error handling for network requests.
- [ ] Optimize UI responsiveness and layout.
- [ ] Perform end-to-end testing from image input to final rendering.

## 4. Implementation Notes
- **Code Style**: No comments unless requested. Follow existing C# conventions.
- **Async**: Use `async/await` for all I/O and network operations to keep the UI responsive.
- **Coordinates**: Ensure image scaling in WPF is accounted for when mapping OCR coordinates to the `Canvas`.
