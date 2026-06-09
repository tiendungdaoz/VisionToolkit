using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisionToolkit.TemplateMatchingNCC.Models;

namespace VisionToolkit.TemplateMatchingNCC.Filtering
{
    /// <summary>
    /// Match Score Filter
    ///
    /// Purpose:
    ///     - Loại bỏ candidate có score thấp
    ///     - Giữ lại các result đạt threshold
    ///
    /// Responsibilities:
    ///     - Sort match result theo score
    ///     - Remove low confidence match
    ///
    /// Workflow:
    ///     - Sort descending score
    ///     - Remove below threshold
    ///
    /// Notes:
    ///     - Dùng sau pyramid refinement
    ///     - Không xử lý overlap
    /// </summary>
    internal static class ScoreFilter
    {
        /// <summary>
        /// Remove match below threshold
        /// </summary>
        /// 
        public static void Filter(List<MatchParameter> matches, double scoreThreshold)
        {
            matches.Sort();

            int removeIndex = matches.Count;

            for (int i = 0; i < matches.Count; i++)
            {
                if (matches[i].MatchScore < scoreThreshold)
                {
                    removeIndex = i;
                    break;
                }
            }

            if (removeIndex < matches.Count)
            {
                matches.RemoveRange(removeIndex, matches.Count - removeIndex);
            }
        }
    }
}
