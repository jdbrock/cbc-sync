using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CBC.Sync
{
    public class CbcFacebookReader
    {
        // ===========================================================================
        // = Private Fields
        // ===========================================================================

        private string[] _sessionTokens = new string[] { "YELLOW", "GREEN", "BLUE", "RED" };

        private string _source;

        private string _lastSession;
        private string _lastBrewery;

        private StringReader _reader;

        private Boolean _expectingBeer = false;

        // ===========================================================================
        // = Construction
        // ===========================================================================

        public CbcFacebookReader(String source)
        {
            _source = source.Trim();
            _reader = new StringReader(_source);
        }

        // ===========================================================================
        // = Public Methods
        // ===========================================================================

        public CbcBeer ReadBeer()
        {
            while (true)
            {
                var line = _reader.ReadLine()?.Trim();

                if (line == null)
                    return null;

                if (_sessionTokens.Contains(line))
                {
                    _lastSession = line.ToLower();
                    _expectingBeer = true;
                }
                else if (String.IsNullOrWhiteSpace(line))
                {
                    _expectingBeer = false;
                }
                else if (!_expectingBeer)
                {
                    _lastBrewery = line;
                }
                else
                {
                    var beer = ParseBeer(line, _lastSession, _lastBrewery);
                    return beer;
                }
            }
        }

        public static Int32 GetSessionNumber(String sessionName)
        {
            switch (sessionName.ToLower())
            {
                case "yellow":
                    return 1;

                case "blue":
                    return 2;

                case "red":
                    return 3;

                case "green":
                    return 4;

                default:
                    throw new NotSupportedException();
            }
        }

        // ===========================================================================
        // = Private Methods
        // ===========================================================================

        private CbcBeer ParseBeer(string line, string lastSession, string lastBrewery)
        {
            var regex = new Regex(@"(?<beerName>[^,]+)\s*,\s*(?<styleName>.+?)\s*,?\s*(?<abv>[0-9]+,?\.?[0-9]*%?)");

            //var split = line.Split(new[] { ", " }, StringSplitOptions.None);

            //var lastIndex = split.Count() - 1;

            //var beerName = split[0];
            //var styleName = String.Join(", ", split, 1, lastIndex - 1);
            //var abv = split[lastIndex].Replace(",", ".").Replace("%", "");

            if (line.StartsWith("--"))
                return null;

            var match = regex.Match(line);

            if (!match.Success)
                throw new InvalidDataException();

            var beerName = match.Groups["beerName"].Value;
            var styleName = match.Groups["styleName"].Value;
            var abv = match.Groups["abv"].Value.Replace(",", ".").Replace("%", "");

            return new CbcBeer
            {
                Id = Hash(lastBrewery + " | " + beerName + " | " + lastSession + " | " + abv + " | " + styleName),
                ABV = Decimal.Parse(abv),
                BeerName = beerName,
                BreweryName = lastBrewery,
                SessionName = ToTitleCase(lastSession),
                StyleName = styleName,
                SessionNumber = GetSessionNumber(lastSession)
            };
        }

        private string ToTitleCase(string lastSession)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(lastSession);
        }

        private string Hash(string input)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(input);
            var hash = Engine.Hasher.ComputeHash(bytes);

            var sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("X2"));

            return sb.ToString();
        }
    }
}
