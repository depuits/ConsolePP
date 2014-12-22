using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ConsolePP
{
	[Serializable]
	public class ConsolePP : EditorWindow, IHasCustomMenu
	{
		static readonly ChannelInfo defaultChannel = new ChannelInfo("default", Color.white);

		const string fieldNameEntryList = "entryList";
		const string fieldNameCommand = "commandField";

		enum EntryState 
		{ 
			Even 		= 0x00, 
			Odd 		= 0x01, 
			Selected 	= 0x02
		}

		enum EntryColor
		{
			LevelColor 		= 0x01,
			ChannelColor 	= 0x02
		}

		[SerializeField] List<LogEntry> entries;
		[SerializeField] List<ChannelInfo> channels;

		[SerializeField] Vector2 scrollPos = Vector2.zero;
		[SerializeField] Vector2 detailsScrollPos = Vector2.zero;

		[SerializeField] int selectedEntry = -1;
		[SerializeField] float entryHeight = EditorGUIUtility.singleLineHeight;
		[SerializeField] EntryColor entryColor = EntryColor.LevelColor;

		[SerializeField] float contentWidth = 0;
	
		[SerializeField] bool clearOnPlay = false;
		[SerializeField] bool errorPause = false;
		[SerializeField] bool clearChannelsOnClear = true;

		[SerializeField] LogEntry.Level levelMask = LogEntry.LevelAll ^ LogEntry.Level.Command;
		[SerializeField] string searchString = "";

		[SerializeField] float dividerPosition = -1;
		bool resizing = false;

		[SerializeField] bool compileStarted = false;
		[SerializeField] bool showCConsole = false;
		[SerializeField] string currentCommand = "";
		
		GUIStyle baseStyle;

		[MenuItem("Window/Console++", false)]
		static void OpenWindow()
		{
			ConsolePP window = (ConsolePP)GetWindow(typeof(ConsolePP), false, "Console++");
			//window.position = new Rect(Screen.width / 2, Screen.height / 2, 500, 250);
			window.ShowPopup();
		}

		void OnEnable()
		{
			hideFlags = HideFlags.HideAndDontSave;
			if( entries == null )
			{
				Log.Warning("Console++", "new sub logger");
				entries = new List<LogEntry>(512);
			}
			if( channels == null )
			{
				channels = new List<ChannelInfo>(32);
				channels.Add(new ChannelInfo(defaultChannel) );
			}

			if( dividerPosition == -1 )
				dividerPosition = position.height / 4 * 3;

			if( baseStyle == null )
				SetupStyle();
			
			Log.OnLogAdded += HandleOnLogAdded;

			Application.RegisterLogCallback(HandleLog);
			//Application.RegisterLogCallbackThreaded(HandleLog);
			EditorApplication.playmodeStateChanged += PlayStateChanged;
		}
		void OnDisable()
		{
			Log.OnLogAdded -= HandleOnLogAdded;

			Application.RegisterLogCallback(null);
			EditorApplication.playmodeStateChanged -= PlayStateChanged;
		}

		void PlayStateChanged()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				if (clearOnPlay)
					ClearLog ();
			}
			else
			{
				Application.RegisterLogCallback(HandleLog);
				//Application.RegisterLogCallbackThreaded(HandleLog);
			}
		}

		void Update()
		{
			if( EditorApplication.isCompiling )
			{
				if( !compileStarted )
				{
					compileStarted = true;

					//TODO add option to leave some entries
					//clear logs with bit set
					entries.RemoveAll( entry => ( (entry.Xb & LogEntry.ExtraBits.Compile) != 0 ) );
				}
			}
			else if( compileStarted )
			{
				compileStarted = false;
			}
		}
		
		void HandleLog (string logString, string stackTrace, LogType type) 
		{
			LogEntry.ExtraBits xb = LogEntry.ExtraBits.None;
			LogEntry.Level lvl = LogEntry.Level.Info;
			switch( type )
			{
			case LogType.Warning:
				lvl = LogEntry.Level.Warning;
				break;
			case LogType.Assert:
			case LogType.Error:
			case LogType.Exception:
				lvl = LogEntry.Level.Error;
				break;
			}

			if( stackTrace.IsNullOrEmpty() )
			{
				// check if warning or error (warning CS0000, error CS0000), not done because only compile warnings and errors have no trace
				// add compiller flag
				xb |= LogEntry.ExtraBits.Compile;
				stackTrace = logString;
			}

			HandleOnLogAdded( new LogEntry( defaultChannel.name, lvl, logString, stackTrace, xb ) );
		}
		void HandleOnLogAdded (LogEntry entry)
		{
			if( errorPause && entry.Type == LogEntry.Level.Error )
				EditorApplication.isPaused = true;

			// add the entry channel if its not yet in the list
			ChannelInfo ci = new ChannelInfo(entry.Id);
			if( !channels.Contains( ci ) )
				channels.Add( ci );

			entries.Add(entry);

			float cw = baseStyle.CalcSize( new GUIContent(entry.Msg) ).x;
			if( cw > contentWidth )
				contentWidth = cw;
			
			if( entry.Type == LogEntry.Level.Command )
				HandleCommand(entry);

			scrollPos.y = Mathf.Infinity; 		// scroll down
			Repaint(); 							// redraw the gui
		}

		/// <summary>
		/// Adds custom items to editor window context menu.
		/// </summary>
		/// <remarks>
		/// <para>This will only work for Unity 4.x+</para>
		/// </remarks>
		/// <param name="menu">Context menu.</param>
		void IHasCustomMenu.AddItemsToMenu(GenericMenu menu) 
		{
			menu.AddItem(new GUIContent("Clear channels"), false, ClearChannels );
			menu.AddItem(new GUIContent("Clear channels on clear"), clearChannelsOnClear, () => { clearChannelsOnClear = !clearChannelsOnClear; } );
			menu.AddSeparator("");

			menu.AddItem(new GUIContent("Re-setup style"), false, SetupStyle);
			menu.AddSeparator("");

			menu.AddItem(new GUIContent("Test ext Log"), false, () => {
				for(int y = 0; y < 5000; ++y)
					Log.Warning("testLarge" + (y%2), "extended log: " + y);
			} );
		}

		void OnGUI()
		{
			GUIStyle toolBar 						= "Toolbar";
			GUIStyle toolbarbutton 					= "toolbarbutton";
			GUIStyle toolbarDropDown 				= "ToolbarDropDown";
			GUIStyle toolbarTextField 				= "ToolbarTextField";
			GUIStyle projectBrowserHeaderBgTop 		= "ProjectBrowserHeaderBgTop";
			GUIStyle consoleMessage 				= "CN Message";
		
#region main Toolbar
			float toolbarHeight = EditorGUIUtility.singleLineHeight;
			GUI.Box( new Rect(0, 0, position.width, toolbarHeight), GUIContent.none, toolBar);
			Rect sizeRect = new Rect(8, 0, 40, toolbarHeight);	
			if( GUI.Button(sizeRect, "Clear", toolbarbutton) )
				ClearLog();
					
			sizeRect.x += sizeRect.width + 8;
			sizeRect.width = 70;

			clearOnPlay = GUI.Toggle(sizeRect, clearOnPlay, "Clear on play", toolbarbutton);
			sizeRect.x += sizeRect.width;

			errorPause = GUI.Toggle(sizeRect, errorPause, "Error pause", toolbarbutton);
			sizeRect.x += sizeRect.width;
			
			float searchMaxWidth = 256;
			float searchMinWidth = 64;
			sizeRect.x = Mathf.Max( sizeRect.x + 8, position.width - searchMaxWidth - 82 );
			sizeRect.width = Mathf.Clamp( position.width - sizeRect.x - 82, searchMinWidth, searchMaxWidth );

			sizeRect.y += 1;
			searchString = SearchBox(sizeRect, searchString);
			sizeRect.y -= 1;

			sizeRect.x += sizeRect.width + 4;
			sizeRect.width = 70;
			levelMask = (LogEntry.Level)EditorExtension.DrawBitMaskField(sizeRect, (int)levelMask, typeof(LogEntry.Level), GUIContent.none, toolbarDropDown);
#endregion
#region channels Toolbar
			sizeRect.x = 4;
			sizeRect.y += sizeRect.height+2;
			sizeRect.width = 64;

			GUI.Box( new Rect(0, sizeRect.y, position.width, sizeRect.height), GUIContent.none, projectBrowserHeaderBgTop);
			for( int c = 0; c < channels.Count; ++c )
			{
				GUI.color = channels[c].color;
				channels[c].enabled = GUI.Toggle(sizeRect, channels[c].enabled, channels[c].name, toolbarbutton);
				sizeRect.x += sizeRect.width;
			}
			GUI.color = Color.white;
			
			float sizeBtnWidth = 20;
			float sizeColorSelect = 70;
			sizeRect.x = Mathf.Max( sizeRect.x + 8, position.width - (sizeBtnWidth * 3) - sizeColorSelect - 12 );
			sizeRect.width = sizeBtnWidth;
			if( GUI.Toggle(sizeRect, entryHeight == EditorGUIUtility.singleLineHeight, "s", toolbarbutton) )
			{
				entryHeight = EditorGUIUtility.singleLineHeight * 1;
			}
			sizeRect.x += sizeRect.width;
			if( GUI.Toggle(sizeRect, entryHeight == EditorGUIUtility.singleLineHeight * 2, "m", toolbarbutton) )
			{
				entryHeight = EditorGUIUtility.singleLineHeight * 2;
			}
			sizeRect.x += sizeRect.width;
			if( GUI.Toggle(sizeRect, entryHeight == EditorGUIUtility.singleLineHeight * 3, "l", toolbarbutton) )
			{
				entryHeight = EditorGUIUtility.singleLineHeight * 3;
			}
			sizeRect.x += sizeRect.width + 4;
			sizeRect.width = sizeColorSelect;
			entryColor = (EntryColor)EditorExtension.DrawBitMaskField(sizeRect, (int)entryColor, typeof(EntryColor), GUIContent.none, toolbarDropDown);
#endregion

			List<LogEntry> drawEntries = FilterEntries(entries);
			
			sizeRect.y += sizeRect.height + 2;
			Rect rPos = new Rect(0, sizeRect.y, position.width, dividerPosition - sizeRect.y - 1 );
			Rect rFrame = new Rect(0, 0, contentWidth + 240, drawEntries.Count * entryHeight);
			GUI.SetNextControlName(fieldNameEntryList);
			scrollPos = GUI.BeginScrollView(rPos, scrollPos, rFrame, false, false);

			int startI = (int)(scrollPos.y / entryHeight);
			int endI = (int)((scrollPos.y + rPos.height) / entryHeight) + 1;

			startI = Mathf.Clamp( startI, 0, int.MaxValue);
			endI = Mathf.Clamp(endI, 0, drawEntries.Count);
			
			for( int i = startI; i < endI; ++i )
			{
				float cw = contentWidth < position.width ? position.width : contentWidth;
				Rect entryRect = new Rect(0, i * entryHeight, cw + 240, entryHeight);

				EntryState es = EntryState.Even;
				if( i % 2 != 0 )
					es = EntryState.Odd;
				if( i == selectedEntry )
					es |= EntryState.Selected;

				if( DrawEntry( entryRect, drawEntries[i], es ) )
					selectedEntry = i;
			}

			GUI.EndScrollView();
			ResizeScrollView();
			if( selectedEntry >= 0 && selectedEntry < drawEntries.Count )
			{
				sizeRect = new Rect(0, dividerPosition+1, position.width, position.height - dividerPosition - EditorGUIUtility.singleLineHeight);		
				if( showCConsole )
					sizeRect.height -= EditorGUIUtility.singleLineHeight;
				DrawEntryDetail(sizeRect, drawEntries[selectedEntry], consoleMessage);
			}

#region bottom Toolbar
			sizeRect = new Rect(0, position.height - EditorGUIUtility.singleLineHeight, position.width / 3, EditorGUIUtility.singleLineHeight);
			if( showCConsole )
				sizeRect.y -= EditorGUIUtility.singleLineHeight;
			bool nShowCConsole = GUI.Toggle( sizeRect, showCConsole, "Command console", toolbarbutton);
			if( nShowCConsole != showCConsole )
			{
				showCConsole = nShowCConsole;
				Repaint();
			}

			sizeRect.x += sizeRect.width;
			GUIStyle bottomToolbar = new GUIStyle( toolBar );
			bottomToolbar.alignment = TextAnchor.MiddleCenter;
			GUI.Label( sizeRect, string.Format("total entries: {0}, shown entries: {1}", entries.Count, drawEntries.Count), bottomToolbar ); 

			sizeRect.x += sizeRect.width;
			bottomToolbar.alignment = TextAnchor.MiddleRight;
			GUI.Label( sizeRect, "Alpha 0.1", bottomToolbar ); 

			if( showCConsole )
			{
				sizeRect = new Rect(0, position.height - EditorGUIUtility.singleLineHeight, position.width - 64, EditorGUIUtility.singleLineHeight);
				GUI.SetNextControlName(fieldNameCommand);
				EditorGUIUtility.AddCursorRect(sizeRect, MouseCursor.Text);
				currentCommand = GUI.TextField( sizeRect, currentCommand, toolbarTextField );
				if( Event.current.type == EventType.mouseDown && !sizeRect.Contains(Event.current.mousePosition))
					GUI.FocusControl( fieldNameEntryList );
				sizeRect.x += sizeRect.width;
				sizeRect.width = position.width - sizeRect.x;
				if( GUI.Button(sizeRect, "send", toolbarbutton ) )
				{
					EvalCommand(currentCommand);
					currentCommand = "";
					Repaint();
				}
			}
#endregion
			
			HandleKeyboardInput();
		}

		void EvalCommand(string command)
		{
			string chId = defaultChannel.name;
			string com = command;
			string[] splitComs = command.Split( new char[]{ ':' }, 2 );
			if( splitComs.Length == 2 )
			{
				chId = splitComs[0];
				com = splitComs[1];
			}

			Log.Command(chId, com);
		}

		void SetupStyle()
		{
			Log.Debug("Console++", "setup style");
			baseStyle = new GUIStyle();
			baseStyle.normal.textColor = Color.white;
			baseStyle.normal.background = EditorGUIUtility.whiteTexture;
		}

		Color GetLevelColor(LogEntry.Level lvl)
		{
			switch(lvl)
			{
			case LogEntry.Level.Error:
				return new Color(1, 0, 0, 1);
			case LogEntry.Level.Warning:
				return new Color(1, 1, 0, 1);
			}

			return new Color(1, 1, 1, 1);
		}

		void ResizeScrollView()
		{
			Rect lineRect = new Rect(0, dividerPosition, position.width, 1);
			float cursorRectSize = 2;
			if( resizing )
				cursorRectSize = 50;
			Rect cursorChangeRect = new Rect(0, dividerPosition - cursorRectSize, position.width, (cursorRectSize * 2) + 1);

			GUI.color = Color.black;
			GUI.DrawTexture(lineRect, EditorGUIUtility.whiteTexture);
			EditorGUIUtility.AddCursorRect(cursorChangeRect, MouseCursor.SplitResizeUpDown);
			GUI.color = Color.white;

			if( Event.current.type == EventType.mouseDown && cursorChangeRect.Contains(Event.current.mousePosition))
				resizing = true;

			if(resizing)
			{
				dividerPosition = Mathf.Clamp( Event.current.mousePosition.y, 64, position.height - 32);
				Repaint();
			}

			if(Event.current.rawType == EventType.MouseUp)
				resizing = false;        
		}
		
		public void ClearLog()
		{
			Clear(clearChannelsOnClear);
		}
		
		public void ClearChannels()
		{
			Clear(true);
		}

		public void Clear(bool clearChannels)
		{
			entries.Clear();
			
			if( clearChannels )
			{
				channels.Clear();
				channels.Add( new ChannelInfo(defaultChannel) );
			}
			
			contentWidth = 0;
			selectedEntry = -1;
			Repaint();
		}


		bool DrawEntry(Rect pos, LogEntry entry, EntryState es)
		{
			bool rv = false;
			if( GUI.Button( pos, GUIContent.none, GUIStyle.none ) )
				rv = true;
			
			Color bc = new Color(1, 1, 1, 0.2f);
			Color tc = new Color (0.2f, 0.2f, 0.2f, 1);
			if( EditorGUIUtility.isProSkin )
				tc = new Color(0.8f, 0.8f, 0.8f, 1);
			
			if( (es & EntryState.Selected) != 0 )
			{
				bc = new Color(0.5f, 0.5f, 0.8f, 1);
				tc = Color.white;
			}
			else if( (es & EntryState.Odd) != 0 )
				bc = new Color(1, 1, 1, 0.4f);

			if( (entryColor & EntryColor.LevelColor) != 0 )
				bc *= GetLevelColor( entry.Type );
			if( (entryColor & EntryColor.ChannelColor) != 0 )
				bc *= GetChannelInfo( entry.Id ).color;

			GUI.backgroundColor = bc;
			GUI.contentColor = tc;
			
			string ttl = entry.Msg;
			int indexOf = ttl.LastIndexOf('\n');

			while( baseStyle.CalcSize( new GUIContent(ttl) ).y > pos.height && indexOf > 0 )
			{
				ttl = ttl.Substring(0, indexOf);
				indexOf = ttl.LastIndexOf('\n');
			}

			baseStyle.alignment = TextAnchor.MiddleCenter;
			GUI.Label( new Rect(pos.x + 0, 		pos.y, 64, pos.height), entry.Frame.ToString("D8"), baseStyle);
			GUI.Label( new Rect(pos.x + 64, 	pos.y, 80, pos.height), entry.Time, baseStyle);
			GUI.Label( new Rect(pos.x + 144, 	pos.y, 96, pos.height), entry.Id, baseStyle);
			baseStyle.alignment = TextAnchor.MiddleLeft;
			GUI.Label( new Rect(pos.x + 240, 	pos.y, pos.width - 240, pos.height), ttl, baseStyle);

			//reset the colors
			GUI.backgroundColor = Color.white;
			GUI.contentColor = Color.white;
			GUI.color = Color.white;

			return rv;
		}
		void DrawEntryDetail(Rect pos, LogEntry entry, GUIStyle style)
		{
			float stackEntryHeight = EditorGUIUtility.singleLineHeight;
			float stackHeight = entry.StackTrace.Count * stackEntryHeight;
			Vector2 cs = style.CalcSize( new GUIContent(entry.Msg) );

			Rect rDetailsFrame = new Rect(0, 0, cs.x, cs.y + stackHeight );
			Rect rLabelFrame = new Rect(0, 0, cs.x, cs.y);
			detailsScrollPos = GUI.BeginScrollView(pos, detailsScrollPos, rDetailsFrame, false, false);
			
			//GUI.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
			GUI.backgroundColor = Color.white;
			GUI.color = new Color(1, 1, 1, 1);
			EditorGUIUtility.AddCursorRect( rLabelFrame, MouseCursor.Text);
			EditorGUI.SelectableLabel( rLabelFrame, entry.Msg, style );
			
			// Draw trace
			float 
				minY = cs.y, 
				maxY = pos.height - stackHeight - 4;
			if( rDetailsFrame.width > pos.width ) // horizontal scrollbar visible
				maxY -= 16;

			float startY = Mathf.Max(minY, maxY);
			float width = Mathf.Max( pos.width, rDetailsFrame.width );

			IList<Stack.Trace> stackTrace = entry.StackTrace;
			for( int i = 0; i < stackTrace.Count; ++i )
			{
				if( GUI.Button( new Rect(0, startY + (i * stackEntryHeight), width, stackEntryHeight), stackTrace[i].method ) )
					stackTrace[i].Open();
			}
			
			GUI.EndScrollView();
			
			//reset the colors
			GUI.backgroundColor = Color.white;
			GUI.contentColor = Color.white;
			GUI.color = Color.white;
		}

		List<LogEntry> FilterEntries(List<LogEntry> entries)
		{
			List<LogEntry> filteredEntries = entries.Where( entry => ( (entry.Type & levelMask) != 0 ) ).ToList();
			if( searchString.IsNotNullOrEmpty() )
				filteredEntries = filteredEntries.Where( entry => entry.Msg.Contains(searchString) ).ToList();
			
			//filter channels
			filteredEntries = filteredEntries.Where( entry =>  GetChannelInfo( entry.Id ).enabled ).ToList();

			return filteredEntries;
		}

		void HandleKeyboardInput()
		{
			Event evt = Event.current;
			
			if (evt.isKey && evt.type == EventType.KeyUp)
			{
				string focused = GUI.GetNameOfFocusedControl();

				if (focused == fieldNameCommand && evt.keyCode == KeyCode.Return)
				{
					EvalCommand(currentCommand);
					currentCommand = "";
					Repaint();
				}
				else if (focused == fieldNameEntryList)
				{
					int newSelectedEntry = selectedEntry;

					if (evt.keyCode == KeyCode.UpArrow)
						--newSelectedEntry;
					else if (evt.keyCode == KeyCode.DownArrow)
						++newSelectedEntry;

					if( selectedEntry != newSelectedEntry )
					{
						selectedEntry = Mathf.Clamp( newSelectedEntry, 0, entries.Count - 1 );
						//TODO move scroll position to keep up with selection
						Repaint();
					}
				}
			}
		}

		string SearchBox(Rect pos, string s)
		{
			GUIStyle toolbarSeachTextField 			= "ToolbarSeachTextField";
			GUIStyle toolbarSeachCancelButton 		= "ToolbarSeachCancelButton";
			GUIStyle toolbarSeachCancelButtonEmpty 	= "ToolbarSeachCancelButtonEmpty";

			Rect rText = pos;
			Rect rBtn = pos;

			rBtn.width = toolbarSeachCancelButton.CalcSize( GUIContent.none ).x;
			rText.width -= rBtn.width;
			rBtn.x += rText.width;

			EditorGUIUtility.AddCursorRect(rText, MouseCursor.Text);
			s = GUI.TextField(rText, s, toolbarSeachTextField);
			if( Event.current.type == EventType.mouseDown && !rText.Contains(Event.current.mousePosition))
				GUI.FocusControl( fieldNameEntryList );

			GUIStyle btnStyle = toolbarSeachCancelButtonEmpty;
			if( s.IsNotNullOrEmpty() )
				btnStyle = toolbarSeachCancelButton;
			
			if (GUI.Button(rBtn, "", btnStyle))
			{
				// Remove focus if cleared
				s = "";
				GUI.FocusControl(null);
			}

			return s;
		}

		public void HandleCommand( LogEntry entry )
		{
			if( entry.Msg.ToLowerInvariant().Equals("help") )
			{
				Log.Command(entry.Id, "Commands will appear in the optional channel parameter or default channel. When needed they are applied on that channel:\nhelp\n[chnl:]show\n[chnl:]hide\n[chnl:]toggle\n[chnl:]clear\nclearall\nclearchannels\ndeselect\nsave\n[chnl:]color [r][g][b][a]\n[chnl:]log [level] msg\nulog msg");
			}
			else if( entry.Msg.ToLowerInvariant().Equals("show") )
			{
				ChannelInfo channel = GetChannelInfo( entry.Id );				
				channel.enabled = true;
			}
			else if( entry.Msg.ToLowerInvariant().Equals("hide") )
			{
				ChannelInfo channel = GetChannelInfo( entry.Id );				
				channel.enabled = false;
			}
			else if( entry.Msg.ToLowerInvariant().Equals("toggle") )
			{
				ChannelInfo channel = GetChannelInfo( entry.Id );				
				channel.enabled = !channel.enabled;
			}
			else if( entry.Msg.ToLowerInvariant().Equals("deselect") )
			{
				selectedEntry = -1;
			}
			else if( entry.Msg.ToLowerInvariant().Equals("clear") )
			{
				entries.RemoveAll( e => (e.Id == entry.Id ) );
				if( entry.Id != defaultChannel.name )
					channels.Remove( GetChannelInfo(entry.Id) );
			}
			else if( entry.Msg.ToLowerInvariant().Equals("clearall") )
			{
				ClearLog();
			}
			else if( entry.Msg.ToLowerInvariant().Equals("clearchannels") )
			{
				ClearChannels();
			}
			else if( entry.Msg.ToLowerInvariant().Equals("save") )
			{
				string file = EditorUtility.SaveFilePanel("save the log file", "", "log.html", "html");
				if( file.IsNotNullOrEmpty() )
				{
					HTMLLog.Write(file, channels, entries);
					Log.Info ("Console++", "saved log to {0}", file);
				}
			}
			else if( entry.Msg.StartsWith("color") )
			{
				// parse rgb
				string[] colors = entry.Msg.Split( new char[]{' '}, 5 );
				List<int> c = new List<int>(){ 255, 255, 255, 255 };
				
				for( int i = 1; i < colors.Length; ++i )
					c[i-1] = int.Parse( colors[i] );
				
				ChannelInfo channel = GetChannelInfo( entry.Id );
				channel.color = new Color(c[0]/255f, c[1]/255f, c[2]/255f, c[3]/255f);
			}
			else if( entry.Msg.StartsWith("log") )
			{
				// parse rgb
				string[] splt = entry.Msg.Split( new char[]{' '}, 3 );
				
				if( splt.Length < 2 )
					return;
				
				LogEntry.Level lvl = LogEntry.Level.Info;
				string msg = splt[1];
				
				if( splt.Length == 3 )
				{
					msg = splt[2];
					switch( splt[1] )
					{
					case "trace":
						lvl = LogEntry.Level.Trace;
						break;
					case "debug":
						lvl = LogEntry.Level.Debug;
						break;
					case "info":
						lvl = LogEntry.Level.Info;
						break;
					case "warning":
						lvl = LogEntry.Level.Warning;
						break;
					case "error":
						lvl = LogEntry.Level.Error;
						break;
					case "command":
						lvl = LogEntry.Level.Command;
						break;
					default:
						msg = string.Format("{0} {1}", splt[1], msg);
						break;
					}
				}
				
				Log.Add(entry.Id, lvl, msg);
			}
			else if( entry.Msg.StartsWith("ulog") )
			{
				// parse rgb
				string[] splt = entry.Msg.Split( new char[]{' '}, 2 );
				
				if( splt.Length < 2 )
					return;

				string msg = splt[1];
				Debug.Log(msg);
			}
		}

		public ChannelInfo GetChannelInfo(string channelName)
		{
			ChannelInfo channel = channels.Find( ch => ch.name == channelName );

			if( channel == null )
			{
				channel = new ChannelInfo(channelName);
				channels.Add( channel );
			}

			return channel;
		}
	}
}
