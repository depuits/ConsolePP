using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ConsolePP
{
	public class HTMLLog 
	{
		public static void Write(string path, IList<ChannelInfo> channels, IList<LogEntry> entries)
		{
			using (System.IO.StreamWriter file = new System.IO.StreamWriter(path))
			{
				file.WriteLine("<script language=\"javascript\" src=\"log.js\"></script>");
				file.WriteLine("<link rel=\"stylesheet\" type=\"text/css\" href=\"log.css\" />");
				file.WriteLine("<div class=\"Header\">");

				foreach( ChannelInfo ch in channels )
				{
					file.WriteLine("<style>.{0} {{ background-color:#{1};] }}</style>", ch.name, ch.color.ToHex());
					file.WriteLine("<input type=\"button\" value=\"{0}\" class=\"{0} Button\" onclick=\"hide_class('{0}')\"/>", ch.name);
				}

				file.WriteLine("<br />");
				file.WriteLine("</div>");
				file.WriteLine("<h1 class=\"date\">{0} {1}</h1>", DateTime.Now.ToString("R"), SystemInfo.operatingSystem);
				file.WriteLine("<p>Click on buttons to toggle visability. Click on STACK buttons to toggle visibility of stack traces.</p>");

				int traceId = 0;
				foreach( LogEntry e in entries )
				{
					file.WriteLine("<p class=\"{0}\"><span class=\"frame\">{1}</span><span class=\"time\">{2}</span><span class=\"logId\">{0}</span><a onclick=\"hide('trace{3}')\">STACK</a> {4}</p>", e.Id, e.Frame.ToString("D8"), e.Time, traceId, e.Msg);
					file.WriteLine("<pre id=\"trace{0}\">", traceId);

					foreach( Stack.Trace t in e.StackTrace )
					{
						if( t.file.IsNullOrEmpty() )
							file.WriteLine(t.method);
						else
							file.WriteLine("{0} (at {1}:{2})", t.method, t.file, t.lineNumber);
					}

					file.WriteLine("</pre>");
					++traceId;
				}
			}
		}
	}
}
