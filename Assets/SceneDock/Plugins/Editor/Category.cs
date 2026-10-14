using System.Collections.Generic;
using System;

namespace SceneDock
{
    [Serializable]
    internal class Category
    {
        public string Name;
        public List<string> ScenesGUIDs = new List<string>();

        public Category(string name) => Name = name;
    }
}

