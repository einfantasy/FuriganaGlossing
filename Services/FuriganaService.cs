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
                throw new System.IO.DirectoryNotFoundException($"UniDic directory not found at: {dicPath}");
            }

            _tagger = MeCabTagger.Create(new MeCabParam(dicPath));
        }

        public async Task<List<FuriganaToken>> GetFuriganaAsync(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<FuriganaToken>();

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
                string reading = ((features.Length >= 7 && features[6] != "*") ? features[7] : "");

                tokens.Add(new FuriganaToken
                {
                    OriginalText = node.Surface,
                    Reading = (ContainsKanji(node.Surface) && !string.IsNullOrEmpty(reading)) ? KatakanaToHiragana(reading) : string.Empty,
                    StartIndex = currentPos,
                    Length = node.Surface.Length
                });

                currentPos += node.Surface.Length;
            }

            return tokens;
        }

        private bool ContainsKanji(string text)
        {
            return Regex.IsMatch(text, @"\p{IsCJKUnifiedIdeographs}");
        }

        private string KatakanaToHiragana(string katakana)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in katakana)
            {
                if (c >= '゠' && c <= 'ヿ')
                {
                    stringBuilder.Append((char)(c - 12448 + 12352));
                }
                else
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString();
        }
    }
}
