using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalLib.Core.Monitors;

namespace asr0421p1
{
    public class TranslationHelper
    {
        /// <summary>
        /// true 是目标语言，false 是源语言
        /// </summary>
        public static Action<ScreenNameEnum, bool, string> TranslationAction;
    }
}
