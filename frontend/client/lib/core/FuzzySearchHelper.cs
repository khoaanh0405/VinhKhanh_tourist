using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace client.lib.core
{
    /// <summary>
    /// Helper tìm kiếm thông minh: hỗ trợ không dấu, partial match, fuzzy (sai chính tả nhẹ).
    /// Dùng chung cho SearchPage, FavoritesPage, MapPage, HomePage.
    /// </summary>
    public static class FuzzySearchHelper
    {
        // ══════════════════════════════════════════════════════════════
        //  1. REMOVE DIACRITICS (bỏ dấu tiếng Việt)
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Chuyển chuỗi có dấu → không dấu.  
        /// Ví dụ: "Ốc Đào" → "Oc Dao", "Bún bò Huế" → "Bun bo Hue"
        /// Xử lý đặc biệt cho 'đ' → 'd' và 'Đ' → 'D'
        /// </summary>
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            // Xử lý đặc biệt cho chữ Đ/đ trước khi normalize
            text = text.Replace("đ", "d").Replace("Đ", "D");

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // ══════════════════════════════════════════════════════════════
        //  2. NORMALIZE TEXT (chuẩn hóa cho so sánh)
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Chuẩn hóa text: lowercase + bỏ dấu + trim khoảng trắng thừa
        /// </summary>
        public static string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var noDiacritics = RemoveDiacritics(text.Trim().ToLowerInvariant());
            // Gộp nhiều khoảng trắng thành 1
            return Regex.Replace(noDiacritics, @"\s+", " ");
        }

        // ══════════════════════════════════════════════════════════════
        //  3. CONTAINS MATCH (partial match không dấu)
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Kiểm tra source có CHỨA query không, bỏ qua dấu + case.
        /// Đây là phương thức tìm kiếm chính (nhanh nhất).
        /// </summary>
        public static bool ContainsNormalized(string? source, string normalizedQuery)
        {
            if (string.IsNullOrEmpty(source)) return false;
            var normalizedSource = NormalizeText(source);
            return normalizedSource.Contains(normalizedQuery);
        }

        // ══════════════════════════════════════════════════════════════
        //  4. STARTS WITH MATCH (ưu tiên kết quả bắt đầu bằng keyword)
        // ══════════════════════════════════════════════════════════════
        public static bool StartsWithNormalized(string? source, string normalizedQuery)
        {
            if (string.IsNullOrEmpty(source)) return false;
            var normalizedSource = NormalizeText(source);
            return normalizedSource.StartsWith(normalizedQuery);
        }

        // ══════════════════════════════════════════════════════════════
        //  5. WORD STARTS WITH (tìm từ bắt đầu bằng query)
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Kiểm tra xem có từ nào trong source bắt đầu bằng query không.
        /// Ví dụ: query="bun" → match "Bún bò Huế" (từ "bun" match)
        /// </summary>
        public static bool AnyWordStartsWith(string? source, string normalizedQuery)
        {
            if (string.IsNullOrEmpty(source)) return false;
            var normalizedSource = NormalizeText(source);
            var words = normalizedSource.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Any(w => w.StartsWith(normalizedQuery));
        }

        // ══════════════════════════════════════════════════════════════
        //  6. LEVENSHTEIN DISTANCE (cho fuzzy match - sai chính tả)
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Tính khoảng cách chỉnh sửa giữa 2 chuỗi.
        /// Giá trị nhỏ = giống nhau hơn. 0 = giống hệt.
        /// </summary>
        public static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int n = s.Length, m = t.Length;
            // Dùng 2 mảng thay vì matrix 2D để tiết kiệm bộ nhớ
            var prev = new int[m + 1];
            var curr = new int[m + 1];

            for (int j = 0; j <= m; j++) prev[j] = j;

            for (int i = 1; i <= n; i++)
            {
                curr[0] = i;
                for (int j = 1; j <= m; j++)
                {
                    int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    curr[j] = Math.Min(
                        Math.Min(curr[j - 1] + 1, prev[j] + 1),
                        prev[j - 1] + cost);
                }
                (prev, curr) = (curr, prev); // Swap
            }

            return prev[m];
        }

        // ══════════════════════════════════════════════════════════════
        //  7. FUZZY MATCH (cho phép sai 1-2 ký tự)
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Kiểm tra xem có từ nào trong source "gần giống" query không.
        /// Cho phép sai tối đa maxDistance ký tự (mặc định: 1 nếu query ngắn, 2 nếu dài).
        /// </summary>
        public static bool FuzzyContains(string? source, string normalizedQuery, int maxDistance = -1)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(normalizedQuery)) return false;

            // Tự động chọn maxDistance dựa trên độ dài query
            if (maxDistance < 0)
                maxDistance = normalizedQuery.Length <= 3 ? 1 : 2;

            var normalizedSource = NormalizeText(source);
            var sourceWords = normalizedSource.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in sourceWords)
            {
                if (LevenshteinDistance(word, normalizedQuery) <= maxDistance)
                    return true;
            }

            // Kiểm tra thêm substring dài hơn (cho trường hợp query là cụm từ)
            if (normalizedQuery.Length > 3)
            {
                // Sliding window trên source
                for (int i = 0; i <= normalizedSource.Length - normalizedQuery.Length; i++)
                {
                    var sub = normalizedSource.Substring(i, normalizedQuery.Length);
                    if (LevenshteinDistance(sub, normalizedQuery) <= maxDistance)
                        return true;
                }
            }

            return false;
        }

        // ══════════════════════════════════════════════════════════════
        //  8. CALCULATE RELEVANCE SCORE (điểm liên quan để sắp xếp)
        // ══════════════════════════════════════════════════════════════
        public static int CalculateRelevanceScore(
            string? name,
            string? description,
            IEnumerable<string?>? foodNames,
            string normalizedQuery)
        {
            if (string.IsNullOrEmpty(normalizedQuery)) return 0;

            int score = 0;

            // Tên khớp chính xác nhất → điểm cao nhất
            if (StartsWithNormalized(name, normalizedQuery))
                score = Math.Max(score, 100);
            else if (AnyWordStartsWith(name, normalizedQuery))
                score = Math.Max(score, 90);
            else if (ContainsNormalized(name, normalizedQuery))
                score = Math.Max(score, 80);

            // Mô tả chứa query
            if (ContainsNormalized(description, normalizedQuery))
                score = Math.Max(score, 60);

            // Tên món ăn chứa query
            if (foodNames != null)
            {
                foreach (var foodName in foodNames)
                {
                    if (StartsWithNormalized(foodName, normalizedQuery))
                    {
                        score = Math.Max(score, 50);
                        break;
                    }
                    if (ContainsNormalized(foodName, normalizedQuery))
                    {
                        score = Math.Max(score, 40);
                        break;
                    }
                }
            }

            // Fuzzy match (fallback cuối cùng)
            if (score == 0)
            {
                if (FuzzyContains(name, normalizedQuery))
                    score = 25;
                else if (FuzzyContains(description, normalizedQuery))
                    score = 15;
                else if (foodNames != null && foodNames.Any(f => FuzzyContains(f, normalizedQuery)))
                    score = 10;
            }

            return score;
        }
    }
}