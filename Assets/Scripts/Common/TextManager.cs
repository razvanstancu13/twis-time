using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
 
/// <summary>
/// TextManager V2
/// Inspired by http://forum.unity3d.com/threads/35617-TextManager-Localization-Script
/// 
/// Reads text files in the 'Assets\Resources' directory into a dictionary. The text file 
/// consists of one line that has the key name, a space and the actual text to display.
/// 
/// Example:
/// 
/// Assume you have a text file called English.txt in Assets\Resources\Languages
/// The file looks like this:
/// 
/// hello Hello and welcome!
/// goodbye Goodbye and thanks for playing!
/// 
/// In Unity code you have to set the Language property with the English.txt asset once:
/// TextManager.LoadResource = "Languages\English";
/// 
/// Then you can retrieve text by calling:
/// TextManager.GetText("hello");
/// This will return a string containing "Hello and welcome!"
/// </summary>
public static class TextManager
{
    private static readonly IDictionary<string, string> TextTable = new Dictionary<string, string>();
 
    private static TextAsset _asset;
    public static TextAsset Language
    {
        set
        {
            _asset = value;
 
            if (_asset == null)
            {
                Debug.LogError(String.Format("No valid asset file."));
            }
            else
            {
                LoadLanguage(_asset);
            }
        }
    }
 
    /// <summary>
    /// Load an asset by its AssetName.
    /// </summary>
    /// <remarks>
    /// The text file must be located within 'Resources' subfolder in Unity ('Assets\Resources' in Visual Studio).
    /// </remarks>
    public static String LoadResource
    {
        set
        {
            Language = (TextAsset)Resources.Load(value, typeof(TextAsset));
        }
    }
 
 
    private static void LoadLanguage(TextAsset asset)
    {
        TextTable.Clear();
 
        var lineNumber = 1;
        using (var reader = new StringReader(asset.text))
	    {
	        string line;
	        while ((line = reader.ReadLine()) != null)
	        {
				line = line.Replace("\\n", System.Environment.NewLine);
                var lineElements = line.Trim().Split(' ');
	            if (lineElements.Length > 1)
                {
                    var key = lineElements[0];
					string val="";
					int i;
					for (i=1;i<lineElements.Length-1;i++) {
						val += lineElements[i];
						val+=" ";
					}
					val+=lineElements[i];
 
                    if (key != null && val != null)
                    {
                        var loweredKey = key.ToLower();
                        if (TextTable.ContainsKey(loweredKey))
                        {
                            Debug.LogWarning(String.Format(
                                     "Duplicate key '{1}' in language file '{0}' detected.", asset.text, key));
                        }
                        TextTable.Add(loweredKey, val);
                    }
                }
                else
                {
                    Debug.LogWarning(String.Format(
                         "Format error in language file '{0}' on line {1}. Each line must be of format: key value", 
                          asset.text, lineNumber));
                }
 
	            lineNumber++;
	        }
	    }
    }

 
    public static string Get(string key)
    {
        var loweredKey = key.ToLower();
 
        var result = String.Format("Couldn't find key '{0}' in dictionary.", key);
 
        if (TextTable.ContainsKey(loweredKey))
        {
            result = TextTable[loweredKey];
        }
 
        return result;
    }
	
	// the game's texts (Resources/lang_en), loaded once; the menu used to set this, the game scene relied on it
	public static void EnsureLoaded()
	{
		if (TextTable.Count == 0)
			Language = Resources.Load<TextAsset>("lang_en");
	}
}