using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollabXR.Environments
{
    public class EnvironmentLibrary
    {
        public readonly List<EnvironmentEntry> entries;
        public EnvironmentLibrary()
        {
            entries = new List<EnvironmentEntry>();
        }

        public void AddEnvironmentEntry(EnvironmentEntry entry)
        {
            entries.Add(entry);
        }

        public void ClearEnvironmentEntries()
        {
            entries.Clear();
        }
    }
}