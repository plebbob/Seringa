﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Seringa.Engine.Exceptions;
using Seringa.Engine.DataObjects;

namespace Seringa.Engine.Utils
{
    public class HtmlHelpers
    {

        public static string GetAnswerFromHtml(string html, string query, ExploitDetails ExploitDetails, bool detailedExceptions)
        {
            string result = string.Empty;

            if (!string.IsNullOrEmpty(html))
            {
                try
                {
                    result = html.Substring(html.IndexOf(ExploitDetails.ResultStart) +
                                                ExploitDetails.ResultStart.Length,
                                                html.IndexOf(ExploitDetails.ResultEnd) - html.IndexOf(ExploitDetails.ResultStart) -
                                                ExploitDetails.ResultStart.Length);
                }
                catch
                {
                    string userFriendlyException = "Could not parse sql injection result.";

                    if (!string.IsNullOrEmpty(ExploitDetails.ErrorStart) && !string.IsNullOrEmpty(ExploitDetails.ErrorEnd))
                        if (html.IndexOf(ExploitDetails.ErrorStart) > -1 && html.IndexOf(ExploitDetails.ErrorEnd) > -1)
                            userFriendlyException = string.Format("Sql exception occured: {0}",
                                                        html.Substring(html.IndexOf(ExploitDetails.ErrorStart) +
                                                        ExploitDetails.ErrorStart.Length,
                                                        html.IndexOf(ExploitDetails.ErrorEnd) - html.IndexOf(ExploitDetails.ErrorStart) -
                                                        ExploitDetails.ErrorStart.Length));

                    if (detailedExceptions)
                        userFriendlyException = string.Format("{0}({1})", userFriendlyException, query);

                    throw new SqlInjException(userFriendlyException);
                }
            }

            if (ExploitDetails.TrimLast)
                result = result.Remove(result.Length - 1, 1);

            return result;
        }


        public static IList<string> GetMultipleAnswersFromHtml(string html, string query, ExploitDetails ExploitDetails, bool detailedExceptions)
        {
            IList<string> results = new List<string>();
            string result = string.Empty;

            if (!string.IsNullOrEmpty(html))
            {
                int startIndex = 0;
                int resultStartIndex = 0; 
                int resultEndIndex = 0;
                while (resultStartIndex != -1)
                {
                    resultStartIndex = html.IndexOf(ExploitDetails.ResultStart, startIndex) + ExploitDetails.ResultStart.Length;
                    if(resultStartIndex == -1)
                        break;
                    resultEndIndex = html.IndexOf(ExploitDetails.ResultEnd) - html.IndexOf(ExploitDetails.ResultStart) -
                                                 ExploitDetails.ResultStart.Length;

                    if (resultStartIndex < resultEndIndex)
                    {
                        try
                        {
                            result = html.Substring(resultStartIndex, resultEndIndex);

                            if (ExploitDetails.TrimLast)
                                result = result.Remove(result.Length - 1, 1);

                            results.Add(result);
                        }
                        catch
                        {
                            string userFriendlyException = "Could not parse sql injection result.";

                            if (!string.IsNullOrEmpty(ExploitDetails.ErrorStart) && !string.IsNullOrEmpty(ExploitDetails.ErrorEnd))
                                if (html.IndexOf(ExploitDetails.ErrorStart) > -1 && html.IndexOf(ExploitDetails.ErrorEnd) > -1)
                                    userFriendlyException = string.Format("Sql exception occured: {0}",
                                                                html.Substring(html.IndexOf(ExploitDetails.ErrorStart) +
                                                                ExploitDetails.ErrorStart.Length,
                                                                html.IndexOf(ExploitDetails.ErrorEnd) - html.IndexOf(ExploitDetails.ErrorStart) -
                                                                ExploitDetails.ErrorStart.Length));

                            if (detailedExceptions)
                                userFriendlyException = string.Format("{0}({1})", userFriendlyException, query);

                            throw new SqlInjException(userFriendlyException);
                        }
                    }

                    startIndex = resultEndIndex;
                }
            }

            
            return results;
        }

    }
}