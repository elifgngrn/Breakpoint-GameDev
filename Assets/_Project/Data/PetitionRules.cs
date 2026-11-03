using UnityEngine;

namespace Breakpoint.Core
{
    [CreateAssetMenu(fileName = "PetitionRules_Default", menuName = "Breakpoint/Puzzle/Petition Rules")]
    public sealed class PetitionRules : ScriptableObject
    {
        [Header("Dilekçe koşulları")]
        [SerializeField, Min(1)] private int minimumLength = 40;
        [SerializeField, Min(1)] private int maximumLength = 500;
        [SerializeField] private string[] formalAddressTerms = { "sayın", "sayin", "müdürlüğüne", "mudurlugune" };
        [SerializeField] private string[] reasonTerms =
        {
            "sağlık", "saglik", "sınav", "sinav", "proje", "ödev", "odev",
            "teslim", "nedeniyle", "sebebiyle", "acil", "ailevi"
        };
        [SerializeField] private string[] requestTerms =
        {
            "çıkış izni", "cikis izni", "izin talep", "izin istiyorum", "izin arz",
            "kapının açıl", "kapinin acil", "çıkmama izin", "cikmama izin"
        };

        [Header("Eksik koşul mesajları")]
        [SerializeField, TextArea] private string tooShortFeedback = "Dilekçen çok kısa. Resmî hitap, mazeret ve açık bir çıkış talebi yaz.";
        [SerializeField, TextArea] private string tooLongFeedback = "Dilekçe çok uzun. En fazla 500 karakter kullan.";
        [SerializeField, TextArea] private string missingFormalAddressFeedback = "Dilekçeye \"Sayın görevli\" gibi resmî bir hitapla başla.";
        [SerializeField, TextArea] private string missingReasonFeedback = "Binadan neden çıkman gerektiğini açıkça belirt.";
        [SerializeField, TextArea] private string missingRequestFeedback = "Dilekçede çıkış iznini açıkça talep et.";

        public int MinimumLength => Mathf.Max(1, minimumLength);
        public int MaximumLength => Mathf.Max(MinimumLength, maximumLength);
        public string[] FormalAddressTerms => formalAddressTerms;
        public string[] ReasonTerms => reasonTerms;
        public string[] RequestTerms => requestTerms;
        public string TooShortFeedback => tooShortFeedback;
        public string TooLongFeedback => tooLongFeedback;
        public string MissingFormalAddressFeedback => missingFormalAddressFeedback;
        public string MissingReasonFeedback => missingReasonFeedback;
        public string MissingRequestFeedback => missingRequestFeedback;
    }
}
