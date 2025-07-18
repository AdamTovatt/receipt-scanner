namespace ReceiptScanner.Providers.Language
{
    public class TesseractLanguageProvider : IlanguageProvider
    {
        private readonly List<string> _languages;

        public TesseractLanguageProvider()
        {
            _languages = new List<string>();
        }

        public TesseractLanguageProvider(params string[] languages)
        {
            _languages = languages.ToList();
        }

        public void AddLanguage(string language)
        {
            if (_languages.Contains(language)) return;

            _languages.Add(language);
        }

        public string GetLanguage()
        {
            return string.Join('+', _languages);
        }
    }
}
