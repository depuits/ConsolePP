using System;
using UnityEngine;
using System.Collections.Generic;

namespace ConsolePP
{
	[Serializable]
	public class LogEntry 
	{
		[Flags]
		public enum Level
		{
			Trace 	 = 0x01,
			Debug 	 = 0x02,
			Info 	 = 0x04,
			Warning  = 0x08,
			Error 	 = 0x10,
			Command  = 0x20
		};

		// not added in enum because of GUI drawing
		public const Level LevelNone = (Level)0x00;
		public const Level LevelAll  = (Level)0xFF;
		
		[Flags] // enum for extra info bits for entry
		public enum ExtraBits
		{
			None	 = 0x00,
			Compile	 = 0x01
		};

		[SerializeField] string id;
		[SerializeField] Level type;
		[SerializeField] string msg;
		[SerializeField] string time;
		[SerializeField] int frame;
		[SerializeField] List<Stack.Trace> stackTrace;

		[SerializeField] ExtraBits xb;

		public string Id { get { return id; } }
		public Level Type { get { return type; } }
		public string Msg { get { return msg; } }
		public string Time { get { return time; } }
		public int Frame { get { return frame; } }
		public IList<Stack.Trace> StackTrace { get { return stackTrace; } }

		public ExtraBits Xb { get { return xb; } set { xb = value; } }

		public LogEntry(string id, Level type, string msg)
		{
			this.id = id;
			this.type = type;
			this.msg = msg;
			
			xb = ExtraBits.None;
			time = DateTime.Now.ToString("HH:mm:ss:ffff");
			frame = UnityEngine.Time.frameCount;

			string t = UnityEngine.StackTraceUtility.ExtractStackTrace();
			// the first line can always be removed because its this constructor
			do
			{
				t = t.Substring( t.IndexOf('\n')+1 );
			}
			while ( t.StartsWith("Log:") );

			stackTrace = Stack.Parse(t, xb);
		}
		
		public LogEntry(string id, Level type, string msg, string t, ExtraBits xb)
		{
			this.id = id;
			this.type = type;
			this.msg = msg;
			this.xb = xb;

			time = DateTime.Now.ToString("HH:mm:ss:ffff");
			frame = UnityEngine.Time.frameCount;

			while ( t.StartsWith("UnityEngine.Debug") )
				t = t.Substring( t.IndexOf('\n')+1 );
			stackTrace = Stack.Parse(t, xb);
		}
	}
}
