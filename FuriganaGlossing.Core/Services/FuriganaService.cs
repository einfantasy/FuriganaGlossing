using FuriganaGlossing.Models;
using MeCab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FuriganaGlossing.Services
{
    public interface IFuriganaService
    {
        Task<List<FuriganaToken>> GetFuriganaAsync(string text);
    }

    public class FuriganaService : IFuriganaService
    {
        private readonly IConfigService _configService;
        private readonly ILogService _logService;
        private MeCabTagger? _tagger;

        public FuriganaService(IConfigService configService, ILogService logService)
        {
            _configService = configService;
            _logService = logService;
        }

        private async Task<MeCabTagger?> InitializeTaggerAsync()
        {
            if (_tagger != null) return _tagger;

            var dicPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dic");
            if (!System.IO.Directory.Exists(dicPath))
            {
                var errorMsg = $"UniDic directory not found at: {dicPath}";
                _logService.Log(errorMsg, LogLevel.Error);
                throw new System.IO.DirectoryNotFoundException(errorMsg);
            }

            _logService.Log($"Initializing MeCab with dictionary at {dicPath}...", LogLevel.Info);

            try
            {
                var tagger = MeCabTagger.Create(new MeCabParam(dicPath));
                if (tagger == null)
                {
                    throw new Exception("MeCabTagger initialization failed and returned null.");
                }

                _tagger = tagger;
                _logService.Log("MeCab initialized successfully.", LogLevel.Info);
                return tagger;
            }
            catch (Exception ex)
            {
                _logService.Log($"Failed to initialize MeCab: {ex.Message}", LogLevel.Error);
                _tagger = null; // 确保状态重置
                return null;
            }
        }

        public async Task<List<FuriganaToken>> GetFuriganaAsync(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<FuriganaToken>();

            _logService.Log("Analyzing text for furigana...", LogLevel.Info);
            var tagger = await InitializeTaggerAsync();
            if (tagger == null)
            {
                _logService.Log("Furigana analysis failed: MeCab not initialized.", LogLevel.Error);
                throw new InvalidOperationException("Furigana analysis failed: MeCab not initialized.");
            }

            var tokens = new List<FuriganaToken>();
            var nodes = tagger.ParseToNodes(text);
            int currentPos = 0;

            foreach (var node in nodes)
            {
                if (string.IsNullOrEmpty(node.Surface) || string.IsNullOrEmpty(node.Feature))
                {
                    currentPos += node.Surface?.Length ?? 0;
                    continue;
                }

                var features = node.Feature.Split(',');
                string reading = string.Empty;
                if (features.Length >= 7)
                {
                    if (features.Length <= 10)
                        reading = (features[6] != "*") ? features[7] : "";
                    else
                        reading = (features[6] != "*") ? features[9] : "";
                }

                //tokens.Add(new FuriganaToken
                //{
                //    OriginalText = node.Surface,
                //    Reading = (ContainsKanji(node.Surface) && !string.IsNullOrEmpty(reading)) ? KatakanaToHiragana(reading) : string.Empty,
                //    StartIndex = currentPos,
                //    Length = node.Surface.Length
                //});
                var alignedTokens = AlignFurigana(
                    node.Surface,
                    reading = (ContainsKanji(node.Surface) && !string.IsNullOrEmpty(reading)) ? KatakanaToHiragana(reading) : string.Empty,
                    currentPos);

                tokens.AddRange(alignedTokens);
                currentPos += node.Surface.Length;
            }

            _logService.Log($"Furigana analysis completed. Found {tokens.Count} tokens.", LogLevel.Info);
            return tokens;
        }

        private bool ContainsKanji(string text)
        {
            return Regex.IsMatch(text, @"\p{IsCJKUnifiedIdeographs}");
        }

        private string KatakanaToHiragana(string katakana)
        {
            if (string.IsNullOrEmpty(katakana)) return katakana;

            return new string(katakana.Select(c =>
                c >= '\u30A1' && c <= '\u30F6'
                    ? (char)(c - 0x60)
                    : c
            ).ToArray());
        }

        private bool IsJapaneseKana(char c)
        {
            return (c >= '\u3040' && c <= '\u309F') ||
                   (c >= '\u30A0' && c <= '\u30FF') ||
                   (c >= '\uFF66' && c <= '\uFF9D');
        }

        private List<FuriganaToken> AlignFurigana(
            string surface,
            string reading,
            int startIndex)
        {
            var result = new List<FuriganaToken>();

            if (string.IsNullOrEmpty(surface))
                return result;

            // 没有读音或没有汉字时，无须拆分注音。
            if (string.IsNullOrEmpty(reading) ||
                !ContainsKanji(surface))
            {
                result.Add(new FuriganaToken
                {
                    OriginalText = surface,
                    Reading = string.Empty,
                    StartIndex = startIndex,
                    Length = surface.Length
                });

                return result;
            }

            // 收集原文中的连续假名片段。
            var segments = new List<(int Start, int Length, bool IsKana)>();

            int pos = 0;

            while (pos < surface.Length)
            {
                bool isKana = IsJapaneseKana(surface[pos]);
                int begin = pos;

                while (pos < surface.Length &&
                       IsJapaneseKana(surface[pos]) == isKana)
                {
                    pos++;
                }

                segments.Add((begin, pos - begin, isKana));
            }

            // 将原文中的片假名锚点统一转换成平假名，
            // 再到词典读音中依次查找。
            var anchors = new List<(int SegmentIndex, int ReadingStart)>();
            int readingSearchPos = 0;

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];

                if (!segment.IsKana)
                    continue;

                string kana = surface.Substring(
                    segment.Start,
                    segment.Length);

                kana = KatakanaToHiragana(kana);

                int found = reading.IndexOf(
                    kana,
                    readingSearchPos,
                    StringComparison.Ordinal);

                // 任何锚点无法对齐时，不进行猜测，
                // 回退为整个词元的完整读音。
                if (found < 0)
                {
                    result.Add(new FuriganaToken
                    {
                        OriginalText = surface,
                        Reading = reading,
                        StartIndex = startIndex,
                        Length = surface.Length
                    });

                    return result;
                }

                anchors.Add((i, found));
                readingSearchPos = found + kana.Length;
            }

            // 如果原文存在假名锚点，
            // 根据锚点位置切分读音。
            int readingCursor = 0;

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];

                string original = surface.Substring(
                    segment.Start,
                    segment.Length);

                string segmentReading = string.Empty;

                if (!segment.IsKana)
                {
                    int nextAnchorPosition = reading.Length;

                    foreach (var anchor in anchors)
                    {
                        if (anchor.SegmentIndex > i)
                        {
                            nextAnchorPosition = anchor.ReadingStart;
                            break;
                        }
                    }

                    int readingLength = nextAnchorPosition - readingCursor;

                    if (readingLength > 0)
                    {
                        segmentReading = reading.Substring(
                            readingCursor,
                            readingLength);
                    }

                    readingCursor = nextAnchorPosition;
                }
                else
                {
                    // 找到当前假名锚点对应的读音位置。
                    var anchor = anchors.Find(
                        a => a.SegmentIndex == i);

                    string normalizedKana =
                        KatakanaToHiragana(original);

                    readingCursor = anchor.ReadingStart
                        + normalizedKana.Length;
                }

                // 只有包含汉字的片段才使用注音。
                string furigana = ContainsKanji(original)
                    ? segmentReading
                    : string.Empty;

                result.Add(new FuriganaToken
                {
                    OriginalText = original,
                    Reading = furigana,
                    StartIndex = startIndex + segment.Start,
                    Length = segment.Length
                });
            }

            return result;
        }

    }
}
