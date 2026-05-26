using System;
using System.Collections.Generic;

namespace Icarus.Core.Saving
{
    [Serializable]
    public sealed class SaveData
    {
        public string currentStage = string.Empty;
        public int featherCount;
        public List<string> collectedFeatherIds = new List<string>();
    }
}
