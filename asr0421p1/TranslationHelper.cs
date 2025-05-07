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
        /// ScreenNameEnum 由哪个屏幕的客户端发起的， string ASR的识别结果。
        /// </summary>
        public static Action<ScreenNameEnum, string> TranslationAction;
        public static Action<ScreenNameEnum, string> TranslationEndAction;
    }
}
