using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OWML.Common;

namespace QSB2.Localization;

public static class QLocalization
{
	private static readonly List<Translation> _translations = new();
	public static Translation Current;

	public static Action LanguageChanged;

	public static void Init()
	{
		// get all translation files
		var directory = new DirectoryInfo(Path.Combine(QSB2.Instance.ModHelper.Manifest.ModFolderPath, "Translations\\"));
		var files = directory.GetFiles("*.json");
		foreach (var file in files)
		{
			var translation = QSB2.Instance.ModHelper.Storage.Load<Translation>($"Translations\\{file.Name}", false);
			var filePath = Path.Combine(QSB2.Instance.ModHelper.Manifest.ModFolderPath, $"Translations\\{file.Name}");

			if (translation == null)
			{
				Logger.Log($"Error - could not load translation at {filePath}", MessageType.Error);
				continue;
			}

			FixMissingEntries(translation);

			_translations.Add(translation);
			Logger.Log($"- Added translation for language {translation.Language}");
		}

		if (_translations.Count == 0)
		{
			Logger.Log("FATAL - No translation files found!", MessageType.Fatal);
			return;
		}

		// just use the system language until the profile is loaded and does SetLanguage
		// hack to stop things from breaking
		{
			var language = TextTranslation.Get().GetSystemLanguage();
			Logger.Log($"Language changed to {language}");
			var newTranslation = _translations.FirstOrDefault(x => x.Language == language);

			if (newTranslation == default)
			{
				Logger.Log($"Error - Could not find translation for language {language}! Defaulting to English.");
				newTranslation = _translations.First(x => x.Language == TextTranslation.Language.ENGLISH);
			}

			Current = newTranslation;
		}

		TextTranslation.Get().OnLanguageChanged += OnLanguageChanged;
	}

	private static void FixMissingEntries(Translation translation)
	{
		var publicFields = typeof(Translation).GetFields(BindingFlags.Public | BindingFlags.Instance);

		var stringFields = publicFields.Where(x => x.FieldType == typeof(string));

		foreach (var stringField in stringFields)
		{
			var value = (string)stringField.GetValue(translation);
			if (string.IsNullOrEmpty(value))
			{
				Logger.Log($"Warning - Language {translation.Language} has missing field of name {stringField.Name}", MessageType.Warning);
				stringField.SetValue(translation, stringField.Name);
			}
		}
	}

	private static void OnLanguageChanged()
	{
		var language = TextTranslation.Get().GetLanguage();
		Logger.Log($"Language changed to {language}");
		var newTranslation = _translations.FirstOrDefault(x => x.Language == language);

		if (newTranslation == default)
		{
			Logger.Log($"Error - Could not find translation for language {language}! Defaulting to English.");
			newTranslation = _translations.First(x => x.Language == TextTranslation.Language.ENGLISH);
		}

		Current = newTranslation;
		LanguageChanged?.Invoke();
	}
}
