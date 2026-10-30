using System;
using System.Collections.Generic;

namespace CollabXR.Environments
{
	public class EnvironmentCategory
	{
		public string name;
		public readonly List<EnvironmentEntry> entries = new List<EnvironmentEntry>();

		public EnvironmentCategory(string name)
		{
			this.name = name;
		}

		public void AddEntry(EnvironmentEntry entry)
		{
			entries.Add(entry);
			entries.Sort((a, b) =>
			{
				if (a.modGUID == Guid.Empty && b.modGUID != Guid.Empty)
				{
					return -1;
				}
				if (a.modGUID != Guid.Empty && b.modGUID == Guid.Empty)
				{
					return 1;
				}

				int nameOrder = string.Compare(a.formattedSceneName, b.formattedSceneName, StringComparison.OrdinalIgnoreCase);
				if (nameOrder != 0)
				{
					return nameOrder;
				}

				int modOrder = a.modGUID.CompareTo(b.modGUID);
				if (modOrder != 0)
				{
					return modOrder;
				}

				int assetOrder = a.assetGUID.CompareTo(b.assetGUID);
				return assetOrder != 0 ? assetOrder : string.Compare(a.scenePath, b.scenePath, StringComparison.Ordinal);
			});
		}
	}

	public class EnvironmentLibrary
	{
		public readonly List<EnvironmentCategory> categories = new();
        public EnvironmentEntry defaultEnvironmentEntry; // a reference to the default environment entry

		public void AddEnvironmentEntry(EnvironmentEntry entry)
		{
			if (entry.assetGUID == Guid.Empty || GetEntry(entry.assetGUID) != null)
			{
				throw new ArgumentException($"Invalid or duplicate environment ID: {entry.assetGUID}");
			}

			string name = string.IsNullOrWhiteSpace(entry.category) ? "Other Environments" : entry.category.Trim();
			EnvironmentCategory category = categories.Find(c =>
			{
				return string.Equals(c.name, name, StringComparison.OrdinalIgnoreCase);
			});
			if (category == null)
			{
				category = new EnvironmentCategory(name);
				categories.Add(category);
				categories.Sort((a, b) =>
				{
                    // always keep built-in environments at the top
					if (a.name == EnvironmentEntry.BUILTIN_CATEGORY && b.name != EnvironmentEntry.BUILTIN_CATEGORY)
					{
						return -1;
					}
					if (b.name == EnvironmentEntry.BUILTIN_CATEGORY && a.name != EnvironmentEntry.BUILTIN_CATEGORY)
					{
						return 1;
					}
					return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
				});
			}

			entry.category = category.name;
			category.AddEntry(entry);
		}

		public void ClearEnvironmentEntries()
		{
			categories.Clear();
			defaultEnvironmentEntry = null;
		}

		public EnvironmentEntry GetEntry(Guid assetGuid)
		{
			foreach (EnvironmentCategory category in categories)
			{
				foreach (EnvironmentEntry entry in category.entries)
				{
					if (entry.assetGUID == assetGuid)
					{
						return entry;
					}
				}
			}

			return null;
		}
	}
}
