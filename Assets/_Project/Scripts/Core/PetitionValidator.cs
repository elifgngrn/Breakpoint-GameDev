using System;

namespace Breakpoint.Core
{
    /// <summary>Kapı kararını LLM'den bağımsız, yerel dilekçe kurallarıyla verir.</summary>
    public static class PetitionValidator
    {
        public static bool Validate(string petition, PetitionRules rules, out string feedback)
        {
            if (rules == null)
            {
                feedback = "Dilekçe kontrolü şu anda kullanılamıyor. Lütfen tekrar dene.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(petition) || petition.Trim().Length < rules.MinimumLength)
            {
                feedback = rules.TooShortFeedback;
                return false;
            }

            string normalized = petition.ToLowerInvariant();
            if (!ContainsAny(normalized, rules.FormalAddressTerms))
            {
                feedback = rules.MissingFormalAddressFeedback;
                return false;
            }

            if (!ContainsAny(normalized, rules.ReasonTerms))
            {
                feedback = rules.MissingReasonFeedback;
                return false;
            }

            if (!ContainsAny(normalized, rules.RequestTerms))
            {
                feedback = rules.MissingRequestFeedback;
                return false;
            }

            feedback = string.Empty;
            return true;
        }

        private static bool ContainsAny(string text, string[] terms)
        {
            if (terms == null) return false;

            foreach (string term in terms)
            {
                if (!string.IsNullOrWhiteSpace(term) && text.IndexOf(term.ToLowerInvariant(), StringComparison.Ordinal) >= 0)
                    return true;
            }

            return false;
        }
    }
}
