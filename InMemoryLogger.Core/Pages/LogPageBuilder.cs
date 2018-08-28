using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace InMemoryLogger.Core.Pages
{
    public class LogPageBuilder : BaseView
    {
        public LogPageBuilder(IMLogPageModel model)
        {
            this.Model = model;
        }

        public IMLogPageModel Model { get; set; }

        public void LogRow(LogInfo log, int level)
        {
            if (log.Severity >= Model.Options.MinLevel &&
                (string.IsNullOrEmpty(Model.Options.NamePrefix) || log.Name.StartsWith(Model.Options.NamePrefix, StringComparison.Ordinal)))
            {
                WriteLiteralTo(Output, "        <tr class=\"logRow\">\r\n            <td>");
                WriteTo(Output, string.Format("{0:MM/dd/yy}", log.Time));
                WriteLiteralTo(Output, "</td>\r\n            <td>");
                WriteTo(Output, string.Format("{0:H:mm:ss}", log.Time));
                WriteLiteralTo(Output, $"</td>\r\n            <td title=\"{log.Name}\">");
                WriteTo(Output, log.Name);
                var severity = log.Severity.ToString().ToLowerInvariant();
                WriteLiteralTo(Output, $"</td>\r\n            <td class=\"{severity}\">");
                WriteTo(Output, log.Severity);
                WriteLiteralTo(Output, $"</td>\r\n            <td title=\"{log.Message}\"> \r\n");
                for (var i = 0; i < level; i++)
                {
                    WriteLiteralTo(Output, "                    <span class=\"tab\"></span>\r\n");
                }
                WriteLiteralTo(Output, "                ");
                WriteTo(Output, log.Message);
                WriteLiteralTo(Output, $"\r\n            </td>\r\n            <td title=\"{log.Exception}\">");
                WriteTo(Output, log.Exception);
                WriteLiteralTo(Output, "</td>\r\n        </tr>\r\n");
            }
        }

        public void Traverse(IMLoggerScopeNode node, int level, Dictionary<string, int> counts)
        {
            LogRow(new LogInfo()
            {
                Name = node.Name,
                Time = node.StartTime,
                Severity = LogLevel.Debug,
                Message = "Beginning " + node.State,
            }, level);

            var messageIndex = 0;
            var childIndex = 0;
            while (messageIndex < node.Messages.Count && childIndex < node.Children.Count)
            {
                if (node.Messages[messageIndex].Time < node.Children[childIndex].StartTime)
                {
                    LogRow(node.Messages[messageIndex], level);
                    counts[node.Messages[messageIndex].Severity.ToString()]++;
                    messageIndex++;
                }
                else
                {
                    Traverse(node.Children[childIndex], level + 1, counts);
                    childIndex++;
                }
            }
            if (messageIndex < node.Messages.Count)
            {
                for (var i = messageIndex; i < node.Messages.Count; i++)
                {
                    LogRow(node.Messages[i], level);
                    counts[node.Messages[i].Severity.ToString()]++;
                }
            }
            else
            {
                for (var i = childIndex; i < node.Children.Count; i++)
                {
                    Traverse(node.Children[i], level + 1, counts);
                }
            }
            // print end of scope
            LogRow(new LogInfo()
            {
                Name = node.Name,
                Time = node.EndTime,
                Severity = LogLevel.Debug,
                Message = string.Format("Completed {0} in {1}ms", node.State, node.EndTime - node.StartTime)
            }, level);
        }

        public override void Execute()
        {
            WriteLiteral("\r\n");
            Response.ContentType = "text/html; charset=utf-8";
            WriteLiteral(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>ASP.NET Core Logs</title>
    <script src=""//ajax.aspnetcdn.com/ajax/jquery/jquery-2.1.1.min.js""></script>
    <style>
        body {
    font-size: .813em;
    white-space: nowrap;
    margin: 20px;
}

col:nth-child(2n) {
    background-color: #FAFAFA;
}

form { 
    display: inline-block; 
}

h1 {
    margin-left: 25px;
}

table {
    margin: 0px auto;
    border-collapse: collapse;
    border-spacing: 0px;
    table-layout: fixed;
    width: 100%;
}

td, th {
    padding: 4px;
}

thead {
    font-size: 1em;
    font-family: Arial;
}

tr {
    height: 23px;
}

#requestHeader {
    border-bottom: solid 1px gray;
    border-top: solid 1px gray;
    margin-bottom: 2px;
    font-size: 1em;
    line-height: 2em;
}

.collapse {
    color: black;
    float: right;
    font-weight: normal;
    width: 1em;
}

.date, .time {
    width: 70px; 
}

.logHeader {
    border-bottom: 1px solid lightgray;
    color: gray;
    text-align: left;
}

.logState {
    text-overflow: ellipsis;
    overflow: hidden;
}

.logTd {
    border-left: 1px solid gray;
    padding: 0px;
}

.logs {
    width: 80%;
}

.logRow:hover {
    background-color: #D6F5FF;
}

.requestRow>td {
    border-bottom: solid 1px gray;
}

.severity {
    width: 80px;
}

.summary {
    color: black;
    line-height: 1.8em;
}

.summary>th {
    font-weight: normal;
}

.tab {
    margin-left: 30px;
}

#viewOptions {
    margin: 20px;
}

#viewOptions > * {
    margin: 5px;
}
        body {
    font-family: 'Segoe UI', Tahoma, Arial, Helvtica, sans-serif;
    line-height: 1.4em;
}

h1 {
    font-family: 'Segoe UI', Helvetica, sans-serif;
    font-size: 2.5em;
}

td {
    text-overflow: ellipsis;
    overflow: hidden;
}

tr:nth-child(2n) {
    background-color: #F6F6F6;
}

.critical {
    background-color: red;
    color: white;
}

.error {
    color: red;
}");
            WriteLiteral("\r\n.information {\r\n    color: blue;\r\n}\r\n\r\n.debug {\r\n    color: black;\r\n}\r\n\r\n.warning {\r\n    color: orange;\r\n}\r\n    </style>\r\n</head>\r\n<body>\r\n    <h1>ASP.NET Core Logs</h1>\r\n    <form id=\"viewOptions\" method=\"get\">\r\n        <select name=\"level\">\r\n");
            foreach (var severity in Enum.GetValues(typeof(LogLevel)))
            {
                var severityInt = (int)severity;
                if ((int)Model.Options.MinLevel == severityInt)
                {
                    WriteLiteral("                    <option");
                    BeginWriteAttribute("value", " value=\"", 6825, "\"", 6845, 1);
                    WriteAttributeValue("", 6833, severityInt, 6833, 12, false);
                    EndWriteAttribute();
                    WriteLiteral(" selected=\"selected\">");
                    Write(severity);
                    WriteLiteral("</option>\r\n");
                }
                else
                {
                    WriteLiteral("                    <option");
                    BeginWriteAttribute("value", " value=\"", 6974, "\"", 6994, 1);
                    WriteAttributeValue("", 6982, severityInt, 6982, 12, false);
                    EndWriteAttribute();
                    WriteLiteral(">");
                    Write(severity);
                    WriteLiteral("</option>\r\n");
                }
            }
            WriteLiteral("        </select>\r\n        <input type=\"text\" name=\"name\"");
            BeginWriteAttribute("value", " value=\"", 7107, "\"", 7140, 1);
            WriteAttributeValue("", 7115, Model.Options.NamePrefix, 7115, 25, false);
            EndWriteAttribute();
            WriteLiteral(@" />
        <input type=""submit"" value=""filter"" />
    </form>
    <form id=""clear"" method=""post"" action="""">
        <button type=""submit"" name=""clear"" value=""1"">Clear Logs</button>
    </form>

    <table id=""requestTable"">
        <thead id=""requestHeader"">
            <tr>
                <th class=""path"">Path</th>
                <th class=""method"">Method</th>
                <th class=""host"">Host</th>
                <th class=""statusCode"">Status Code</th>
                <th class=""logs"">Logs</th>
            </tr>
        </thead>
        <colgroup>
            <col />
            <col />
            <col />
            <col />
            <col />
        </colgroup>");
            foreach (var activity in Model.Activities.Reverse())
            {
                WriteLiteral("            <tbody>\r\n                <tr class=\"requestRow\">\r\n");
                var activityPath = Model.Path.Value + "/" + activity.Id;
                if (activity.HttpInfo != null)
                {
                    WriteLiteral("                        \t<td><a");
                    BeginWriteAttribute("href", " href=\"", 8204, "\"", 8224, 1);
                    WriteAttributeValue("", 8211, activityPath, 8211, 13, false);
                    EndWriteAttribute();
                    BeginWriteAttribute("title", " title=\"", 8225, "\"", 8256, 1);
                    WriteAttributeValue("", 8233, activity.HttpInfo.Path, 8233, 23, false);
                    EndWriteAttribute();
                    WriteLiteral(">");
                    Write(activity.HttpInfo.Path);
                    WriteLiteral("</a></td>\r\n                            <td>");
                    Write(activity.HttpInfo.Method);
                    WriteLiteral("</td>\r\n                            <td>");
                    Write(activity.HttpInfo.Host);
                    WriteLiteral("</td>\r\n                            <td>");
                    Write(activity.HttpInfo.StatusCode);
                    WriteLiteral("</td>\r\n");
                }
                else if (activity.RepresentsScope)
                {
                    WriteLiteral("                            <td colspan=\"4\"><a");
                    BeginWriteAttribute("href", " href=\"", 8646, "\"", 8666, 1);
                    WriteAttributeValue("", 8653, activityPath, 8653, 13, false);
                    EndWriteAttribute();
                    BeginWriteAttribute("title", " title=\"", 8667, "\"", 8695, 1);
                    WriteAttributeValue("", 8675, activity.Root.State, 8675, 20, false);
                    EndWriteAttribute();
                    WriteLiteral(">");
                    Write(activity.Root.State);
                    WriteLiteral("</a></td>\r\n");
                }
                else
                {
                    WriteLiteral("                            <td colspan=\"4\"><a");
                    BeginWriteAttribute("href", " href=\"", 8858, "\"", 8878, 1);
                    WriteAttributeValue("", 8865, activityPath, 8865, 13, false);
                    EndWriteAttribute();
                    WriteLiteral(">Non-scope Log</a></td>\r\n");
                }
                WriteLiteral(@"                    <td class=""logTd"">
                        <table class=""logTable"">
                            <thead class=""logHeader"">
                                <tr class=""headerRow"">
                                    <th class=""date"">Date</th>
                                    <th class=""time"">Time</th>
                                    <th class=""name"">Name</th>
                                    <th class=""severity"">Severity</th>
                                    <th class=""state"">State</th>
                                    <th>Error<span class=""collapse"">^</span></th>
                                </tr>
                            </thead>");
                var counts = new Dictionary<string, int>();
                counts["Critical"] = 0;
                counts["Error"] = 0;
                counts["Warning"] = 0;
                counts["Information"] = 0;
                counts["Debug"] = 0;
                WriteLiteral("                            <tbody class=\"logBody\">\r\n");
                if (!activity.RepresentsScope)
                {
                    // message not within a scope
                    var logInfo = activity.Root.Messages.FirstOrDefault();
                    LogRow(logInfo, 0);
                    counts[logInfo.Severity.ToString()] = 1;
                }
                else
                {
                    Traverse(activity.Root, 0, counts);
                }
                WriteLiteral("                            </tbody>\r\n                            <tbody class=\"summary\">\r\n                                <tr class=\"logRow\">\r\n                                    <td>");
                Write(activity.Time.ToString("MM-dd-yyyy HH:mm:ss"));
                WriteLiteral("</td>\r\n");
                foreach (var kvp in counts)
                {
                    if (string.Equals("Debug", kvp.Key))
                    {
                        WriteLiteral("                                            <td>");
                        Write(kvp.Value);
                        WriteLiteral(" ");
                        Write(kvp.Key);
                        WriteLiteral("<span class=\"collapse\">v</span></td>\r\n");
                    }
                    else
                    {
                        WriteLiteral("                                            <td>");
                        Write(kvp.Value);
                        WriteLiteral(" ");
                        Write(kvp.Key);
                        WriteLiteral("</td>\r\n");
                    }
                }
                WriteLiteral("                                </tr>\r\n                            </tbody>\r\n                        </table>\r\n                    </td>\r\n                </tr>\r\n            </tbody>\r\n");
            }
            WriteLiteral(@"    </table>
    <script type=""text/javascript"">
        $(document).ready(function () {
            $("".logBody"").hide();
            $("".logTable > thead"").hide();
            $("".logTable > thead"").click(function () {
                $(this).closest("".logTable"").find(""tbody"").hide();
                $(this).closest("".logTable"").find("".summary"").show();
                $(this).hide();
            });
            $("".logTable > .summary"").click(function () {
                $(this).closest("".logTable"").find(""tbody"").show();
                $(this).closest("".logTable"").find(""thead"").show();
                $(this).hide();
            });
        });
    </script>
</body>
</html>");
        }
    }
}
