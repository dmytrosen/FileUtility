using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUtility.Entities
{
    public enum ESplitFileType
    {
        NotSet = 0,
        SplitByNumberOfRows,
        SplitByNumberOfEqualFiles,
        ExtractRequiredRows
    }
}
