using FuriganaGlossing.Models;
using MeCab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FuriganaGlossing.Services
{
    public interface IFuriganaService
    {
        Task<List<FuriganaToken>> GetFuriganaAsync(string text);
    }

    public class FuriganaService : IFuriganaService
    {
        private readonly IConfigService _configService;
        private MeCabTagger _tagger;

        public FuriganaService(IConfigService configService)
        {
            _configService = configService;
        }

        private async Task InitializeTaggerAsync()
        {
            if (_tagger != null) return;

            var dicPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dic");
            if (!System.IO.Directory.Exists(dicPath))
            {
                var errorMsg = $"UniDic directory not found at: {dicPath}";
                App.LogService.Log(errorMsg, LogLevel.Error);
                throw new System.IO.DirectoryNotFoundException(errorMsg);
            }

            App.LogService.Log($"Initializing MeCab with dictionary at {dicPath}...", LogLevel.Info);

            try
            {
                _tagger = MeCabTagger.Create(new MeCabParam(dicPath));
                if (_tagger == null)
                {
                    throw new Exception("MeCabTagger initialization failed and returned null.");
                }

                App.LogService.Log("MeCab initialized successfully.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                App.LogService.Log($"Failed to initialize MeCab: {ex.Message}", LogLevel.Error);
                _tagger = null; // 确保状态重置
            }
        }

        public async Task<List<FuriganaToken>> GetFuriganaAsync(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<FuriganaToken>();

            App.LogService.Log("Analyzing text for furigana...", LogLevel.Info);
            await InitializeTaggerAsync();

            var tokens = new List<FuriganaToken>();
            var nodes = _tagger.ParseToNodes(text);
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
                    if(features.Length <= 10)
                        reading = (features[6] != "*") ? features[7] : "";
                    else
                        reading = (features[6] != "*") ? features[9] : "";
                }

                tokens.Add(new FuriganaToken
                {
                    OriginalText = node.Surface,
                    Reading = (ContainsKanji(node.Surface) && !string.IsNullOrEmpty(reading)) ? KatakanaToHiragana(reading) : string.Empty,
                    StartIndex = currentPos,
                    Length = node.Surface.Length
                });

                currentPos += node.Surface.Length;
            }

            App.LogService.Log($"Furigana analysis completed. Found {tokens.Count} tokens.", LogLevel.Info);
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
    }
}
