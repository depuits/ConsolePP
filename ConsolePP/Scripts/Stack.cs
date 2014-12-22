using System;
using System.Collections.Generic;

namespace ConsolePP
{
	[Serializable]
	public class Stack
	{
		[Serializable]
		public class Trace
		{
			public string method;
			public string file;
			public int lineNumber;
			
			public bool CanOpen { get { return (file != null); } }
		}

		public static List<Trace> Parse(string trace, LogEntry.ExtraBits xb)
		{
			if( (xb & LogEntry.ExtraBits.Compile) != 0 )
				return ParseTraceCompile(trace);
			
			return ParseTrace(trace);
		}

		static List<Trace> ParseTrace(string trace)
		{
			List<Trace> stackTrace = new List<Trace>();
			
			int endLineChar = trace.IndexOf('\n');
			while ( endLineChar != -1 )
			{
				string line = trace.Substring(0, endLineChar);
				trace = trace.Substring( trace.IndexOf('\n')+1 );
				endLineChar = trace.IndexOf('\n');
				
				//parse the line
				string m = line;
				string f = null;
				int l = 0;
				
				int chr = line.IndexOf(')');
				if( chr != -1 )
				{
					m = line.Substring( 0, chr + 1 );
					if( line.Length >= chr + 6 )
					{				
						line = line.Substring( chr + 6 );
						chr = line.IndexOf(':');
						if( chr != -1 )
						{
							f = line.Substring(0, chr);
						
							line = line.Substring( chr + 1 );
							chr = line.IndexOf(')');
							if( chr != -1 )
							{
								if( !int.TryParse(line.Substring(0, chr), out l) )
									l = 0;
							}
						}
					}
				}
				
				stackTrace.Add( new Trace(){ file = f, method = m, lineNumber = l } );
			}
			
			return stackTrace;
		}
		static List<Trace> ParseTraceCompile(string trace)
		{
			// this code probably needs some more safety checks
			List<Trace> stackTrace = new List<Trace>();
			
			string m = trace;
			string f = null;
			int l = 0;

			//parse the line
			int chr = trace.IndexOf('(');
			if( chr != -1 )
			{
				f = trace.Substring( 0, chr );
			
				trace = trace.Substring( chr+1 );
				chr = trace.IndexOf(',');
				if( chr != -1 )
				{
					l = int.Parse( trace.Substring(0, chr) );
				
					chr = trace.IndexOf(':');
					if( chr != -1 )
					{
						trace = trace.Substring( chr + 2 );
						chr = trace.IndexOf(':');
						if( chr != -1 )
							m = trace.Substring(0, chr);
					}
				}
			}
			
			stackTrace.Add( new Trace(){ file = f, method = m, lineNumber = l } );
			
			return stackTrace;
		}
		
		//	Trace example:
		//		UnityEngine.Debug:Log(Object)
		//		Cartamundidigital.Thylacine.Input.Commons.PostBuildTrigger:OnPostProcessBuild(BuildTarget, String) (at Assets/Input/Commons/Editor/PostBuildTrigger.cs:226)
		//		UnityEditor.HostView:OnGUI()

		//	Trace compile example:
		//		Assets/Games/SantasLittleBouncer/Scripts/FallingCube.cs(20,14): warning CS0114: `FallingCube.Update()' hides inherited member `SpawnObject.Update()'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword
	}
}
