using UnityEngine;
using UnityEditor;
using System.Collections;

namespace ConsolePP
{
	public static class EditorExtension
	{
		public static bool Open(this Stack.Trace t)
		{
			if( !t.CanOpen )
				return false;
			
			UnityEngine.Object o = AssetDatabase.LoadAssetAtPath(t.file, typeof(MonoScript));
			return AssetDatabase.OpenAsset(o, t.lineNumber);
		}

		public static int DrawBitMaskField (Rect aPosition, int aMask, System.Type aType, GUIContent aLabel)
		{
			return DrawBitMaskField( aPosition, aMask, aType, aLabel, GUIStyle.none);
		}
		public static int DrawBitMaskField (Rect aPosition, int aMask, System.Type aType, GUIContent aLabel, GUIStyle style)
		{
			var itemNames = System.Enum.GetNames(aType);
			var itemValues = System.Enum.GetValues(aType) as int[];
			
			int val = aMask;
			int maskVal = 0;
			for(int i = 0; i < itemValues.Length; i++)
			{
				if (itemValues[i] != 0)
				{
					if ((val & itemValues[i]) == itemValues[i])
						maskVal |= 1 << i;
				}
				else if (val == 0)
					maskVal |= 1 << i;
			}

			int newMaskVal = 0;
			if( style != GUIStyle.none )
				newMaskVal = EditorGUI.MaskField(aPosition, aLabel, maskVal, itemNames, style);
			else
				newMaskVal = EditorGUI.MaskField(aPosition, aLabel, maskVal, itemNames);

			int changes = maskVal ^ newMaskVal;
			
			for(int i = 0; i < itemValues.Length; i++)
			{
				if ((changes & (1 << i)) != 0)            // has this list item changed?
				{
					if ((newMaskVal & (1 << i)) != 0)     // has it been set?
					{
						if (itemValues[i] == 0)           // special case: if "0" is set, just set the val to 0
						{
							val = 0;
							break;
						}
						else
							val |= itemValues[i];
					}
					else                                  // it has been reset
					{
						val &= ~itemValues[i];
					}
				}
			}
			return val;
		}
	}
}
